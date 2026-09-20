using System;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    [DbContext(typeof(AcademyDeskDbContext))]
    [Migration("20260920050000_AddAssignmentStudentTarget")]
    public partial class AddAssignmentStudentTarget : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(name: "StudentId", table: "Assignments", type: "uniqueidentifier", nullable: true);
            migrationBuilder.CreateIndex(name: "IX_Assignments_AcademyId_BatchId_StudentId_CreatedAtUtc", table: "Assignments", columns: new[] { "AcademyId", "BatchId", "StudentId", "CreatedAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Assignments_AcademyId_BatchId_StudentId_CreatedAtUtc", table: "Assignments");
            migrationBuilder.DropColumn(name: "StudentId", table: "Assignments");
        }
    }
}
