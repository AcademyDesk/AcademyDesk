using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class AddStaffAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AcademyId_TeacherId",
                table: "AspNetUsers",
                columns: new[] { "AcademyId", "TeacherId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AcademyId_TeacherId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "AspNetUsers");
        }
    }
}
