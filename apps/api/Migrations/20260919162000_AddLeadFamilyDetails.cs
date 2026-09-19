using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadFamilyDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "Leads",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentName",
                table: "Leads",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ParentName",
                table: "Leads");
        }
    }
}
