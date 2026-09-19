using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCertificateTemplateMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Certificates SET TemplateKey = 'classic' WHERE TemplateKey IS NULL OR TemplateKey = '';");
            migrationBuilder.Sql("UPDATE Certificates SET VerificationCode = UPPER(LEFT(REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), 16)) WHERE VerificationCode IS NULL OR VerificationCode = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
