using Microsoft.AspNetCore.Mvc;
using FinanceApp.Data;
using FinanceApp.Models;
using Microsoft.EntityFrameworkCore;
using FinanceApp.Data.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
            //if (ModelState.IsValid)
            //{
            //    await _expensesService.Add(expense);
            //    
            //    return RedirectToAction("Index");
            //}
            //return View(expense);
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
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Register(RegisterViewModel model)
        //{
        //    if(!ModelState.IsValid) return View(model);
        //    var user = new IdentityUser { UserName = model.Email, Email = model.Email };
        //    var result = await _userManager.CreateAsync(user,model.Password);
        //    if (result.Succeeded) 
        //    {
        //        await _signInManager.SignInAsync(user, isPersistent: false);
        //        return RedirectToAction("Index","Expenses");
        //    }
        //    foreach (var err in result.Errors) ModelState.AddModelError("",err.Description);
        //    return View(model);
        //}
    }
}
/*
 * this controller is a small, clean ASP.NET Core MVC controller that uses dependency injection to talk to an IExpensesService. I’ll explain what each part does, why it’s written that way, what the framework does for you, and a few practical improvements you can make.

File header / usings
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Data;
using FinanceApp.Models;
using Microsoft.EntityFrameworkCore;
using FinanceApp.Data.Service;


These bring in MVC types (Controller, IActionResult, etc.) and your app types (Expense, service interfaces). Some usings (like Microsoft.EntityFrameworkCore) may be unused in this file — they’re usually needed in services/repositories, not controllers.

Controller class & constructor injection
public class ExpensesController : Controller
{
    private readonly IExpensesService _expensesService;
    public ExpensesController(IExpensesService expensesService)
    {
        _expensesService = expensesService;
    }
    ...
}


ExpensesController inherits Controller, so it can return views, JSON, redirects, etc.

The constructor expects an IExpensesService — this is dependency injection (DI). At runtime ASP.NET will provide an implementation registered in Startup / Program.cs (e.g., services.AddScoped<IExpensesService, ExpensesService>()).

_expensesService is stored in a private readonly field for use by actions.

Index action — list all expenses
public async Task<IActionResult> Index()
{
    var expenses = await _expensesService.GetAll();
    return View(expenses);
}


This is an async action that returns an IActionResult.

_expensesService.GetAll() is awaited, so it should return Task<IEnumerable<Expense>> (or similar).

return View(expenses) renders the Views/Expenses/Index.cshtml view, passing expenses as the model. The view will receive that model as @model IEnumerable<FinanceApp.Models.Expense> (like your earlier Razor table).

Create (GET) — show the form
public IActionResult Create()
{
    return View();
}


Returns the Create view that contains the form for adding a new expense. No model is passed (or you could pass a view model / empty Expense instance).

Create (POST) — accept form submission
[HttpPost]
public async Task<IActionResult> Create(Expense expense)
{
    if (ModelState.IsValid)
    {
        await _expensesService.Add(expense);
        return RedirectToAction("Index");
    }
    return View(expense);
}


[HttpPost] means this action responds to POST requests (your Razor form posted to Create).

The parameter Expense expense is populated by model binding from form fields (names like Description, Amount, etc. must match the property names).

ModelState.IsValid checks server-side validation based on DataAnnotations on the Expense model (like [Required], [Range], etc.). If invalid, the same Create view is returned with the posted expense so validation messages can be shown.

If valid, it calls _expensesService.Add(expense) (async) and then redirects to Index. Using RedirectToAction(nameof(Index)) is a bit safer for refactorability.

Improvement / Security: add [ValidateAntiForgeryToken] to protect against CSRF:

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Expense expense) { ... }


Also make sure your form includes the antiforgery token (the form tag helper does this automatically for POST).

GetChart — returns JSON chart data
public IActionResult GetChart()
{
    var data = _expensesService.GetChartData();
    return Json(data);
}


This action returns JSON (not a View). fetch('/Expenses/GetChart') from your JS expects this endpoint.

_expensesService.GetChartData() should return a structure serializable to JSON that the client expects, e.g. an IEnumerable of objects with category and total properties:

new[] {
  new { category = "Food", total = 150.25m },
  new { category = "Transport", total = 80m }
}


return Json(data) serializes data to JSON and returns it to the client.

Notes & improvements:

If GetChartData() is asynchronous (database call), prefer public async Task<IActionResult> GetChart() and var data = await _expensesService.GetChartData();.

Modern convention: return Ok(data) (from ControllerBase) or return Json(data) is fine in MVC controllers. Alternatively decorate with [HttpGet].

Consider returning ActionResult<IEnumerable<ChartEntry>> where ChartEntry is a small DTO:

public class ChartEntry { public string Category { get; set; } public decimal Total { get; set; } }


This makes expected JSON shape explicit and easier to test.

Example IExpensesService (what it likely contains)
public interface IExpensesService
{
    Task<IEnumerable<Expense>> GetAll();
    Task Add(Expense expense);
    IEnumerable<object> GetChartData(); // or Task<IEnumerable<ChartEntry>> GetChartData();
}


GetAll() returns all expenses for the Index view.

Add() persists a new expense.

GetChartData() groups / sums expenses by category for the chart.

Example implementation of GetChartData() (LINQ)

If you use EF Core inside the service, an example:

public async Task<IEnumerable<ChartEntry>> GetChartDataAsync()
{
    return await _context.Expenses
        .GroupBy(e => e.Category)
        .Select(g => new ChartEntry {
            Category = g.Key ?? "Uncategorized",
            Total = g.Sum(e => e.Amount)
        })
        .ToListAsync();
}


Return this structure to the controller, which returns it as JSON.

Small suggestions & best practices

Add [ValidateAntiForgeryToken] to POST actions and ensure the view includes the antiforgery token (the tag helper does that automatically).

Use nameof(Index) when redirecting: RedirectToAction(nameof(Index)).

Make GetChart async if service operation is async; return Ok(data) or Json(data).

Return typed DTOs (e.g., ChartEntry) for clarity instead of anonymous objects (anonymous objects are fine but harder to test).

Consider error handling: if the service throws an exception, you could log and return appropriate status (500) or a user-friendly message.

If GetChart will be called from different origins, make sure CORS is configured correctly. If it's same-origin, nothing extra is required.

Minimal improved GetChart example
[HttpGet]
public async Task<IActionResult> GetChart()
{
    var data = await _expensesService.GetChartDataAsync(); // returns Task<IEnumerable<ChartEntry>>
    return Ok(data); // returns 200 + JSON payload
}

How this fits the UI you already have

Index() passes the list of Expense objects to your Razor view that renders the table and the <canvas>.

Your JavaScript fetch('/Expenses/GetChart') expects the JSON produced by GetChart(), maps category and total, then renders the Chart.js pie chart.
 */