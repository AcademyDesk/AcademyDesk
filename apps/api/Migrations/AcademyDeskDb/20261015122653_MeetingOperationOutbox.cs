using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingOperationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 32768, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingOperations", x => x.Id);
                    table.UniqueConstraint("AK_MeetingOperations_AcademyId_Id", x => new { x.AcademyId, x.Id });
                    table.CheckConstraint("CK_MeetingOperation_Status", "[Status] IN ('Held','PendingDispatch','Leased','Dispatching','AwaitingReview')");
                    table.CheckConstraint("CK_MeetingOperation_Version", "[Version] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "MeetingOutbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingOutbox", x => x.Id);
                    table.CheckConstraint("CK_MeetingOutbox_Lease", "([LeaseId] IS NULL AND [LeaseExpiresAtUtc] IS NULL) OR ([LeaseId] IS NOT NULL AND [LeaseExpiresAtUtc] IS NOT NULL)");
                    table.CheckConstraint("CK_MeetingOutbox_Version", "[Version] >= 0");
                    table.ForeignKey(
                        name: "FK_MeetingOutbox_MeetingOperations_AcademyId_OperationId",
                        columns: x => new { x.AcademyId, x.OperationId },
                        principalTable: "MeetingOperations",
                        principalColumns: new[] { "AcademyId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOperations_AcademyId_Kind_IdempotencyKey",
                table: "MeetingOperations",
                columns: new[] { "AcademyId", "Kind", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOperations_AcademyId_ReceiptId",
                table: "MeetingOperations",
                columns: new[] { "AcademyId", "ReceiptId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingOutbox_AcademyId_OperationId",
                table: "MeetingOutbox",
                columns: new[] { "AcademyId", "OperationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingOperations]) OR EXISTS (SELECT 1 FROM [MeetingOutbox]) THROW 51000, 'Meeting operation downgrade blocked: retain operation and recovery evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingOutbox");

            migrationBuilder.DropTable(
                name: "MeetingOperations");
        }
    }
}
