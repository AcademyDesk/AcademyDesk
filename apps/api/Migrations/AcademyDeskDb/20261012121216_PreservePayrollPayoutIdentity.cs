using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class PreservePayrollPayoutIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IdentityRecordedAtUtc",
                table: "PayrollPayouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffUserIdSnapshot",
                table: "PayrollPayouts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeacherIdSnapshot",
                table: "PayrollPayouts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerNameSnapshot",
                table: "PayrollPayouts",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerTypeSnapshot",
                table: "PayrollPayouts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityRecordedAtUtc",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "StaffUserIdSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "TeacherIdSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "WorkerNameSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "WorkerTypeSnapshot",
                table: "PayrollPayouts");
        }
    }
}
