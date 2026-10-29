using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingAccountReviewConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_MeetingOAuthExchanges_AcademyId_Provider_ActorUserId_Id",
                table: "MeetingOAuthExchanges",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" });

            migrationBuilder.CreateTable(
                name: "MeetingAccountReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    BindingVersion = table.Column<int>(type: "int", nullable: false),
                    RegistrationFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReviewHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 32768, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingAccountReviews", x => x.Id);
                    table.CheckConstraint("CK_MeetingAccountReview_State", "([Status] = 'AwaitingConfirmation' AND [Version] = 0 AND [ConfirmedAtUtc] IS NULL) OR ([Status] = 'ConfirmedNotConnected' AND [Version] = 1 AND [ConfirmedAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_MeetingAccountReview_Versions", "[Generation] > 0 AND [BindingVersion] > 0");
                    table.ForeignKey(
                        name: "FK_MeetingAccountReviews_MeetingOAuthExchanges_AcademyId_Provider_ActorUserId_ExchangeId",
                        columns: x => new { x.AcademyId, x.Provider, x.ActorUserId, x.ExchangeId },
                        principalTable: "MeetingOAuthExchanges",
                        principalColumns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingAccountReviews_MeetingProviderBindings_AcademyId_Provider_BindingId",
                        columns: x => new { x.AcademyId, x.Provider, x.BindingId },
                        principalTable: "MeetingProviderBindings",
                        principalColumns: new[] { "AcademyId", "Provider", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAccountReviews_AcademyId_Provider_ActorUserId_ExchangeId",
                table: "MeetingAccountReviews",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "ExchangeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAccountReviews_AcademyId_Provider_BindingId",
                table: "MeetingAccountReviews",
                columns: new[] { "AcademyId", "Provider", "BindingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAccountReviews_ExchangeId",
                table: "MeetingAccountReviews",
                column: "ExchangeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingAccountReviews]) THROW 51000, 'Account review downgrade blocked: retain confirmation evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingAccountReviews");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MeetingOAuthExchanges_AcademyId_Provider_ActorUserId_Id",
                table: "MeetingOAuthExchanges");
        }
    }
}
