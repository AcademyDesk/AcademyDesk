using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using AcademyDesk.Api.Tests.Infrastructure;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollHistoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reloaded_SQL_UTC_payment_time_has_explicit_zone_without_changing_stored_ticks(bool legacy)
    {
        await using var db = Db(); var profile = Profile(); var payout = Legacy(profile.AcademyId, profile.Id);
        // SQL datetime2 materializes UTC columns with Unspecified Kind.
        payout.PaidAtUtc = new DateTime(2026, 10, 1, 0, 15, 0, DateTimeKind.Unspecified);
        if (!legacy) { payout.WorkerNameSnapshot = profile.WorkerName; payout.WorkerTypeSnapshot = profile.WorkerType; payout.IdentityRecordedAtUtc = payout.PaidAtUtc; }
        db.PayrollPayouts.Add(payout); await db.SaveChangesAsync();
        var before = System.Text.Json.JsonSerializer.Serialize(payout);
        var row = Assert.Single(await List(db, profile.AcademyId));
        Assert.Equal(DateTimeKind.Utc, row.PaidAtUtc!.Value.Kind);
        Assert.Equal(payout.PaidAtUtc!.Value.Ticks, row.PaidAtUtc.Value.Ticks);
        using var json = System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(row));
        Assert.Equal("2026-10-01T00:15:00Z", json.RootElement.GetProperty("PaidAtUtc").GetString());
        Assert.Equal(legacy ? "LegacyUnverified" : "PayoutSnapshot", row.IdentitySource);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(Assert.Single(await db.PayrollPayouts.AsNoTracking().ToListAsync())));
    }

    [Fact]
    public void Model_matches_migration_snapshot_without_database_access()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=qa_model_only;Integrated Security=True;Connect Timeout=1").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Migration_only_adds_nullable_identity_columns_without_backfill_or_finance_changes()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=qa_model_only;Integrated Security=True;Connect Timeout=1").Options);
        var migrations = db.GetService<IMigrationsAssembly>();
        var migration = migrations.CreateMigration(migrations.Migrations.Single(x => x.Key.EndsWith("_PreservePayrollPayoutIdentity", StringComparison.Ordinal)).Value, "Microsoft.EntityFrameworkCore.SqlServer");
        Assert.Equal(5, migration.UpOperations.Count);
        Assert.All(migration.UpOperations, op => { var column = Assert.IsType<AddColumnOperation>(op); Assert.Equal("PayrollPayouts", column.Table); Assert.True(column.IsNullable); Assert.Null(column.DefaultValue); Assert.Null(column.DefaultValueSql); });
    }
    [Theory]
    [InlineData("Teacher")]
    [InlineData("Staff")]
    public async Task New_payout_retains_server_profile_identity_at_recording(string workerType)
    {
        await using var db = Db();
        var profile = Profile(); profile.WorkerType = workerType;
        profile.TeacherId = workerType == "Teacher" ? Guid.NewGuid() : null;
        profile.StaffUserId = workerType == "Staff" ? Guid.NewGuid().ToString() : null;
        db.PayrollProfiles.Add(profile); await db.SaveChangesAsync();
        var started = DateTime.UtcNow;
        var response = await Pay(db, profile);
        var stored = Assert.Single(await db.PayrollPayouts.AsNoTracking().ToListAsync());
        Assert.Equal(profile.WorkerName, stored.WorkerNameSnapshot);
        Assert.Equal(workerType, stored.WorkerTypeSnapshot);
        Assert.Equal(profile.TeacherId, stored.TeacherIdSnapshot);
        Assert.Equal(profile.StaffUserId, stored.StaffUserIdSnapshot);
        Assert.InRange(stored.IdentityRecordedAtUtc!.Value, started, DateTime.UtcNow);
        Assert.Equal(new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc), stored.PaidAtUtc);
        Assert.Equal("PayoutSnapshot", response.IdentitySource);
        Assert.Equal(response, Assert.Single(await List(db, profile.AcademyId)));
        Assert.Equal(800m, stored.NetAmount);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("link")]
    [InlineData("inactive")]
    public async Task Profile_edits_do_not_relabel_or_modify_old_payouts(string mode)
    {
        await using var db = Db(); var profile = Profile();
        db.PayrollProfiles.Add(profile); await db.SaveChangesAsync(); var original = await Pay(db, profile);
        var before = System.Text.Json.JsonSerializer.Serialize(Assert.Single(await db.PayrollPayouts.AsNoTracking().ToListAsync()));
        var teacher = new Teacher { AcademyId = profile.AcademyId, FirstName = "Synthetic", LastName = "Replacement" }; db.Teachers.Add(teacher); await db.SaveChangesAsync();
        var request = new SavePayrollProfileRequest(mode == "type" ? "Teacher" : "Staff", mode == "type" ? teacher.Id : null,
            mode == "type" ? null : mode == "link" ? Guid.NewGuid().ToString() : profile.StaffUserId, mode == "name" ? "Synthetic replacement" : profile.WorkerName,
            "Monthly", 1000m, null, null, profile.EffectiveFrom, mode != "inactive");
        Assert.IsType<OkObjectResult>((await PayrollFixture.Controller(db, request.StaffUserId).UpdateProfile(profile.AcademyId, profile.Id, request, default)).Result);
        Assert.Equal(original, Assert.Single(await List(db, profile.AcademyId)));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(Assert.Single(await db.PayrollPayouts.AsNoTracking().ToListAsync())));
    }

    [Fact]
    public async Task Missing_profile_does_not_hide_recorded_payout()
    {
        await using var db = Db(); var profile = Profile(); db.PayrollProfiles.Add(profile); await db.SaveChangesAsync();
        var original = await Pay(db, profile); db.PayrollProfiles.Remove(profile); await db.SaveChangesAsync();
        Assert.Equal(original, Assert.Single(await List(db, profile.AcademyId)));
    }

    [Theory]
    [InlineData("all")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("time")]
    public async Task Legacy_or_incomplete_identity_is_explicitly_unknown_not_current_profile(string missing)
    {
        await using var db = Db(); var profile = Profile(); db.PayrollProfiles.Add(profile);
        var payout = Legacy(profile.AcademyId, profile.Id);
        payout.WorkerNameSnapshot = missing is "all" or "name" ? null : "Partial snapshot";
        payout.WorkerTypeSnapshot = missing is "all" or "type" ? null : "Teacher";
        payout.IdentityRecordedAtUtc = missing is "all" or "time" ? null : DateTime.UtcNow;
        db.PayrollPayouts.Add(payout); await db.SaveChangesAsync();
        var before = System.Text.Json.JsonSerializer.Serialize(payout);
        var row = Assert.Single(await List(db, profile.AcademyId));
        Assert.Equal("Historical identity not recorded", row.WorkerName);
        Assert.Equal("Unknown (legacy)", row.WorkerType);
        Assert.Equal("LegacyUnverified", row.IdentitySource);
        Assert.Equal(800m, row.NetAmount); Assert.Equal(payout.PayslipNumber, row.PayslipNumber);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(Assert.Single(await db.PayrollPayouts.AsNoTracking().ToListAsync())));
    }

    [Fact]
    public async Task Payout_scope_is_its_academy_not_the_linked_mutable_profile()
    {
        await using var db = Db(); var academy = Guid.NewGuid(); var foreign = Profile();
        db.PayrollProfiles.Add(foreign);
        var ownLegacy = Legacy(academy, foreign.Id); var foreignPayout = Legacy(foreign.AcademyId, foreign.Id);
        foreignPayout.PayslipNumber = "QA-FOREIGN"; db.PayrollPayouts.AddRange(ownLegacy, foreignPayout); await db.SaveChangesAsync();
        var row = Assert.Single(await List(db, academy));
        Assert.Equal(ownLegacy.Id, row.Id); Assert.Equal("Historical identity not recorded", row.WorkerName);
        Assert.DoesNotContain(foreign.WorkerName, row.WorkerName);
    }

    private static async Task<PayrollPayoutSummary> Pay(AcademyDeskDbContext db, PayrollProfile p) =>
        Assert.IsType<PayrollPayoutSummary>(Assert.IsType<CreatedResult>((await PayrollFixture.Controller(db).Pay(p.AcademyId,
            new(p.Id, "September", null, null, 200m, null, null, new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc)), default)).Result).Value);
    private static async Task<IReadOnlyList<PayrollPayoutSummary>> List(AcademyDeskDbContext db, Guid academy) =>
        Assert.IsAssignableFrom<IReadOnlyList<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await PayrollFixture.Controller(db).Payouts(academy, default)).Result).Value);
    private static PayrollProfile Profile() => new() { AcademyId = Guid.NewGuid(), WorkerName = "Synthetic original worker", WorkerType = "Staff", StaffUserId = Guid.NewGuid().ToString(), MonthlyAmount = 1000m };
    private static PayrollPayout Legacy(Guid academy, Guid profile) => new() { AcademyId = academy, PayrollProfileId = profile, PayslipNumber = "QA-LEGACY", PeriodLabel = "September", GrossAmount = 1000m, Deductions = 200m, NetAmount = 800m };
    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-payroll-history-" + Guid.NewGuid()).Options);
}
