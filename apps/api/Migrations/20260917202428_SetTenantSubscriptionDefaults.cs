using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class SetTenantSubscriptionDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionStatus",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Trial",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionPlan",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Trial",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "StudentLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "StaffLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EnabledModulesJson",
                table: "Academies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "[\"Core\"]",
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.Sql("UPDATE [Academies] SET [SubscriptionPlan] = 'Trial' WHERE [SubscriptionPlan] = ''; UPDATE [Academies] SET [SubscriptionStatus] = 'Trial' WHERE [SubscriptionStatus] = ''; UPDATE [Academies] SET [StudentLimit] = 100 WHERE [StudentLimit] = 0; UPDATE [Academies] SET [StaffLimit] = 10 WHERE [StaffLimit] = 0; UPDATE [Academies] SET [EnabledModulesJson] = '[\"Core\"]' WHERE [EnabledModulesJson] = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionStatus",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Trial");

            migrationBuilder.AlterColumn<string>(
                name: "SubscriptionPlan",
                table: "Academies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Trial");

            migrationBuilder.AlterColumn<int>(
                name: "StudentLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<int>(
                name: "StaffLimit",
                table: "Academies",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 10);

            migrationBuilder.AlterColumn<string>(
                name: "EnabledModulesJson",
                table: "Academies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldDefaultValue: "[\"Core\"]");
        }
    }
}
