using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherAttendanceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TeacherAttendanceMarkedAtUtc",
                table: "ClassSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeacherAttendanceStatus",
                table: "ClassSessions",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeacherAttendanceMarkedAtUtc",
                table: "ClassSessions");

            migrationBuilder.DropColumn(
                name: "TeacherAttendanceStatus",
                table: "ClassSessions");
        }
    }
}
