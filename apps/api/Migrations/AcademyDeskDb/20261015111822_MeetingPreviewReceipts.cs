using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingPreviewReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingPreviewReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 32768, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingPreviewReceipts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingPreviewReceipts_AcademyId_ActorUserId_ExpiresAtUtc",
                table: "MeetingPreviewReceipts",
                columns: new[] { "AcademyId", "ActorUserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingPreviewReceipts_AcademyId_ActorUserId_IdempotencyKey",
                table: "MeetingPreviewReceipts",
                columns: new[] { "AcademyId", "ActorUserId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingPreviewReceipts]) THROW 51000, 'Meeting receipt downgrade blocked: preserve reviewed draft evidence with an approved compatible restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingPreviewReceipts");
        }
    }
}
