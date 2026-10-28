using AcademyDesk.Api.Tests.Infrastructure;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollHistoryTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Profile_update_rejects_foreign_or_missing_teacher_without_changing_profile(bool foreign)
    {
        await using var db = CreateDb();
        var profile = new PayrollProfile { AcademyId = Guid.NewGuid(), WorkerType = "Staff", WorkerName = "Original", MonthlyAmount = 1000m };
        var teacher = new Teacher { AcademyId = Guid.NewGuid(), FirstName = "Foreign", LastName = "Teacher" };
        db.Add(profile); if (foreign) db.Add(teacher); await db.SaveChangesAsync();
        var before = System.Text.Json.JsonSerializer.Serialize(profile);
        var result = await PayrollFixture.Controller(db).UpdateProfile(profile.AcademyId, profile.Id,
            new("Teacher", teacher.Id, null, "Changed", "Monthly", 2000m, null, null, null), default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        db.ChangeTracker.Clear();
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(await db.PayrollProfiles.SingleAsync()));
    }

    [Theory]
    [InlineData("rename")]
    [InlineData("reassign")]
    [InlineData("deactivate")]
    [InlineData("remove")]
    public async Task Existing_payout_retains_paid_identity_after_profile_change(string change)
    {
        await using var db = CreateDb();
        var profile = new PayrollProfile { AcademyId = Guid.NewGuid(), WorkerType = "Staff", StaffUserId = Guid.NewGuid().ToString(), WorkerName = "Paid Worker", MonthlyAmount = 1000m };
        db.Add(profile); await db.SaveChangesAsync();
        var replacementStaff = Guid.NewGuid().ToString();
        var replacementTeacher = new Teacher { AcademyId = profile.AcademyId, FirstName = "Synthetic", LastName = "Replacement" };
        db.Add(replacementTeacher); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db, replacementStaff);
        var paid = Assert.IsType<PayrollPayoutSummary>(Assert.IsType<CreatedResult>((await controller.Pay(profile.AcademyId,
            new(profile.Id, "Synthetic period", null, null, 100m, null, null, null), default)).Result).Value);
        var original = Assert.Single(Assert.IsType<List<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await controller.Payouts(profile.AcademyId, default)).Result).Value));
        Assert.Equal("Paid Worker", paid.WorkerName);
        Assert.Equal(paid, original);
        if (change == "remove") db.Remove(profile);
        else
        {
            var update = await controller.UpdateProfile(profile.AcademyId, profile.Id,
                new(change == "reassign" ? "Teacher" : "Staff", change == "reassign" ? replacementTeacher.Id : null, change == "reassign" ? null : replacementStaff,
                    "Current Worker", "Monthly", 2000m, null, null, null, change != "deactivate"), default);
            Assert.IsType<OkObjectResult>(update.Result);
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var after = Assert.Single(Assert.IsType<List<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await controller.Payouts(profile.AcademyId, default)).Result).Value));
        Assert.Equal(original, after);
    }

    [Fact]
    public async Task Payout_with_foreign_profile_never_discloses_foreign_identity()
    {
        await using var db = CreateDb();
        var academy = Guid.NewGuid();
        var foreign = new PayrollProfile { AcademyId = Guid.NewGuid(), WorkerType = "Staff", WorkerName = "FOREIGN PRIVATE" };
        db.Add(foreign);
        db.Add(new PayrollPayout { AcademyId = academy, PayrollProfileId = foreign.Id, PayslipNumber = "QA-FOREIGN", PeriodLabel = "Synthetic" });
        await db.SaveChangesAsync();
        var row = Assert.Single(Assert.IsType<List<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await PayrollFixture.Controller(db).Payouts(academy, default)).Result).Value));
        Assert.Equal("Unavailable worker", row.WorkerName);
        Assert.Equal("Unknown", row.WorkerType);
        Assert.Empty(Assert.IsType<List<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await PayrollFixture.Controller(db).Payouts(foreign.AcademyId, default)).Result).Value));
    }

    [Fact]
    public async Task Missing_legacy_profile_keeps_payout_visible_without_inventing_identity()
    {
        await using var db = CreateDb();
        var row = new PayrollPayout { AcademyId = Guid.NewGuid(), PayrollProfileId = Guid.NewGuid(), PayslipNumber = "QA-ORPHAN", PeriodLabel = "Synthetic" };
        db.Add(row); await db.SaveChangesAsync();
        var summary = Assert.Single(Assert.IsType<List<PayrollPayoutSummary>>(Assert.IsType<OkObjectResult>((await PayrollFixture.Controller(db).Payouts(row.AcademyId, default)).Result).Value));
        Assert.Equal(row.Id, summary.Id);
        Assert.Equal("Unavailable worker", summary.WorkerName);
        Assert.Equal("Unknown", summary.WorkerType);
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-payroll-history-" + Guid.NewGuid()).Options);
}
