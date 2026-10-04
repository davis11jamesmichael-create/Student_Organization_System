using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentOrganizationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueMembershipPerMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Decision D2: one membership row per member. MySQL refuses to drop an
            // index that a foreign key depends on, so the drop and the unique add
            // must happen in a single ALTER, which MySQL validates at the end.
            migrationBuilder.Sql(
                "ALTER TABLE `Memberships` " +
                "DROP INDEX `IX_Memberships_MemberID`, " +
                "ADD UNIQUE INDEX `IX_Memberships_MemberID` (`MemberID`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `Memberships` " +
                "DROP INDEX `IX_Memberships_MemberID`, " +
                "ADD INDEX `IX_Memberships_MemberID` (`MemberID`);");
        }
    }
}
