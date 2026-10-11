using Microsoft.AspNetCore.Identity;
using StudentOrganizationSystem.Data;
using StudentOrganizationSystem.Models;
using StudentOrganizationSystem.Services;
using Xunit;

namespace StudentOrganizationSystem.Tests;

/// <summary>
/// C1 and C2 from `boltai.md`: the seeded administrator no longer has a published
/// password, and a sign-in cookie stops working the moment the account behind it
/// changes. Both rules are pure, so they are tested directly.
/// </summary>
public class SecurityHardeningTests
{
    // ── C2: the stamp / role / active check ──

    [Fact]
    public void A_matching_stamp_keeps_the_session()
    {
        Assert.True(SecurityStampGuard.IsCurrent(
            "abc", "abc", isActive: true, "Admin", "Admin"));
    }

    [Fact]
    public void A_stale_stamp_ends_the_session()
    {
        Assert.False(SecurityStampGuard.IsCurrent(
            "old", "new", isActive: true, "Admin", "Admin"));
    }

    [Fact]
    public void A_missing_stamp_on_either_side_ends_the_session()
    {
        Assert.False(SecurityStampGuard.IsCurrent(
            null, "new", isActive: true, "Admin", "Admin"));

        Assert.False(SecurityStampGuard.IsCurrent(
            string.Empty, "new", isActive: true, "Admin", "Admin"));

        Assert.False(SecurityStampGuard.IsCurrent(
            "new", string.Empty, isActive: true, "Admin", "Admin"));
    }

    [Fact]
    public void A_deactivated_account_ends_the_session()
    {
        Assert.False(SecurityStampGuard.IsCurrent(
            "abc", "abc", isActive: false, "Admin", "Admin"));
    }

    [Fact]
    public void A_changed_role_ends_the_session()
    {
        // The case the audit called out: an Admin demoted while signed in.
        Assert.False(SecurityStampGuard.IsCurrent(
            "abc", "abc", isActive: true, "Admin", "Officer"));
    }

    [Fact]
    public void The_role_comparison_is_exact()
    {
        Assert.False(SecurityStampGuard.IsCurrent(
            "abc", "abc", isActive: true, "admin", "Admin"));
    }

    [Fact]
    public void An_empty_role_matches_an_empty_role()
    {
        Assert.True(SecurityStampGuard.IsCurrent(
            "abc", "abc", isActive: true, null, string.Empty));
    }

    [Fact]
    public void Every_new_stamp_is_long_and_unique()
    {
        var first = SecurityStamps.New();
        var second = SecurityStamps.New();

        Assert.Equal(32, first.Length);
        Assert.NotEqual(first, second);
    }

    // ── C2: what the cookie carries ──

    [Fact]
    public void The_cookie_carries_the_stamp_and_the_first_run_flag()
    {
        var admin = new User
        {
            UserID = 7,
            Username = "admin",
            MustChangePassword = true,
            Role = new Role { RoleName = AppRoles.Admin }
        };

        var principal = UserClaims.Build(admin);

        Assert.Equal(
            admin.SecurityStamp,
            principal.FindFirst(AppClaims.SecurityStamp)?.Value);

        Assert.Equal(
            "true",
            principal.FindFirst(AppClaims.MustChangePassword)?.Value);

        Assert.True(principal.IsInRole(AppRoles.Admin));
    }

    [Fact]
    public void An_ordinary_account_does_not_carry_the_first_run_flag()
    {
        var member = new User { UserID = 9, Username = "ana" };

        var principal = UserClaims.Build(member);

        Assert.Null(principal.FindFirst(AppClaims.MustChangePassword));
        Assert.NotNull(principal.FindFirst(AppClaims.SecurityStamp));
    }

    // ── C1: which pages stay open during the forced change ──

    [Theory]
    [InlineData("/Account/ChangePassword", true)]
    [InlineData("/Account/Logout", true)]
    [InlineData("/css/site.css", true)]
    [InlineData("/uploads/avatars/1_ab.png", true)]
    [InlineData("/Member/Index", false)]
    [InlineData("/", false)]
    [InlineData("/Account/LogoutSomething", false)]
    [InlineData("/User/Index", false)]
    public void Only_the_change_password_round_trip_stays_open(
        string path,
        bool expected)
    {
        Assert.Equal(expected, MustChangePasswordPolicy.IsAllowed(path));
    }

    // ── C1: the seeder never stores a published password ──

    [Fact]
    public async Task The_first_admin_never_gets_the_old_literal_password()
    {
        using var db = TestDb.Create();
        var hasher = new PasswordHasher<User>();

        var outcome = await DbInitializer.EnsureFirstAdminAsync(db, hasher);

        Assert.Equal("admin", outcome.AdminUsername);
        Assert.False(string.IsNullOrWhiteSpace(outcome.GeneratedPassword));

        var admin = db.Users.Single(u => u.Username == "admin");

        // The generated password really is the stored one...
        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                outcome.GeneratedPassword!));

        // ...and the literal the audit found is not.
        Assert.Equal(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(admin, admin.PasswordHash, "admin123"));

        Assert.True(admin.MustChangePassword);
        Assert.False(string.IsNullOrEmpty(admin.SecurityStamp));
    }

    [Fact]
    public async Task A_configured_password_is_used_and_not_reported_back()
    {
        using var db = TestDb.Create();
        var hasher = new PasswordHasher<User>();

        var outcome = await DbInitializer.EnsureFirstAdminAsync(
            db,
            hasher,
            "s3cret-from-config");

        Assert.Null(outcome.GeneratedPassword);

        var admin = db.Users.Single(u => u.Username == "admin");

        Assert.Equal(
            PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(
                admin,
                admin.PasswordHash,
                "s3cret-from-config"));
    }

    [Fact]
    public async Task The_seeder_never_adds_a_second_admin()
    {
        using var db = TestDb.Create();
        var hasher = new PasswordHasher<User>();

        await DbInitializer.EnsureFirstAdminAsync(db, hasher);
        var second = await DbInitializer.EnsureFirstAdminAsync(db, hasher);

        Assert.Null(second.AdminUsername);
        Assert.Single(db.Users.Where(u => u.Username == "admin"));
    }
}