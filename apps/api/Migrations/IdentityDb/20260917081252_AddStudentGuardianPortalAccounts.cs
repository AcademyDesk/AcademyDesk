using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations.IdentityDb
{
    /// <inheritdoc />
    public partial class AddStudentGuardianPortalAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GuardianId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AcademyId_GuardianId",
                table: "AspNetUsers",
                columns: new[] { "AcademyId", "GuardianId" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_AcademyId_StudentId",
                table: "AspNetUsers",
                columns: new[] { "AcademyId", "StudentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AcademyId_GuardianId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_AcademyId_StudentId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GuardianId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "AspNetUsers");
        }
    }
}
