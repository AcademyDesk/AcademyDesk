using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAcademyFinanceGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademyFinanceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxLabel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaxRatePercent = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: false),
                    DefaultPaymentTermsDays = table.Column<int>(type: "int", nullable: false),
                    TaxInclusivePricing = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademyFinanceSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademyFinanceSettings_AcademyId",
                table: "AcademyFinanceSettings",
                column: "AcademyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademyFinanceSettings");
        }
    }
}
