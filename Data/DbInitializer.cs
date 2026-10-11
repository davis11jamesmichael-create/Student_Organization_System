using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// What one run of the seeding did. <see cref="GeneratedPassword"/> is set
        /// only when a brand-new administrator was created without a configured
        /// password, so Program.cs can print it exactly once (C1).
        /// </summary>
        public sealed record SeedOutcome(
            string? AdminUsername,
            string? GeneratedPassword);

        public static async Task<SeedOutcome> InitializeAsync(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            bool seedSampleData = false,
            string? adminPassword = null)
        {
            // Make sure the database is available
            await context.Database.MigrateAsync();

            // The Roles table holds the role names used by
            // [Authorize(Roles = AppRoles.Admin)] in the controllers
            // (AUDIT.md P0.6, decision D3).
            await EnsureRoleAsync(
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

            var outcome = await EnsureFirstAdminAsync(
                context,
                passwordHasher,
                adminPassword);

            // C2: rows created before the SecurityStamp column existed carry an
            // empty stamp, which would read as "stale" and sign them out for good.
            await BackfillSecurityStampsAsync(context);

            // Development-only sample data (AUDIT.md P1.12). Guarded inside, so
            // calling it when the admin already exists is harmless.
            if (seedSampleData)
            {
                await SeedSampleDataAsync(context, passwordHasher);
            }

            return outcome;
        }

        /// <summary>
        /// Creates the first administrator when there is none. C1: the password is
        /// never a literal in this file - it comes from the <c>Seed:AdminPassword</c>
        /// setting, or, when that is not configured, a random one is generated and
        /// handed back so the caller can print it once. Either way the account must
        /// replace it at the first sign-in.
        /// </summary>
        public static async Task<SeedOutcome> EnsureFirstAdminAsync(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher,
            string? configuredPassword = null)
        {
            var adminRole = await EnsureRoleAsync(context, AppRoles.Admin);

            var adminExists = await context.Users
                .AnyAsync(u => u.RoleID == adminRole.RoleID);

            if (adminExists)
            {
                return new SeedOutcome(null, null);
            }

            var generated = string.IsNullOrWhiteSpace(configuredPassword);
            var password = generated
                ? GeneratePassword()
                : configuredPassword!;

            var admin = new User
            {
                Username = "admin",
                RoleID = adminRole.RoleID,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = DateTime.Now
            };

            // The plain password is hashed here and never stored.
            admin.PasswordHash = passwordHasher.HashPassword(admin, password);

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            return new SeedOutcome(
                admin.Username,
                generated ? password : null);
        }

        /// <summary>
        /// Gives every account without a stamp a fresh one. Idempotent, so after the
        /// first run it simply does nothing.
        /// </summary>
        private static async Task BackfillSecurityStampsAsync(
            ApplicationDbContext context)
        {
            var stampless = await context.Users
                .Where(u => string.IsNullOrEmpty(u.SecurityStamp))
                .ToListAsync();

            if (stampless.Count == 0)
            {
                return;
            }

            foreach (var user in stampless)
            {
                user.SecurityStamp = SecurityStamps.New();
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 16 characters from an unambiguous alphabet: readable from a console,
        /// long enough to resist guessing, and always past the 6-character minimum
        /// the password forms enforce.
        /// </summary>
        private const string PasswordAlphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        private static string GeneratePassword() =>
            RandomNumberGenerator.GetString(PasswordAlphabet, 16);

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
                new Activity { ActivityTitle = "General Assembly", ActivityDate = DateTime.Today.AddDays(-21), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0), Location = "Main Auditorium", Description = "Start-of-term orientation for all members.", FacultyInCharge = "Prof. Maria Santos", IsActive = true, CreatedAt = DateTime.Now },
                new Activity { ActivityTitle = "Coding Bootcamp", ActivityDate = DateTime.Today.AddDays(-7), StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Computer Lab 2", Description = "Hands-on session for first and second years.", FacultyInCharge = "Engr. Ramon Cruz", IsActive = true, CreatedAt = DateTime.Now },
                new Activity { ActivityTitle = "Outreach Program", ActivityDate = DateTime.Today.AddDays(14), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(11, 0, 0), Location = "Barangay Hall", Description = "Community outreach (upcoming).", FacultyInCharge = "Prof. Ana Reyes", IsActive = true, CreatedAt = DateTime.Now }
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

            // Sample announcements.
            context.Announcements.AddRange(
                new Announcement
                {
                    Title = "Welcome to the New Semester!",
                    Body = "We are excited to kick off the new semester. Please check the Activities page for upcoming events and make sure your membership details are up to date.",
                    Audience = AnnouncementAudience.All,
                    IsPinned = true,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-10)
                },
                new Announcement
                {
                    Title = "Outreach Program Volunteers Needed",
                    Body = "We are looking for volunteers for the upcoming Outreach Program. Please sign up at the secretariat office before the deadline.",
                    Audience = AnnouncementAudience.Members,
                    IsPinned = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-5)
                },
                new Announcement
                {
                    Title = "Officer Meeting — This Friday",
                    Body = "All officers are required to attend the meeting this Friday at 3:00 PM in Room 201. Attendance is mandatory.",
                    Audience = AnnouncementAudience.Staff,
                    IsPinned = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now.AddDays(-2)
                }
            );

            await context.SaveChangesAsync();

            // B3: sample comments on the first two activities.
            var adminUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == "admin");

            var officerUser = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == "officer");

            if (adminUser != null && activities.Count >= 2)
            {
                context.ActivityComments.AddRange(
                    new ActivityComment
                    {
                        ActivityID = activities[0].ActivityID,
                        UserID = adminUser.UserID,
                        Body = "Great turnout for the General Assembly! Thanks to everyone who attended.",
                        CreatedAt = activities[0].ActivityDate.AddHours(13)
                    },
                    new ActivityComment
                    {
                        ActivityID = activities[1].ActivityID,
                        UserID = adminUser.UserID,
                        Body = "Reminder: please bring your laptops for the Coding Bootcamp.",
                        CreatedAt = activities[1].ActivityDate.AddDays(-1)
                    }
                );

                if (officerUser != null)
                {
                    context.ActivityComments.Add(new ActivityComment
                    {
                        ActivityID = activities[0].ActivityID,
                        UserID = officerUser.UserID,
                        Body = "Slides from the assembly have been uploaded to the shared drive.",
                        CreatedAt = activities[0].ActivityDate.AddHours(15)
                    });
                }

                await context.SaveChangesAsync();
            }
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