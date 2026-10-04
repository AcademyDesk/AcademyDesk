using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPentaApprovalAndSyntheticUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalMode",
                table: "PentaExecutions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

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
                name: "PentaUsageEntries");

            migrationBuilder.DropTable(
                name: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "ApprovalMode",
                table: "PentaExecutions");
        }
    }
}
