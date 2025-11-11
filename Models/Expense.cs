using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.SqlServer.Server;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace FinanceApp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        /*= null!; is assigning null to the property but using the null-forgiving operator (!) to tell the compiler:
        “I know this is null for now, trust me — this property will be non-null at runtime before it’s used.”
        It’s a way to silence the compiler warning about a non-nullable reference not being initialized.
        the trailing ! (null-forgiving operator) tells the compiler to suppress the 
        warning and treat the resulting expression as non-nullable.
        [Required] is a data-annotation used for model validation. When a form posts, 
        the MVC model binder/validation pipeline will mark the model invalid if 
        Description is empty/null. In Razor views this also enables client-side 
        validation (if scripts are present).*/
        [Required]
        public string Description { get; set; } = null!;
        //it should be decimal instead of double for money values
        //[Required(ErrorMessage = "Amount is required.")]
        //[Range(0.01, 99999999.99, ErrorMessage = "Amount must be greater than zero.")]
        //[Column(TypeName = "decimal(18,2)")]
        //[DataType(DataType.Currency)]
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public double Amount { get; set; }
        [Required]
        public string Category { get; set; } = null!;
        /*each time when user create Expense it will put the actual time*/
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
/*
model (formatted for clarity)
using System;
using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public double Amount { get; set; }

        [Required]
        public string Category { get; set; } = null!;

        // each time when user create Expense it will put the actual time 
public DateTime Date { get; set; } = DateTime.Now;
    }
}

Line - by - line explanation

public class Expense
Defines a POCO (plain old CLR object) that represents an expense. EF Core will map this to a database table by convention.

public int Id { get; set; }
By convention EF Core treats a property named Id as the primary key.

[Required] on Description, Category
DataAnnotation for validation. When model binding runs (e.g., form POST), ModelState.IsValid will be false if these properties are missing or empty. In Razor views asp-validation-for will show messages. Note: [Required] checks that the value is not null (and for strings it also treats empty as invalid by default).

public string Description { get; set; } = null!;
The = null!; is the null - forgiving operator — it tells the compiler “I promise this will be assigned at runtime, don’t warn me about nullable reference types.” It stops warnings but doesn’t actually prevent null at runtime. An alternative is to initialize to string.Empty for safety: = string.Empty;.

[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")] public double Amount { get; set; }
Range attribute enforces the amount must be at least 0.01. The validation framework will add an error message when the value is outside range.
Important: you’re using double for money — this is not recommended because double is a binary floating point type and can give rounding / precision errors for currency.Use decimal for money(and in EF map it to decimal(18, 2)), see suggestions below.

public DateTime Date { get; set; } = DateTime.Now;
The property initializer sets the Date at instance creation time to the current local time. That means:

When you create a new Expense() in code(or when model binder creates an instance and you don't supply a Date value), Date will default to DateTime.Now.

If the form posts a Date field, model binding will set the value from the form (overriding the default).
Consider using DateTime.UtcNow if you want timezone-independent timestamps.Also, if you want the DB itself to provide the timestamp (instead of the app), configure a default value in migrations or in SQL.

Practical recommendations (what to change and why)

Use decimal for money

public decimal Amount { get; set; }


And annotate the DB column precision:

[Column(TypeName = "decimal(18, 2)")]


This avoids floating point rounding errors and stores values exactly.

Prefer initializing strings to string.Empty

public string Description { get; set; } = string.Empty;


This avoids runtime null values and removes the need for null!.

Date handling

If you want UTC timestamps:
public DateTime Date { get; set; } = DateTime.UtcNow;

If you want the user to pick a date via <input type="date">, keep the property but format it in the view using [DisplayFormat] and accept the input in the Create form.

If you want the DB to set the default timestamp, configure it in the migration (or set default SQL value).

Client-side validation

To get immediate feedback in the browser, include the unobtrusive validation scripts in your layout:

< script src = "~/lib/jquery/dist/jquery.min.js" ></ script >
< script src = "~/lib/jquery-validation/dist/jquery.validate.min.js" ></ script >
< script src = "~/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js" ></ script >


Use asp - validation -for in the view so the DataAnnotations show up.

Culture & parsing

Number and date parsing depends on server culture.If users enter 12, 34 vs 12.34 you may need to handle culture or use an < input type = "number" step = "0.01" > and / or format values using the user’s culture.

Improved version(recommended)

Drop -in replacement that addresses the above points:

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceApp.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(200, MinimumLength = 1)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, 99999999.99, ErrorMessage = "Amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime Date { get; set; } = DateTime.UtcNow;
    }
}


Notes:

decimal + [Column(TypeName = "decimal(18,2)")] ensures safe currency storage in EF Core / SQL Server.

DataType and DisplayFormat help Razor produce proper HTML (e.g., type = "date" value format).

DateTime.UtcNow is usually a safer default for servers.

Example view snippets (how to render and validate)

Razor inputs for Amount and Date with validation spans:

< div class= "mb-3" >
  < label asp -for= "Amount" class= "form-label" ></ label >
  < input asp -for= "Amount" class= "form-control" type = "number" step = "0.01" />
  < span asp - validation -for= "Amount" class= "text-danger" ></ span >
</ div >

< div class= "mb-3" >
  < label asp -for= "Date" class= "form-label" ></ label >
  < input asp -for= "Date" class= "form-control" type = "date" />
  < span asp - validation -for= "Date" class= "text-danger" ></ span >
</ div >


Make sure your layout includes the validation scripts (see above) for client-side checks.

Small caveats & gotchas

= DateTime.Now runs when the object is instantiated, not once globally — but if you want timezone neutrality prefer UtcNow.

[Required] on strings rejects empty strings by default.

Range uses double parameters; when switching to decimal you still use [Range(typeof(decimal), "0.01", "99999999.99")] if needed, or validate in code/service for more complex rules.

If you need DB-level constraints (e.g., non-negative amount), add migrations with a check constraint or validate before saving.
 */