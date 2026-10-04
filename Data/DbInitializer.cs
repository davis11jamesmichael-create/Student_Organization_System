using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            bool seedSampleData = false)
        {
            // Make sure the database is available
            await context.Database.MigrateAsync();

            // The Roles table holds the role names used by
            // [Authorize(Roles = AppRoles.Admin)] in the controllers
            // (AUDIT.md P0.6, decision D3).
            var adminRole = await EnsureRoleAsync(
                context,
                AppRoles.Admin);

            await EnsureRoleAsync(
                context,
                AppRoles.Officer);

            // Member is the role a self-registered student gets. Seeded like the
            // others so AccountController.Register can always find it.
            await EnsureRoleAsync(
                context,
                AppRoles.Member);

            // Stop if an admin account already exists
            var adminExists = await context.Users
                .AnyAsync(u => u.RoleID == adminRole.RoleID);

            if (adminExists)
            {
                // The first admin is already there; only the dev sample data might
                // still be missing (it is skipped entirely on a real database).
                if (seedSampleData)
                {
                    await SeedSampleDataAsync(context, passwordHasher);
                }

                return;
            }

            // Create the first administrator
            var admin = new User
            {
                Username = "admin",
                RoleID = adminRole.RoleID,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            // Hash the password before storing it
            admin.PasswordHash =
                passwordHasher.HashPassword(
                    admin,
                    "admin123"
                );

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            // Development-only sample data (AUDIT.md P1.12).
            if (seedSampleData)
            {
                await SeedSampleDataAsync(context, passwordHasher);
            }
        }

        /// <summary>
        /// Fills an EMPTY database with a small, realistic demo set so the modules
        /// can be shown without hand-typing data. Guarded three ways: it only runs
        /// in Development, only when there are no members, and it never overwrites
        /// anything. On a real deployment this method is never called.
        /// </summary>
        private static async Task SeedSampleDataAsync(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
        {
            if (await context.Members.AnyAsync())
            {
                return;
            }

            var members = new List<Member>
            {
                new Member { StudentID = "2024-00001", FullName = "Reyes, Ana M.", Course = "BS Computer Science", YearLevel = "3rd Year", IsActive = true, CreatedAt = DateTime.Now },
                new Member { StudentID = "2024-00002", FullName = "Cruz, Ben P.", Course = "BS Information Technology", YearLevel = "2nd Year", IsActive = true, CreatedAt = DateTime.Now },
                new Member { StudentID = "2024-00003", FullName = "Santos, Carla L.", Course = "BS Computer Science", YearLevel = "4th Year", IsActive = true, CreatedAt = DateTime.Now },
                new Member { StudentID = "2024-00004", FullName = "Delos Reyes, Miguel A.", Course = "BS Information Systems", YearLevel = "1st Year", IsActive = true, CreatedAt = DateTime.Now },
                new Member { StudentID = "2024-00005", FullName = "Villanueva, Grace T.", Course = "BS Computer Science", YearLevel = "2nd Year", IsActive = false, CreatedAt = DateTime.Now }
            };

            // Decision D2: every member gets a matching membership row.
            foreach (var member in members)
            {
                MembershipPolicy.Ensure(member);
            }

            context.Members.AddRange(members);

            var activities = new List<Activity>
            {
                new Activity { ActivityTitle = "General Assembly", ActivityDate = DateTime.Today.AddDays(-21), Location = "Main Auditorium", Description = "Start-of-term orientation for all members.", IsActive = true, CreatedAt = DateTime.Now },
                new Activity { ActivityTitle = "Coding Bootcamp", ActivityDate = DateTime.Today.AddDays(-7), Location = "Computer Lab 2", Description = "Hands-on session for first and second years.", IsActive = true, CreatedAt = DateTime.Now },
                new Activity { ActivityTitle = "Outreach Program", ActivityDate = DateTime.Today.AddDays(14), Location = "Barangay Hall", Description = "Community outreach (upcoming).", IsActive = true, CreatedAt = DateTime.Now }
            };

            context.Activities.AddRange(activities);

            // Save once so the ids exist for the history rows below.
            await context.SaveChangesAsync();

            var activeMembers = members.Where(m => m.IsActive).ToList();
            var finished = activities.Take(2).ToList();

            // Attendance for the two activities that already happened.
            foreach (var activity in finished)
            {
                for (var i = 0; i < activeMembers.Count; i++)
                {
                    context.Attendances.Add(new Attendance
                    {
                        ActivityID = activity.ActivityID,
                        MemberID = activeMembers[i].MemberID,
                        Status = i % 4 == 0
                            ? AttendanceStatus.Absent
                            : AttendanceStatus.Present,
                        RecordedAt = activity.ActivityDate
                    });
                }
            }

            // Participation for the first activity only.
            var roles = new[]
            {
                "Organizer",
                "Committee Member",
                "Volunteer",
                "Participant"
            };

            for (var i = 0; i < activeMembers.Count; i++)
            {
                context.Participations.Add(new Participation
                {
                    ActivityID = activities[0].ActivityID,
                    MemberID = activeMembers[i].MemberID,
                    ParticipationType = roles[i % roles.Length],
                    RecordedAt = activities[0].ActivityDate
                });
            }

            // A second login, so the Admin/Officer split can be demonstrated.
            var officerRoleId = (await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RoleName == AppRoles.Officer))?.RoleID;

            if (officerRoleId.HasValue &&
                !await context.Users.AnyAsync(u => u.Username == "officer"))
            {
                var officer = new User
                {
                    Username = "officer",
                    RoleID = officerRoleId.Value,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                officer.PasswordHash =
                    passwordHasher.HashPassword(officer, "officer123");

                context.Users.Add(officer);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns the role with this name and creates it when it is missing, so
        /// restarting the app never duplicates a row (the seeding stays idempotent).
        /// </summary>
        private static async Task<Role> EnsureRoleAsync(
            ApplicationDbContext context,
            string roleName)
        {
            var role = await context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);

            if (role != null)
            {
                return role;
            }

            role = new Role
            {
                RoleName = roleName
            };

            context.Roles.Add(role);
            await context.SaveChangesAsync();

            return role;
        }
    }
}