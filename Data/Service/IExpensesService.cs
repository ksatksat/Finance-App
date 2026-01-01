using FinanceApp.Models;
namespace FinanceApp.Data.Service
{
    //MODEL LAYER
    public interface IExpensesService
    {
        Task<IEnumerable<Expense>> GetAll(string UserId);
        Task Add(Expense expense, string UserId);
        Task<IEnumerable<ChartEntry>> GetChartDataAsync(string UserId);
        Task<Expense?> GetByIdAsync(int id, string UserId);
        Task DeleteAsync(int id, string UserId);
        //update
        Task UpdateAsync(Expense expense, string UserId);
    }
}


/*
 * 
 * 
 * quick summary

This is a small, focused service interface (IExpensesService) that 
declares the operations the application needs to work with Expense 
data. The controller depends on this abstraction so data access 
and business rules can live in a separate, testable class 
instead of in controllers.

Below I’ll explain each method, the intended contract/behavior, good 
error handling and security requirements, and then give a ready-to-copy 
EF Core implementation sketch and a few recommended improvements 
(signatures, cancellation, return types, DI registration).

Interface — method-by-method (what each method is for)
public interface IExpensesService
{
    Task<IEnumerable<Expense>> GetAll(string UserId);
    Task Add(Expense expense, string UserId);
    Task<IEnumerable<ChartEntry>> GetChartDataAsync(string UserId);
    Task<Expense?> GetByIdAsync(int id, string UserId);
    Task DeleteAsync(int id, string UserId);
    Task UpdateAsync(Expense expense, string UserId);
}


Task<IEnumerable<Expense>> GetAll(string UserId)
Return all expenses that belong to the specified user. Typical 
behavior: the service must filter by UserId and return only 
that user’s records (security boundary).

Task Add(Expense expense, string UserId)
Create / persist a new expense for the user. The service should 
assign expense.UserId = UserId (don’t trust client-provided 
UserId) and save it to the database.

Task<IEnumerable<ChartEntry>> GetChartDataAsync(string UserId)
Return aggregated results (e.g., category totals) to feed the 
chart. Again, results must be filtered by UserId. Typically 
projects data into ChartEntry with Category and Total.

Task<Expense?> GetByIdAsync(int id, string UserId)
Return a single expense by id only if it belongs to the user. 
Return null when not found (controller turns null into NotFound()).

Task DeleteAsync(int id, string UserId)
Delete the given expense only if it belongs to UserId. Decide 
policy: either return a boolean if nothing was deleted, or 
throw an exception (controller currently assumes success; you 
may throw InvalidOperationException and controller maps 
that to NotFound during update — be deliberate).

Task UpdateAsync(Expense expense, string UserId)
Update the persisted expense. Best practice: the service 
should load the existing entity from DB, confirm ownership 
(existing.UserId == UserId), then apply only the allowed 
field updates and save. On missing entity or ownership 
mismatch, throw InvalidOperationException (or a more 
specific custom exception).

Important contract & security requirements

Always filter by UserId — the service enforces the 
authorization boundary; controllers should not rely 
solely on client-provided values.

Do not trust the incoming Expense.UserId — set the 
user id server-side in Add and ignore/verify it in Update.

Decide exception vs return — choose a consistent 
policy for "not found" vs "forbidden":

Return null/false for not-found and let controller interpret it, or

Throw a specific exception (NotFoundException, ForbiddenException) 
and map to appropriate HTTP codes in the controller.
Your controller currently expects null from GetByIdAsync and 
catches InvalidOperationException for update — keep 
this contract or align both sides.

Suggested small signature improvements

Use userId camelCase parameter name (C# parameter naming convention).

Accept CancellationToken optionally for all methods in a production app.

Consider returning concrete collection types or read-only 
collections if ordered enumeration is expected 
(e.g., Task<IReadOnlyList<Expense>> GetAllAsync(string userId, CancellationToken ct = default);).

Example improved signature set:

Task<IReadOnlyList<Expense>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
Task AddAsync(Expense expense, string userId, CancellationToken cancellationToken = default);
Task<IReadOnlyList<ChartEntry>> GetChartDataAsync(string userId, CancellationToken cancellationToken = default);
Task<Expense?> GetByIdAsync(int id, string userId, CancellationToken cancellationToken = default);
Task DeleteAsync(int id, string userId, CancellationToken cancellationToken = default);
Task UpdateAsync(Expense expense, string userId, CancellationToken cancellationToken = default);

Example EF Core implementation (concise, copy-paste)

This implementation shows the patterns: ownership checks, 
mapping, safe update and aggregation for charts.

using Microsoft.EntityFrameworkCore;
using FinanceApp.Data;    // your DbContext namespace
using FinanceApp.Models;

public class ExpensesService : IExpensesService
{
    private readonly FinanceAppContext _db;

    public ExpensesService(FinanceAppContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Expense>> GetAll(string userId)
    {
        return await _db.Expenses
                        .Where(e => e.UserId == userId)
                        .OrderByDescending(e => e.Date)
                        .ToListAsync();
    }

    public async Task Add(Expense expense, string userId)
    {
        // enforce ownership server-side
        expense.UserId = userId;
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ChartEntry>> GetChartDataAsync(string userId)
    {
        return await _db.Expenses
                        .Where(e => e.UserId == userId)
                        .GroupBy(e => e.Category)
                        .Select(g => new ChartEntry { Category = g.Key ?? "Unknown", Total = g.Sum(e => e.Amount) })
                        .ToListAsync();
    }

    public async Task<Expense?> GetByIdAsync(int id, string userId)
    {
        return await _db.Expenses
                        .Where(e => e.Id == id && e.UserId == userId)
                        .FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var expense = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) throw new InvalidOperationException("Expense not found."); // controller expects this in some cases
        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Expense expense, string userId)
    {
        var existing = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == expense.Id && e.UserId == userId);
        if (existing == null) throw new InvalidOperationException("Expense not found or access denied.");

        // apply allowed updates explicitly (prevent overposting)
        existing.Description = expense.Description;
        existing.Amount = expense.Amount;
        existing.Category = expense.Category;
        existing.Date = expense.Date;

        await _db.SaveChangesAsync();
    }
}


Note: the implementation throws InvalidOperationException for 
not-found cases to match the controller behavior you showed earlier. 
For clearer semantics, create and throw a NotFoundException 
or ForbiddenException and handle them in controller.

How to register the service in DI

In Program.cs (or Startup.cs):

builder.Services.AddScoped<IExpensesService, ExpensesService>();

Testing & mocking

Because the controller depends on IExpensesService, you can 
unit-test the controller by mocking IExpensesService. Implement 
integration tests against the concrete ExpensesService using 
an in-memory or SQLite database.

Additional advanced considerations

Transactions: If Add must also create related entities, 
ensure transactional behavior or use IDbContextTransaction.

Caching: If chart data is expensive and updates are infrequent, 
cache results per-user with a short TTL.

Paging: If GetAll can return many records, consider paged API 
(page, pageSize) or streaming (IAsyncEnumerable).

Concurrency: If multiple clients update the same expense, 
consider a concurrency token (RowVersion) and propagate 
concurrency exceptions appropriately.
 */