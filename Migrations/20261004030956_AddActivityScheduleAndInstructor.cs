using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentOrganizationSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityScheduleAndInstructor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "Activities",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "FacultyInCharge",
                table: "Activities",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "Activities",
                type: "time(6)",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            // Rows that predate the schedule columns get the default 00:00-00:00,
            // which the "end after start" rule would reject the moment one is
            // edited. Give them an ordinary 9-to-5 slot instead so the schedule is
            // valid straight away (and the derived status reads sensibly).
            migrationBuilder.Sql(
                "UPDATE `Activities` " +
                "SET `StartTime` = '09:00:00', `EndTime` = '17:00:00' " +
                "WHERE `StartTime` = '00:00:00' AND `EndTime` = '00:00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "FacultyInCharge",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Activities");
        }
    }
}
