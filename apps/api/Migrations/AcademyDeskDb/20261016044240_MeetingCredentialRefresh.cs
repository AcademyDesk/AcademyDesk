using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingCredentialRefresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges");

            migrationBuilder.DropIndex(
                name: "IX_MeetingAccountReviews_ExchangeId",
                table: "MeetingAccountReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges",
                sql: "([Status] = 'Started' AND [Version] = 0) OR ([Status] = 'OutcomeUnknown' AND [Version] = 1) OR ([Status] = 'TokensQuarantined' AND [Version] >= 1 AND [Version] % 2 = 1) OR ([Status] = 'RefreshStarted' AND [Version] >= 2 AND [Version] % 2 = 0) OR ([Status] = 'RefreshUnknown' AND [Version] >= 3 AND [Version] % 2 = 1)");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAccountReviews_ExchangeId_BindingVersion",
                table: "MeetingAccountReviews",
                columns: new[] { "ExchangeId", "BindingVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [MeetingOAuthExchanges] WHERE [Version] >= 2)
                   OR EXISTS (SELECT [ExchangeId] FROM [MeetingAccountReviews] GROUP BY [ExchangeId] HAVING COUNT(*) > 1)
                    THROW 51000, 'Meeting refresh evidence cannot be downgraded. Retain the current schema and reconcile through an approved procedure.', 1;
                """);

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges");

            migrationBuilder.DropIndex(
                name: "IX_MeetingAccountReviews_ExchangeId_BindingVersion",
                table: "MeetingAccountReviews");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthExchange_State",
                table: "MeetingOAuthExchanges",
                sql: "([Status] = 'Started' AND [Version] = 0) OR ([Status] IN ('TokensQuarantined','OutcomeUnknown') AND [Version] = 1)");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAccountReviews_ExchangeId",
                table: "MeetingAccountReviews",
                column: "ExchangeId",
                unique: true);
        }
    }
}
