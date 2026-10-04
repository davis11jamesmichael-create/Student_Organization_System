using Microsoft.Extensions.Caching.Memory;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// The small rules and policies that the controllers lean on. These are pure, so
/// they are cheap to test and they are also where the Phase 1 / Phase 2 fixes live
/// (the attendance whitelist, the D2 standing mirror, the sign-in lockout).
/// </summary>
public class ModelRulesTests
{
    [Theory]
    [InlineData("Present", true)]
    [InlineData("Absent", true)]
    [InlineData("present", false)]
    [InlineData("Hacked", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void AttendanceStatus_whitelists_only_known_values(string? value, bool expected)
    {
        Assert.Equal(expected, AttendanceStatus.IsValid(value));
    }

    [Theory]
    [InlineData("Active", true)]
    [InlineData("Suspended", true)]
    [InlineData("Inactive", true)]
    [InlineData("Deleted", false)]
    [InlineData(null, false)]
    public void MembershipStatus_whitelists_only_known_values(string? value, bool expected)
    {
        Assert.Equal(expected, MembershipStatus.IsValid(value));
    }

    [Fact]
    public void MembershipStatus_only_offers_Active_and_Suspended_by_hand()
    {
        Assert.Equal(new[] { "Active", "Suspended" }, MembershipStatus.Selectable);
        Assert.DoesNotContain(MembershipStatus.Inactive, MembershipStatus.Selectable);
    }

    [Theory]
    [InlineData("  Organizer  ", "Organizer")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void ParticipationTypes_normalize_trims(string? value, string expected)
    {
        Assert.Equal(expected, ParticipationTypes.Normalize(value));
    }

    [Fact]
    public void StandingFor_follows_the_active_flag()
    {
        Assert.Equal(MembershipStatus.Active, MembershipPolicy.StandingFor(true));
        Assert.Equal(MembershipStatus.Inactive, MembershipPolicy.StandingFor(false));
    }

    [Fact]
    public void Ensure_creates_a_membership_matching_the_member()
    {
        var member = new Member
        {
            IsActive = true,
            CreatedAt = new DateTime(2024, 6, 1)
        };

        var membership = MembershipPolicy.Ensure(member);

        Assert.Single(member.Memberships);
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(new DateTime(2024, 6, 1), membership.DateJoined);
    }

    [Fact]
    public void Ensure_is_idempotent()
    {
        var member = new Member { IsActive = true };

        var first = MembershipPolicy.Ensure(member);
        var second = MembershipPolicy.Ensure(member);

        Assert.Same(first, second);
        Assert.Single(member.Memberships);
    }

    [Fact]
    public void SyncStanding_forces_Inactive_when_the_member_is_deactivated()
    {
        var member = new Member { IsActive = true };
        MembershipPolicy.Ensure(member).Status = MembershipStatus.Suspended;

        member.IsActive = false;
        MembershipPolicy.SyncStanding(member);

        Assert.Equal(MembershipStatus.Inactive, member.Memberships.Single().Status);
    }

    [Fact]
    public void SyncStanding_returns_a_deactivated_member_to_Active()
    {
        var member = new Member { IsActive = true };
        MembershipPolicy.Ensure(member).Status = MembershipStatus.Active;

        member.IsActive = false;
        MembershipPolicy.SyncStanding(member);

        Assert.Equal(MembershipStatus.Inactive, member.Memberships.Single().Status);

        member.IsActive = true;
        MembershipPolicy.SyncStanding(member);

        Assert.Equal(MembershipStatus.Active, member.Memberships.Single().Status);
    }

    [Fact]
    public void SyncStanding_keeps_a_hand_set_suspension_while_the_member_is_active()
    {
        var member = new Member { IsActive = true };
        MembershipPolicy.Ensure(member).Status = MembershipStatus.Suspended;

        MembershipPolicy.SyncStanding(member);

        Assert.Equal(MembershipStatus.Suspended, member.Memberships.Single().Status);
    }

    [Fact]
    public void LoginAttemptTracker_locks_out_after_the_limit_and_resets()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new LoginAttemptTracker(cache);
        var key = LoginAttemptTracker.BuildKey("admin", "127.0.0.1");

        for (var i = 1; i < LoginAttemptTracker.MaxAttempts; i++)
        {
            tracker.RecordFailure(key);
            Assert.False(tracker.IsLockedOut(key));
        }

        tracker.RecordFailure(key);
        Assert.True(tracker.IsLockedOut(key));

        tracker.Reset(key);
        Assert.False(tracker.IsLockedOut(key));
    }

    [Fact]
    public void LoginAttemptTracker_keys_ignore_username_case_and_space()
    {
        Assert.Equal(
            LoginAttemptTracker.BuildKey("admin", "127.0.0.1"),
            LoginAttemptTracker.BuildKey(" ADMIN ", "127.0.0.1"));
    }

    [Fact]
    public void Report_rows_compute_total_and_rate()
    {
        var row = new ActivityAttendanceReportRow { Present = 3, Absent = 1 };

        Assert.Equal(4, row.Total);
        Assert.Equal(75, row.Rate);

        var empty = new MemberReportRow();

        Assert.Equal(0, empty.Total);
        Assert.Equal(0, empty.Rate);
    }

    [Fact]
    public void PagedResult_reports_a_full_page()
    {
        var paged = new PagedResult<int>
        {
            Items = new List<int> { 1, 2 },
            Page = 2,
            PageSize = 2,
            TotalCount = 4
        };

        Assert.Equal(2, paged.TotalPages);
        Assert.Equal(3, paged.FirstItem);
        Assert.Equal(4, paged.LastItem);
        Assert.True(paged.HasPrevious);
        Assert.False(paged.HasNext);
    }

    [Fact]
    public void PagedResult_ToPager_drops_empty_filters()
    {
        var paged = new PagedResult<string>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1
        };

        var pager = paged.ToPager("Member", "Index", new Dictionary<string, string>
        {
            ["search"] = string.Empty,
            ["status"] = "Active"
        });

        Assert.False(pager.RouteValues.ContainsKey("search"));
        Assert.Equal("Active", pager.RouteValues["status"]);
    }
}
