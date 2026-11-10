using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class TrackClassSessionTeacherInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FollowsBatchTeacher",
                table: "ClassSessions",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [ClassSessions] WITH (UPDLOCK,HOLDLOCK) WHERE [FollowsBatchTeacher] IS NOT NULL)
                    THROW 51029, 'Class teacher inheritance has been recorded and must be preserved.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "FollowsBatchTeacher",
                table: "ClassSessions");
        }
    }
}
