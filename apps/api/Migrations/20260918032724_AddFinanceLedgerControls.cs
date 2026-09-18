using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceLedgerControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAtUtc",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReconciliationReference",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdjustedAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAtUtc",
                table: "FinanceAdjustments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReconciledAtUtc",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ReconciliationReference",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AdjustedAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AppliedAtUtc",
                table: "FinanceAdjustments");
        }
    }
}
