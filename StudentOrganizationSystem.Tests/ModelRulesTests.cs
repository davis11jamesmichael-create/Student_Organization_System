using System.ComponentModel.DataAnnotations;
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

    [Fact]
    public void ActivityStatus_is_derived_from_the_clock()
    {
        var activity = new Activity
        {
            ActivityDate = new DateTime(2024, 6, 1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsActive = true
        };

        // Before the start.
        Assert.Equal(ActivityStatus.Upcoming,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 8, 59, 0)));

        // On the start and end boundaries (inclusive), and in between: Ongoing.
        Assert.Equal(ActivityStatus.Ongoing,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 9, 0, 0)));
        Assert.Equal(ActivityStatus.Ongoing,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 12, 0, 0)));
        Assert.Equal(ActivityStatus.Ongoing,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 17, 0, 0)));

        // After the end.
        Assert.Equal(ActivityStatus.Finished,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 17, 0, 1)));
    }

    [Fact]
    public void ActivityStatus_cancel_flag_wins_over_the_clock()
    {
        var activity = new Activity
        {
            ActivityDate = new DateTime(2024, 6, 1),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsActive = false
        };

        // Even while the clock says "Ongoing", a cancelled activity reads Cancelled.
        Assert.Equal(ActivityStatus.Cancelled,
            ActivityStatus.For(activity, new DateTime(2024, 6, 1, 12, 0, 0)));
    }

    [Fact]
    public void ActivityStatus_filter_and_badge_values_are_known()
    {
        Assert.True(ActivityStatus.IsFilterable(ActivityStatus.Upcoming));
        Assert.True(ActivityStatus.IsFilterable(ActivityStatus.Ongoing));
        Assert.True(ActivityStatus.IsFilterable(ActivityStatus.Cancelled));
        Assert.True(ActivityStatus.IsFilterable(ActivityStatus.Finished));

        // The old Active/Inactive words are no longer valid filters.
        Assert.False(ActivityStatus.IsFilterable("Active"));
        Assert.False(ActivityStatus.IsFilterable("Inactive"));
        Assert.False(ActivityStatus.IsFilterable(null));

        Assert.Equal("badge-upcoming", ActivityStatus.BadgeClass(ActivityStatus.Upcoming));
        Assert.Equal("badge-ongoing", ActivityStatus.BadgeClass(ActivityStatus.Ongoing));
        Assert.Equal("badge-cancelled", ActivityStatus.BadgeClass(ActivityStatus.Cancelled));
        Assert.Equal("badge-finished", ActivityStatus.BadgeClass(ActivityStatus.Finished));
    }

    [Fact]
    public void Activity_rejects_an_end_time_before_the_start()
    {
        var activity = new Activity
        {
            ActivityTitle = "Bad schedule",
            Location = "Hall",
            StartTime = new TimeSpan(15, 0, 0),
            EndTime = new TimeSpan(14, 0, 0)
        };

        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            activity,
            new ValidationContext(activity),
            results,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Activity.EndTime)));
    }

    [Fact]
    public void MemberRecords_combine_attendance_and_participation_into_one_row()
    {
        var activity = new Activity
        {
            ActivityID = 1,
            ActivityTitle = "General Assembly",
            ActivityDate = new DateTime(2024, 6, 1),
            Location = "Hall"
        };

        var attendance = new Attendance
        {
            ActivityID = 1,
            MemberID = 1,
            Status = "Present",
            RecordedAt = new DateTime(2024, 6, 1, 12, 0, 0),
            Activity = activity
        };

        var participation = new Participation
        {
            ActivityID = 1,
            MemberID = 1,
            ParticipationType = "Organizer",
            RecordedAt = new DateTime(2024, 6, 1, 9, 0, 0),
            Activity = activity
        };

        var row = Assert.Single(MemberRecords.Combine(
            new[] { attendance },
            new[] { participation }));

        Assert.Equal("General Assembly", row.ActivityTitle);
        Assert.Equal("Organizer", row.ParticipationType);
        Assert.Equal("Present", row.AttendanceStatus);

        // "Recorded" is the later of the two saves.
        Assert.Equal(new DateTime(2024, 6, 1, 12, 0, 0), row.RecordedAt);
    }

    [Fact]
    public void MemberRecords_keep_a_row_when_only_one_side_exists()
    {
        var assembly = new Activity
        {
            ActivityID = 1,
            ActivityTitle = "Assembly",
            ActivityDate = new DateTime(2024, 6, 1)
        };

        var cleanup = new Activity
        {
            ActivityID = 2,
            ActivityTitle = "Cleanup Drive",
            ActivityDate = new DateTime(2024, 6, 8)
        };

        var attendance = new Attendance
        {
            ActivityID = 1,
            MemberID = 1,
            Status = "Absent",
            RecordedAt = new DateTime(2024, 6, 1),
            Activity = assembly
        };

        var participation = new Participation
        {
            ActivityID = 2,
            MemberID = 1,
            ParticipationType = "Volunteer",
            RecordedAt = new DateTime(2024, 6, 8),
            Activity = cleanup
        };

        var rows = MemberRecords.Combine(new[] { attendance }, new[] { participation });

        Assert.Equal(2, rows.Count);

        // Newest activity first.
        Assert.Equal(2, rows[0].ActivityID);

        var withAttendance = rows.Single(r => r.ActivityID == 1);
        Assert.Equal("Absent", withAttendance.AttendanceStatus);
        Assert.Null(withAttendance.ParticipationType);

        var withParticipation = rows.Single(r => r.ActivityID == 2);
        Assert.Equal("Volunteer", withParticipation.ParticipationType);
        Assert.Null(withParticipation.AttendanceStatus);
    }
}
