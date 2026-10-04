using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// The unique indexes are the real guard against the duplicate-key bugs Phase 1
/// fixed. These tests prove the database rejects each duplicate, so the friendly
/// controller-level error can only ever be a nicer message, never the only check.
/// </summary>
public class DatabaseConstraintTests
{
    [Fact]
    public async Task StudentID_is_unique()
    {
        using var db = TestDb.Create();

        db.Members.Add(NewMember("2024-00001"));
        await db.SaveChangesAsync();

        db.Members.Add(NewMember("2024-00001"));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Attendance_is_unique_per_activity_and_member()
    {
        using var db = TestDb.Create();
        var (member, activity) = await SeedMemberAndActivityAsync(db);

        db.Attendances.Add(new Attendance
        {
            ActivityID = activity.ActivityID,
            MemberID = member.MemberID,
            Status = AttendanceStatus.Present
        });
        await db.SaveChangesAsync();

        db.Attendances.Add(new Attendance
        {
            ActivityID = activity.ActivityID,
            MemberID = member.MemberID,
            Status = AttendanceStatus.Absent
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Participation_is_unique_per_activity_and_member()
    {
        using var db = TestDb.Create();
        var (member, activity) = await SeedMemberAndActivityAsync(db);

        db.Participations.Add(new Participation
        {
            ActivityID = activity.ActivityID,
            MemberID = member.MemberID,
            ParticipationType = "Organizer"
        });
        await db.SaveChangesAsync();

        db.Participations.Add(new Participation
        {
            ActivityID = activity.ActivityID,
            MemberID = member.MemberID,
            ParticipationType = "Volunteer"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task A_member_can_only_have_one_membership()
    {
        using var db = TestDb.Create();

        var member = NewMember("2024-00002");
        db.Members.Add(member);
        await db.SaveChangesAsync();

        db.Memberships.Add(new Membership
        {
            MemberID = member.MemberID,
            DateJoined = DateTime.Now,
            Status = MembershipStatus.Active
        });
        await db.SaveChangesAsync();

        db.Memberships.Add(new Membership
        {
            MemberID = member.MemberID,
            DateJoined = DateTime.Now,
            Status = MembershipStatus.Suspended
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Username_is_unique()
    {
        using var db = TestDb.Create();

        var role = new Role { RoleName = AppRoles.Admin };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Username = "admin",
            RoleID = role.RoleID,
            PasswordHash = "x"
        });
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Username = "admin",
            RoleID = role.RoleID,
            PasswordHash = "y"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static Member NewMember(string studentId)
    {
        return new Member
        {
            StudentID = studentId,
            FullName = "Test Member",
            Course = "BS Computer Science",
            YearLevel = "1st Year"
        };
    }

    private static async Task<(Member Member, Activity Activity)> SeedMemberAndActivityAsync(
        ApplicationDbContext db)
    {
        var member = NewMember("2024-00003");

        var activity = new Activity
        {
            ActivityTitle = "Test Activity",
            ActivityDate = DateTime.Today,
            Location = "Room 1"
        };

        db.Members.Add(member);
        db.Activities.Add(activity);
        await db.SaveChangesAsync();

        return (member, activity);
    }
}
