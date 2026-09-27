using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            IPasswordHasher<User> passwordHasher)
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

            // Stop if an admin account already exists
            var adminExists = await context.Users
                .AnyAsync(u => u.RoleID == adminRole.RoleID);

            if (adminExists)
            {
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