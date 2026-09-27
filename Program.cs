using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add MVC services
builder.Services.AddControllersWithViews();

// Add Entity Framework Core with MySQL
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

// A fixed server version keeps startup free of a database round trip.
// ServerVersion.AutoDetect() connects immediately, so a stopped MySQL crashed the
// app with a raw driver stack trace before it could even explain the problem
// (AUDIT.md P0.4). Change this line only if the target server is not MySQL 8.
var serverVersion = new MySqlServerVersion(new Version(8, 0, 36));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        serverVersion
    )
);

// Add password hashing service
builder.Services.AddScoped<IPasswordHasher<User>,
    PasswordHasher<User>>();

// Add cookie authentication
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Create the database if it does not exist, apply the migrations and seed the
// first administrator. The whole block is guarded, because on a fresh machine a
// stopped MySQL service or a wrong password is the most likely failure: the user
// then gets one readable line instead of a driver stack trace (AUDIT.md P0.4).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context =
            services.GetRequiredService<ApplicationDbContext>();

        var passwordHasher =
            services.GetRequiredService<IPasswordHasher<User>>();

        await DbInitializer.InitializeAsync(
            context,
            passwordHasher);
    }
    catch (Exception exception)
    {
        // The useful sentence is in the innermost exception ("Unable to connect to
        // any of the specified MySQL hosts.", "Access denied for user ..."), not in
        // the EF Core wrapper that suggests enabling retry-on-failure.
        var reason = exception;
        while (reason.InnerException != null)
        {
            reason = reason.InnerException;
        }

        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Startup");

        // Log the reason only. Passing the exception object prints a 30-line stack
        // trace above the readable message, which is exactly what P0.4 is about.
        logger.LogError(
            "Startup stopped: MySQL is not reachable or the database could not be prepared ({Reason}).",
            reason.Message);

        Console.WriteLine();
        Console.WriteLine("==============================================================");
        Console.WriteLine(" MySQL is not reachable, so the system cannot start.");
        Console.WriteLine();
        Console.WriteLine(" Please check:");
        Console.WriteLine("   1. the MySQL service is running (Windows service 'MySQL80');");
        Console.WriteLine("   2. ConnectionStrings:DefaultConnection in");
        Console.WriteLine("      appsettings.Development.json has the correct server,");
        Console.WriteLine("      port, user and password for this machine;");
        Console.WriteLine("   3. that MySQL account is allowed to create the database");
        Console.WriteLine("      'StudentOrganizationDB' (the migrations do it for you).");
        Console.WriteLine();
        Console.WriteLine($" Details: {reason.GetType().Name}: {reason.Message}");
        Console.WriteLine("==============================================================");
        Console.WriteLine();

        // Stop with a non-zero exit code - a half-started app that fails on
        // every page is harder to diagnose than one clear message.
        Environment.ExitCode = 1;
        return;
    }
}

// Configure HTTP request pipeline.
// Both branches end in HomeController.Error, which now exists (AUDIT.md P0.3).
if (app.Environment.IsDevelopment())
{
    // While developing, show the real exception and stack trace.
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Turn bare 404 / 403 answers (a mistyped address, a missing record) into the
// same readable page instead of an empty browser error.
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();