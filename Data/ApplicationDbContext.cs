using Microsoft.EntityFrameworkCore;
using StudentOrganizationSystem.Models;

namespace StudentOrganizationSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Member> Members { get; set; }

        public DbSet<Membership> Memberships { get; set; }

        public DbSet<Activity> Activities { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Participation> Participations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Unique student ID
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.StudentID)
                .IsUnique();

            // Prevent duplicate attendance
            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new
                {
                    a.ActivityID,
                    a.MemberID
                })
                .IsUnique();

            // Prevent duplicate participation
            modelBuilder.Entity<Participation>()
                .HasIndex(p => new
                {
                    p.ActivityID,
                    p.MemberID
                })
                .IsUnique();

            // Decision D2: one membership row per member, so Members 1 -> 1
            // Memberships. "History" is the row's DateJoined / UpdatedAt plus the
            // member's attendance and participation, not extra membership rows.
            modelBuilder.Entity<Membership>()
                .HasIndex(m => m.MemberID)
                .IsUnique();

            // User -> Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Member (one login per member).
            // Optional on the User side, because Admin and Officer accounts have
            // no Members row. Optional on the Member side, because a member can
            // exist before they register an account.
            modelBuilder.Entity<User>()
                .HasOne(u => u.Member)
                .WithOne(m => m.User)
                .HasForeignKey<User>(u => u.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            // Membership -> Member
            modelBuilder.Entity<Membership>()
                .HasOne(m => m.Member)
                .WithMany(m => m.Memberships)
                .HasForeignKey(m => m.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            // Attendance -> Activity
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Activity)
                .WithMany(a => a.Attendances)
                .HasForeignKey(a => a.ActivityID)
                .OnDelete(DeleteBehavior.Restrict);

            // Attendance -> Member
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany(m => m.Attendances)
                .HasForeignKey(a => a.MemberID)
                .OnDelete(DeleteBehavior.Restrict);

            // Participation -> Activity
            modelBuilder.Entity<Participation>()
                .HasOne(p => p.Activity)
                .WithMany(a => a.Participations)
                .HasForeignKey(p => p.ActivityID)
                .OnDelete(DeleteBehavior.Restrict);

            // Participation -> Member
            modelBuilder.Entity<Participation>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Participations)
                .HasForeignKey(p => p.MemberID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}