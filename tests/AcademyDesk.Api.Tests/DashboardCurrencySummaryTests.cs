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

public sealed class DashboardCurrencySummaryTests
{
    [Theory] [InlineData("AcademyAdmin")] [InlineData("Owner")]
    public async Task Mixed_http_dashboard_matches_finance_groups_without_cross_unit_scalars(string role)
    {
        using var f = new Fixture(); await f.Seed(role);
        await f.Change(db => {
            var inr = Bill(db, f.Academy, "INR", 1000m, 200m); var usd = Bill(db, f.Academy, "USD", 100m, 10m);
            Pay(db, inr, 300m, "Completed"); Pay(db, inr, 100m, "Reconciled"); Pay(db, usd, 50m, "Reconciled");
            Bill(db, Guid.NewGuid(), "EUR", 9999m);
            db.Add(new Payment { AcademyId = Guid.NewGuid(), InvoiceId = inr.Id, Amount = 9999m });
        });
        var result = await f.Get();
        foreach (var key in new[] { "totalInvoiced", "totalPaid", "outstandingBalance", "currency" }) Assert.Equal(JsonValueKind.Null, result.GetProperty(key).ValueKind);
        Assert.True(result.GetProperty("hasMixedCurrencies").GetBoolean());
        var groups = Groups(result); Assert.Equal(new[] { "INR", "USD" }, groups.Keys);
        Assert.Equal(400m, groups["INR"].GetProperty("outstanding").GetDecimal()); Assert.Equal(40m, groups["USD"].GetProperty("outstanding").GetDecimal());
        await f.AgreeWithFinance(result); await f.Unchanged();
    }
    [Theory] [InlineData("INR")] [InlineData("USD")] [InlineData("EUR")] [InlineData(" usd ")]
    public async Task Single_currency_http_retains_exact_cents_and_explicit_unit(string currency)
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => { var i = Bill(db, f.Academy, currency, 100.55m, .05m); Pay(db, i, 10.25m, "Completed"); Pay(db, i, 20.10m, "Reconciled"); });
        var r = await f.Get(); Assert.Equal(currency.Trim().ToUpperInvariant(), r.GetProperty("currency").GetString()); Assert.False(r.GetProperty("hasMixedCurrencies").GetBoolean());
        Money(r, 100.55m, 30.35m, 70.15m); await f.AgreeWithFinance(r); await f.Unchanged();
    }
    [Theory] [InlineData("Completed", true)] [InlineData("Reconciled", true)] [InlineData("Pending", false)] [InlineData("Processing", false)] [InlineData("Failed", false)] [InlineData("Voided", false)]
    public async Task Collection_status_is_shared_with_finance(string status, bool paid)
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => Pay(db, Bill(db, f.Academy, "USD", 100m), 70m, status));
        var r = await f.Get(); Money(r, 100m, paid ? 70m : 0m, paid ? 30m : 100m); await f.AgreeWithFinance(r); await f.Unchanged();
    }
    [Theory] [InlineData("Cancelled")] [InlineData("cancelled")] [InlineData("CANCELLED")]
    public async Task Cancelled_bill_preserves_gross_history_without_becoming_receivable(string status)
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => Bill(db, f.Academy, "USD", 100m).Status = status);
        var r = await f.Get(); Money(r, 100m, 0m, 0m); await f.AgreeWithFinance(r); await f.Unchanged();
    }
    [Fact] public async Task Overpayment_cannot_hide_another_student_balance()
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => { Pay(db, Bill(db, f.Academy, "INR", 100m), 150m, "Completed"); Bill(db, f.Academy, "INR", 80.15m); });
        var r = await f.Get(); Money(r, 180.15m, 150m, 80.15m); await f.AgreeWithFinance(r); await f.Unchanged();
    }
    [Fact] public async Task Different_payment_currency_does_not_reduce_the_invoice_balance()
    {
        using var f = new Fixture(); await f.Seed(); await f.Change(db => Pay(db, Bill(db, f.Academy, "USD", 100m), 50m, "Reconciled", "INR"));
        var r = await f.Get(); Assert.Equal(100m, Groups(r)["USD"].GetProperty("outstanding").GetDecimal()); Assert.Equal(50m, Groups(r)["INR"].GetProperty("collected").GetDecimal());
        await f.AgreeWithFinance(r); await f.Unchanged();
    }
    [Fact] public async Task Empty_academy_has_no_invented_currency_and_zero_compatibility_values()
    {
        using var f = new Fixture(); await f.Seed(); var r = await f.Get(); Money(r, 0, 0, 0); Assert.Empty(Groups(r)); Assert.Equal(JsonValueKind.Null, r.GetProperty("currency").ValueKind); await f.Unchanged();
    }
    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("FinanceUser")]
    public async Task Existing_dashboard_role_denial_is_preserved(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Change(db => Bill(db, f.Academy, "USD", 100m));
        using var r = await f.Client.GetAsync(f.Path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); await f.Unchanged();
    }
    [Fact] public async Task Anonymous_and_foreign_tenant_requests_are_denied_without_writes()
    {
        using var f = new Fixture(); await f.Seed(); f.Client.DefaultRequestHeaders.Authorization = null;
        using (var r = await f.Client.GetAsync(f.Path)) Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        await f.Login(); using (var r = await f.Client.GetAsync($"/api/academies/{Guid.NewGuid()}/dashboard")) Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); await f.Unchanged();
    }
    [Fact] public async Task Nonfinancial_counts_attendance_timezone_schedule_and_activity_are_preserved()
    {
        using var f = new Fixture(); await f.Seed(); var now = DateTime.UtcNow; var start = now.Date.AddHours(12); Guid session = default;
        await f.Change(db => {
            db.Academies.Single(x => x.Id == f.Academy).TimeZone = "UTC";
            db.Add(new Student { AcademyId = f.Academy, FirstName = "Active", LastName = "Student" });
            db.Add(new Student { AcademyId = f.Academy, FirstName = "Inactive", LastName = "Student", IsActive = false });
            db.Add(new Student { AcademyId = Guid.NewGuid(), FirstName = "Foreign", LastName = "Student" });
            var teacher = new Teacher { AcademyId = f.Academy, FirstName = "Synthetic", LastName = "Teacher" }; db.Add(teacher);
            var batch = new Batch { AcademyId = f.Academy, Name = "Synthetic batch" }; db.Add(batch);
            var s = new ClassSession { AcademyId = f.Academy, BatchId = batch.Id, TeacherId = teacher.Id, StartUtc = start, EndUtc = start.AddHours(1), RoomName = "Room A" }; session = s.Id; db.Add(s);
            db.Add(new ClassSession { AcademyId = f.Academy, BatchId = batch.Id, StartUtc = start, EndUtc = start.AddHours(1), Status = "Cancelled" });
            foreach (var status in new[] { "Present", "Online", "Absent" }) db.Add(new AttendanceRecord { AcademyId = f.Academy, Status = status });
            db.Add(new AttendanceRecord { AcademyId = f.Academy, MarkedAtUtc = now.AddDays(-40) });
            db.Add(new Lead { AcademyId = f.Academy, FullName = "Open lead" }); db.Add(new Lead { AcademyId = f.Academy, FullName = "Lost lead", Stage = "Lost" });
            db.Add(new AuditLog { AcademyId = f.Academy, Action = "Synthetic readback", EntityType = "Fixture" });
        });
        var r = await f.Get(); Assert.Equal(1, r.GetProperty("activeStudents").GetInt32()); Assert.Equal(1, r.GetProperty("activeTeachers").GetInt32()); Assert.Equal(1, r.GetProperty("activeBatches").GetInt32()); Assert.Equal(1, r.GetProperty("openLeads").GetInt32());
        Assert.Equal(3, r.GetProperty("attendanceRecordsLast30Days").GetInt32()); Assert.Equal(2, r.GetProperty("presentAttendanceLast30Days").GetInt32()); Assert.Equal(1, r.GetProperty("classesToday").GetInt32());
        var row = Assert.Single(r.GetProperty("todaySchedule").EnumerateArray()); Assert.Equal(session, row.GetProperty("id").GetGuid()); Assert.Equal("Synthetic Teacher", row.GetProperty("teacherName").GetString()); Assert.Equal("Room A", row.GetProperty("roomName").GetString());
        Assert.Equal("Synthetic readback", Assert.Single(r.GetProperty("recentActivity").EnumerateArray()).GetProperty("action").GetString()); await f.Unchanged();
    }
    private static Invoice Bill(AcademyDeskDbContext db, Guid academy, string currency, decimal total, decimal adjusted = 0m)
    { var i = new Invoice { AcademyId = academy, StudentId = Guid.NewGuid(), InvoiceNumber = Guid.NewGuid().ToString(), Currency = currency, TotalAmount = total, AdjustedAmount = adjusted }; db.Add(i); return i; }
    private static void Pay(AcademyDeskDbContext db, Invoice i, decimal amount, string status, string? currency = null) => db.Add(new Payment { AcademyId = i.AcademyId, InvoiceId = i.Id, Amount = amount, Currency = currency ?? i.Currency, Status = status });
    private static Dictionary<string, JsonElement> Groups(JsonElement r) => r.GetProperty("byCurrency").EnumerateArray().ToDictionary(x => x.GetProperty("currency").GetString()!, x => x);
    private static void Money(JsonElement r, decimal billed, decimal paid, decimal outstanding) { Assert.Equal(billed, r.GetProperty("totalInvoiced").GetDecimal()); Assert.Equal(paid, r.GetProperty("totalPaid").GetDecimal()); Assert.Equal(outstanding, r.GetProperty("outstandingBalance").GetDecimal()); }
    private sealed class Fixture : IDisposable
    {
        private readonly HttpFactory factory = new(); public HttpClient Client { get; } public Guid Academy = Guid.NewGuid(); public string Path => $"/api/academies/{Academy}/dashboard"; private string? snapshot;
        public Fixture() => Client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string role = "AcademyAdmin")
        {
            await Change(db => db.Add(new Academy { Id = Academy, Name = "Synthetic dashboard academy", EnabledModulesJson = "[\"Core\",\"FinanceControls\"]" }));
            await using var scope = factory.Services.CreateAsyncScope(); var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { UserName = "dashboard-actor@example.invalid", Email = "dashboard-actor@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic dashboard actor", AcademyId = Academy };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded); await Login();
        }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "dashboard-actor@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task Change(Action<AcademyDeskDbContext> action) { await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); action(db); await db.SaveChangesAsync(); snapshot = await Snapshot(db); }
        private static async Task<string> Snapshot(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audit = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        public async Task<JsonElement> Get() { using var r = await Client.GetAsync(Path); Assert.Equal(HttpStatusCode.OK, r.StatusCode); return JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone(); }
        public async Task AgreeWithFinance(JsonElement dashboard) { using var r = await Client.GetAsync($"/api/academies/{Academy}/finance-governance/summary"); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Assert.Equal(json.RootElement.GetProperty("byCurrency").GetRawText(), dashboard.GetProperty("byCurrency").GetRawText()); }
        public async Task Unchanged() { await using var scope = factory.Services.CreateAsyncScope(); Assert.Equal(snapshot, await Snapshot(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>())); }
        public void Dispose() { Client.Dispose(); factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"dashboard-currency-http-{Guid.NewGuid():N}";
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
