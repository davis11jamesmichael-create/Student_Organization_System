using Microsoft.AspNetCore.Mvc;
using StudentOrganizationSystem.Controllers;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// The Index filters and paging, driven through the real controllers against a
/// SQLite database. These actions only touch ViewBag, so no HTTP context is
/// needed - the controller can be called directly.
/// </summary>
public class ListFilterTests
{
    [Fact]
    public async Task Member_search_matches_name_or_student_id()
    {
        using var db = TestDb.Create();
        await SeedMembersAsync(db);

        var controller = new MemberController(db);

        var byName = Model<Member>(await controller.Index("Cruz", null, 1));
        Assert.Single(byName);
        Assert.Contains("Cruz", byName[0].FullName);

        var byId = Model<Member>(await controller.Index("2024-00003", null, 1));
        Assert.Single(byId);
    }

    [Fact]
    public async Task Member_status_filter_splits_active_and_inactive()
    {
        using var db = TestDb.Create();
        await SeedMembersAsync(db);

        var controller = new MemberController(db);

        var active = Model<Member>(await controller.Index(null, "Active", 1));
        Assert.NotEmpty(active);
        Assert.All(active, m => Assert.True(m.IsActive));

        var inactive = Model<Member>(await controller.Index(null, "Inactive", 1));
        Assert.NotEmpty(inactive);
        Assert.All(inactive, m => Assert.False(m.IsActive));
    }

    [Fact]
    public async Task Activity_filters_by_title_and_status()
    {
        using var db = TestDb.Create();

        var now = DateTime.Now;

        // Upcoming: starts an hour from now, so the clock makes it Upcoming.
        db.Activities.Add(new Activity
        {
            ActivityTitle = "General Assembly",
            ActivityDate = now.Date,
            StartTime = now.TimeOfDay.Add(TimeSpan.FromHours(1)),
            EndTime = now.TimeOfDay.Add(TimeSpan.FromHours(2)),
            Location = "Hall",
            IsActive = true
        });

        // Cancelled: the manual flag wins over the clock.
        db.Activities.Add(new Activity
        {
            ActivityTitle = "Cancelled Picnic",
            ActivityDate = now.Date,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            Location = "Park",
            IsActive = false
        });

        await db.SaveChangesAsync();

        var controller = new ActivityController(db);

        Assert.Single(Model<Activity>(await controller.Index("Assembly", null, null, 1)));

        var upcoming = Model<Activity>(
            await controller.Index(null, null, ActivityStatus.Upcoming, 1));
        Assert.All(upcoming, a => Assert.True(a.IsActive));

        var cancelled = Model<Activity>(
            await controller.Index(null, null, ActivityStatus.Cancelled, 1));
        Assert.All(cancelled, a => Assert.False(a.IsActive));

        // An unknown status is ignored (treated as "All"), not rendered as empty.
        Assert.Equal(2, Model<Activity>(
            await controller.Index(null, null, "Hacked", 1)).Count);
    }

    [Fact]
    public async Task Paging_windows_the_list_and_clamps_a_wild_page_number()
    {
        using var db = TestDb.Create();
        await SeedMembersAsync(db);

        var controller = new MemberController(db);

        // 25 members at 20 per page -> page 2 holds the last 5.
        Assert.Equal(5, Model<Member>(await controller.Index(null, null, 2)).Count);

        // ?page=999 must not come back empty.
        Assert.NotEmpty(Model<Member>(await controller.Index(null, null, 999)));
    }

    private static List<T> Model<T>(IActionResult result)
    {
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<T>>(view.Model);

        return model.ToList();
    }

    private static async Task SeedMembersAsync(ApplicationDbContext db)
    {
        for (var i = 1; i <= 25; i++)
        {
            db.Members.Add(new Member
            {
                StudentID = $"2024-{i:00000}",
                FullName = i == 3 ? "Cruz, Ben P." : $"Member {i:00}",
                Course = "BS Computer Science",
                YearLevel = "1st Year",
                IsActive = i % 5 != 0
            });
        }

        await db.SaveChangesAsync();
    }
}
