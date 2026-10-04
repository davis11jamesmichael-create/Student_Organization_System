using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// A throw-away SQLite database for one test. SQLite (unlike EF Core's in-memory
/// provider) really enforces the unique indexes, which is exactly what the
/// duplicate-key tests need to exercise.
/// </summary>
internal static class TestDb
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        var context = new ApplicationDbContext(options);

        // An open connection keeps the in-memory database alive for the test.
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }
}
