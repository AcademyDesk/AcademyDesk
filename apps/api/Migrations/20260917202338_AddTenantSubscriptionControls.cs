using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSubscriptionControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnabledModulesJson",
                table: "Academies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StaffLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StudentLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndsAtUtc",
                table: "Academies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionPlan",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledModulesJson",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "StaffLimit",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "StudentLimit",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndsAtUtc",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlan",
                table: "Academies");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Academies");
        }
    }
}
