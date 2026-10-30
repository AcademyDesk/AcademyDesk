using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using IdentityDbContext = AcademyDesk.Api.Data.IdentityDbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AcademyDesk.Api.Tests;

public sealed class PlatformCurrencySummaryTests
{
    private sealed class Fixture : IDisposable
    {
        public AcademyDeskDbContext Db = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public IdentityDbContext Identity = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public UserManager<ApplicationUser> Users;
        public PlatformControlController Controller;
        public Fixture(bool owner = true)
        {
            var user = new ApplicationUser { IsPlatformOwner = owner, UserName = "synthetic@example.invalid", IsActive = true };
            Identity.Users.Add(user); Identity.SaveChanges();
            Users = new(new UserStore<ApplicationUser, ApplicationRole, IdentityDbContext, Guid>(Identity), null!, new PasswordHasher<ApplicationUser>(), [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
            Controller = new(Db, Users) { ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "Synthetic")) } } };
        }
        public void Add(string currency, decimal amount, string status = "Issued") => Db.PlatformBillingInvoices.Add(new() { AcademyId = Guid.NewGuid(), InvoiceNumber = Guid.NewGuid().ToString(), Currency = currency, Amount = amount, Status = status });
        public async Task<JsonElement> Summary()
        {
            await Db.SaveChangesAsync(); Db.ChangeTracker.Clear();
            var result = Assert.IsType<OkObjectResult>(await Controller.Overview(default));
            return JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        public void Dispose() { Users.Dispose(); Identity.Dispose(); Db.Dispose(); }
    }
    [Fact]
    public async Task Mixed_platform_currencies_do_not_emit_a_combined_money_scalar()
    {
        using var f = new Fixture(); f.Add("INR", 600.05m, "Paid"); f.Add("INR", 400.20m); f.Add("USD", 20.10m, "Paid"); f.Add("USD", 80.40m, "Overdue");
        f.Add("EUR", 99999m, "Draft"); f.Add("EUR", 99999m, "Void");
        var result = await f.Summary();
        foreach (var key in new[] { "totalBilled", "collectedBilling", "outstandingBilling" }) Assert.Equal(JsonValueKind.Null, result.GetProperty(key).ValueKind);
        Assert.True(result.GetProperty("hasMixedCurrencies").GetBoolean()); Assert.Equal(1, result.GetProperty("overdueInvoices").GetInt32());
        var rows = result.GetProperty("currencyTotals").EnumerateArray().ToArray(); Assert.Equal(2, rows.Length);
        Assert.Equal("INR", rows[0].GetProperty("currency").GetString()); Assert.Equal(1000.25m, rows[0].GetProperty("totalBilled").GetDecimal());
        Assert.Equal(600.05m, rows[0].GetProperty("collectedBilling").GetDecimal()); Assert.Equal(400.20m, rows[0].GetProperty("outstandingBilling").GetDecimal());
        Assert.Equal("USD", rows[1].GetProperty("currency").GetString()); Assert.Equal(100.50m, rows[1].GetProperty("totalBilled").GetDecimal());
        Assert.Equal(20.10m, rows[1].GetProperty("collectedBilling").GetDecimal()); Assert.Equal(80.40m, rows[1].GetProperty("outstandingBilling").GetDecimal());
        Assert.Empty(f.Db.ChangeTracker.Entries());
    }
    [Theory]
    [InlineData("INR")]
    [InlineData("USD")]
    public async Task Single_currency_retains_numeric_legacy_contract(string currency)
    {
        using var f = new Fixture(); f.Add(currency, 20.01m, "Paid"); f.Add(currency, 10.02m, "Payment submitted");
        var result = await f.Summary(); Assert.Equal(currency, result.GetProperty("currency").GetString());
        Assert.Equal(30.03m, result.GetProperty("totalBilled").GetDecimal()); Assert.Equal(20.01m, result.GetProperty("collectedBilling").GetDecimal());
        Assert.Equal(10.02m, result.GetProperty("outstandingBilling").GetDecimal());
    }
    [Theory]
    [InlineData("rupees")]
    [InlineData("")]
    public async Task Invalid_currency_stays_explicit_without_default_INR(string currency)
    {
        using var f = new Fixture(); f.Add(currency, 10m); var result = await f.Summary();
        Assert.True(result.GetProperty("hasInvalidCurrency").GetBoolean()); Assert.Equal(JsonValueKind.Null, result.GetProperty("totalBilled").ValueKind);
        Assert.Equal(currency.Trim().ToUpperInvariant(), Assert.Single(result.GetProperty("currencyTotals").EnumerateArray()).GetProperty("currency").GetString());
    }
    [Fact]
    public async Task Empty_book_has_no_currency_but_keeps_legacy_zero()
    {
        using var f = new Fixture(); var result = await f.Summary(); Assert.Equal(0m, result.GetProperty("totalBilled").GetDecimal());
        Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind); Assert.Empty(result.GetProperty("currencyTotals").EnumerateArray());
    }
    [Fact]
    public async Task Non_platform_owner_still_cannot_read_platform_summary()
    {
        using var f = new Fixture(false); f.Add("USD", 100m); await f.Db.SaveChangesAsync();
        Assert.IsType<ForbidResult>(await f.Controller.Overview(default));
    }
}
