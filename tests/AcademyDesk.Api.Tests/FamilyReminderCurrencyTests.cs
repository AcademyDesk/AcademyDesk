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

public sealed class FamilyReminderCurrencyTests
{
    [Theory]
    [InlineData("Student", "Completed", true)] [InlineData("Student", "Reconciled", true)] [InlineData("Student", "Pending", false)] [InlineData("Student", "Processing", false)] [InlineData("Student", "Failed", false)] [InlineData("Student", "Voided", false)]
    [InlineData("Guardian", "Completed", true)] [InlineData("Guardian", "Reconciled", true)] [InlineData("Guardian", "Pending", false)] [InlineData("Guardian", "Processing", false)] [InlineData("Guardian", "Failed", false)] [InlineData("Guardian", "Voided", false)]
    public async Task Family_detail_and_document_match_only_accepted_own_tenant_invoice_currency(string role, string status, bool collected)
    {
        using var f = new Fixture(); await f.Seed(role); Invoice? bill = null;
        await f.Change(db => { bill = f.Bill(db, "USD", 100.55m, .05m); Pay(db, bill, 20.10m, status, " usd "); Pay(db, bill, 70.15m, "Completed", "INR"); Pay(db, bill, 500m, "Completed", "USD", f.Foreign); });
        await f.Login(); var before = await f.Snapshot(); var details = await f.Details(); var row = Assert.Single(details.GetProperty("invoices").EnumerateArray());
        Assert.Equal(collected ? 80.40m : 100.50m, row.GetProperty("balance").GetDecimal()); Assert.Equal("USD", row.GetProperty("currency").GetString());
        var html = await f.Document(bill!.Id); Assert.Contains($"Paid: USD {(collected ? 20.10m : 0m):0.00}", html); Assert.Contains($"Balance: USD {(collected ? 80.40m : 100.50m):0.00}", html); Assert.Equal(before, await f.Snapshot());
    }
    [Theory] [InlineData("FinanceUser")] [InlineData("Owner")] [InlineData("AcademyAdmin")]
    public async Task Reminder_uses_exact_same_currency_balance_and_preserves_ledger_and_actor_audit(string role)
    {
        using var f = new Fixture(); await f.Seed(role); Invoice? bill = null;
        await f.Change(db => { bill = f.Bill(db, "USD", 100.55m, .05m); Pay(db, bill, 20.10m, "Completed", " usd "); Pay(db, bill, 10.25m, "Reconciled", "USD"); Pay(db, bill, 70.15m, "Reconciled", "INR"); Pay(db, bill, 500m, "Completed", "USD", f.Foreign); });
        await f.Login(); var before = await f.Snapshot(false); var queued = await f.Queue(bill!.Id); Assert.Equal(1, queued.GetProperty("queuedCount").GetInt32());
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var n = Assert.Single(await db.Notifications.ToListAsync());
        Assert.Equal(f.Academy, n.AcademyId); Assert.Equal(f.Student, n.RecipientId); Assert.Equal("InApp", n.Channel); Assert.Null(n.SentAtUtc); Assert.Contains("USD 70.15", n.Message);
        var audit = Assert.Single(await db.AuditLogs.Where(x => x.ActorUserId != null).ToListAsync()); Assert.Equal(f.Actor, audit.ActorUserId); Assert.Equal(before, await f.Snapshot(false));
    }
    [Theory] [InlineData("Completed", true)] [InlineData("Reconciled", true)] [InlineData("Pending", false)] [InlineData("Processing", false)] [InlineData("Failed", false)] [InlineData("Voided", false)]
    public async Task Reminder_payment_states_agree_with_the_existing_read_contract(string status, bool collected)
    {
        using var f = new Fixture(); await f.Seed("FinanceUser"); Invoice? bill = null; await f.Change(db => { bill = f.Bill(db, "USD", 100m); Pay(db, bill, 20.10m, status, " usd "); Pay(db, bill, 100m, "Completed", "INR"); }); await f.Login();
        Assert.Equal(1, (await f.Queue(bill!.Id)).GetProperty("queuedCount").GetInt32()); await using var scope = f.Factory.Services.CreateAsyncScope(); Assert.Contains(collected ? "USD 79.90" : "USD 100.00", Assert.Single(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Notifications.ToListAsync()).Message);
    }
    [Theory] [InlineData("Cancelled")] [InlineData("cancelled")] [InlineData("CANCELLED")]
    public async Task Cancelled_history_is_preserved_while_the_document_labels_zero_collectible_amount(string status)
    {
        using var f = new Fixture(); await f.Seed(); Invoice? bill = null; await f.Change(db => { bill = f.Bill(db, "USD", 100m); bill.Status = status; }); await f.Login(); var before = await f.Snapshot();
        var row = Assert.Single((await f.Details()).GetProperty("invoices").EnumerateArray()); Assert.Equal(status, row.GetProperty("status").GetString()); Assert.Equal(100m, row.GetProperty("balance").GetDecimal());
        var html = await f.Document(bill!.Id); Assert.Contains("Balance before cancellation: USD 100.00", html); Assert.Contains("Amount due: USD 0.00", html); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Downloaded_HTML_encodes_legacy_currency_text_instead_of_executing_it()
    {
        using var f = new Fixture(); await f.Seed(); Invoice? bill = null; await f.Change(db => bill = f.Bill(db, "<script>bad()</script>", 100m)); await f.Login(); var before = await f.Snapshot();
        var html = await f.Document(bill!.Id); Assert.DoesNotContain("<script>", html); Assert.Contains("&lt;script&gt;bad()&lt;/script&gt;", html); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Guardian_finance_denial_retains_empty_finance_projection_and_refuses_document()
    {
        using var f = new Fixture(); await f.Seed("Guardian", finance: false); Invoice? bill = null; await f.Change(db => bill = f.Bill(db, "USD", 100m)); await f.Login(); var before = await f.Snapshot();
        Assert.Empty((await f.Details()).GetProperty("invoices").EnumerateArray()); using var r = await f.Client.GetAsync(f.DocPath(bill!.Id)); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Revoked_parent_link_refuses_both_details_and_document()
    {
        using var f = new Fixture(); await f.Seed("Guardian"); Invoice? bill = null; await f.Change(db => { bill = f.Bill(db, "USD", 100m); db.StudentGuardians.Single().AccessRevokedAtUtc = DateTime.UtcNow; }); await f.Login(); var before = await f.Snapshot();
        foreach (var path in new[] { f.DetailPath, f.DocPath(bill!.Id) }) { using var r = await f.Client.GetAsync(path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); } Assert.Equal(before, await f.Snapshot());
    }
    [Theory] [InlineData("Student", false)] [InlineData("Guardian", false)] [InlineData("Student", true)] [InlineData("Guardian", true)]
    public async Task Unlinked_or_foreign_student_and_invoice_cannot_be_read(string role, bool foreign)
    {
        using var f = new Fixture(); await f.Seed(role); var other = Guid.NewGuid(); Invoice? bill = null;
        await f.Change(db => { db.Add(new Student { Id = other, AcademyId = foreign ? f.Foreign : f.Academy, FirstName = "Other", LastName = "Student" }); bill = f.Bill(db, "USD", 100m, student: other, academy: foreign ? f.Foreign : f.Academy); }); await f.Login(); var before = await f.Snapshot();
        foreach (var path in new[] { $"/api/portal/students/{other}", $"/api/portal/students/{other}/invoices/{bill!.Id}/download" }) { using var r = await f.Client.GetAsync(path); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); } Assert.Equal(before, await f.Snapshot());
    }
    [Fact] public async Task Anonymous_family_reads_are_unauthorized()
    { using var f = new Fixture(); await f.Seed(); foreach (var path in new[] { f.DetailPath, f.DocPath(Guid.NewGuid()) }) { using var r = await f.Client.GetAsync(path); Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); } }
    [Theory] [InlineData("Student")] [InlineData("Guardian")] [InlineData("Teacher")]
    public async Task Unprivileged_role_cannot_queue_financial_reminders(string role)
    { using var f = new Fixture(); await f.Seed(role); await f.Login(); var before = await f.Snapshot(); using var r = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { channel = "InApp" }); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Foreign_academy_cannot_queue_a_reminder()
    { using var f = new Fixture(); await f.Seed("FinanceUser"); await f.Login(); var before = await f.Snapshot(); using var r = await f.Client.PostAsJsonAsync($"/api/academies/{f.Foreign}/fee-reminders", new { channel = "InApp" }); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Empty_family_history_stays_empty_without_read_side_writes()
    { using var f = new Fixture(); await f.Seed(); await f.Login(); var before = await f.Snapshot(); Assert.Empty((await f.Details()).GetProperty("invoices").EnumerateArray()); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Overpayment_never_offsets_another_invoice_balance()
    { using var f = new Fixture(); await f.Seed(); await f.Change(db => { Pay(db, f.Bill(db, "USD", 100m), 150m, "Completed", "USD"); f.Bill(db, "USD", 80.15m); }); await f.Login(); var rows = (await f.Details()).GetProperty("invoices").EnumerateArray().ToArray(); Assert.Equal(80.15m, rows.Sum(x => x.GetProperty("balance").GetDecimal())); }
    private static void Pay(AcademyDeskDbContext db, Invoice i, decimal amount, string status, string currency, Guid? academy = null) => db.Add(new Payment { AcademyId = academy ?? i.AcademyId, InvoiceId = i.Id, Amount = amount, Status = status, Currency = currency });
    private sealed class Fixture : IDisposable
    {
        public HttpFactory Factory = new(); public HttpClient Client { get; } public Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Actor = Guid.NewGuid();
        public string DetailPath => $"/api/portal/students/{Student}"; public string DocPath(Guid invoice) => DetailPath + $"/invoices/{invoice}/download";
        public Fixture() => Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string role = "Student", bool finance = true)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(new Academy { Id = Academy, Name = "Synthetic currency academy", EnabledModulesJson = "[\"Core\",\"Finance\",\"FinanceControls\"]" }); db.Add(new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Guardian" });
            db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true, CanViewFinance = finance }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = true })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { Id = Actor, UserName = "family-currency@example.invalid", Email = "family-currency@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic family actor", AcademyId = Academy, StudentId = role == "Student" ? Student : null, GuardianId = role == "Guardian" ? Guardian : null };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        public Invoice Bill(AcademyDeskDbContext db, string currency, decimal total, decimal adjusted = 0, Guid? student = null, Guid? academy = null) { var i = new Invoice { AcademyId = academy ?? Academy, StudentId = student ?? Student, InvoiceNumber = $"FAMILY-{Guid.NewGuid():N}", TotalAmount = total, AdjustedAmount = adjusted, Currency = currency, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) }; db.Add(i); return i; }
        public async Task Change(Action<AcademyDeskDbContext> change) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); change(db); await db.SaveChangesAsync(); }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "family-currency@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task<JsonElement> Details() { using var r = await Client.GetAsync(DetailPath); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Document(Guid invoice) { using var r = await Client.GetAsync(DocPath(invoice)); Assert.Equal(HttpStatusCode.OK, r.StatusCode); return await r.Content.ReadAsStringAsync(); }
        public async Task<JsonElement> Queue(Guid invoice) { using var r = await Client.PostAsJsonAsync($"/api/academies/{Academy}/fee-reminders", new { invoiceId = invoice, channel = "InApp" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Snapshot(bool audit = true) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audit = audit ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null }); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"family-reminder-currency-{Guid.NewGuid():N}";
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
