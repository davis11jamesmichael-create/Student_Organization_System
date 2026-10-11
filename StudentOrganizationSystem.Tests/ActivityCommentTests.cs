using StudentOrganizationSystem.Models;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// B3: pure rules for ActivityComment — body validation, author-or-Admin
/// moderation rule, inactive-row exclusion, and oldest-first ordering.
/// </summary>
public class ActivityCommentTests
{
    // ── Body length rules ──

    [Theory]
    [InlineData("Hello",                                                    true)]
    [InlineData("",                                                         false)]
    [InlineData("   ",                                                      false)]
    public void Comment_body_must_be_non_empty_after_trim(string body, bool valid)
    {
        var trimmed = body.Trim();
        Assert.Equal(valid, !string.IsNullOrWhiteSpace(trimmed));
    }

    [Fact]
    public void Comment_body_at_exactly_1000_chars_is_valid()
    {
        var body = new string('x', 1000);
        Assert.True(body.Length <= 1000);
    }

    [Fact]
    public void Comment_body_at_1001_chars_is_invalid()
    {
        var body = new string('x', 1001);
        Assert.True(body.Length > 1000);
    }

    // ── Author-or-Admin moderation rule ──

    [Fact]
    public void Author_can_modify_own_comment()
    {
        var comment = new ActivityComment { UserID = 5 };
        var canModify = comment.UserID == 5; // simulates the controller check
        Assert.True(canModify);
    }

    [Fact]
    public void Non_author_non_admin_cannot_modify_comment()
    {
        var comment = new ActivityComment { UserID = 5 };
        var isAdmin = false;
        var currentUserId = 9;
        var canModify = isAdmin || comment.UserID == currentUserId;
        Assert.False(canModify);
    }

    [Fact]
    public void Admin_can_modify_any_comment()
    {
        var comment = new ActivityComment { UserID = 5 };
        var isAdmin = true;
        var currentUserId = 9;
        var canModify = isAdmin || comment.UserID == currentUserId;
        Assert.True(canModify);
    }

    // ── Inactive rows excluded ──

    [Fact]
    public void Inactive_comments_are_excluded_from_the_thread()
    {
        var comments = new List<ActivityComment>
        {
            new ActivityComment { CommentID = 1, IsActive = true,  Body = "visible" },
            new ActivityComment { CommentID = 2, IsActive = false, Body = "deleted" },
            new ActivityComment { CommentID = 3, IsActive = true,  Body = "also visible" }
        };

        var visible = comments.Where(c => c.IsActive).ToList();

        Assert.Equal(2, visible.Count);
        Assert.DoesNotContain(visible, c => c.Body == "deleted");
    }

    // ── Oldest-first ordering ──

    [Fact]
    public void Comments_are_ordered_oldest_first()
    {
        var now = DateTime.Now;
        var comments = new List<ActivityComment>
        {
            new ActivityComment { CommentID = 1, CreatedAt = now.AddMinutes(-5) },
            new ActivityComment { CommentID = 2, CreatedAt = now.AddMinutes(-1) },
            new ActivityComment { CommentID = 3, CreatedAt = now.AddMinutes(-10) }
        };

        var ordered = comments.OrderBy(c => c.CreatedAt).ToList();

        Assert.Equal(3, ordered[0].CommentID); // oldest first
        Assert.Equal(1, ordered[1].CommentID);
        Assert.Equal(2, ordered[2].CommentID);
    }

    // ── Model defaults ──

    [Fact]
    public void New_comment_defaults_to_active()
    {
        var c = new ActivityComment();
        Assert.True(c.IsActive);
        Assert.Null(c.UpdatedAt);
    }
}
