using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    [DbContext(typeof(AcademyDeskDbContext))]
    [Migration("20260919150000_AddMakeupDeliveryDetails")]
    public partial class AddMakeupDeliveryDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('MakeupClasses', 'DeliveryMode') IS NULL ALTER TABLE [MakeupClasses] ADD [DeliveryMode] nvarchar(30) NOT NULL DEFAULT N'Offline';");
            migrationBuilder.Sql("IF COL_LENGTH('MakeupClasses', 'MeetingLink') IS NULL ALTER TABLE [MakeupClasses] ADD [MeetingLink] nvarchar(1000) NULL;");
            migrationBuilder.Sql("IF COL_LENGTH('MakeupClasses', 'UsesNextScheduledClass') IS NULL ALTER TABLE [MakeupClasses] ADD [UsesNextScheduledClass] bit NOT NULL DEFAULT 0;");
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // These columns may have been introduced by a previous local development run.
        }
    }
}
