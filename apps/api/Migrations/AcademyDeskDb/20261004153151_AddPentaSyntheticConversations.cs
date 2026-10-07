using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class AddPentaSyntheticConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PentaConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextVersion = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaConversations", x => x.Id);
                    table.UniqueConstraint("AK_PentaConversations_AcademyId_ActorUserId_Id", x => new { x.AcademyId, x.ActorUserId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "PentaConversationTurns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedContextVersion = table.Column<long>(type: "bigint", nullable: false),
                    CompletedContextVersion = table.Column<long>(type: "bigint", nullable: false),
                    Capability = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InputDigest = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaConversationTurns", x => x.Id);
                    table.UniqueConstraint("AK_PentaConversationTurns_AcademyId_ActorUserId_ConversationId_Id", x => new { x.AcademyId, x.ActorUserId, x.ConversationId, x.Id });
                    table.ForeignKey(
                        name: "FK_PentaConversationTurns_PentaConversations_AcademyId_ActorUserId_ConversationId",
                        columns: x => new { x.AcademyId, x.ActorUserId, x.ConversationId },
                        principalTable: "PentaConversations",
                        principalColumns: new[] { "AcademyId", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PentaConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TurnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaConversationMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaConversationMessages_PentaConversationTurns_AcademyId_ActorUserId_ConversationId_TurnId",
                        columns: x => new { x.AcademyId, x.ActorUserId, x.ConversationId, x.TurnId },
                        principalTable: "PentaConversationTurns",
                        principalColumns: new[] { "AcademyId", "ActorUserId", "ConversationId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversationMessages_AcademyId_ActorUserId_ConversationId_Sequence",
                table: "PentaConversationMessages",
                columns: new[] { "AcademyId", "ActorUserId", "ConversationId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversationMessages_AcademyId_ActorUserId_ConversationId_TurnId",
                table: "PentaConversationMessages",
                columns: new[] { "AcademyId", "ActorUserId", "ConversationId", "TurnId" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversations_AcademyId_ActorUserId_ExpiresAtUtc",
                table: "PentaConversations",
                columns: new[] { "AcademyId", "ActorUserId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversations_AcademyId_ActorUserId_RequestId",
                table: "PentaConversations",
                columns: new[] { "AcademyId", "ActorUserId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversationTurns_AcademyId_ActorUserId_ConversationId_CompletedContextVersion",
                table: "PentaConversationTurns",
                columns: new[] { "AcademyId", "ActorUserId", "ConversationId", "CompletedContextVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaConversationTurns_AcademyId_ActorUserId_ConversationId_RequestId",
                table: "PentaConversationTurns",
                columns: new[] { "AcademyId", "ActorUserId", "ConversationId", "RequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PentaConversationMessages");

            migrationBuilder.DropTable(
                name: "PentaConversationTurns");

            migrationBuilder.DropTable(
                name: "PentaConversations");
        }
    }
}
