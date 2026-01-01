using Microsoft.AspNetCore.Mvc;
using FinanceApp.Data;
using FinanceApp.Models;
using Microsoft.EntityFrameworkCore;
using FinanceApp.Data.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
//CONTROLLER LAYER
namespace FinanceApp.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly IExpensesService _expensesService;
        public ExpensesController(IExpensesService expensesService)
        {
            _expensesService = expensesService;
        }
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var expenses = await _expensesService.GetAll(userId);
            return View(expenses);
        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            if(!ModelState.IsValid) return View(expense);
            var userId = GetUserId();
            await _expensesService.Add(expense, userId);
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> GetChart()
        {
            var data = await _expensesService.GetChartDataAsync(GetUserId());
            return Json(data);
        }
        //delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _expensesService.GetByIdAsync(id.Value, GetUserId());
            if (expense == null) return NotFound();
            return View(expense);
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _expensesService.DeleteAsync(id, GetUserId());
            return RedirectToAction(nameof(Index));
        }
        //update
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var expense = await _expensesService.GetByIdAsync(id.Value, GetUserId());
            if (expense == null) return NotFound();
            return View(expense);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Expense expense)
        {
            if (id != expense.Id) return BadRequest();
            if (!ModelState.IsValid) return View(expense);
            try
            {
                await _expensesService.UpdateAsync(expense, GetUserId());
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
/*
Nice controller — clean, well-structured, and uses a service layer 
(IExpensesService) so the controller stays thin. Below I’ll explain 
what every part does, why it’s written that way, and give practical
recommendations and small code examples you can copy to harden and 
improve it.

File-level summary

This is an ASP.NET Core MVC controller that manages Expense 
entities for the currently authenticated user. It:

Is protected by [Authorize] (all actions require authentication).

Uses dependency injection to get an IExpensesService which 
encapsulates data access/business logic.

Exposes the usual CRUD endpoints: Index, Create (GET/POST), Edit 
(GET/POST), Delete (GET + POST), plus GetChart (returns JSON for charts).

Uses ClaimTypes.NameIdentifier to identify the current user 
and ensures each operation is executed for that user.

Line-by-line / section explanation
[Authorize]
public class ExpensesController : Controller


[Authorize] requires callers to be authenticated. Good: chart 
endpoint and all CRUD operations are protected.

private readonly IExpensesService _expensesService;
public ExpensesController(IExpensesService expensesService)
{
    _expensesService = expensesService;
}


IExpensesService is injected by DI. The controller delegates data 
operations to this service (keeps controller testable and 
focused on HTTP concerns).

private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;


Helper that returns the current user’s id from the claims principal 
(Identity stores it under ClaimTypes.NameIdentifier).

! (null-forgiving) asserts the value is not null. This is okay because 
[Authorize] is applied, but safer code checks or an exception with 
a clear message is preferable.

Index
public async Task<IActionResult> Index()
{
    var userId = GetUserId();
    var expenses = await _expensesService.GetAll(userId);
    return View(expenses);
}


Gets the user id, asks the service for that user’s expenses, and 
returns the Index view with the list (server-side rendering).

GetAll presumably returns IEnumerable<Expense> or a view model collection.

Create (GET + POST)
public IActionResult Create()
{
    return View();
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Expense expense)
{
    if(!ModelState.IsValid) return View(expense);
    var userId = GetUserId();
    await _expensesService.Add(expense, userId);
    return RedirectToAction("Index");
}


GET returns the empty form.

POST:

Model binding binds posted form fields into the Expense parameter.

ModelState.IsValid checks data-annotations on Expense.

Add(expense, userId) should set expense.UserId = userId and 
persist — the service should handle ownership and security.

Redirects to Index on success.

Notes: consider using a view model instead of binding Expense 
directly to prevent overposting.

GetChart
public async Task<IActionResult> GetChart()
{
    var data = await _expensesService.GetChartDataAsync(GetUserId());
    return Json(data);
}


Returns JSON aggregation data for Chart.js (e.g., Category/Total entries).

Protected by [Authorize] (because controller has it); service must filter by user id.

Delete (GET + POST)
public async Task<IActionResult> Delete(int? id)
{
    if (id == null) return NotFound();
    var expense = await _expensesService.GetByIdAsync(id.Value, GetUserId());
    if (expense == null) return NotFound();
    return View(expense);
}

[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    await _expensesService.DeleteAsync(id, GetUserId());
    return RedirectToAction(nameof(Index));
}


GET loads the item and shows a confirmation view.

POST actually deletes the item. ActionName("Delete") maps this POST 
action to the same route name Delete (pattern: GET Delete shows 
view, POST Delete performs deletion, method name DeleteConfirmed 
disambiguates the method in code).

Service method must validate that the item belongs to this user 
and return appropriate behavior if not found/forbidden.

Edit (GET + POST)
public async Task<IActionResult> Edit(int? id)
{
    if (id == null) return NotFound();
    var expense = await _expensesService.GetByIdAsync(id.Value, GetUserId());
    if (expense == null) return NotFound();
    return View(expense);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Expense expense)
{
    if (id != expense.Id) return BadRequest();
    if (!ModelState.IsValid) return View(expense);
    try
    {
        await _expensesService.UpdateAsync(expense, GetUserId());
    }
    catch (InvalidOperationException)
    {
        return NotFound();
    }
    return RedirectToAction(nameof(Index));
}


GET returns the edit form loaded with the entity.

POST:

Confirms route id matches model expense.Id (defense against tampering).

Validates model state.

Calls service UpdateAsync — the service must ensure the user 
is authorized and that the entity exists. It throws 
InvalidOperationException if update fails, which the 
controller maps to NotFound().

Important behavior & security notes

Authentication & Authorization: [Authorize] guards the entire 
controller. The service must still check ownership on every 
action — don’t rely only on controller-level auth.

GetUserId null-safety: User.FindFirstValue(...) might be null 
in rare hosting cases. Prefer defensive code or explicit exception:

private string GetUserId()
{
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("User id missing.");
    return id;
}


Overposting risk: Binding Expense directly in POST actions can 
allow unwanted properties to be set. Safer pattern: use a 
dedicated view model and map only allowed fields to the entity in the service.

Anti-forgery: POST methods use [ValidateAntiForgeryToken] — good.

Error handling: controller catches InvalidOperationException for 
update; ensure service throws predictable exceptions and consider 
more specific exceptions (e.g., NotFoundException, UnauthorizedAccessException) 
for clearer handling.

Return types: controller uses NotFound(), BadRequest() and 
RedirectToAction(...) — consistent and idiomatic.

JSON endpoint: GetChart returns Json(data) — ensure it returns 
safe, filtered data (only this user’s).

Suggested improvements (copy-paste)
1) Add logging
private readonly ILogger<ExpensesController> _logger;
public ExpensesController(IExpensesService expensesService, ILogger<ExpensesController> logger)
{
    _expensesService = expensesService;
    _logger = logger;
}


Use _logger.LogInformation/Warning/Error(...) in catch 
blocks to record useful diagnostics.

2) Use view models to prevent overposting

Define ExpenseCreateModel and ExpenseEditModel with only 
the fields you accept from the form.

public class ExpenseCreateModel
{
    [Required] public string Description { get; set; } = "";
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
    public string Category { get; set; } = "";
    public DateTime Date { get; set; }
}


Controller POST:

public async Task<IActionResult> Create(ExpenseCreateModel model)
{
    if (!ModelState.IsValid) return View(model);
    var expense = new Expense { Description = model.Description, Amount = model.Amount, /*...*//* };
await _expensesService.Add(expense, GetUserId());
return RedirectToAction(nameof(Index));
}

3) Tighten GetUserId safely
private string GetUserId()
{
    var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");
    return id;
}

4) More specific exceptions from service

Have IExpensesService.UpdateAsync throw e.g. NotFoundException 
or UnauthorizedAccessException which the controller maps to NotFound() or Forbid().

5) Example IExpensesService interface
public interface IExpensesService
{
    Task<IEnumerable<Expense>> GetAll(string userId);
    Task Add(Expense expense, string userId);
    Task<IEnumerable<ChartEntry>> GetChartDataAsync(string userId);
    Task<Expense?> GetByIdAsync(int id, string userId);
    Task DeleteAsync(int id, string userId);
    Task UpdateAsync(Expense expense, string userId);
}


Service implementation should ensure userId is applied (e.g., set 
expense.UserId in Add, check expense.UserId == userId in Update/Delete).

6) Concurrency handling

If EF Core concurrency tokens are used, the service should catch 
DbUpdateConcurrencyException and return an appropriate result 
so controller can inform the user.

Example: safe Update flow inside service (pseudo)
public async Task UpdateAsync(Expense updated, string userId)
{
    var existing = await _dbContext.Expenses.FindAsync(updated.Id);
    if (existing == null || existing.UserId != userId)
        throw new InvalidOperationException("Not found or forbidden.");
    existing.Description = updated.Description;
    existing.Amount = updated.Amount;
    // ...
    await _dbContext.SaveChangesAsync();
}

Summary / checklist

Controller is well - organized and correctly delegates to a 
service layer.

Ensure IExpensesService enforces ownership checks and does 
not trust client-provided UserId.

Prefer view models for create/edit POSTs (prevents overposting).

Add logging and more specific exception types in the service 
for clearer controller responses.

Make GetUserId defensive (or throw with clear message) instead of using !.
 */