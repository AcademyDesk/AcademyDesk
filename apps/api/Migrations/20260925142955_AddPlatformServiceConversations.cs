using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformServiceConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcademyRespondedAtUtc",
                table: "PlatformSupportCases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademyResponse",
                table: "PlatformSupportCases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "PlatformBillingInvoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentSubmittedAtUtc",
                table: "PlatformBillingInvoices",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademyRespondedAtUtc",
                table: "PlatformSupportCases");

            migrationBuilder.DropColumn(
                name: "AcademyResponse",
                table: "PlatformSupportCases");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "PlatformBillingInvoices");

            migrationBuilder.DropColumn(
                name: "PaymentSubmittedAtUtc",
                table: "PlatformBillingInvoices");
        }
    }
}
