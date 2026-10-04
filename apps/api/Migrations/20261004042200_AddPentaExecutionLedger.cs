using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPentaExecutionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    ResultCorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_PentaAttempts_AcademyId_ExecutionId",
                table: "PentaAttempts",
                columns: new[] { "AcademyId", "ExecutionId" },
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
                name: "IX_PentaTasks_AcademyId_ActorUserId_CreatedAtUtc",
                table: "PentaTasks",
                columns: new[] { "AcademyId", "ActorUserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PentaAttempts");

            migrationBuilder.DropTable(
                name: "PentaExecutions");

            migrationBuilder.DropTable(
                name: "PentaTasks");
        }
    }
}
