using Microsoft.AspNetCore.Identity;// IdentityUser, UserManager, SignInManager
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Models.ViewModels;// RegisterViewModel, LoginViewModel
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
//CONTROLLER LAYER
namespace FinanceApp.Controllers
{
    public class AccountController : Microsoft.AspNetCore.Mvc.Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        public AccountController(UserManager<IdentityUser> userManager, 
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View();
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(!ModelState.IsValid) return View(model);
            var user = new IdentityUser { UserName = model.Email, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Expenses");
            }
            foreach(var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid) return View(model);
            var result = await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Expenses");
            }
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
/*
this is a classic ASP.NET Core AccountController that uses ASP.NET Core Identity to handle user 
registration, login, logout, and access-denied handling. I’ll explain it top-to-bottom and 
call out what each part does, how the flow works, and a few security / improvement notes.

Quick overview

Uses UserManager<IdentityUser> and SignInManager<IdentityUser> (injected) to manage 
users and sign-ins.

Exposes actions: Register (GET/POST), Login (GET/POST), Logout (POST), AccessDenied (GET).

Uses ModelState validation and antiforgery protection for POSTs.

Redirects to Expenses/Index after successful auth, and uses returnUrl to send users 
back to where they came from.

Usings and class declaration
using Microsoft.AspNetCore.Identity;    // IdentityUser, UserManager, SignInManager
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Models.ViewModels;     // RegisterViewModel, LoginViewModel
using Microsoft.AspNetCore.Authorization;


These are the namespaces needed for Identity, MVC controllers, authorization 
attributes and the view models.

The controller inherits from Controller:

public class AccountController : Controller


This gives access to ModelState, View(), RedirectToAction(), Url, ViewData, etc.

Constructor & dependency injection
private readonly UserManager<IdentityUser> _userManager;
private readonly SignInManager<IdentityUser> _signInManager;

public AccountController(UserManager<IdentityUser> userManager, 
                         SignInManager<IdentityUser> signInManager)
{
    _userManager = userManager;
    _signInManager = signInManager;
}


UserManager handles creating users, finding users, updating password, etc.

SignInManager handles signing users in/out and checking credentials.

These are injected by the DI container (you configure Identity in 
Program.cs / Startup.cs).

Register (GET)
[HttpGet]
[AllowAnonymous]
public IActionResult Register() => View();


Returns the registration view (Views/Account/Register.cshtml).

[AllowAnonymous] means unauthenticated users can access it.

Register (POST)
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegisterViewModel model)
{
    if(!ModelState.IsValid) return View(model);

    var user = new IdentityUser { UserName = model.Email, Email = model.Email };
    var result = await _userManager.CreateAsync(user, model.Password);

    if (result.Succeeded)
    {
        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Expenses");
    }

    foreach(var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }
    return View(model);
}


Flow:

Check ModelState.IsValid — ensures RegisterViewModel data annotations 
(like [Required], [EmailAddress], [Compare]) passed.

Create an IdentityUser using the email as username.

CreateAsync(user, password) creates user + hashes password and 
stores it (via configured store, e.g., EF Core).

If creation succeeds: signs the user in (SignInAsync) and redirects 
to Expenses/Index.

isPersistent: false → session cookie (not a persistent cookie 
across browser restarts).

If creation failed, collects Identity errors (password too weak, duplicate 
email, etc.) and places them into ModelState so the view can show 
them (e.g., @Html.ValidationSummary()).

Notes:

The view must include @Html.AntiForgeryToken() or 
<form asp-antiforgery="true"> so [ValidateAntiForgeryToken] works.

ModelState.AddModelError(string.Empty, ...) adds a model-level 
error (shows in validation summary).

Login (GET)
[HttpGet]
[AllowAnonymous]
public IActionResult Login(string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    return View();
}


Presents the login form. If the user was redirected to login from a 
protected page, that returnUrl is preserved so they can be 
redirected back after login.

Login (POST)
[HttpPost]
[AllowAnonymous]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
{
    ViewData["ReturnUrl"] = returnUrl;
    if (!ModelState.IsValid) return View(model);

    var result = await _signInManager.PasswordSignInAsync(model.Email!, 
        model.Password!, model.RememberMe, lockoutOnFailure: false);

    if (result.Succeeded)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Expenses");
    }

    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
    return View(model);
}


Flow:

Validate ModelState.

PasswordSignInAsync(email, password, isPersistent, 
lockoutOnFailure: false) checks credentials and signs in (if valid).

model.RememberMe controls if cookie persists across browser restarts.

lockoutOnFailure: false means failed attempts don't count 
toward lockout — you might set true to protect against brute force.

If success:

If returnUrl is provided and Url.IsLocalUrl(returnUrl) is true 
(security check to avoid open redirects), redirect back there.

Otherwise redirect to Expenses/Index.

If unsuccessful, adds a generic "Invalid login attempt." 
message — generic messages avoid leaking whether email exists.

Logout (POST)
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();
    return RedirectToAction("Index", "Home");
}


Signs the user out (deletes cookie) and redirects to Home.

It's implemented as POST with antiforgery to avoid CSRF-based 
logouts (recommended).

AccessDenied (GET)
[HttpGet]
[AllowAnonymous]
public IActionResult AccessDenied() => View();


Renders a view shown when an authenticated user tries to 
access something they don’t have permission for (403).

Expected ViewModel shapes (typical)

RegisterViewModel often contains:

string Email

string Password

string ConfirmPassword
with data annotations like [Required], [EmailAddress], 
[Compare("Password")].

LoginViewModel often contains:

string Email

string Password

bool RememberMe

Where Identity is configured

You should have Identity set up in Program.cs / Startup.cs 
(e.g. services.AddIdentity<IdentityUser, IdentityRole>() or 
AddDefaultIdentity) and cookie options configured. The DI 
will provide UserManager and SignInManager.

Security / improvement suggestions

Email confirmation: before signing the user in after registration, 
require email confirmation (RequireConfirmedEmail = true) 
and send confirmation email.

Stronger error handling: log errors from result.Errors for 
diagnostics (but show user-friendly messages).

Lockout on failure: set lockoutOnFailure: true in PasswordSignInAsync 
and configure lockout options to mitigate brute-force attacks.

Use Url.IsLocalUrl (already done) — good protection vs open redirect.

Avoid auto-signin after registration if you require email 
verification.

Use view-model validation attributes to validate password 
patterns on the client & server.

Consider extending IdentityUser with application-specific 
fields (e.g., AppUser : IdentityUser) if you need more properties.

Add logging (ILogger) to the controller for audit purposes 
(login success/failure, registration errors).

Show errors in the Razor view with @Html.ValidationSummary() 
and field-level messages with @Html.ValidationMessageFor().
 */