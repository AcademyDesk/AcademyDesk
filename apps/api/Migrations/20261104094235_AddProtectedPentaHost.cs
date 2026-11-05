using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProtectedPentaHost : Migration
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
                name: "PentaMiniSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    PendingRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProtectedState = table.Column<string>(type: "nvarchar(max)", maxLength: 12000, nullable: false),
                    ProtectedOnboardingState = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                name: "PentaTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Capability = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaTasks", x => x.Id);
                    table.UniqueConstraint("AK_PentaTasks_AcademyId_Id", x => new { x.AcademyId, x.Id });
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

            migrationBuilder.CreateTable(
                name: "PentaExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    InputDigest = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovalMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "None"),
                    ResultCorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ProposalPayloadJson = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaExecutions", x => x.Id);
                    table.UniqueConstraint("AK_PentaExecutions_AcademyId_Id", x => new { x.AcademyId, x.Id });
                    table.ForeignKey(
                        name: "FK_PentaExecutions_PentaTasks_AcademyId_TaskId",
                        columns: x => new { x.AcademyId, x.TaskId },
                        principalTable: "PentaTasks",
                        principalColumns: new[] { "AcademyId", "Id" },
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

            migrationBuilder.CreateTable(
                name: "PentaApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApproverUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DecisionDigest = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PolicyVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaApprovals_PentaExecutions_AcademyId_ExecutionId",
                        columns: x => new { x.AcademyId, x.ExecutionId },
                        principalTable: "PentaExecutions",
                        principalColumns: new[] { "AcademyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PentaAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaAttempts_PentaExecutions_AcademyId_ExecutionId",
                        columns: x => new { x.AcademyId, x.ExecutionId },
                        principalTable: "PentaExecutions",
                        principalColumns: new[] { "AcademyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PentaUsageReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnitsReserved = table.Column<int>(type: "int", nullable: false),
                    UnitKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PriceVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    InputTokensReserved = table.Column<int>(type: "int", nullable: true),
                    OutputTokensReserved = table.Column<int>(type: "int", nullable: true),
                    InputTokensObserved = table.Column<int>(type: "int", nullable: true),
                    OutputTokensObserved = table.Column<int>(type: "int", nullable: true),
                    InputUsdPerMillion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    OutputUsdPerMillion = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaUsageReservations", x => x.Id);
                    table.UniqueConstraint("AK_PentaUsageReservations_AcademyId_Id", x => new { x.AcademyId, x.Id });
                    table.ForeignKey(
                        name: "FK_PentaUsageReservations_PentaExecutions_AcademyId_ExecutionId",
                        columns: x => new { x.AcademyId, x.ExecutionId },
                        principalTable: "PentaExecutions",
                        principalColumns: new[] { "AcademyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PentaUsageEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitsObserved = table.Column<int>(type: "int", nullable: false),
                    ActualCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PentaUsageEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PentaUsageEntries_PentaUsageReservations_AcademyId_ReservationId",
                        columns: x => new { x.AcademyId, x.ReservationId },
                        principalTable: "PentaUsageReservations",
                        principalColumns: new[] { "AcademyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PentaApprovals_AcademyId_ExecutionId",
                table: "PentaApprovals",
                columns: new[] { "AcademyId", "ExecutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaAttempts_AcademyId_ExecutionId",
                table: "PentaAttempts",
                columns: new[] { "AcademyId", "ExecutionId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PentaExecutions_AcademyId_ActorUserId_ToolName_IdempotencyKey",
                table: "PentaExecutions",
                columns: new[] { "AcademyId", "ActorUserId", "ToolName", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaExecutions_AcademyId_TaskId",
                table: "PentaExecutions",
                columns: new[] { "AcademyId", "TaskId" });

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

            migrationBuilder.CreateIndex(
                name: "IX_PentaOnboardingDrafts_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaOnboardingDrafts",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaOnboardingDrafts_AcademyId_ActorUserId_SessionId",
                table: "PentaOnboardingDrafts",
                columns: new[] { "AcademyId", "ActorUserId", "SessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaTasks_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaTasks",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PentaUsageEntries_AcademyId_ReservationId_EventKey",
                table: "PentaUsageEntries",
                columns: new[] { "AcademyId", "ReservationId", "EventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaUsageReservations_AcademyId_ExecutionId",
                table: "PentaUsageReservations",
                columns: new[] { "AcademyId", "ExecutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PentaUsageReservations_AcademyId_WindowStartUtc_ActorUserId",
                table: "PentaUsageReservations",
                columns: new[] { "AcademyId", "WindowStartUtc", "ActorUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PentaApprovals");

            migrationBuilder.DropTable(
                name: "PentaAttempts");

            migrationBuilder.DropTable(
                name: "PentaConversationMessages");

            migrationBuilder.DropTable(
                name: "PentaMiniTurns");

            migrationBuilder.DropTable(
                name: "PentaOnboardingDrafts");

            migrationBuilder.DropTable(
                name: "PentaUsageEntries");

            migrationBuilder.DropTable(
                name: "PentaConversationTurns");

            migrationBuilder.DropTable(
                name: "PentaMiniSessions");

            migrationBuilder.DropTable(
                name: "PentaUsageReservations");

            migrationBuilder.DropTable(
                name: "PentaConversations");

            migrationBuilder.DropTable(
                name: "PentaExecutions");

            migrationBuilder.DropTable(
                name: "PentaTasks");
        }
    }
}
