using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class PayrollNoCashDueStatements : Migration
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Never fabricate a payment timestamp for an earnings statement.
            // Restore/compatible forward migration is required if null rows exist.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [PayrollPayouts] WHERE [PaidAtUtc] IS NULL) THROW 51000, 'Payroll downgrade blocked: no-cash-due statements require a compatible restore plan.', 1;");
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
