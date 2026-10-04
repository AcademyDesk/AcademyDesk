using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.AcademyDeskDb
{
    /// <inheritdoc />
    public partial class AddPentaPricedBudgetSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InputTokensObserved",
                table: "PentaUsageReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputTokensReserved",
                table: "PentaUsageReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InputUsdPerMillion",
                table: "PentaUsageReservations",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "PentaUsageReservations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokensObserved",
                table: "PentaUsageReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokensReserved",
                table: "PentaUsageReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutputUsdPerMillion",
                table: "PentaUsageReservations",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderName",
                table: "PentaUsageReservations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputTokensObserved",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "InputTokensReserved",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "InputUsdPerMillion",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "OutputTokensObserved",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "OutputTokensReserved",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "OutputUsdPerMillion",
                table: "PentaUsageReservations");

            migrationBuilder.DropColumn(
                name: "ProviderName",
                table: "PentaUsageReservations");
        }
    }
}
