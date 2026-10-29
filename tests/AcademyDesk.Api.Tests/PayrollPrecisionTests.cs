using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Tests;

public sealed class PayrollPrecisionTests
{
    [Theory]
    [InlineData("Monthly", "1000.001")]
    [InlineData("Monthly", "10000000000000000")]
    [InlineData("SessionBlock", "1000.001")]
    [InlineData("SessionBlock", "10000000000000000")]
    public async Task Profile_create_and_update_reject_unrepresentable_money_without_writes(string model, string raw)
    {
        await using var db = Db(); var profile = Profile(model); db.Add(profile); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db); var amount = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        var request = new SavePayrollProfileRequest("Staff", null, profile.StaffUserId, "Changed synthetic", model,
            model == "Monthly" ? amount : null, model == "SessionBlock" ? amount : null, model == "SessionBlock" ? 4 : null, null);
        var before = await State(db);
        Assert.IsType<BadRequestObjectResult>((await controller.CreateProfile(profile.AcademyId, request, default)).Result);
        Assert.Equal(before, await State(db));
        Assert.IsType<BadRequestObjectResult>((await controller.UpdateProfile(profile.AcademyId, profile.Id, request, default)).Result);
        Assert.Equal(before, await State(db));
    }

    [Theory]
    [InlineData("Monthly", "0.001", null)]
    [InlineData("SessionBlock", "0.001", "1000")]
    [InlineData("SessionBlock", "0", "1000.001")]
    [InlineData("SessionBlock", "0", "10000000000000000")]
    public async Task Payout_rejects_rounding_and_overflow_without_writes(string model, string deduction, string? gross)
    {
        await using var db = Db(); var profile = Profile(model); db.Add(profile); await db.SaveChangesAsync();
        var controller = PayrollFixture.Controller(db); var before = await State(db);
        var parse = (string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsType<BadRequestObjectResult>((await controller.Pay(profile.AcademyId,
            new(profile.Id, "Synthetic precision", 4, gross is null ? null : parse(gross), parse(deduction), "Cash", null, null), default)).Result);
        Assert.Equal(before, await State(db));
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("SessionBlock")]
    public async Task Maximum_stored_money_retains_one_cent_net(string model)
    {
        await using var db = Db(); var profile = Profile(model);
        if (model == "Monthly") profile.MonthlyAmount = 9999999999999999.99m; else profile.AmountPerCycle = 9999999999999999.99m;
        db.Add(profile); await db.SaveChangesAsync(); var controller = PayrollFixture.Controller(db);
        var result = await controller.Pay(profile.AcademyId, new(profile.Id, "Synthetic maximum", 4, null, 9999999999999999.98m, "Cash", null, null), default);
        var row = Assert.IsType<PayrollPayoutSummary>(Assert.IsType<CreatedResult>(result.Result).Value);
        Assert.Equal("9999999999999999.99", row.GrossAmountExact); Assert.Equal("9999999999999999.98", row.DeductionsExact); Assert.Equal("0.01", row.NetAmountExact);
        var stored = Assert.Single(await db.PayrollPayouts.ToArrayAsync()); Assert.Equal(0.01m, stored.NetAmount);
    }

    private static PayrollProfile Profile(string model) => new() { AcademyId = Guid.NewGuid(), WorkerType = "Staff", StaffUserId = Guid.NewGuid().ToString(), WorkerName = "Synthetic precision worker", PaymentModel = model, MonthlyAmount = model == "Monthly" ? 1000m : null, AmountPerCycle = model == "SessionBlock" ? 1000m : null, SessionsPerCycle = model == "SessionBlock" ? 4 : null };
    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-payroll-precision-" + Guid.NewGuid()).Options);
    private static async Task<string> State(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().ToArrayAsync(), Payouts = await db.PayrollPayouts.AsNoTracking().ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().ToArrayAsync() });
}
