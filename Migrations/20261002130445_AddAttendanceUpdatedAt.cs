using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentOrganizationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Attendances",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Attendances");
        }
    }
}
