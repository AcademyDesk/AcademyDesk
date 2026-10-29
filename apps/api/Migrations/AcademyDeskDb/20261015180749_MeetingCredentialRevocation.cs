using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class MeetingCredentialRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingCredentialRevocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExchangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    BindingVersion = table.Column<int>(type: "int", nullable: false),
                    Fence = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProtectedEnvelope = table.Column<string>(type: "nvarchar(max)", maxLength: 32768, nullable: false),
                    DeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcademyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingCredentialRevocations", x => x.Id);
                    table.CheckConstraint("CK_MeetingCredentialRevocation_State", "([Status] IN ('Started','ManualRequired') AND [Version] = 0) OR ([Status] IN ('ProviderAcknowledged','OutcomeUnknown') AND [Version] = 1)");
                    table.CheckConstraint("CK_MeetingCredentialRevocation_Versions", "[Generation] > 0 AND [BindingVersion] > 0");
                    table.ForeignKey(
                        name: "FK_MeetingCredentialRevocations_MeetingOAuthExchanges_AcademyId_Provider_ActorUserId_ExchangeId",
                        columns: x => new { x.AcademyId, x.Provider, x.ActorUserId, x.ExchangeId },
                        principalTable: "MeetingOAuthExchanges",
                        principalColumns: new[] { "AcademyId", "Provider", "ActorUserId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MeetingCredentialRevocations_MeetingProviderBindings_AcademyId_Provider_BindingId",
                        columns: x => new { x.AcademyId, x.Provider, x.BindingId },
                        principalTable: "MeetingProviderBindings",
                        principalColumns: new[] { "AcademyId", "Provider", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCredentialRevocations_AcademyId_Provider_ActorUserId_ExchangeId",
                table: "MeetingCredentialRevocations",
                columns: new[] { "AcademyId", "Provider", "ActorUserId", "ExchangeId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCredentialRevocations_AcademyId_Provider_BindingId",
                table: "MeetingCredentialRevocations",
                columns: new[] { "AcademyId", "Provider", "BindingId" });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingCredentialRevocations_ExchangeId",
                table: "MeetingCredentialRevocations",
                column: "ExchangeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [MeetingCredentialRevocations]) THROW 51000, 'Revocation downgrade blocked: retain disconnect and uncertain-outcome evidence with an approved restore plan.', 1;");
            migrationBuilder.DropTable(
                name: "MeetingCredentialRevocations");
        }
    }
}
