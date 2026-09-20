using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherClassMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClassSessionId",
                table: "LearningResources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "LearningResources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningResources_AcademyId_ClassSessionId_StudentId",
                table: "LearningResources",
                columns: new[] { "AcademyId", "ClassSessionId", "StudentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LearningResources_AcademyId_ClassSessionId_StudentId",
                table: "LearningResources");

            migrationBuilder.DropColumn(
                name: "ClassSessionId",
                table: "LearningResources");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "LearningResources");
        }
    }
}
