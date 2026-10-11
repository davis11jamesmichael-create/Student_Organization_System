using StudentOrganizationSystem.Models;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// B2: pure rules for the Notification model — category whitelist and defaults.
/// </summary>
public class NotificationTests
{
    // ── Category whitelist ──

    [Theory]
    [InlineData("General",      true)]
    [InlineData("Announcement", true)]
    [InlineData("Activity",     true)]
    [InlineData("Account",      true)]
    [InlineData("general",      false)]
    [InlineData("Other",        false)]
    [InlineData("",             false)]
    [InlineData(null,           false)]
    public void NotificationCategory_whitelists_only_known_values(string? value, bool expected)
    {
        Assert.Equal(expected, NotificationCategory.IsValid(value));
    }

    // ── Model defaults ──

    [Fact]
    public void New_notification_defaults_to_General_category_and_unread()
    {
        var n = new Notification();

        Assert.Equal(NotificationCategory.General, n.Category);
        Assert.False(n.IsRead);
        Assert.Null(n.ReadAt);
        Assert.Null(n.Link);
    }

    // ── IsRead / ReadAt pairing ──

    [Fact]
    public void Marking_read_sets_both_IsRead_and_ReadAt()
    {
        var n = new Notification();
        var now = DateTime.Now;

        n.IsRead = true;
        n.ReadAt = now;

        Assert.True(n.IsRead);
        Assert.Equal(now, n.ReadAt);
    }
}
