using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingOAuthExchangeQuarantine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingOAuthExchanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    BindingVersion = table.Column<int>(type: "int", nullable: false),
                    Fence = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 262144, nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingOAuthExchanges", x => x.Id);
                    table.CheckConstraint("CK_MeetingOAuthExchange_State", "([Status] = 'Started' AND [Version] = 0) OR ([Status] IN ('TokensQuarantined','OutcomeUnknown') AND [Version] = 1)");
                    table.CheckConstraint("CK_MeetingOAuthExchange_Versions", "[Generation] > 0 AND [BindingVersion] > 0");
                    table.ForeignKey(
                        name: "FK_MeetingOAuthExchanges_MeetingOAuthAttempts_AcademyId_Provider_ActorUserId_AttemptId",
                        columns: x => new { x.AcademyId, x.Provider, x.ActorUserId, x.AttemptId },
                        principalTable: "MeetingOAuthAttempts",
                        principalColumns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingOAuthExchanges_MeetingProviderBindings_AcademyId_Provider_BindingId",
                        columns: x => new { x.AcademyId, x.Provider, x.BindingId },
                        principalTable: "MeetingProviderBindings",
                        principalColumns: new[] { "AcademyId", "Provider", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthExchanges_AcademyId_Provider_ActorUserId_AttemptId",
                table: "MeetingOAuthExchanges",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "AttemptId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthExchanges_AcademyId_Provider_BindingId",
                table: "MeetingOAuthExchanges",
                columns: new[] { "AcademyId", "Provider", "BindingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthExchanges_AttemptId",
                table: "MeetingOAuthExchanges",
                column: "AttemptId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingOAuthExchanges]) THROW 51000, 'Meeting exchange downgrade blocked: retain encrypted credential and exchange evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingOAuthExchanges");
        }
    }
}
