using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseAcademicDeliveryControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CourseCode",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMode",
                table: "Courses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Courses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LearningOutcomes",
                table: "Courses",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaximumAge",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAge",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "Courses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionMinutes",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubjectArea",
                table: "Courses",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeeklySessions",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Batches",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchCode",
                table: "Batches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMode",
                table: "Batches",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EnrollmentStatus",
                table: "Batches",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MeetingPattern",
                table: "Batches",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomName",
                table: "Batches",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WaitlistCapacity",
                table: "Batches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_AcademyId_CourseCode",
                table: "Courses",
                columns: new[] { "AcademyId", "CourseCode" },
                unique: true,
                filter: "[CourseCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_AcademyId_BatchCode",
                table: "Batches",
                columns: new[] { "AcademyId", "BatchCode" },
                unique: true,
                filter: "[BatchCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_AcademyId_CourseCode",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Batches_AcademyId_BatchCode",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "CourseCode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "LearningOutcomes",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MaximumAge",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "MinimumAge",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SessionMinutes",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SubjectArea",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "WeeklySessions",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "BatchCode",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "EnrollmentStatus",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "MeetingPattern",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "RoomName",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "WaitlistCapacity",
                table: "Batches");
        }
    }
}
