using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    [DbContext(typeof(AcademyDeskDbContext))]
    [Migration("20260919130000_StandardizeAcademyTimeZone")]
    public partial class StandardizeAcademyTimeZone : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Academies] SET [TimeZone] = N'Asia/Kolkata' WHERE [TimeZone] IS NULL OR [TimeZone] <> N'Asia/Kolkata';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Existing time zones cannot be recovered safely after standardisation.
        }
    }
}
