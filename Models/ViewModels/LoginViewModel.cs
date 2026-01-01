using System.ComponentModel.DataAnnotations;
namespace FinanceApp.Models.ViewModels
{
    //MODEL LAYER
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "E-mail")]
        public string? Email { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
/*
 *LoginViewModel

Below is a focused, line-by-line explanation of what your LoginViewModel does 
and how it is used in an ASP.NET Core application, plus short examples 
showing how it ties into a controller and a Razor view.

Purpose

LoginViewModel is a simple data container (a view model) that represents 
the information a user provides on a login form: email, password, and 
whether they want the site to “remember” them. The class uses data 
annotation attributes to declare validation rules and UI metadata.

Key parts and what they mean

using System.ComponentModel.DataAnnotations;
Imports the attributes used for validation and display hints 
([Required], [EmailAddress], [Display], [DataType], etc.).

public class LoginViewModel
The view model type. It is typically passed from a Razor view to a 
controller action (model binding) and back when redisplaying the form.

public string? Email { get; set; }

string? indicates the property may be null (nullable reference type).

[Required] — validation attribute that makes the field mandatory. 
For strings, an empty string is considered invalid.

[EmailAddress] — validates the value matches an email address 
pattern (server-side).

[Display(Name = "E-mail")] — provides a friendly label used by 
helpers such as @Html.LabelFor / <label asp-for="...">.

public string? Password { get; set; }

string? again nullable.

[Required] — must be provided.

[DataType(DataType.Password)] — a UI hint indicating this 
value is a password. When rendering, helpers will produce 
an <input type="password"> (so the browser masks it). Important: 
DataType.Password is not a validation attribute; it only affects 
display/input rendering and some scaffolding behavior.

public bool RememberMe { get; set; }

A simple boolean. Default is false. Typically corresponds to a 
checkbox that determines whether the authentication cookie is persistent.

How validation works (summary)

Model binding populates LoginViewModel from form input when 
the form is posted.

ASP.NET Core runs model validation based on your data annotations.

In the controller action you check ModelState.IsValid. If false, 
you usually return the view with the model so validation 
messages can be shown.

If you include client validation scripts (jQuery Unobtrusive + 
Validation), many of these checks will run in the browser 
as well (client-side) before the POST is sent.

Typical controller usage (example)
[HttpGet]
public IActionResult Login()
{
    return View(new LoginViewModel());
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel model)
{
    if (!ModelState.IsValid)
    {
        // validation failed — redisplay form with errors
        return View(model);
    }

    // perform authentication (example)
    var signInResult = await _signInManager.PasswordSignInAsync(
        userName: model.Email, 
        password: model.Password, 
        isPersistent: model.RememberMe, 
        lockoutOnFailure: true);

    if (signInResult.Succeeded) return RedirectToAction("Index", "Home");

    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
    return View(model);
}

Typical Razor view snippet (using tag helpers)
<form asp-action="Login" method="post">
    <div asp-validation-summary="ModelOnly"></div>

    <div class="form-group">
        <label asp-for="Email"></label>
        <input asp-for="Email" class="form-control" />
        <span asp-validation-for="Email" class="text-danger"></span>
    </div>

    <div class="form-group">
        <label asp-for="Password"></label>
        <input asp-for="Password" class="form-control" />
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>

    <div class="form-group form-check">
        <input asp-for="RememberMe" class="form-check-input" />
        <label asp-for="RememberMe" class="form-check-label"></label>
    </div>

    <button type="submit" class="btn btn-primary">Log in</button>
</form>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}


Notes:

asp-validation-for and _ValidationScriptsPartial enable 
client-side error display when you include the standard 
validation scripts.

DataType.Password causes asp-for="Password" to render type="password".

Common pitfalls & suggestions

[EmailAddress] only checks format. It doesn’t check 
that the email exists or is verified.

Nullable reference types vs. [Required]: marking the 
property string? allows null at compile time; [Required] 
enforces a runtime validation rule. If you prefer 
non-nullable properties, you could use 
string Email { get; set; } = string.Empty; to avoid nullable warnings.

Custom error messages: You can pass ErrorMessage = "..." 
to [Required] or [EmailAddress] to show custom text.

Password handling: Never store or log the raw password. Use 
proper hashing and the ASP.NET Core identity APIs 
(or another secure approach) for authentication.

Extra constraints: consider [StringLength] or [MinLength] 
if you need explicit length rules.
 */