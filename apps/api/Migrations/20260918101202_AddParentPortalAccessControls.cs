using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddParentPortalAccessControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AccessGrantedAtUtc",
                table: "StudentGuardians",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessRevokedAtUtc",
                table: "StudentGuardians",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAccessPortal",
                table: "StudentGuardians",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageLeave",
                table: "StudentGuardians",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewAcademicProgress",
                table: "StudentGuardians",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewDocuments",
                table: "StudentGuardians",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewFinance",
                table: "StudentGuardians",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessGrantedAtUtc",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "AccessRevokedAtUtc",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "CanAccessPortal",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "CanManageLeave",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "CanViewAcademicProgress",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "CanViewDocuments",
                table: "StudentGuardians");

            migrationBuilder.DropColumn(
                name: "CanViewFinance",
                table: "StudentGuardians");
        }
    }
}
