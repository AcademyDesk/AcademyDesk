using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class FinanceCurrencySummaryTests
{
    [Fact]
    public async Task Mixed_currency_summary_has_separate_exact_amounts_and_no_flat_money_total()
    {
        await using var f = new Fixture(); var inr = f.Invoice("INR", 1000m, 200m); var usd = f.Invoice("USD", 100m, 10m);
        f.Payment(inr, 300m, "Completed"); f.Payment(inr, 100m, "Reconciled"); f.Payment(usd, 50m, "Reconciled");
        f.Db.Add(new Invoice { AcademyId = Guid.NewGuid(), StudentId = Guid.NewGuid(), InvoiceNumber = "FOREIGN", Currency = "EUR", TotalAmount = 9000m });
        f.Db.Add(new Payment { AcademyId = Guid.NewGuid(), InvoiceId = inr.Id, Currency = "INR", Amount = 5000m });
        await f.Db.SaveChangesAsync(); var before = await f.Snapshot(); f.Db.ChangeTracker.Clear(); var result = await f.Summary();
        foreach (var field in new[] { "grossBilled", "approvedAdjustments", "collected", "outstanding", "reconciled" }) Assert.Equal(JsonValueKind.Null, result.GetProperty(field).ValueKind);
        Assert.True(result.GetProperty("hasMixedCurrencies").GetBoolean()); Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind);
        var groups = Groups(result); Assert.Equal(new[] { "INR", "USD" }, groups.Keys);
        Money(groups["INR"], 1000m, 200m, 400m, 400m, 100m); Money(groups["USD"], 100m, 10m, 50m, 40m, 50m);
        Assert.Empty(f.Db.ChangeTracker.Entries()); Assert.Equal(before, await f.Snapshot()); Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("INR")] [InlineData("USD")] [InlineData("EUR")]
    public async Task Single_currency_retains_legacy_numbers_and_explicit_unit_without_rounding(string currency)
    {
        await using var f = new Fixture(); var invoice = f.Invoice(currency, 100.55m, .05m);
        f.Payment(invoice, 10.25m, "Completed"); f.Payment(invoice, 20.10m, "Reconciled"); await f.Db.SaveChangesAsync();
        var result = await f.Summary(); Assert.False(result.GetProperty("hasMixedCurrencies").GetBoolean()); Assert.Equal(currency, result.GetProperty("currency").GetString());
        Money(result, 100.55m, .05m, 30.35m, 70.15m, 20.10m); Money(Assert.Single(Groups(result)).Value, 100.55m, .05m, 30.35m, 70.15m, 20.10m);
    }

    [Theory]
    [InlineData("Completed", true)] [InlineData("Reconciled", true)] [InlineData("Voided", false)]
    [InlineData("Failed", false)] [InlineData("Pending", false)] [InlineData("Processing", false)]
    public async Task Collection_states_agree_across_summary_queue_and_follow_up(string status, bool collected)
    {
        await using var f = new Fixture(); var invoice = f.Invoice("INR", 100m); f.Payment(invoice, 100m, status); await f.Db.SaveChangesAsync();
        var result = await f.Summary(); Assert.Equal(collected ? 100m : 0m, result.GetProperty("collected").GetDecimal());
        Assert.Equal(collected ? 0m : 100m, result.GetProperty("outstanding").GetDecimal());
        var queue = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>((await f.Controller.Collections(f.Academy, default)).Result).Value);
        Assert.Equal(collected ? 0 : 1, queue.Count); Assert.Equal(queue.Count, result.GetProperty("overdueInvoices").GetInt32());
        var followUp = await f.Controller.CreateFollowUp(f.Academy, invoice.Id, new(null, null, null, null), default);
        if (collected) Assert.IsType<ConflictObjectResult>(followUp); else Assert.IsType<OkObjectResult>(followUp);
        Assert.Equal(collected ? 0 : 1, await f.Db.AdminWorkItems.CountAsync());
    }

    [Fact]
    public async Task One_invoice_overpayment_cannot_hide_another_invoice_or_currency_debt()
    {
        await using var f = new Fixture(); var overpaid = f.Invoice("INR", 100m); f.Payment(overpaid, 150m, "Completed");
        f.Invoice("INR", 80m); f.Invoice("USD", 20m); await f.Db.SaveChangesAsync(); var result = await f.Summary();
        Assert.Equal(80m, Groups(result)["INR"].GetProperty("outstanding").GetDecimal()); Assert.Equal(20m, Groups(result)["USD"].GetProperty("outstanding").GetDecimal());
    }

    [Theory] [InlineData("Cancelled")] [InlineData("cancelled")] [InlineData("CANCELLED")]
    public async Task Cancellation_preserves_gross_history_but_never_becomes_receivable(string status)
    {
        await using var f = new Fixture(); var invoice = f.Invoice("INR", 100m); invoice.Status = status; await f.Db.SaveChangesAsync(); var result = await f.Summary();
        Assert.Equal(100m, result.GetProperty("grossBilled").GetDecimal()); Assert.Equal(0m, result.GetProperty("outstanding").GetDecimal());
        Assert.Equal(0, result.GetProperty("overdueInvoices").GetInt32()); Assert.IsType<ConflictObjectResult>(await f.Controller.CreateFollowUp(f.Academy, invoice.Id, new(null, null, null, null), default));
    }

    [Fact]
    public async Task Money_in_a_different_currency_cannot_offset_the_invoice()
    {
        await using var f = new Fixture(); var invoice = f.Invoice("USD", 100m); f.Payment(invoice, 50m, "Reconciled", "INR"); await f.Db.SaveChangesAsync(); var result = await f.Summary();
        var groups = Groups(result); Assert.Equal(50m, groups["INR"].GetProperty("collected").GetDecimal()); Assert.Equal(100m, groups["USD"].GetProperty("outstanding").GetDecimal());
        var queue = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>((await f.Controller.Collections(f.Academy, default)).Result).Value);
        Assert.Equal(100m, Assert.Single(queue).Balance); Assert.IsType<OkObjectResult>(await f.Controller.CreateFollowUp(f.Academy, invoice.Id, new(null, null, null, null), default));
    }

    [Fact]
    public async Task Currency_case_and_whitespace_are_grouped_without_inventing_a_rate()
    {
        await using var f = new Fixture(); var invoice = f.Invoice(" usd ", 100m); f.Payment(invoice, 20m, "Completed", "USD"); await f.Db.SaveChangesAsync();
        var result = await f.Summary(); Assert.Equal("USD", result.GetProperty("currency").GetString()); Money(result, 100m, 0m, 20m, 80m, 0m);
        var queue = Assert.IsType<List<CollectionInvoiceSummary>>(Assert.IsType<OkObjectResult>((await f.Controller.Collections(f.Academy, default)).Result).Value); Assert.Equal(80m, Assert.Single(queue).Balance);
    }

    [Fact]
    public async Task Empty_academy_has_no_currency_and_zero_legacy_amounts()
    {
        await using var f = new Fixture(); var result = await f.Summary(); Assert.Empty(Groups(result)); Assert.Equal(JsonValueKind.Null, result.GetProperty("currency").ValueKind);
        Assert.False(result.GetProperty("hasMixedCurrencies").GetBoolean()); Money(result, 0, 0, 0, 0, 0);
    }

    [Theory]
    [InlineData("AcademyAdmin", true)] [InlineData("AcademyAdmin", false)]
    [InlineData("FinanceUser", true)] [InlineData("FinanceUser", false)]
    public async Task Actual_http_serialization_is_currency_safe_and_tenant_scoped(string role, bool mixed)
    {
        using var factory = new HttpFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        var academy = await SeedHttp(factory, role, true, mixed); var path = $"/api/academies/{academy}/finance-governance/summary";
        using (var denied = await client.GetAsync(path)) Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await Login(client); string before; await using (var scope = factory.Services.CreateAsyncScope()) before = await HttpSnapshot(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        using (var response = await client.GetAsync(path))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var result = body.RootElement;
            Assert.Equal(mixed, result.GetProperty("hasMixedCurrencies").GetBoolean()); Assert.Equal(mixed ? 2 : 1, Groups(result).Count);
            if (mixed) Assert.Equal(JsonValueKind.Null, result.GetProperty("grossBilled").ValueKind);
            else { Assert.Equal("USD", result.GetProperty("currency").GetString()); Assert.Equal(100m, result.GetProperty("grossBilled").GetDecimal()); }
        }
        using (var denied = await client.GetAsync($"/api/academies/{Guid.NewGuid()}/finance-governance/summary")) Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await using var verify = factory.Services.CreateAsyncScope(); var db = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); Assert.Equal(before, await HttpSnapshot(db)); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("Teacher", true)] [InlineData("FinanceUser", false)]
    public async Task Missing_permission_or_subscription_denies_without_money_or_audit_changes(string role, bool module)
    {
        using var factory = new HttpFactory(); using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false }); var academy = await SeedHttp(factory, role, module, true); await Login(client);
        string before; await using (var scope = factory.Services.CreateAsyncScope()) before = await HttpSnapshot(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        using var denied = await client.GetAsync($"/api/academies/{academy}/finance-governance/summary"); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await using var verify = factory.Services.CreateAsyncScope(); var db = verify.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); Assert.Equal(before, await HttpSnapshot(db)); Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    private static async Task<string> HttpSnapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
    private static async Task<Guid> SeedHttp(HttpFactory factory, string role, bool module, bool mixed)
    {
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var academy = Guid.NewGuid();
        db.Add(new Academy { Id = academy, Name = "Synthetic currency academy", EnabledModulesJson = module ? "[\"Core\",\"FinanceControls\"]" : "[\"Core\"]" });
        db.Add(new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "HTTP-USD", Currency = "USD", TotalAmount = 100m });
        if (mixed) db.Add(new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = "HTTP-INR", Currency = "INR", TotalAmount = 1000m }); await db.SaveChangesAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { UserName = "currency-actor@example.invalid", Email = "currency-actor@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic currency actor", AcademyId = academy };
        Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded); return academy;
    }
    private static async Task Login(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "currency-actor@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());
    }

    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"finance-currency-http-{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing"); builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            { ["Database:ApplyMigrationsOnStartup"] = "false", ["Bootstrap:PlatformOwnerEmail"] = "", ["Bootstrap:PlatformOwnerPassword"] = "", ["MediaStorage:Enabled"] = "false", ["Penta:Enabled"] = "false" }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AcademyDeskDbContext>>(); services.RemoveAll<DbContextOptions<IdentityDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>(); services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>(); services.RemoveAll<AcademyDeskDbContext>(); services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<AcademyDeskDbContext>(o => o.UseInMemoryDatabase(database + "-domain")); services.AddDbContext<IdentityDbContext>(o => o.UseInMemoryDatabase(database + "-identity"));
            });
        }
    }

    private static Dictionary<string, JsonElement> Groups(JsonElement json) => json.GetProperty("byCurrency").EnumerateArray().ToDictionary(x => x.GetProperty("currency").GetString()!, x => x);
    private static void Money(JsonElement x, decimal gross, decimal adjusted, decimal collected, decimal outstanding, decimal reconciled)
    { Assert.Equal(gross, x.GetProperty("grossBilled").GetDecimal()); Assert.Equal(adjusted, x.GetProperty("approvedAdjustments").GetDecimal()); Assert.Equal(collected, x.GetProperty("collected").GetDecimal()); Assert.Equal(outstanding, x.GetProperty("outstanding").GetDecimal()); Assert.Equal(reconciled, x.GetProperty("reconciled").GetDecimal()); }
    private sealed class Fixture : IAsyncDisposable
    {
        public Guid Academy = Guid.NewGuid();
        public AcademyDeskDbContext Db = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public IdentityDbContext Identity = new(new DbContextOptionsBuilder<IdentityDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public FinanceGovernanceController Controller => new(Db, Identity);
        public Invoice Invoice(string currency, decimal amount, decimal adjusted = 0m) { var x = new Invoice { AcademyId = Academy, StudentId = Guid.NewGuid(), InvoiceNumber = Guid.NewGuid().ToString(), Currency = currency, TotalAmount = amount, AdjustedAmount = adjusted, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) }; Db.Add(x); return x; }
        public void Payment(Invoice invoice, decimal amount, string status, string? currency = null) => Db.Add(new Payment { AcademyId = Academy, InvoiceId = invoice.Id, Amount = amount, Status = status, Currency = currency ?? invoice.Currency });
        public async Task<JsonElement> Summary() => JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await Controller.Summary(Academy, default)).Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        public async Task<string> Snapshot() => JsonSerializer.Serialize(new { invoices = await Db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await Db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await Identity.DisposeAsync(); }
    }
}
