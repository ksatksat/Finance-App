using FinanceApp.Data.Service;
using FinanceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//MODEL LAYER
namespace FinanceApp.Data.Service 
{
    public class ExpensesService : IExpensesService
    {
        private readonly FinanceAppContext _context;
        public ExpensesService(FinanceAppContext context) => _context = context;
        public async Task Add(Expense expense, string userId)
        {
            expense.UserId = userId;
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Expense>> GetAll(string userId)
        {
            //var expenses = await _context.Expenses.ToListAsync();
            //return expenses;
            return await _context.Expenses
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
        public async Task<IEnumerable<ChartEntry>> GetChartDataAsync(string userId)
        {
            return await _context.Expenses
                .Where(e =>  e.UserId == userId)
                .GroupBy(e => e.Category)
                .Select(g => new ChartEntry
                {
                    Category = g.Key!,
                    Total = g.Sum(e => e.Amount)
                })
                .ToListAsync();
        }
        //delete
        public async Task<Expense?> GetByIdAsync(int id, string userId)
        {
            var expence = await _context.Expenses.FindAsync(id);
            if(expence == null||expence.UserId != userId)return null;
            return expence;
        }
        public async Task DeleteAsync(int id, string userId)
        {
            var entity = await _context.Expenses.FindAsync(id);
            if (entity == null|| entity.UserId != userId) return;
            _context.Expenses.Remove(entity);
            await _context.SaveChangesAsync();
        }
        //update
        public async Task UpdateAsync(Expense expense, string userId)
        {
            var existing = await _context.Expenses.FindAsync(expense.Id);
            if (existing == null||existing.UserId != userId) throw new InvalidOperationException("Expense not found");
            existing.Description = expense.Description;
            existing.Amount = expense.Amount;
            existing.Category = expense.Category;
            existing.Date = expense.Date;
            await _context.SaveChangesAsync();
        }
    }
}
/*
 this is a clean, typical service-layer implementation that 
encapsulates data access for Expense entities. I’ll explain 
what each part does, why it’s written that way, and then give 
concrete suggestions and improved snippets you can adopt 
for robustness, performance, and security.

1) File-level overview
using FinanceApp.Data.Service;
using FinanceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FinanceApp.Data.Service
{
    public class ExpensesService : IExpensesService
    {
        ...
    }
}


The service lives in FinanceApp.Data.Service and implements the 
IExpensesService interface (you already register the implementation with DI).

It depends on FinanceAppContext (EF DbContext) to talk to the database.

IExpensesService is the contract used by controllers or other 
components — this is good separation of concerns for testability.

2) Constructor / dependency injection
private readonly FinanceAppContext _context;
public ExpensesService(FinanceAppContext context) => _context = context;


FinanceAppContext is injected by ASP.NET Core DI.

The context is stored as a readonly field.

Because DbContext is registered as scoped, the service must also be 
scoped (which you did in Program.cs). This prevents longer-lived 
singletons from capturing a scoped DbContext.

3) Method-by-method explanation
Add
public async Task Add(Expense expense, string userId)
{
    expense.UserId = userId;
    _context.Expenses.Add(expense);
    await _context.SaveChangesAsync();
}


Sets UserId on the Expense to bind the record to the current user.

Calls Add so EF marks the entity as Added.

SaveChangesAsync() commits the insert to the DB.

Note: no validation is performed here (assumes caller validated model). 
Also this uses one DB round trip.

GetAll
public async Task<IEnumerable<Expense>> GetAll(string userId)
{
    return await _context.Expenses
        .Where(e => e.UserId == userId)
        .OrderByDescending(e => e.Date)
        .ToListAsync();
}


Returns all expenses for the given userId, ordered newest-first by Date.

Uses LINQ-to-Entities; the Where/OrderByDescending are translated to SQL.

ToListAsync() executes the query and materializes a List<Expense>.

If this result set may be large, consider pagination (skip/take) 
to avoid returning too many rows at once.

For read-only queries you may add .AsNoTracking() to lower 
memory overhead and avoid change-tracking.

GetChartDataAsync
public async Task<IEnumerable<ChartEntry>> GetChartDataAsync(string userId)
{
    return await _context.Expenses
        .Where(e =>  e.UserId == userId)
        .GroupBy(e => e.Category)
        .Select(g => new ChartEntry
        {
            Category = g.Key!,
            Total = g.Sum(e => e.Amount)
        })
        .ToListAsync();
}


Produces aggregated data grouped by Category, projecting to 
ChartEntry (assumed: { string Category; decimal Total; }).

The grouping and sum run in the database which is efficient.

The code uses null-forgiving g.Key! — meaning Category might be 
nullable in the model; consider handling null explicitly 
(e.g., g.Key ?? "Uncategorized").

Ensure Amount is a decimal type for accurate money calculations.

GetByIdAsync
public async Task<Expense?> GetByIdAsync(int id, string userId)
{
    var expence = await _context.Expenses.FindAsync(id);
    if(expence == null||expence.UserId != userId)return null;
    return expence;
}


Uses FindAsync(id) to get the entity by primary key.

Then checks UserId to ensure the current user actually owns 
this entity; returns null otherwise.

Note: FindAsync only accepts primary key(s). If you want the DB 
to enforce the userId condition on the query (a single SQL 
query that includes the user check), use:

await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);


That approach avoids an extra in-memory check and is clearer for authorization.

DeleteAsync
public async Task DeleteAsync(int id, string userId)
{
    var entity = await _context.Expenses.FindAsync(id);
    if (entity == null|| entity.UserId != userId) return;
    _context.Expenses.Remove(entity);
    await _context.SaveChangesAsync();
}


Finds the entity and ensures it belongs to userId. If not found 
or not owned, it quietly returns.

Remove + SaveChangesAsync commits deletion.

Consider whether you prefer an exception for missing/unauthorized 
deletes or silent return (both are valid patterns depending on API design).

UpdateAsync
public async Task UpdateAsync(Expense expense, string userId)
{
    var existing = await _context.Expenses.FindAsync(expense.Id);
    if (existing == null||existing.UserId != userId) throw new InvalidOperationException("Expense not found");
    existing.Description = expense.Description;
    existing.Amount = expense.Amount;
    existing.Category = expense.Category;
    existing.Date = expense.Date;
    await _context.SaveChangesAsync();
}


Loads the existing entity by primary key, checks ownership, 
updates fields, and saves.

Because existing is tracked, changing properties is enough 
for EF to generate an UPDATE on SaveChangesAsync().

Throws InvalidOperationException when not found or unauthorized; 
the controller should translate that to the appropriate 
HTTP response (404 or 403).

Consider concurrency handling (e.g., a rowversion 
concurrency token) to avoid lost updates in concurrent scenarios.

4) Security concerns & ownership checks

You correctly check UserId on read, update, and delete operations 
— this prevents one user from operating on another user’s data.

Ensure the controller always passes the current user id 
(from User.Identity / UserManager) and never accepts userId from the client.

If a controller forgets to pass the current user id, your 
checks could be bypassed — enforce usage via clear controller 
patterns or encapsulate user resolution in the service or an authorization policy.

5) Performance & correctness suggestions

Prefer filtered queries for single-entity retrieval
Replace FindAsync(id) + UserId check with a single DB query:

var entity = await _context.Expenses
    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);


Use AsNoTracking() for read-only queries (faster, less memory):

return await _context.Expenses
    .AsNoTracking()
    .Where(e => e.UserId == userId)
    .OrderByDescending(e => e.Date)
    .ToListAsync();


Pagination for GetAll:

public async Task<IReadOnlyList<Expense>> GetAllPage(string userId, int page, int pageSize)
{
    return await _context.Expenses
        .AsNoTracking()
        .Where(e => e.UserId == userId)
        .OrderByDescending(e => e.Date)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}


Indexing: add a DB index on (UserId, Date) to speed up 
queries that filter by user and order by date.

Null categories: handle null categories in the grouping:

Category = g.Key ?? "Uncategorized"


Decimal/Precision for money: ensure the Amount property is 
decimal with appropriate precision in EF mapping, e.g. [Column(TypeName = "decimal(18,2)")].

Cancellation tokens: add CancellationToken parameters for better control:

public async Task Add(Expense expense, string userId, CancellationToken ct = default)
{
    ...
    await _context.SaveChangesAsync(ct);
}


Concurrency token: if multiple clients can edit the same 
expense, add a concurrency token (e.g., byte[] RowVersion) 
and handle DbUpdateConcurrencyException in UpdateAsync.

Error handling strategy: decide on consistent behavior 
— return null for not found, or throw exceptions; ensure 
controllers map results to proper HTTP responses (404/403/400/500).

Unit testing: because you use IExpensesService and FinanceAppContext, 
tests can use an in-memory DB (or better: Sqlite in-memory) 
and verify behavior, including ownership checks.

6) Suggested improved code (small refactor)

Here are a few practical improvements applied to GetByIdAsync, DeleteAsync, UpdateAsync, and GetAll:

public async Task<Expense?> GetByIdAsync(int id, string userId, CancellationToken ct = default)
{
    return await _context.Expenses
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
}

public async Task DeleteAsync(int id, string userId, CancellationToken ct = default)
{
    var entity = await _context.Expenses
        .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

    if (entity == null) return; // or throw NotFoundException

    _context.Expenses.Remove(entity);
    await _context.SaveChangesAsync(ct);
}

public async Task UpdateAsync(Expense expense, string userId, CancellationToken ct = default)
{
    var existing = await _context.Expenses
        .FirstOrDefaultAsync(e => e.Id == expense.Id && e.UserId == userId, ct);

    if (existing == null) throw new InvalidOperationException("Expense not found");

    existing.Description = expense.Description;
    existing.Amount = expense.Amount;
    existing.Category = expense.Category;
    existing.Date = expense.Date;

    try
    {
        await _context.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        // handle concurrency conflict: throw a custom exception, return result, etc.
        throw;
    }
}

public async Task<IReadOnlyList<Expense>> GetAll(string userId, int page = 1, int pageSize = 50, CancellationToken ct = default)
{
    return await _context.Expenses
        .AsNoTracking()
        .Where(e => e.UserId == userId)
        .OrderByDescending(e => e.Date)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);
}

7) Final notes

Your service is well-structured and covers standard CRUD and an aggregation query for charts.

The most important improvements are: make single-query 
checks that include UserId, consider AsNoTracking() for 
reads, add pagination for lists, add DB indexes, and add 
concurrency handling if needed.

Decide on consistent error semantics (silent return vs exceptions) 
and ensure controller-layer maps those to correct HTTP responses.
 */