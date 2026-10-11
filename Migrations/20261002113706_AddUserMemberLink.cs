using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentOrganizationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMemberLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MemberID",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MemberID",
                table: "Users",
                column: "MemberID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Members_MemberID",
                table: "Users",
                column: "MemberID",
                principalTable: "Members",
                principalColumn: "MemberID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Members_MemberID",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_MemberID",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MemberID",
                table: "Users");
        }
    }
}
