using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingProviderOAuthIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingProviderBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingProviderBindings", x => x.Id);
                    table.UniqueConstraint("AK_MeetingProviderBindings_AcademyId_Provider_Id", x => new { x.AcademyId, x.Provider, x.Id });
                    table.CheckConstraint("CK_MeetingProviderBinding_Provider", "[Provider] IN ('GoogleWorkspace','Microsoft365','Zoom')");
                    table.CheckConstraint("CK_MeetingProviderBinding_Status", "[Status] IN ('NotConnected','AuthorizationPending','VerificationRequired','Disconnected')");
                    table.CheckConstraint("CK_MeetingProviderBinding_Versions", "[Version] >= 0 AND [Generation] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "MeetingOAuthAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    StateHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingOAuthAttempts", x => x.Id);
                    table.CheckConstraint("CK_MeetingOAuthAttempt_Generation", "[Generation] > 0");
                    table.CheckConstraint("CK_MeetingOAuthAttempt_State", "([Status] = 'Pending' AND [Version] = 0 AND [ClaimedAtUtc] IS NULL) OR ([Status] = 'Claimed' AND [Version] = 1 AND [ClaimedAtUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MeetingOAuthAttempts_MeetingProviderBindings_AcademyId_Provider_BindingId",
                        columns: x => new { x.AcademyId, x.Provider, x.BindingId },
                        principalTable: "MeetingProviderBindings",
                        principalColumns: new[] { "AcademyId", "Provider", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthAttempts_AcademyId_ActorUserId_ExpiresAtUtc",
                table: "MeetingOAuthAttempts",
                columns: new[] { "AcademyId", "ActorUserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthAttempts_AcademyId_Provider_BindingId",
                table: "MeetingOAuthAttempts",
                columns: new[] { "AcademyId", "Provider", "BindingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOAuthAttempts_StateHash",
                table: "MeetingOAuthAttempts",
                column: "StateHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingProviderBindings_AcademyId_Provider",
                table: "MeetingProviderBindings",
                columns: new[] { "AcademyId", "Provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingOAuthAttempts]) OR EXISTS (SELECT 1 FROM [MeetingProviderBindings]) THROW 51000, 'Meeting connection downgrade blocked: retain authorization evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingOAuthAttempts");

            migrationBuilder.DropTable(
                name: "MeetingProviderBindings");
        }
    }
}
