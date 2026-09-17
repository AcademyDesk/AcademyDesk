using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class AddAcademyCustomRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcademyId",
                table: "AspNetRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PermissionsJson",
                table: "AspNetRoles",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_AcademyId_Name",
                table: "AspNetRoles",
                columns: new[] { "AcademyId", "Name" },
                unique: true,
                filter: "[AcademyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_AcademyId_Name",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "AcademyId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "PermissionsJson",
                table: "AspNetRoles");
        }
    }
}
