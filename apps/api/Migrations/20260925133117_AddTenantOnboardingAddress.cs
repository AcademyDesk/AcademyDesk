using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOnboardingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "TenantOnboardingProfiles",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "TenantOnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "TenantOnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "TenantOnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "TenantOnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "TenantOnboardingProfiles");

            migrationBuilder.DropColumn(
                name: "State",
                table: "TenantOnboardingProfiles");
        }
    }
}
