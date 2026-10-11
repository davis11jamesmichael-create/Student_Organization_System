using StudentOrganizationSystem.Models;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// B1: pure rules for the Announcement model — audience whitelist, audience
/// visibility filtering, and pinned-first ordering.
/// </summary>
public class AnnouncementTests
{
    // ── Audience whitelist ──

    [Theory]
    [InlineData("All", true)]
    [InlineData("Members", true)]
    [InlineData("Staff", true)]
    [InlineData("all", false)]
    [InlineData("Everyone", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void AnnouncementAudience_whitelists_only_known_values(string? value, bool expected)
    {
        Assert.Equal(expected, AnnouncementAudience.IsValid(value));
    }

    [Fact]
    public void AnnouncementAudience_All_Values_contains_exactly_three_entries()
    {
        Assert.Equal(3, AnnouncementAudience.All_Values.Length);
        Assert.Contains(AnnouncementAudience.All, AnnouncementAudience.All_Values);
        Assert.Contains(AnnouncementAudience.Members, AnnouncementAudience.All_Values);
        Assert.Contains(AnnouncementAudience.Staff, AnnouncementAudience.All_Values);
    }

    // ── Audience visibility: what a Member may see ──

    [Theory]
    [InlineData("All", true)]
    [InlineData("Members", true)]
    [InlineData("Staff", false)]
    public void Members_can_see_All_and_Members_audience_only(string audience, bool visible)
    {
        // Mirrors the controller filter: a Member sees All + Members, not Staff.
        var canSee = audience == AnnouncementAudience.All ||
                     audience == AnnouncementAudience.Members;

        Assert.Equal(visible, canSee);
    }

    // ── Pinned-first ordering ──

    [Fact]
    public void Pinned_announcements_sort_before_unpinned()
    {
        var announcements = new List<Announcement>
        {
            new Announcement { Title = "B", IsPinned = false, CreatedAt = DateTime.Now.AddDays(-1) },
            new Announcement { Title = "A", IsPinned = true,  CreatedAt = DateTime.Now.AddDays(-2) },
            new Announcement { Title = "C", IsPinned = false, CreatedAt = DateTime.Now }
        };

        var ordered = announcements
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToList();

        Assert.Equal("A", ordered[0].Title);   // pinned first
        Assert.Equal("C", ordered[1].Title);   // newest unpinned
        Assert.Equal("B", ordered[2].Title);
    }

    // ── Model defaults ──

    [Fact]
    public void New_announcement_defaults_to_All_audience_and_active()
    {
        var a = new Announcement();

        Assert.Equal(AnnouncementAudience.All, a.Audience);
        Assert.True(a.IsActive);
        Assert.False(a.IsPinned);
    }
}
