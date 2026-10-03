using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollNetTests
{
    [Theory]
    [InlineData(0, true, 1000)]
    [InlineData(999, true, 1)]
    [InlineData(999.99, true, 0.01)]
    [InlineData(1000.01, false, 0)]
    [InlineData(1001, false, 0)]
    [InlineData(-0.01, false, 0)]
    [InlineData(-1, false, 0)]
    public async Task Monthly_deductions_cannot_create_negative_net(decimal deductions, bool accepted, decimal net)
    {
        await using var db = CreateDb();
        var profile = Profile("Monthly");
        db.PayrollProfiles.Add(profile);
        await db.SaveChangesAsync();
        var result = await new PayrollController(db).Pay(profile.AcademyId,
            new(profile.Id, "QA period", null, null, deductions, null, null, null), default);
        await AssertPayoutAsync(db, profile, result, accepted, 1000m, net, null);
    }

    [Theory]
    [InlineData(null, null, 1000.01, false, 0)]
    [InlineData("250", 3, 250.01, false, 0)]
    [InlineData("250", 3, 249.99, true, 0.01)]
    [InlineData("0", 3, 0, false, 0)]
    [InlineData("250", 0, 0, false, 0)]
    [InlineData("250", 3, -1, false, 0)]
    public async Task Session_block_guard_uses_effective_gross(string? grossValue, int? sessions, decimal deductions, bool accepted, decimal net)
    {
        decimal? gross = grossValue is null ? null : decimal.Parse(grossValue, System.Globalization.CultureInfo.InvariantCulture);
        await using var db = CreateDb();
        var profile = Profile("SessionBlock");
        db.PayrollProfiles.Add(profile);
        await db.SaveChangesAsync();
        var result = await new PayrollController(db).Pay(profile.AcademyId,
            new(profile.Id, "QA period", sessions, gross, deductions, null, null, null), default);
        await AssertPayoutAsync(db, profile, result, accepted, gross ?? 1000m, net, sessions ?? 12);
    }

    [Fact]
    public async Task Monthly_model_keeps_profile_gross_instead_of_request_override()
    {
        await using var db = CreateDb();
        var profile = Profile("Monthly");
        db.PayrollProfiles.Add(profile);
        await db.SaveChangesAsync();
        var result = await new PayrollController(db).Pay(profile.AcademyId,
            new(profile.Id, "QA period", 3, 1m, 999m, null, null, null), default);
        await AssertPayoutAsync(db, profile, result, true, 1000m, 1m, null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Foreign_or_inactive_profile_remains_unpayable(bool foreign)
    {
        await using var db = CreateDb();
        var profile = Profile("Monthly");
        profile.IsActive = foreign;
        db.PayrollProfiles.Add(profile);
        await db.SaveChangesAsync();
        var result = await new PayrollController(db).Pay(foreign ? Guid.NewGuid() : profile.AcademyId,
            new(profile.Id, "QA period", null, null, 0m, null, null, null), default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await db.PayrollPayouts.ToListAsync());
    }

    private static async Task AssertPayoutAsync(AcademyDeskDbContext db, PayrollProfile profile,
        ActionResult<PayrollPayoutSummary> result, bool accepted, decimal gross, decimal net, int? sessions)
    {
        if (!accepted)
        {
            Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Empty(await db.PayrollPayouts.ToListAsync());
        }
        else
        {
            var body = Assert.IsType<PayrollPayoutSummary>(Assert.IsType<CreatedResult>(result.Result).Value);
            var row = Assert.Single(await db.PayrollPayouts.ToListAsync());
            Assert.Equal(gross, body.GrossAmount);
            Assert.Equal(net, body.NetAmount);
            Assert.Equal(net, row.NetAmount);
            Assert.Equal(sessions, row.SessionsCovered);
            Assert.Equal("Paid", row.Status);
            Assert.Equal("BankTransfer", row.PaymentMethod);
            Assert.Null(row.Reference);
        }
        Assert.Equal(1000m, profile.MonthlyAmount ?? profile.AmountPerCycle);
        Assert.True(profile.IsActive);
    }

    private static PayrollProfile Profile(string model) => new()
    { AcademyId = Guid.NewGuid(), WorkerType = "Staff", WorkerName = "Synthetic QA", PaymentModel = model,
        MonthlyAmount = model == "Monthly" ? 1000m : null,
        AmountPerCycle = model == "SessionBlock" ? 1000m : null,
        SessionsPerCycle = model == "SessionBlock" ? 12 : null };

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase("qa-payroll-net-" + Guid.NewGuid()).Options);
}
