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

public sealed class InvoiceCurrencyReadbackTests
{
    [Theory]
    [InlineData("AcademyAdmin", "USD")] [InlineData("AcademyAdmin", " usd ")]
    [InlineData("Owner", "USD")] [InlineData("Owner", " usd ")]
    [InlineData("FinanceUser", "USD")] [InlineData("FinanceUser", " usd ")]
    public async Task Invoice_http_balance_counts_only_its_own_currency_and_preserves_raw_metadata(string role, string currency)
    {
        using var f = new Fixture(); await f.Seed(role);
        await f.Change(db => {
            var bill = Bill(db, f.Academy, currency, 100.55m, .05m); Pay(db, bill, 20.10m, "Reconciled", " USD ");
            Pay(db, bill, 50m, "Completed", "INR"); Pay(db, bill, 500m, "Pending", "USD");
            db.Add(new Payment { AcademyId = Guid.NewGuid(), InvoiceId = bill.Id, Amount = 500m, Currency = "USD" });
            Bill(db, Guid.NewGuid(), "EUR", 9999m);
        });
        var row = Assert.Single((await f.Get()).EnumerateArray());
        Assert.Equal(currency, row.GetProperty("currency").GetString()); Assert.Equal(100.55m, row.GetProperty("totalAmount").GetDecimal()); Assert.Equal(.05m, row.GetProperty("adjustedAmount").GetDecimal());
        Assert.Equal(20.10m, row.GetProperty("paidAmount").GetDecimal()); Assert.Equal(80.40m, row.GetProperty("balance").GetDecimal());
        Assert.Equal("Issued", row.GetProperty("status").GetString());
        var groups = (await f.Finance()).GetProperty("byCurrency").EnumerateArray().ToDictionary(x => x.GetProperty("currency").GetString()!);
        Assert.Equal(row.GetProperty("balance").GetDecimal(), groups["USD"].GetProperty("outstanding").GetDecimal()); Assert.Equal(50m, groups["INR"].GetProperty("collected").GetDecimal()); await f.Unchanged();
    }
    [Theory] [InlineData("Completed", true)] [InlineData("Reconciled", true)] [InlineData("Pending", false)] [InlineData("Processing", false)] [InlineData("Failed", false)] [InlineData("Voided", false)]
    public async Task Invoice_http_payment_states_retain_the_accepted_collection_contract(string status, bool collected)
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => Pay(db, Bill(db, f.Academy, "EUR", 100.55m, .05m), 20.10m, status));
        var row = Assert.Single((await f.Get()).EnumerateArray()); Assert.Equal(collected ? 20.10m : 0m, row.GetProperty("paidAmount").GetDecimal()); Assert.Equal(collected ? 80.40m : 100.50m, row.GetProperty("balance").GetDecimal()); await f.Unchanged();
    }
    [Theory] [InlineData("Cancelled")] [InlineData("cancelled")] [InlineData("CANCELLED")]
    public async Task Invoice_history_retains_cancelled_rows_and_the_unmodified_balance_DTO(string status)
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => Bill(db, f.Academy, "USD", 100m).Status = status);
        var row = Assert.Single((await f.Get()).EnumerateArray()); Assert.Equal(status, row.GetProperty("status").GetString()); Assert.Equal(100m, row.GetProperty("balance").GetDecimal());
        Assert.Equal(0m, (await f.Finance()).GetProperty("outstanding").GetDecimal()); await f.Unchanged();
    }
    [Fact] public async Task Overpayment_is_clamped_per_invoice_and_cannot_hide_another_student_balance()
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => { Pay(db, Bill(db, f.Academy, "USD", 100m), 150m, "Completed"); Bill(db, f.Academy, "USD", 80.15m); });
        var rows = (await f.Get()).EnumerateArray().ToArray(); Assert.Equal(2, rows.Length); Assert.Contains(rows, x => x.GetProperty("balance").GetDecimal() == 0m); Assert.Contains(rows, x => x.GetProperty("balance").GetDecimal() == 80.15m);
        Assert.Equal(80.15m, (await f.Finance()).GetProperty("outstanding").GetDecimal()); await f.Unchanged();
    }
    [Fact] public async Task Empty_invoice_history_stays_empty_and_read_only() { using var f = new Fixture(); await f.Seed(); Assert.Empty((await f.Get()).EnumerateArray()); await f.Unchanged(); }
    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Sales")]
    public async Task Existing_permission_denials_remain_enforced(string role)
    { using var f = new Fixture(); await f.Seed(role); using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); await f.Unchanged(); }
    [Fact] public async Task Anonymous_and_foreign_tenant_requests_are_denied_without_money_or_audit_changes()
    {
        using var f = new Fixture(); await f.Seed(); f.Client.DefaultRequestHeaders.Authorization = null;
        using (var r = await f.Client.GetAsync(f.Path)) Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        await f.Login(); using (var r = await f.Client.GetAsync($"/api/academies/{Guid.NewGuid()}/invoices")) Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); await f.Unchanged();
    }
    [Fact] public async Task Missing_Finance_module_remains_denied()
    {
        using var f = new Fixture(); await f.Seed("FinanceUser"); await f.Change(db => db.Academies.Single(x => x.Id == f.Academy).EnabledModulesJson = "[\"Core\",\"FinanceControls\"]");
        using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); await f.Unchanged();
    }
    private static Invoice Bill(AcademyDeskDbContext db, Guid academy, string currency, decimal total, decimal adjusted = 0m)
    { var i = new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = Guid.NewGuid().ToString(), Currency = currency, TotalAmount = total, AdjustedAmount = adjusted }; db.Add(i); return i; }
    private static void Pay(AcademyDeskDbContext db, Invoice i, decimal amount, string status, string? currency = null) => db.Add(new Payment { AcademyId = i.AcademyId, InvoiceId = i.Id, Amount = amount, Currency = currency ?? i.Currency, Status = status });
    private sealed class Fixture : IDisposable
    {
        private readonly HttpFactory factory = new(); public HttpClient Client { get; } public Guid Academy = Guid.NewGuid(); public string Path => $"/api/academies/{Academy}/invoices"; private string? snapshot;
        public Fixture() => Client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string role = "AcademyAdmin")
        {
            await Change(db => db.Add(new Academy { Id = Academy, Name = "Synthetic dashboard academy", EnabledModulesJson = "[\"Core\",\"Finance\",\"FinanceControls\"]" }));
            await using var scope = factory.Services.CreateAsyncScope(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { UserName = "invoice-read-actor@example.invalid", Email = "invoice-read-actor@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic dashboard actor", AcademyId = Academy };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded); await Login();
        }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "invoice-read-actor@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task Change(Action<AcademyDeskDbContext> action) { await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); action(db); await db.SaveChangesAsync(); snapshot = await Snapshot(db); }
        private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audit = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        public async Task<JsonElement> Get() { using var r = await Client.GetAsync(Path); Assert.Equal(HttpStatusCode.OK, r.StatusCode); return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone(); }
        public async Task<JsonElement> Finance() { using var r = await Client.GetAsync($"/api/academies/{Academy}/finance-governance/summary"); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task Unchanged() { await using var scope = factory.Services.CreateAsyncScope(); Assert.Equal(snapshot, await Snapshot(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>())); }
        public void Dispose() { Client.Dispose(); factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"invoice-read-currency-http-{Guid.NewGuid():N}";
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

}
