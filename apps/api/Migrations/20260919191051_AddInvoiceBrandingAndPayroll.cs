using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBrandingAndPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceAuthorityName",
                table: "AcademyFinanceSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceAuthorityTitle",
                table: "AcademyFinanceSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceLogoUrl",
                table: "AcademyFinanceSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceSignatureUrl",
                table: "AcademyFinanceSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceTemplateKey",
                table: "AcademyFinanceSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PayrollPayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayslipNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SessionsCovered = table.Column<int>(type: "int", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPayouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    WorkerName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    PaymentModel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AmountPerCycle = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SessionsPerCycle = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayouts_AcademyId_PayrollProfileId_PaidAtUtc",
                table: "PayrollPayouts",
                columns: new[] { "AcademyId", "PayrollProfileId", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPayouts_AcademyId_PayslipNumber",
                table: "PayrollPayouts",
                columns: new[] { "AcademyId", "PayslipNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollProfiles_AcademyId_StaffUserId",
                table: "PayrollProfiles",
                columns: new[] { "AcademyId", "StaffUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollProfiles_AcademyId_TeacherId",
                table: "PayrollProfiles",
                columns: new[] { "AcademyId", "TeacherId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollPayouts");

            migrationBuilder.DropTable(
                name: "PayrollProfiles");

            migrationBuilder.DropColumn(
                name: "InvoiceAuthorityName",
                table: "AcademyFinanceSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceAuthorityTitle",
                table: "AcademyFinanceSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceLogoUrl",
                table: "AcademyFinanceSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceSignatureUrl",
                table: "AcademyFinanceSettings");

            migrationBuilder.DropColumn(
                name: "InvoiceTemplateKey",
                table: "AcademyFinanceSettings");
        }
    }
}
