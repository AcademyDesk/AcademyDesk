using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademyDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotPayrollWorkerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerNameSnapshot",
                table: "PayrollPayouts",
                type: "nvarchar(240)",
                maxLength: 240,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkerTypeSnapshot",
                table: "PayrollPayouts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Freeze the best identity still available for legacy rows. This
            // cannot reconstruct names changed before this upgrade. Never copy
            // a different academy's profile identity into a historical payout.
            migrationBuilder.Sql("""
                UPDATE payout
                SET [WorkerNameSnapshot] = profile.[WorkerName],
                    [WorkerTypeSnapshot] = profile.[WorkerType]
                FROM [PayrollPayouts] AS payout
                INNER JOIN [PayrollProfiles] AS profile
                    ON profile.[Id] = payout.[PayrollProfileId]
                    AND profile.[AcademyId] = payout.[AcademyId];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkerNameSnapshot",
                table: "PayrollPayouts");

            migrationBuilder.DropColumn(
                name: "WorkerTypeSnapshot",
                table: "PayrollPayouts");
        }
    }
}
