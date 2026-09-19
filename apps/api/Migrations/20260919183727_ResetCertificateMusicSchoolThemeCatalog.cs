using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class ResetCertificateMusicSchoolThemeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Certificates SET TemplateKey = 'music-recital' WHERE TemplateKey NOT IN ('music-recital','music-conservatory','music-rhythm','music-spotlight','music-symphony','music-acoustic','music-vocal','music-orchestra','music-virtuoso','music-practice','school-honours','school-crest','school-scholar','school-merit','school-graduation');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
