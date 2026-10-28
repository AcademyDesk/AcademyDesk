using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollPeriodTests
{
    private const string DuplicateMessage = "A regular payroll statement already exists for this worker and pay period. Additional amounts require a separately approved payroll adjustment.";

    [Theory]
    [InlineData("Teacher", "Monthly", false)]
    [InlineData("Teacher", "SessionBlock", false)]
    [InlineData("Teacher", "Monthly", true)]
    [InlineData("Teacher", "SessionBlock", true)]
    [InlineData("Staff", "Monthly", false)]
    [InlineData("Staff", "SessionBlock", false)]
    [InlineData("Staff", "Monthly", true)]
    public async Task Same_worker_cannot_repeat_a_period_through_another_profile(string worker, string model, bool historicalNoCash)
    {
        await using var db = CreateDb();
        var profile = Profile(worker, model); var alternate = Profile(worker, model);
        alternate.AcademyId = profile.AcademyId; alternate.TeacherId = profile.TeacherId; alternate.StaffUserId = profile.StaffUserId;
        if (profile.TeacherId is Guid teacherId)
            db.Teachers.Add(new Teacher { Id = teacherId, AcademyId = profile.AcademyId, FirstName = "Synthetic", LastName = "SharedPayrollWorker" });
        db.PayrollProfiles.AddRange(profile, alternate); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db);
        if (historicalNoCash)
        {
            db.PayrollPayouts.Add(new PayrollPayout { AcademyId = profile.AcademyId, PayrollProfileId = profile.Id,
                PayslipNumber = "QA-HISTORICAL-NO-CASH", PeriodLabel = "QA regular period", GrossAmount = 1000m, Deductions = 1000m,
                NetAmount = 0m, Status = "NoCashDue", PaymentMethod = "NoCashDue", PaidAtUtc = null,
                WorkerNameSnapshot = profile.WorkerName, WorkerTypeSnapshot = profile.WorkerType,
                TeacherIdSnapshot = profile.TeacherId, StaffUserIdSnapshot = profile.StaffUserId, IdentityRecordedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        else Assert.IsType<CreatedResult>((await controller.Pay(profile.AcademyId, Request(profile, "QA regular period"), default)).Result);
        var rejected = Assert.IsType<ConflictObjectResult>((await controller.Pay(profile.AcademyId, Request(alternate, "  qa   REGULAR\tperiod  "), default)).Result);
        Assert.Equal(DuplicateMessage, rejected.Value!.GetType().GetProperty("message")!.GetValue(rejected.Value));
        var row = Assert.Single(await db.PayrollPayouts.ToListAsync());
        Assert.Equal(profile.Id, row.PayrollProfileId); Assert.Equal(historicalNoCash ? 0 : 1000, row.NetAmount);
        Assert.Equal(historicalNoCash ? "NoCashDue" : "Paid", row.Status);
        if (historicalNoCash) { Assert.Null(row.PaidAtUtc); Assert.Null(row.Reference); Assert.Equal("NoCashDue", row.PaymentMethod); }
    }

    [Theory]
    [InlineData("2026-10", "October 2026")]
    [InlineData("Oct 2026", "2026/10")]
    [InlineData(" October   2026 ", "oct 2026")]
    [InlineData("QA period", " qa\tPERIOD ")]
    public async Task Equivalent_recorded_periods_cannot_be_paid_twice(string first, string second)
    {
        await using var db = CreateDb(); var profile = Profile("Teacher", "Monthly");
        db.PayrollProfiles.Add(profile); await db.SaveChangesAsync(); var controller = PayrollFixture.Controller(db);
        Assert.IsType<CreatedResult>((await controller.Pay(profile.AcademyId, Request(profile, first), default)).Result);
        Assert.IsType<ConflictObjectResult>((await controller.Pay(profile.AcademyId, Request(profile, second), default)).Result);
        Assert.Single(await db.PayrollPayouts.ToListAsync());
    }

    [Fact]
    public async Task Different_worker_or_distinct_period_remains_payable()
    {
        await using var db = CreateDb(); var first = Profile("Teacher", "Monthly"); var other = Profile("Teacher", "Monthly"); other.AcademyId = first.AcademyId;
        db.PayrollProfiles.AddRange(first, other); await db.SaveChangesAsync(); var controller = PayrollFixture.Controller(db);
        foreach (var request in new[] { Request(first, "2026-10"), Request(first, "2026-11"), Request(other, "2026-10") })
            Assert.IsType<CreatedResult>((await controller.Pay(first.AcademyId, request, default)).Result);
        Assert.Equal(3, await db.PayrollPayouts.CountAsync());
    }

    [Fact]
    public async Task Recorded_worker_identity_is_not_rewritten_from_a_reassigned_profile()
    {
        await using var db = CreateDb(); var profile = Profile("Teacher", "Monthly"); var priorWorker = profile.TeacherId;
        db.PayrollProfiles.Add(profile); await db.SaveChangesAsync(); var controller = PayrollFixture.Controller(db);
        Assert.IsType<CreatedResult>((await controller.Pay(profile.AcademyId, Request(profile, "2026-10"), default)).Result);
        profile.TeacherId = Guid.NewGuid(); await db.SaveChangesAsync(); controller = PayrollFixture.Controller(db);
        Assert.IsType<CreatedResult>((await controller.Pay(profile.AcademyId, Request(profile, "2026-10"), default)).Result);
        var rows = await db.PayrollPayouts.ToListAsync(); Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.TeacherIdSnapshot == priorWorker); Assert.Contains(rows, r => r.TeacherIdSnapshot == profile.TeacherId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_legacy_identity_blocks_same_profile_period_without_rewriting_history(bool recordedTimestampOnly)
    {
        await using var db = CreateDb(); var profile = Profile("Teacher", "Monthly");
        var recordedAt = recordedTimestampOnly ? DateTime.UtcNow : (DateTime?)null;
        var legacy = new PayrollPayout { AcademyId = profile.AcademyId, PayrollProfileId = profile.Id, PayslipNumber = "SYNTHETIC-LEGACY", PeriodLabel = "Oct 2026", GrossAmount = 1000, NetAmount = 1000, IdentityRecordedAtUtc = recordedAt };
        db.PayrollProfiles.Add(profile); db.PayrollPayouts.Add(legacy); await db.SaveChangesAsync(); var controller = PayrollFixture.Controller(db);
        Assert.IsType<ConflictObjectResult>((await controller.Pay(profile.AcademyId, Request(profile, "2026-10"), default)).Result);
        var retained = Assert.Single(await db.PayrollPayouts.ToListAsync()); Assert.Equal(legacy.Id, retained.Id);
        Assert.Equal(recordedAt, retained.IdentityRecordedAtUtc); Assert.Null(retained.TeacherIdSnapshot); Assert.Equal("Oct 2026", retained.PeriodLabel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public async Task Empty_or_unbounded_period_is_rejected_without_a_statement(int length)
    {
        await using var db = CreateDb(); var profile = Profile("Teacher", "Monthly"); db.PayrollProfiles.Add(profile); await db.SaveChangesAsync();
        Assert.IsType<BadRequestObjectResult>((await PayrollFixture.Controller(db).Pay(profile.AcademyId, Request(profile, new string('x', length)), default)).Result);
        Assert.Empty(await db.PayrollPayouts.ToListAsync());
    }

    private static CreatePayrollPayoutRequest Request(PayrollProfile profile, string label, bool noCash = false) => new(profile.Id, label, null, null, noCash ? 1000 : 0, "Offline", null, null);
    private static PayrollProfile Profile(string worker, string model) => new() { AcademyId = Guid.NewGuid(), WorkerType = worker, TeacherId = worker == "Teacher" ? Guid.NewGuid() : null, StaffUserId = worker == "Staff" ? Guid.NewGuid().ToString() : null, WorkerName = "Synthetic payroll period", PaymentModel = model, MonthlyAmount = model == "Monthly" ? 1000 : null, AmountPerCycle = model == "SessionBlock" ? 1000 : null, SessionsPerCycle = model == "SessionBlock" ? 4 : null };
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-payroll-period-" + Guid.NewGuid()).Options);
}
