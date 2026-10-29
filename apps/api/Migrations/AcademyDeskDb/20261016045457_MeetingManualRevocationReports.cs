using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingManualRevocationReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations",
                sql: "([Status] IN ('Started','ManualRequired') AND [Version] = 0) OR ([Status] IN ('ProviderAcknowledged','OutcomeUnknown') AND [Version] = 1) OR ([Status] = 'ManualReportedUnverified' AND (([Provider] IN ('Microsoft365','Zoom') AND [Version] = 1) OR ([Provider] = 'GoogleWorkspace' AND [Version] = 2)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingCredentialRevocations] WHERE [Status] = 'ManualReportedUnverified') THROW 51000, 'Manual revocation report evidence cannot be downgraded; retain the current schema and use an approved reconciliation procedure.', 1;");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations",
                sql: "([Status] IN ('Started','ManualRequired') AND [Version] = 0) OR ([Status] IN ('ProviderAcknowledged','OutcomeUnknown') AND [Version] = 1)");
        }
    }
}
