using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class AddPentaMiniPrivateSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PentaMiniSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    PendingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProtectedState = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaMiniSessions", x => x.Id);
                    table.UniqueConstraint("AK_PentaMiniSessions_AcademyId_ActorUserId_Id", x => new { x.AcademyId, x.ActorUserId, x.Id });
                    table.ForeignKey(
                        name: "FK_PentaMiniSessions_Academies_AcademyId",
                        column: x => x.AcademyId,
                        principalTable: "Academies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PentaMiniTurns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedVersion = table.Column<long>(type: "bigint", nullable: false),
                    InputDigest = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProtectedReceipt = table.Column<string>(type: "nvarchar(max)", maxLength: 60000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaMiniTurns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaMiniTurns_PentaMiniSessions_AcademyId_ActorUserId_SessionId",
                        columns: x => new { x.AcademyId, x.ActorUserId, x.SessionId },
                        principalTable: "PentaMiniSessions",
                        principalColumns: new[] { "AcademyId", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PentaMiniSessions_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaMiniSessions",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaMiniTurns_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaMiniTurns",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaMiniTurns_AcademyId_ActorUserId_SessionId_RequestId",
                table: "PentaMiniTurns",
                columns: new[] { "AcademyId", "ActorUserId", "SessionId", "RequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PentaMiniTurns");

            migrationBuilder.DropTable(
                name: "PentaMiniSessions");
        }
    }
}
