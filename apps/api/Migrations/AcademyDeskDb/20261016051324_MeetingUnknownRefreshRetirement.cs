using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingUnknownRefreshRetirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges",
                sql: "([Status] = 'Started' AND [Version] = 0) OR ([Status] = 'OutcomeUnknown' AND [Version] = 1) OR ([Status] = 'TokensQuarantined' AND [Version] >= 1 AND [Version] % 2 = 1) OR ([Status] = 'RefreshStarted' AND [Version] >= 2 AND [Version] % 2 = 0) OR ([Status] = 'RefreshUnknown' AND [Version] >= 3 AND [Version] % 2 = 1) OR ([Status] = 'RefreshAbandoned' AND [Version] >= 4 AND [Version] % 2 = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations",
                sql: "([Status] IN ('Started','ManualRequired') AND [Version] = 0) OR ([Status] IN ('ProviderAcknowledged','OutcomeUnknown') AND [Version] = 1) OR ([Status] = 'ManualReportedUnverified' AND (([Provider] IN ('Microsoft365','Zoom') AND [Version] = 1) OR ([Provider] = 'GoogleWorkspace' AND [Version] IN (1,2))))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [MeetingOAuthExchanges] WHERE [Status] = 'RefreshAbandoned')
                   OR EXISTS (SELECT 1 FROM [MeetingCredentialRevocations] WHERE [Provider] = 'GoogleWorkspace' AND [Status] = 'ManualReportedUnverified' AND [Version] = 1)
                    THROW 51000, 'Unknown refresh retirement evidence cannot be downgraded; retain the schema and use an approved reconciliation procedure.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges",
                sql: "([Status] = 'Started' AND [Version] = 0) OR ([Status] = 'OutcomeUnknown' AND [Version] = 1) OR ([Status] = 'TokensQuarantined' AND [Version] >= 1 AND [Version] % 2 = 1) OR ([Status] = 'RefreshStarted' AND [Version] >= 2 AND [Version] % 2 = 0) OR ([Status] = 'RefreshUnknown' AND [Version] >= 3 AND [Version] % 2 = 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingCredentialRevocation_State",
                table: "MeetingCredentialRevocations",
                sql: "([Status] IN ('Started','ManualRequired') AND [Version] = 0) OR ([Status] IN ('ProviderAcknowledged','OutcomeUnknown') AND [Version] = 1) OR ([Status] = 'ManualReportedUnverified' AND (([Provider] IN ('Microsoft365','Zoom') AND [Version] = 1) OR ([Provider] = 'GoogleWorkspace' AND [Version] = 2)))");
        }
    }
}
