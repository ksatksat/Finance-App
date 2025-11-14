using Microsoft.EntityFrameworkCore;
using FinanceApp.Models;
namespace FinanceApp.Data
{
    public class FinanceAppContext : DbContext
    {
        public FinanceAppContext (DbContextOptions<FinanceAppContext> options)
            : base(options)
        {
        }
        public DbSet<Expense> Expenses { get; set; } = default!;
    }
}
/*
Explanation — FinanceAppContext (EF Core DbContext)

This class is your Entity Framework Core DbContext — the central EF type that represents a session with the database and gives you access to tables (as DbSet<T>), change tracking, and query execution.

using Microsoft.EntityFrameworkCore;
using FinanceApp.Models;

namespace FinanceApp.Data
{
    public class FinanceAppContext : DbContext
    {
        public FinanceAppContext (DbContextOptions<FinanceAppContext> options)
            : base(options)
        {
        }

        public DbSet<Expense> Expenses { get; set; } = default!;
    }
}

Line-by-line (concise)

public class FinanceAppContext : DbContext
Inherits from DbContext. EF Core uses this class to map your C# classes (entities) to database tables and to run LINQ queries.

public FinanceAppContext(DbContextOptions<FinanceAppContext> options) : base(options)
Constructor receives DbContextOptions (connection string, provider, other settings). The call to base(options) passes those options to EF Core. These options are normally configured and injected by ASP.NET Core DI (e.g., builder.Services.AddDbContext<FinanceAppContext>(...)).

public DbSet<Expense> Expenses { get; set; } = default!;
A DbSet<T> represents a table (or queryable set) of Expense entities. You use _context.Expenses to read/write expenses with LINQ. The = default!; suppresses nullable warnings (it’s assigned by framework at runtime).

What DbContext gives you

Query capability: _context.Expenses.Where(...).ToListAsync() — LINQ is translated to SQL and executed by the database.

Change tracking: add/update/delete entities and call SaveChangesAsync() to persist.

Transactions (via SaveChanges or explicit transactions).

Model configuration via conventions, DataAnnotations, or OnModelCreating.

Conventions & mapping behaviour

By convention EF uses the DbSet name (Expenses) as the table name; if no DbSet exists EF uses the entity type name.

Property-to-column mapping follows C# property names by default; you can override via DataAnnotations or Fluent API (OnModelCreating).

Typical improvements / common additions
1) Configure precision for money

Prefer decimal for money and explicitly set SQL column type:

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Expense>()
        .Property(e => e.Amount)
        .HasColumnType("decimal(18,2)");
}

2) Make queries read-only where appropriate

Use .AsNoTracking() on read-only queries to avoid tracking overhead:

var list = await _context.Expenses.AsNoTracking().ToListAsync();

3) Registering & lifetime

You register the context in Program.cs (you already have):

builder.Services.AddDbContext<FinanceAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));


AddDbContext registers the context as scoped (one instance per HTTP request) which is the recommended lifetime.

4) Migrations & database creation

Typical EF Core commands:

Add migration:

dotnet ef migrations add InitialCreate


Apply migration (create/update DB):

dotnet ef database update

5) Example usage in code
// read
var recent = await _context.Expenses
    .Where(e => e.Date >= DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(e => e.Date)
    .ToListAsync();

// add
_context.Expenses.Add(new Expense { Description = "Coffee", Amount = 3.50m, Category = "Food" });
await _context.SaveChangesAsync();

Thread-safety & scope

DbContext is not thread-safe. Use it only from the thread handling the current request and prefer the scoped DI lifetime. Don’t store it in static fields.

Why DbSet<T> is useful

Acts like a queryable collection (IQueryable<T>): you can build LINQ queries which EF translates to efficient SQL.

Tracks added/modified/deleted entities so SaveChangesAsync() persists them.
 */