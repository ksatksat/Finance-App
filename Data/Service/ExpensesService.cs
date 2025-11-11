using FinanceApp.Data.Service;
using FinanceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FinanceApp.Data.Service 
{
    public class ExpensesService : IExpensesService
    {
        private readonly FinanceAppContext _context;
        public ExpensesService(FinanceAppContext context)
        {
            _context = context;
        }
        public async Task Add(Expense expense)
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Expense>> GetAll()
        {
            var expenses = await _context.Expenses.ToListAsync();
            return expenses;
        }
        public IQueryable GetChartData()
        {
            var data = _context.Expenses
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    Total = g.Sum(e => e.Amount)
                });
            return data;
        }
    }
}
/*
this ExpensesService is an EF Core-backed implementation of your IExpensesService. Below I’ll explain what each part does, point out a few pitfalls you should be aware of, and give a recommended, safer version you can drop in instead.

What the code does (line-by-line & method summary)
public class ExpensesService : IExpensesService
{
    private readonly FinanceAppContext _context;
    public ExpensesService(FinanceAppContext context)
    {
        _context = context;
    }


Constructor injection: FinanceAppContext (your EF DbContext) is injected by DI and stored for use in methods. Because DbContext is usually registered as scoped, ExpensesService should also be scoped (you registered it that way in Program.cs), which is correct.

Add
public async Task Add(Expense expense)
{
    _context.Expenses.Add(expense);
    await _context.SaveChangesAsync();
}


Adds the provided expense entity to the Expenses DbSet and persists changes to the database asynchronously.

Good: async SaveChangesAsync() prevents blocking threads.

Note: no validation or error handling here — the controller should have performed ModelState.IsValid, but consider service-level checks if business rules exist.

GetAll
public async Task<IEnumerable<Expense>> GetAll()
{
    var expenses = await _context.Expenses.ToListAsync();
    return expenses;
}


Loads all Expense rows from the DB into memory and returns them as IEnumerable<Expense>.

ToListAsync() materializes the query (executes SQL) while the DbContext is alive — good.

Consider ordering (e.g., by Date) and AsNoTracking() for read-only queries to improve performance.

GetChartData
public IQueryable GetChartData()
{
    var data = _context.Expenses
        .GroupBy(e => e.Category)
        .Select(g => new
        {
            Category = g.Key,
            Total = g.Sum(e => e.Amount)
        });
    return data;
}


Builds a LINQ expression that groups expenses by Category and selects anonymous objects { Category, Total }.

It returns an IQueryable (non-generic) that represents the deferred query — the SQL is not executed until the caller enumerates it.

This design works but has several drawbacks (see problems & recommended changes below).

Problems / pitfalls & why to change them

Returning non-generic IQueryable / anonymous projection leaks implementation details

Caller gets a raw IQueryable that still depends on EF Core query translation. This couples callers to EF and can cause DbContext lifetime issues if the query is enumerated after the context is disposed.

Returning anonymous objects also forces the method signature to use IQueryable (non-generic) — it loses type information and is awkward to use in controllers or tests.

Potential DbContext lifetime issues

If you return an IQueryable and the controller enumerates it after the DI scope ended (rare in typical controllers, but possible in background code or tests), it will throw. Safer approach: materialize (call ToListAsync) inside the service and return concrete results.

Serialization & execution

In your controller you currently do var data = _expensesService.GetChartData(); return Json(data); — Json(...) will enumerate the IQueryable and execute the SQL while the controller's request scope is active, so it will work. Still, materializing earlier (and returning a typed DTO) is cleaner and easier to reason about.

Missing read optimizations

For read-only queries use .AsNoTracking() to avoid change tracking overhead.

Type of Amount

Your Expense.Amount is currently double (based on earlier messages). Summing doubles for money is risky; prefer decimal. If Amount is double, Total is double — be aware of rounding issues for currency.

No Async suffix or Async on GetChartData

Naming convention: use GetChartDataAsync and make it return Task<IEnumerable<ChartEntry>> (async) so intent is clear.

Recommended improvements (explanations + code)

Create a small DTO for chart results:

public class ChartEntry
{
    public string Category { get; set; } = "";
    public decimal Total { get; set; }  // use decimal if Amount is decimal
}


Update the interface (preferred):

Task<IEnumerable<Expense>> GetAllAsync();
Task AddAsync(Expense expense);
Task<IEnumerable<ChartEntry>> GetChartDataAsync();


Improved service implementation:

using Microsoft.EntityFrameworkCore;
using FinanceApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ExpensesService : IExpensesService
{
    private readonly FinanceAppContext _context;
    public ExpensesService(FinanceAppContext context) => _context = context;

    public async Task AddAsync(Expense expense)
    {
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Expense>> GetAllAsync()
    {
        return await _context.Expenses
                             .AsNoTracking()
                             .OrderByDescending(e => e.Date)
                             .ToListAsync();
    }

    public async Task<IEnumerable<ChartEntry>> GetChartDataAsync()
    {
        // Materialize into a typed DTO while the DbContext is alive
        return await _context.Expenses
                             .AsNoTracking()
                             .GroupBy(e => e.Category ?? "Uncategorized")
                             .Select(g => new ChartEntry
                             {
                                 Category = g.Key!,
                                 Total = g.Sum(e => e.Amount) // ensure Amount is decimal if you want exact money sums
                             })
                             .ToListAsync();
    }
}


Why this is better:

AsNoTracking() reduces overhead for read-only queries.

ToListAsync() materializes queries inside the service; no IQueryable leakage.

ChartEntry is strongly typed and serializes cleanly to JSON for your JS chart.

Async method names make intent clear.

Controller usage (example)
[HttpGet]
public async Task<IActionResult> GetChart()
{
    var data = await _expensesService.GetChartDataAsync();
    return Json(data); // returns typed JSON array suitable for Chart.js
}

Extra suggestions

Add cancellation tokens to async methods if you expect long-running queries.

If Amount remains double, consider converting to decimal for money; update DB column mapping with [Column(TypeName = "decimal(18,2)")].

If you expect many rows and only need top categories, consider paging or limiting results in the query.

Add unit tests by mocking IExpensesService to return deterministic ChartEntry data for front-end tests.
 */