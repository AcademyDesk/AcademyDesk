using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMinorParentPortalAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE link
                SET CanAccessPortal = 1,
                    CanViewAcademicProgress = 1,
                    CanViewFinance = 1,
                    CanViewDocuments = 1,
                    CanManageLeave = 1,
                    AccessGrantedAtUtc = COALESCE(AccessGrantedAtUtc, SYSUTCDATETIME()),
                    AccessRevokedAtUtc = NULL
                FROM StudentGuardians AS link
                INNER JOIN Students AS student ON student.Id = link.StudentId
                WHERE student.DateOfBirth IS NOT NULL
                  AND student.DateOfBirth > DATEADD(year, -18, CONVERT(date, SYSUTCDATETIME()));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
