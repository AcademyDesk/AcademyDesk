using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOnboardingProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantOnboardingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentSection = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BusinessType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    OperatingSince = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BranchSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinanceModel = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    BillingFrequency = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PaymentCollectionMethods = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeacherPaymentModels = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TeacherCount = table.Column<int>(type: "int", nullable: true),
                    StudentCount = table.Column<int>(type: "int", nullable: true),
                    SubjectCount = table.Column<int>(type: "int", nullable: true),
                    SubjectTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryModes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassRatios = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BatchAndClassSetup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperationalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantOnboardingProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantOnboardingProfiles_AcademyId",
                table: "TenantOnboardingProfiles",
                column: "AcademyId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantOnboardingProfiles");
        }
    }
}
