using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class AddPentaOnboardingDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PentaOnboardingDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextVersion = table.Column<long>(type: "bigint", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProtectedBody = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaOnboardingDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaOnboardingDrafts_PentaMiniSessions_AcademyId_ActorUserId_SessionId",
                        columns: x => new { x.AcademyId, x.ActorUserId, x.SessionId },
                        principalTable: "PentaMiniSessions",
                        principalColumns: new[] { "AcademyId", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PentaOnboardingDrafts_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaOnboardingDrafts",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaOnboardingDrafts_AcademyId_ActorUserId_SessionId",
                table: "PentaOnboardingDrafts",
                columns: new[] { "AcademyId", "ActorUserId", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PentaOnboardingDrafts");
        }
    }
}
