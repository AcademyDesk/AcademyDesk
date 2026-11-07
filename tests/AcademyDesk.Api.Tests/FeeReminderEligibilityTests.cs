using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class FeeReminderEligibilityTests
{
    [Theory]
    [InlineData("Cancelled", true)] [InlineData("Cancelled", false)]
    [InlineData("cancelled", true)] [InlineData("cancelled", false)]
    [InlineData("CANCELLED", true)] [InlineData("CANCELLED", false)]
    [InlineData("CaNcElLeD", true)] [InlineData("CaNcElLeD", false)]
    public async Task Cancelled_invoice_never_creates_a_targeted_or_bulk_collection_notice(string status, bool targeted)
    {
        await using var db = CreateDb(); var invoice = Invoice(Guid.NewGuid(), status);
        db.Add(invoice); await db.SaveChangesAsync(); var before = await Ledger(db);
        var result = await new FeeRemindersController(db).Queue(invoice.AcademyId, new(targeted ? invoice.Id : null, "InApp"), default);
        Assert.Equal(0, Body(result).QueuedCount);
        db.ChangeTracker.Clear(); Assert.Equal(before, await Ledger(db));
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Bulk_reminders_preserve_net_balances_currencies_due_dates_and_other_tenants()
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid();
        var partial = Invoice(academy); partial.AdjustedAmount = 200m;
        var usd = Invoice(academy); usd.Currency = "USD"; usd.TotalAmount = 50m;
        var settled = Invoice(academy); var overpaid = Invoice(academy);
        var cancelled = Invoice(academy, "Cancelled"); var foreign = Invoice(Guid.NewGuid());
        var future = Invoice(academy); future.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        db.AddRange(partial, usd, settled, overpaid, cancelled, foreign, future);
        db.Payments.AddRange(new Payment { AcademyId = academy, InvoiceId = partial.Id, Amount = 600m, Status = "Reconciled" },
            new Payment { AcademyId = academy, InvoiceId = settled.Id, Amount = 1000m, Status = "Completed" },
            new Payment { AcademyId = academy, InvoiceId = overpaid.Id, Amount = 1100m, Status = "Reconciled" });
        await db.SaveChangesAsync(); var before = await Ledger(db);
        var result = Body(await new FeeRemindersController(db).Queue(academy, new(null, "InApp"), default));
        Assert.Equal(2, result.QueuedCount);
        db.ChangeTracker.Clear(); Assert.Equal(before, await Ledger(db));
        var notices = await db.Notifications.AsNoTracking().ToListAsync(); Assert.Equal(2, notices.Count);
        Assert.Contains(notices, x => x.RecipientId == partial.StudentId && x.Message.Contains("INR 200.00"));
        Assert.Contains(notices, x => x.RecipientId == usd.StudentId && x.Message.Contains("USD 50.00"));
        Assert.All(notices, x => { Assert.Equal(academy, x.AcademyId); Assert.Equal("InApp", x.Channel); Assert.Equal("Queued", x.Status); Assert.Null(x.SentAtUtc); });
        Assert.Equal("FeeRemindersQueued", Assert.Single(await db.AuditLogs.ToListAsync()).Action);
    }

    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Foreign_or_missing_target_is_still_empty_without_touching_other_invoices(bool foreign)
    {
        await using var db = CreateDb(); var invoice = Invoice(Guid.NewGuid()); db.Add(invoice); await db.SaveChangesAsync();
        var result = Body(await new FeeRemindersController(db).Queue(Guid.NewGuid(), new(foreign ? invoice.Id : Guid.NewGuid(), "InApp"), default));
        Assert.Equal(0, result.QueuedCount); Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Explicit_active_future_invoice_retains_existing_targeted_policy()
    {
        await using var db = CreateDb(); var invoice = Invoice(Guid.NewGuid()); invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));
        db.Add(invoice); await db.SaveChangesAsync();
        Assert.Equal(1, Body(await new FeeRemindersController(db).Queue(invoice.AcademyId, new(invoice.Id, "InApp"), default)).QueuedCount);
        Assert.Equal(invoice.StudentId, Assert.Single(await db.Notifications.ToListAsync()).RecipientId);
    }

    private static Invoice Invoice(Guid academy, string status = "Issued") => new()
    { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = $"Synthetic-{Guid.NewGuid():N}", TotalAmount = 1000m, Currency = "INR", Status = status, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)) };
    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase($"reminder-eligibility-{Guid.NewGuid():N}").Options);
    private static FeeReminderResult Body(ActionResult<FeeReminderResult> response) => Assert.IsType<FeeReminderResult>(Assert.IsType<OkObjectResult>(response.Result).Value);
    private static async Task<string> Ledger(AcademyDeskDbContext db) => JsonSerializer.Serialize(new
    { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
}
