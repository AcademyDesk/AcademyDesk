using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateBrandingAndTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemplateKey",
                table: "Certificates",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "Certificates",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CertificateAccentColor",
                table: "Academies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateLogoUrl",
                table: "Academies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateSignatoryName",
                table: "Academies",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemplateKey",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "CertificateAccentColor",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "CertificateLogoUrl",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "CertificateSignatoryName",
                table: "Academies");
        }
    }
}
