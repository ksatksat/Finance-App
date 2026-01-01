using FinanceApp.Data;
using FinanceApp.Data.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<FinanceAppContext>(options =>
    options.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnectionString")));

//this requires the an extra package:
//dotnet add "D:\APPS_from_ASP_Book\FinanceApp\FinanceApp\FinanceApp\FinanceApp.csproj" package Microsoft.AspNetCore.Identity.UI --version 9.0.10
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
}).AddEntityFrameworkStores<FinanceAppContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddScoped<IExpensesService, ExpensesService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceAppContext>();
    db.Database.Migrate();   // <-- ensure DB schema (Identity tables) exist
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Expenses}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

//xatma123@gmail.com pass: Aaa222
//maxatid@gmail.com pass: Bbb222

/*
Overview

This is a typical ASP.NET Core Program.cs for an MVC web 
app that uses Entity Framework Core and ASP.NET Core Identity. 
It configures services (dependency injection), the EF Core 
DbContext, Identity, cookie paths, an application-scoped 
service, runs pending EF migrations at startup, and sets up
the HTTP request pipeline (middleware + routing). Below I 
explain each section and call, why it’s there, and important 
notes / common pitfalls.

Top: using directives
using FinanceApp.Data;
using FinanceApp.Data.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


These bring types into scope: your FinanceAppContext and service 
interfaces/implementations, plus EF Core and Identity APIs.

Builder and basic services
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


WebApplication.CreateBuilder(args) creates the host and DI 
container configuration object.

AddControllersWithViews() registers MVC controller support and 
Razor views (standard for server-side MVC apps). It sets up 
model binding, filters, view engines, etc.

Registering the EF Core DbContext
builder.Services.AddDbContext<FinanceAppContext>(options =>
    options.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnectionString")));


Registers FinanceAppContext with the DI container.

Configures it to use SQL Server via UseSqlServer(...). The 
connection string is read from configuration (e.g., appsettings.json) 
under the key "ConnectionStrings:DefaultConnectionString".

DbContext is registered with scoped lifetime by default 
(one instance per HTTP request).

Important: ensure DefaultConnectionString exists in configuration 
and points to the correct SQL Server. If not, the app will fail 
at runtime when trying to connect.

Identity setup
//this requires the an extra package:
//dotnet add ... package Microsoft.AspNetCore.Identity.UI --version 9.0.10
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
}).AddEntityFrameworkStores<FinanceAppContext>();


AddDefaultIdentity<IdentityUser>() adds the default Identity 
services and UI for the IdentityUser type (you can supply a 
custom user class if needed).

The options lambda configures password policy:

RequireDigit = true → passwords must include at least one digit

RequiredLength = 6 → minimum length 6

RequireNonAlphanumeric = false → punctuation/symbols are not required

.AddEntityFrameworkStores<FinanceAppContext>() tells Identity 
to persist user/role data in your FinanceAppContext using EF Core.

Note about the commented package: the project may need 
Microsoft.AspNetCore.Identity.UI if you intend to use the 
built-in Razor UI pages for login/register. Without it, 
you still can implement your own account pages.

Cookie paths for auth
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});


Sets where to redirect unauthenticated users (LoginPath) and 
where to redirect when a user is authenticated but lacks 
required permissions (AccessDeniedPath).

These should match routes/controllers you implement (e.g., AccountController.Login).

Register application services
builder.Services.AddScoped<IExpensesService, ExpensesService>();


Registers your business/service layer interface IExpensesService and its implementation ExpensesService.

AddScoped means one instance per HTTP request. Other lifetimes:

Singleton — one instance for app lifetime.

Transient — a new instance every time injected.

Controllers can get IExpensesService via constructor injection.

Building the app and applying migrations at startup
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceAppContext>();
    db.Database.Migrate();   // <-- ensure DB schema (Identity tables) exist
}


builder.Build() finalizes the app.

The CreateScope() block obtains a scoped service provider 
to resolve FinanceAppContext and then calls db.Database.Migrate().

Migrate() will apply any pending EF Core migrations to the 
database — useful to ensure the database schema (including Identity tables) is present.

Caution: Applying migrations automatically on startup is 
convenient in development and small deployments, but in 
production you may prefer explicit migration management 
(CI/CD step or admin-controlled) because automatic migrations 
can be risky or require elevated DB permissions.

Environment-specific error pages
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}


In non-development (production-like) environments it registers a 
generic exception handler route (/Home/Error) and enables HSTS 
(HTTP Strict Transport Security).

In development it enables the Developer Exception Page which 
shows detailed error traces (helpful for debugging).

Standard middleware registrations
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();


Explain in order and purpose:

UseHttpsRedirection() — redirect HTTP → HTTPS.

UseStaticFiles() — serve static files from wwwroot 
(JS, CSS, images). It runs early so static requests are 
served without hitting MVC routing.

UseRouting() — enables endpoint routing; parses route data 
and establishes an Endpoint.

UseAuthentication() — validates credentials 
(sets HttpContext.User) for the request.

UseAuthorization() — enforces authorization 
policies/attributes using HttpContext.User.

Order matters: UseRouting() must occur before endpoint-aware 
middleware (UseAuthentication, UseAuthorization), and 
UseAuthentication() must come before UseAuthorization(). 
This code follows the expected order.

Custom/static asset mapping and routing
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Expenses}/{action=Index}/{id?}")
    .WithStaticAssets();


MapStaticAssets() and .WithStaticAssets() are custom extension 
methods (not part of the framework core). They likely register 
endpoints for additional static content, a SPA fallback, or 
something app-specific. Without seeing their implementation, 
I can only note they are custom and you should look them 
up in your project to understand exactly what they do.

MapControllerRoute(...) sets the default controller route. 
Here default controller is Expenses and default action 
Index. id? is optional route parameter.

Note: MapControllerRoute registers endpoints after the 
middleware pipeline; endpoint mapping is where the 
routing decisions are finalized.

Run
app.Run();


Starts the web host and begins processing requests.

Additional notes, pitfalls, and suggestions

Connection string — verify appsettings.json (or environment 
variables) include "ConnectionStrings": { "DefaultConnectionString": "Server=...;Database=...;..." }.

Migrations in production — consider controlling migrations 
outside startup if DB permissions or schema changes are sensitive.

Seeding initial data — if you need an initial admin user/roles, 
do that inside the startup scope (after migrations) using 
UserManager/RoleManager and check whether a user exists before creating.

Identity UI — if you rely on Identity UI pages (login/register), 
ensure Microsoft.AspNetCore.Identity.UI package is referenced 
and your project includes the endpoints; otherwise implement 
account controllers/views manually.

Static assets custom code — check where MapStaticAssets() and 
WithStaticAssets() are defined to understand what they do 
(they may be adding routes for images, a SPA, or other behavior).

Service lifetimes and DbContext — DbContext and scoped 
services should not be captured in singletons. Keep 
IExpensesService scoped (as you did) so it can depend on FinanceAppContext.

Password policy — the policy you set is permissive (min length 6, 
digits required, non-alphanumeric not required). Adjust to your 
security needs (e.g., require uppercase, longer length) if the 
app needs stronger passwords.

HTTPS + HSTS — HSTS should only be enabled in production, as 
implemented. Ensure a valid TLS certificate is configured 
for HTTPS to avoid browser warnings.

Quick mapping: file → responsibility

Program.cs (this file): app startup, DI, middleware routing.

FinanceAppContext: EF Core DbContext that contains 
DbSet<Expense> etc. and will also be the Identity store.

IExpensesService / ExpensesService: business logic for 
expenses, injected into controllers.

AccountController (or Identity UI): handles login, register, 
access denied pages referenced by cookies config.

appsettings.json: contains connection strings and other config.
 */
/*this is a standard appsettings.json for an ASP.NET Core app. 
 * I’ll explain each section, the meaning of the keys and 
 * values, and give practical suggestions (development vs 
 * production, security, and common pitfalls).

File content and purpose

appsettings.json is the default configuration file loaded 
by an ASP.NET Core application. It typically contains 
environment-agnostic configuration like logging, connection 
strings, feature flags, etc. Values are available through 
IConfiguration and helper methods such as Configuration.GetConnectionString(...) 
(which your Program.cs already uses).

Top-level sections in your file
Logging
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}


LogLevel controls the minimum log level that will be emitted for 
categories.

Common log levels (from most verbose to least): Trace, Debug, 
Information, Warning, Error, Critical, None.

"Default": "Information" means your application will log Information,
Warning, Error, and Critical messages (and higher), but not Debug 
or Trace messages for categories that do not have an explicit override.

"Microsoft.AspNetCore": "Warning" raises the threshold for the 
Microsoft.AspNetCore category so only Warning and above are 
logged for the framework messages. This reduces noise (startup 
noise, normal request logs, etc.) while keeping app-level Information logs.

Practical notes:

In development you may want Default = Debug to see more 
detail while debugging.

In production consider raising the level to Warning or Error to 
reduce log volume and cost.

AllowedHosts
"AllowedHosts": "*"


Controls allowed Host headers for the app when using host 
filtering middleware. "*" allows requests with any host header.

Recommendation: For production set this to your hostnames (e.g., 
"AllowedHosts": "example.com;www.example.com") to prevent Host 
header attacks and accidental requests from unintended domains.

ConnectionStrings
"ConnectionStrings": {
  "DefaultConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=financeApp_data;Integrated Security=True;Pooling=False;Encrypt=False;Trust Server Certificate=True"
}


This contains a named connection string your app uses to connect to the database 
(referenced by GetConnectionString("DefaultConnectionString")).

Break down of the connection-string components

Data Source=(localdb)\\mssqllocaldb

localdb is Microsoft SQL Server Express LocalDB — a lightweight, 
developer-only SQL Server instance. It runs under the developer's 
account and is intended for development/testing only.

Initial Catalog=financeApp_data

The database name to use (creates or connects to the financeApp_data DB).

Integrated Security=True

Uses Windows Authentication (the currently signed-in Windows user) 
to log into SQL Server. No username/password in the string.

Implication: Works on developer machines with LocalDB or a domain 
environment, but not suitable for many production scenarios where 
SQL authentication or managed identity is required.

Pooling=False

Turns off connection pooling. Default for SQL client is Pooling=True. 
Disabling pooling can reduce performance when your app opens/ closes 
lots of connections; leave pooling enabled in most cases.

Encrypt=False

Disables TLS encryption for the connection between client and SQL Server.

Important: For production, set Encrypt=True so the SQL client uses 
encrypted connections. Many cloud database providers require/enforce encryption.

Trust Server Certificate=True

Instructs the client to accept the server certificate even if 
it is self-signed or not trusted.

This setting is common in development with self-signed certs or LocalDB 
but should not be used in production unless you fully understand the
trust implications. In production you should validate the server 
certificate or use a certificate issued by a trusted CA.

Summary of suitability

The provided connection string is appropriate for local development 
(LocalDB + Windows auth + no encryption).

For production:

Use a real SQL Server instance (or managed database).

Use Encrypt=True and remove Trust Server Certificate=True 
(or ensure the server certificate is trusted).

Use SQL authentication or managed identity (e.g., User ID=...;Password=...;), 
or an Azure AD managed identity where supported.

Consider enabling connection pooling (remove Pooling=False) 
for better performance.

How ASP.NET Core reads and overrides these values

appsettings.json is loaded first.

Files named appsettings.{Environment}.json (e.g., appsettings.Development.json) 
are loaded next and override the base values when ASPNETCORE_ENVIRONMENT matches.

Environment variables override both JSON settings. Environment 
variables for hierarchical keys use __ (double underscore): e.g. 
ConnectionStrings__DefaultConnectionString.

User secrets (for development) provide another override and are 
useful to store connection strings or API keys locally without 
committing to source control.

Example: set connection string via user-secrets (development):

dotnet user-secrets set 
"ConnectionStrings:DefaultConnectionString" "Server=...;Database=...;User Id=...;Password=...;"


Example environment variables:

Windows (PowerShell):

$env:ConnectionStrings__DefaultConnectionString = "Server=...;Database=...;User Id=...;Password=...;"


Linux/macOS (bash):

export ConnectionStrings__DefaultConnectionString="Server=...;Database=...;User Id=...;Password=...;"

Security & operational recommendations

Do not store production secrets in appsettings.json in source 
control. Use environment variables, managed identity, or a 
secrets store (Azure Key Vault, AWS Secrets Manager).

Connection encryption: Use Encrypt=True in production and validate 
server certificate. Avoid Trust Server Certificate=True in production.

Authentication: Prefer managed identities or securely stored SQL 
credentials. Integrated Security=True is OK for local dev 
but not always for hosted environments.

Connection pooling: Remove Pooling=False unless you have a 
specific reason to disable pooling. Pooling improves 
performance and reduces resource usage.

AllowedHosts: Replace "*" with your production hostnames.

Logging: Consider sending logs to a centralized store 
(Seq, ELK, Application Insights) and control verbosity 
per environment (appsettings.Development.json vs appsettings.Production.json).

Example of a safer production-like connection string
"ConnectionStrings": {
  "DefaultConnectionString": "Data Source=sqlserver-prod.example.com;Initial Catalog=financeApp_data;User ID=app_user;Password=StrongPasswordHere;Encrypt=True;Trusted_Connection=False;Pooling=True"
}


(Prefer storing the User ID/Password in an environment 
variable or secret store instead of the file.)

Quick tips you can apply now

Add appsettings.Development.json with more verbose logging:

{
  "Logging": { "LogLevel": { "Default": "Debug", "Microsoft.AspNetCore": "Warning" } }
}


Use dotnet user-secrets to keep your local connection 
string out of source control.

When you move to staging/production, set ASPNETCORE_ENVIRONMENT=Production 
and configure your production connection string through 
the host environment’s configuration mechanism.*/
/*Migrations are a core part of the Entity Framework Core workflow 
 * and understanding them will make developing and deploying 
 * database-backed apps much easier. I’ll explain what a migration 
 * is, why you need it, the typical workflow, the pieces that EF 
 * Core creates, and best practices — with short examples so it’s practical.

What is a migration?

A migration is a set of code instructions (generated by EF Core) 
that describes how to change the database schema to match 
changes you made to your C# model classes (your DbContext 
and entity types).
Think of a migration as a versioned, repeatable recipe for 
evolving your database schema over time.

Migrations are part of the Code-First approach: you change C# classes 
(models), then generate SQL (via migration) to apply those 
changes to the database.

Why you need migrations

Keep schema and code in sync. When you add/rename/remove entities, 
columns, constraints, etc., the database must reflect those 
changes. Migrations apply them safely and in the correct order.

Version control of schema changes. Each migration is a 
source-code file you commit; team members can apply the 
same changes reproducibly.

Repeatable deployments. Migrations let you apply the same 
schema updates on developer machines, staging, and production.

Support for data migrations. Migrations can include 
data-manipulation logic (e.g., backfilling values) 
when you change shape of data.

Automatic detection. EF compares the model snapshot to 
the current model to scaffold an appropriate migration.*/

/*
this is the Program.cs (minimal-host) bootstrap for an ASP.NET 
Core MVC app. I’ll walk through it top-to-bottom, explain what 
each line does, point out a couple of non-standard lines
you have (and what they likely mean), and offer small 
improvements you may want to add.

What this file does (high level)

This file configures and starts the web application:

Builds a WebApplicationBuilder (builder) which collects 
configuration and service registrations.

Registers services with the Dependency Injection container 
(MVC, EF Core DbContext, your app service).

Builds the WebApplication (app) which contains the middleware 
pipeline.

Configures request pipeline (error handling, HTTPS, routing, 
authorization, endpoints).

Runs the web server.

Line-by-line explanation
var builder = WebApplication.CreateBuilder(args);


Creates the builder for the minimal hosting model (aggregates 
configuration, logging, and DI container). args typically 
come from static void Main(string[] args).

builder.Services.AddControllersWithViews();


Registers MVC services for controllers and Razor views (needed 
for traditional MVC apps). This adds routing, model binding, 
view engines, etc.

builder.Services.AddDbContext<FinanceAppContext>(options =>
    options.UseSqlServer(builder.Configuration.
    GetConnectionString("DefaultConnectionString")));


Registers your Entity Framework Core DbContext (FinanceAppContext) in DI.

.UseSqlServer(...) configures EF Core to use SQL Server with a 
connection string fetched from configuration (appsettings.json, 
environment variables, or secrets). Make sure your 
appsettings.json has a ConnectionStrings section with key DefaultConnectionString.

builder.Services.AddScoped<IExpensesService, ExpensesService>();


Registers your IExpensesService implementation (ExpensesService) 
with scoped lifetime:

Scoped = one instance per HTTP request (common for DbContext-backed services).

This lets controllers get IExpensesService via constructor injection.

var app = builder.Build();


Builds the WebApplication which we will now configure (wire 
middleware and endpoints).

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


Checks the environment.

If not Development (i.e., Production), it installs a generic 
exception handler page (redirects to /Home/Error) and enables
HSTS (HTTP Strict Transport Security).

In Development you would typically want app.UseDeveloperExceptionPage() 
instead (helpful stack traces).

app.UseHttpsRedirection();


Redirects HTTP requests to HTTPS. Good for security.

app.UseRouting();


Adds the routing middleware. This extracts route data and prepares 
for endpoint matching. UseRouting() must come before 
UseAuthorization() and endpoint mapping.

app.UseAuthorization();


Adds authorization middleware to enforce [Authorize] attributes 
or policy-based authorization. If you use authentication, 
add app.UseAuthentication() before UseAuthorization().

app.MapStaticAssets();


Not a built-in method — likely a custom extension method in 
your project that maps or configures static assets (CSS/JS/images) 
or sets up endpoints for static content. If you don’t have this, 
you’d normally call app.UseStaticFiles() to serve the wwwroot files.

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


MapControllerRoute(...) maps the default MVC route with 
controller/action/id pattern. This is what enables /Expenses/Index and /Expenses/Create.

.WithStaticAssets() again looks like a custom extension that 
probably configures static-asset-related endpoint behavior for 
the route chain. This is not standard — check your project 
for extension methods named MapStaticAssets and WithStaticAssets 
to see what they do exactly.

app.Run();


Starts the web server and begins processing requests. This 
call blocks until the server shuts down.

 */

/*
Кратко — что делает MapStaticAssets()

MapStaticAssets() — это расширяющий метод для IEndpointRouteBuilder 
(то есть его обычно вызывают на app в Program.cs). Он мэпит (регистрирует) 
статические файлы, которые были сгенерированы во время сборки, 
как HTTP-эндпойнты, так чтобы эти файлы могли отдавать клиентам.

Иначе говоря: этот метод помогает автоматически подключить 
статические ресурсы, которые не лежат непосредственно в wwwroot, 
а были подготовлены на этапе сборки (например, ресурсы, 
упакованные в библиотеку, сгенерированные ассеты из других проектов и т.п.).

Параметры и поведение

Сигнатура (в подсказке):
IEndpointRouteBuilder.MapStaticAssets([string? staticAssetsManifestPath = null])

staticAssetsManifestPath — путь к manifest файлу, который 
описывает, какие статические файлы нужно смэпить.

Если передать null (по умолчанию), метод попытается 
использовать IHostEnvironment.ApplicationName, чтобы 
найти манифесты для текущего приложения.

Если указан относительный путь, то поиск будет в AppContext.BaseDirectory 
(обычно — папка с исполняемым файлом).

Можно указать и полный путь до манифеста, если он лежит не в стандартном месте.

Чем это отличается от UseStaticFiles() и UseBlazorFrameworkFiles()?

app.UseStaticFiles() — стандартный middleware, который отдаёт 
файлы из wwwroot (или из StaticFileOptions.FileProvider, если 
вы настроили другое место). Это middleware в pipeline обработки запросов.

app.MapStaticAssets() — мэпит файлы сгенерированные во время 
сборки / поставляемые манифестом как эндпойнты (специфичный 
механизм для build-produced assets и пакетов).

UseBlazorFrameworkFiles() — ещё одна специализированная помощница 
(для Blazor WASM), которая мэпит клиентские файлы Blazor. Эти 
вызовы могут сосуществовать — у каждого своя задача.

Если у вас обычный сайт, где все ресурсы лежат в wwwroot, то 
UseStaticFiles() достаточно. MapStaticAssets() нужен в более 
специфичных сценариях (RCL, билд-процессы, SDK-произведённые ассеты и т.п.).

Пример (минимальный Program.cs)
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();        // сервируем файлы из wwwroot
app.MapStaticAssets();       // регистрируем build-produced static assets (если есть)

app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();


Если манифест лежит в нестандартном месте:

app.MapStaticAssets("path/to/static-assets-manifest.json");

Когда это нужно на практике

Когда статические ресурсы формируются автоматически в процессе 
сборки (например, инструмент/пакет положил ассеты рядом с 
исполняемым файлом и создал manifest).

Когда используете библиотеки/пакеты, которые встраивают 
статические файлы и создают manifest для их экспозиции.

Для некоторых шаблонов приложений (например, SPA / Blazor) — 
сборщик SDK может генерировать такие manifest-файлы, и MapStaticAssets() 
упростит их доступ.

Как проверить, что оно сработало

Посмотрите в выходную папку (bin/…/publish или AppContext.BaseDirectory) 
— должен быть файл-манифест (обычно JSON) и соответствующие ассеты.

Запустите приложение и откройте URL к ожидаемому файлу — он должен отдаваться.

Включите логирование (Information/Debug) — часто там видно, 
какие файлы/эндпойнты были зарегистрированы.
 */

/*
Что делает WithStaticAssets()

WithStaticAssets() — это расширяющий метод, который добавляет к 
создаваемым эндпойнтам метаданные о статических ресурсах. 
В подсказке видно: он 
«Adds a Microsoft.AspNetCore.Components.ResourceAssetCollection metadata instance to the endpoints». 
То есть метод не сам отдает файлы — он помечает (помещает metadata) 
маршруты/экшны контроллера, чтобы другие механизмы (например, 
инфраструктура, читающая манифесты статических ассетов или 
специальная middleware/endpoint-логика) могли понять, какие 
сборочные/ресурсные файлы связаны с этим маршрутом и как их отдавать.

Зачем это нужно

Когда у вас есть ассеты, которые были сгенерированы/включены не 
в wwwroot, а через SDK/библиотеки (RCL, Blazor, build-produced assets), 
и эти ассеты логически «принадлежат» какому-то маршруту/файлу вашего приложения.

Добавив метаданные через WithStaticAssets(), вы связываете 
контроллерный маршрут с наборами статических ресурсов, которые 
потом будут экспонироваться (например, MapStaticAssets() или 
другая логика читает эти метаданные и регистрирует соответствующие 
конечные точки/файлы).

Параметры

Сигнатура обычно выглядит так:

ControllerActionEndpointConventionBuilder.WithStaticAssets(string? manifestPath = null)


manifestPath — опционально: путь к manifest-файлу, описывающему ресурсы. Если null, 
используется стандартный поиск (например, по IHostEnvironment.ApplicationName 
/ AppContext.BaseDirectory) для нахождения manifest'ов.

Отличие от UseStaticFiles() и MapStaticAssets()

UseStaticFiles() — middleware, отдаёт файлы из wwwroot (или указанного IFileProvider).

MapStaticAssets() — регистрирует (мапит) build-produced ассеты как эндпойнты, читая manifest.

WithStaticAssets() — не отдаёт ничего сам по себе; он помечает 
конкретный маршрутизируемый эндпойнт (контроллер/экшн) метаданными 
о ресурсах, чтобы другие части инфраструктуры знали: «этот 
маршрут связан с такими-то статическими ассетами».

Пример использования
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets(); // добавляем метаданные ассетов к этому маршруту


или с указанием манифеста:

app.MapControllerRoute(...).WithStaticAssets("path/to/static-assets-manifest.json");

Когда не нужен

Для простого сайта, где все файлы в wwwroot, WithStaticAssets() 
не нужен. Нужен в сценариях с RCL, Blazor/SPA и прочими 
пакетами/инструментами, которые создают и регистрируют ассеты через манифесты.
 */

/*
that file is an appsettings.json configuration file used 
by ASP.NET Core. It stores application settings (logging, 
host rules, connection strings, etc.) that the framework 
reads at startup via the IConfiguration system. I’ll 
explain each section, what the values mean, and give 
practical security / deployment tips.

Full file (for reference)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=financeApp_data;Integrated Security=True;Pooling=False;Encrypt=False;Trust Server Certificate=True"
  }
}

Section-by-section explanation
Logging
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}


Controls the built-in logging behavior (what severity levels are recorded).

"Default": "Information" — by default the app will emit logs 
at Information level and above (Information, Warning, Error, Critical). 
Debug and Trace are ignored.

"Microsoft.AspNetCore": "Warning" — overrides the logging level for the 
Microsoft.AspNetCore category so framework/infrastructure logs from 
that namespace are limited to Warning+ (reduces noisy framework logs).

You can add more categories 
(e.g., "Microsoft.EntityFrameworkCore": "Information") or change 
levels for development (Debug) vs production (Warning/Error).

AllowedHosts
"AllowedHosts": "*"


Controls host header filtering when using the built-in host 
filtering middleware (UseHostFiltering / HostFiltering is 
enabled by default in some templates).

"*" means accept requests for any host header (development convenience).

For production it’s more secure to restrict to specific host names:

"AllowedHosts": "example.com;www.example.com"


This helps mitigate host header attacks.

ConnectionStrings
"ConnectionStrings": {
  "DefaultConnectionString": "Data Source=(localdb)\\mssqllocaldb;Initial Catalog=financeApp_data;Integrated Security=True;Pooling=False;Encrypt=False;Trust Server Certificate=True"
}


Standard place to store DB connection strings. Use 
builder.Configuration.GetConnectionString("DefaultConnectionString") 
or Configuration["ConnectionStrings:DefaultConnectionString"] to read it in code.

The value is a SQL Server connection string. Breakdown of the parts:

Data Source=(localdb)\\mssqllocaldb
Points to the LocalDB SQL Server instance (developer/local machine DB). 
Note the JSON \\ is an escaped backslash — correct for JSON.

Initial Catalog=financeApp_data
The database name.

Integrated Security=True
Use Windows Authentication (current Windows account). 
No DB username/password required.

Pooling=False
Disables ADO.NET connection pooling. Default is True normally; 
pooling off may be okay for local dev but you usually want 
pooling enabled (performance reason) for production.

Encrypt=False
Disables TLS encryption for SQL Server connection. On production 
you usually want Encrypt=True so traffic to the DB is encrypted.

Trust Server Certificate=True
Accepts the server certificate even if it is self-signed or 
not trusted. This is commonly used with LocalDB or dev instances 
but should not be used in production unless you understand the consequences.

How ASP.NET Core reads this

At startup WebApplication.CreateBuilder(args) wires configuration 
sources (appsettings.json, appsettings.{Environment}.json, 
environment variables, user secrets, command-line args).

Code like this uses the connection string:

builder.Services.AddDbContext<FinanceAppContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

Practical recommendations & security notes

Use LocalDB only for development
(localdb)\mssqllocaldb is fine for dev on Windows. For production 
use a proper SQL Server / Azure SQL instance.

Don’t store secrets in appsettings.json for production

Use environment variables, Azure Key Vault, or the Secret Manager 
for sensitive values (passwords, keys).

For example, in production store a connection string with credentials 
in an environment variable or Key Vault and keep appsettings.json commit-safe.

Switch to Encrypt=True in production
Ensure secure database connections; avoid Trust Server Certificate=True 
unless you have a trusted certificate chain or an explicit reason.

Enable connection pooling in production
Remove Pooling=False (allow default pooling) for performance.

Avoid AllowedHosts: "*" in production
Set host names explicitly to reduce attack surface.

Use environment-specific configuration
Create appsettings.Development.json / appsettings.Production.json to 
override values per environment. The environment-specific file loaded 
later will override appsettings.json settings.

Async-safe logging and filtering
If logs are noisy, tune category levels or add a structured logger 
like Serilog for file/elastic sinks and filtering.

Example: production-ready connection string (SQL authentication)
"ConnectionStrings": {
  "DefaultConnectionString": "Server=tcp:mydbserver.database.windows.net,1433;Initial Catalog=financeApp_db;User ID=dbuser;Password=YourStrongPassword!;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=True"
}


Uses SQL auth (User ID + Password), TLS encryption enabled, 
and MultipleActiveResultSets=True if you rely on MARS.

Important: Do not commit production passwords to source control 
— use secrets or environment variables.

How to use User Secrets (dev) / env vars (prod)

Local dev: dotnet user-secrets init then dotnet user-secrets set 
"ConnectionStrings:DefaultConnectionString" "<value>".

Production: set ConnectionStrings__DefaultConnectionString 
environment variable (note : is replaced by __).

Quick tips for debugging connection issues

Make sure LocalDB instance exists and the DB financeApp_data 
is created (migrations or Update-Database).

If connecting with Integrated Security from a service account 
(Linux container), Windows Integrated auth won't work; 
use SQL auth or a different setup.

If you get TLS/certificate errors, prefer Encrypt=True with a 
valid cert instead of Trust Server Certificate=True in production.
 */