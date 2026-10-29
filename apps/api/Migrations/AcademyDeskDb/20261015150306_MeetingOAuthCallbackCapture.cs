using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingOAuthCallbackCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthAttempt_State",
                table: "MeetingOAuthAttempts");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_MeetingOAuthAttempts_AcademyId_Provider_ActorUserId_Id",
                table: "MeetingOAuthAttempts",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" });

            migrationBuilder.CreateTable(
                name: "MeetingOAuthCallbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingOAuthCallbacks", x => x.Id);
                    table.CheckConstraint("CK_MeetingOAuthCallback_State", "([Status] = 'Captured' AND [Version] = 0) OR ([Status] = 'Consumed' AND [Version] = 1)");
                    table.ForeignKey(
                        name: "FK_MeetingOAuthCallbacks_MeetingOAuthAttempts_AcademyId_Provider_ActorUserId_AttemptId",
                        columns: x => new { x.AcademyId, x.Provider, x.ActorUserId, x.AttemptId },
                        principalTable: "MeetingOAuthAttempts",
                        principalColumns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthAttempt_State",
                table: "MeetingOAuthAttempts",
                sql: "([Status] = 'Pending' AND [Version] = 0 AND [ClaimedAtUtc] IS NULL) OR ([Status] = 'CallbackCaptured' AND [Version] = 1 AND [ClaimedAtUtc] IS NULL) OR ([Status] = 'Claimed' AND [Version] IN (1,2) AND [ClaimedAtUtc] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthCallbacks_AcademyId_Provider_ActorUserId_AttemptId",
                table: "MeetingOAuthCallbacks",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "AttemptId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthCallbacks_AttemptId",
                table: "MeetingOAuthCallbacks",
                column: "AttemptId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingOAuthCallbacks]) OR EXISTS (SELECT 1 FROM [MeetingOAuthAttempts] WHERE [Status] = 'CallbackCaptured' OR [Version] = 2) THROW 51000, 'Meeting callback downgrade blocked: retain callback authorization evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingOAuthCallbacks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_MeetingOAuthAttempts_AcademyId_Provider_ActorUserId_Id",
                table: "MeetingOAuthAttempts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MeetingOAuthAttempt_State",
                table: "MeetingOAuthAttempts");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MeetingOAuthAttempt_State",
                table: "MeetingOAuthAttempts",
                sql: "([Status] = 'Pending' AND [Version] = 0 AND [ClaimedAtUtc] IS NULL) OR ([Status] = 'Claimed' AND [Version] = 1 AND [ClaimedAtUtc] IS NOT NULL)");
        }
    }
}
