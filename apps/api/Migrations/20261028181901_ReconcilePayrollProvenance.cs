using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReconcilePayrollProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAtUtc",
                table: "PayrollPayouts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve legacy no-cash history; never invent a paid timestamp on downgrade.
            // Check before dropping any provenance columns so rejection is atomic.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [PayrollPayouts] WHERE [PaidAtUtc] IS NULL) THROW 51000, 'Payroll downgrade blocked: historical statements require a compatible restore plan.', 1;");

            migrationBuilder.DropColumn(
                name: "IdentityRecordedAtUtc",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "StaffUserIdSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "TeacherIdSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "PaidAtUtc",
                table: "PayrollPayouts",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
