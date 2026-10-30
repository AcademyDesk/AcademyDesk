using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class FinanceCurrencySummaryTests
{
    private static AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static IdentityDbContext Identity() => new(new DbContextOptionsBuilder<IdentityDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static async Task<JsonElement> Summary(AcademyDeskDbContext db, IdentityDbContext identity, Guid academy)
    {
        var result = Assert.IsType<OkObjectResult>(await new FinanceGovernanceController(db, identity).Summary(academy, default));
        return JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    private static Invoice Invoice(Guid academy, string currency, decimal amount, decimal adjusted = 0) => new()
    {
        AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = Guid.NewGuid().ToString(),
        Currency = currency, TotalAmount = amount, AdjustedAmount = adjusted,
        DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)
    };

    [Fact]
    public async Task Mixed_currency_original_reproduction_keeps_exact_totals_and_never_emits_combined_money()
    {
        using var db = Context(); using var identity = Identity(); var academy = Guid.NewGuid();
        var inr = Invoice(academy, "INR", 1000.25m, 100.10m);
        var usd = Invoice(academy, "USD", 100.50m, 10.20m);
        db.AddRange(inr, usd, Invoice(Guid.NewGuid(), "EUR", 99999m));
        db.AddRange(new Payment { AcademyId = academy, InvoiceId = inr.Id, Currency = "INR", Amount = 600.05m },
            new Payment { AcademyId = academy, InvoiceId = usd.Id, Currency = "USD", Amount = 20.10m, Status = "Reconciled" },
            new Payment { AcademyId = academy, InvoiceId = usd.Id, Currency = "USD", Amount = 5m, Status = "Voided" },
            new Payment { AcademyId = Guid.NewGuid(), InvoiceId = usd.Id, Currency = "EUR", Amount = 99999m });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await Summary(db, identity, academy);
        foreach (var field in new[] { "grossBilled", "approvedAdjustments", "collected", "outstanding", "reconciled" })
            Assert.Equal(JsonValueKind.Null, result.GetProperty(field).ValueKind);
        Assert.True(result.GetProperty("hasMixedCurrencies").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind);
        var groups = result.GetProperty("currencyTotals").EnumerateArray().ToArray();
        Assert.Equal(new[] { "INR", "USD" }, groups.Select(x => x.GetProperty("currency").GetString()));
        AssertTotals(groups[0], 1000.25m, 100.10m, 600.05m, 300.10m, 0m);
        AssertTotals(groups[1], 100.50m, 10.20m, 20.10m, 70.20m, 20.10m);
        Assert.Equal(2, result.GetProperty("overdueInvoices").GetInt32());
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("INR")]
    [InlineData("USD")]
    public async Task Single_currency_retains_numeric_contract_and_identifies_its_unit(string currency)
    {
        using var db = Context(); using var identity = Identity(); var academy = Guid.NewGuid();
        var invoice = Invoice(academy, currency, 1000m, 200m); db.Add(invoice);
        db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = currency, Amount = 600m, Status = "Reconciled" });
        await db.SaveChangesAsync();
        var result = await Summary(db, identity, academy);
        Assert.False(result.GetProperty("hasMixedCurrencies").GetBoolean());
        Assert.Equal(currency, result.GetProperty("currency").GetString());
        AssertTotals(result, 1000m, 200m, 600m, 200m, 600m);
        AssertTotals(Assert.Single(result.GetProperty("currencyTotals").EnumerateArray()), 1000m, 200m, 600m, 200m, 600m);
    }

    [Fact]
    public async Task Normalizes_currency_case_and_whitespace_without_netting_other_currencies()
    {
        using var db = Context(); using var identity = Identity(); var academy = Guid.NewGuid();
        var invoice = Invoice(academy, " usd ", 100m); db.Add(invoice);
        db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = "uSd", Amount = 40m });
        await db.SaveChangesAsync();
        var result = await Summary(db, identity, academy);
        Assert.Equal("USD", result.GetProperty("currency").GetString());
        AssertTotals(Assert.Single(result.GetProperty("currencyTotals").EnumerateArray()), 100m, 0m, 40m, 60m, 0m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("rupees")]
    [InlineData("12$")]
    public async Task Malformed_stored_currency_is_explicit_and_never_assumed_to_be_INR(string currency)
    {
        using var db = Context(); using var identity = Identity(); var academy = Guid.NewGuid();
        db.Add(Invoice(academy, currency, 100m)); await db.SaveChangesAsync();
        var result = await Summary(db, identity, academy);
        Assert.True(result.GetProperty("hasInvalidCurrency").GetBoolean());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("grossBilled").ValueKind);
        var row = Assert.Single(result.GetProperty("currencyTotals").EnumerateArray());
        Assert.Equal(currency.Trim().ToUpperInvariant(), row.GetProperty("currency").GetString());
        Assert.Equal(100m, row.GetProperty("grossBilled").GetDecimal());
    }

    [Fact]
    public async Task Empty_book_has_no_invented_currency_and_preserves_zero_numeric_contract()
    {
        using var db = Context(); using var identity = Identity();
        var result = await Summary(db, identity, Guid.NewGuid());
        Assert.Empty(result.GetProperty("currencyTotals").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind);
        AssertTotals(result, 0, 0, 0, 0, 0);
    }

    [Fact]
    public async Task Payment_only_currency_and_negative_balance_cannot_offset_another_currency()
    {
        using var db = Context(); using var identity = Identity(); var academy = Guid.NewGuid();
        db.Add(Invoice(academy, "INR", 100m));
        db.Add(new Payment { AcademyId = academy, InvoiceId = Guid.NewGuid(), Currency = "USD", Amount = 500m });
        await db.SaveChangesAsync();
        var result = await Summary(db, identity, academy);
        var rows = result.GetProperty("currencyTotals").EnumerateArray().ToArray();
        AssertTotals(rows[0], 100, 0, 0, 100, 0);
        AssertTotals(rows[1], 0, 0, 500, 0, 0);
        Assert.Equal(JsonValueKind.Null, result.GetProperty("outstanding").ValueKind);
    }

    private static void AssertTotals(JsonElement row, decimal gross, decimal adjusted, decimal collected, decimal outstanding, decimal reconciled)
    {
        Assert.Equal(gross, row.GetProperty("grossBilled").GetDecimal());
        Assert.Equal(adjusted, row.GetProperty("approvedAdjustments").GetDecimal());
        Assert.Equal(collected, row.GetProperty("collected").GetDecimal());
        Assert.Equal(outstanding, row.GetProperty("outstanding").GetDecimal());
        Assert.Equal(reconciled, row.GetProperty("reconciled").GetDecimal());
    }
}
