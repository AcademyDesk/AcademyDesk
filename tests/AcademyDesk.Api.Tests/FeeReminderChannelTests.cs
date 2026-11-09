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

public sealed class FeeReminderChannelTests
{
    public static IEnumerable<object?[]> Accepted()
    { foreach (var role in new[] { "FinanceUser", "AcademyAdmin", "Owner" }) foreach (var channel in new string?[] { null, "InApp", " inapp ", "INAPP" }) yield return new object?[] { role, channel }; }
    public static IEnumerable<object[]> Rejected()
    { foreach (var role in new[] { "FinanceUser", "AcademyAdmin", "Owner" }) foreach (var channel in new[] { "Email", "WhatsApp", "", "Bogus" }) yield return new object[] { role, channel }; }
    [Theory] [MemberData(nameof(Accepted))]
    public async Task Supported_channel_defaults_and_normalization_only_create_owned_InApp_notice(string role, string? channel)
    {
        using var f = new Fixture(); await f.Seed(role); Invoice? invoice = null; await f.Change(db => invoice = f.Bill(db, "USD", 100.55m, .05m)); await f.Login(); var before = await f.Snapshot(false, false);
        using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { invoiceId = invoice!.Id, channel }); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var notice = Assert.Single(await db.Notifications.ToListAsync()); Assert.Equal("InApp", notice.Channel); Assert.Equal(f.Academy, notice.AcademyId); Assert.Equal(f.Student, notice.RecipientId); Assert.Null(notice.SentAtUtc); Assert.Contains("USD 100.50", notice.Message); Assert.Single(await db.AuditLogs.Where(x => x.ActorUserId == f.Actor).ToListAsync()); Assert.Equal(before, await f.Snapshot(false, false));
    }
    [Theory] [MemberData(nameof(Rejected))]
    public async Task External_unknown_and_blank_channels_cannot_bypass_communication_policy(string role, string channel)
    { using var f = new Fixture(); await f.Seed(role); await f.Change(db => f.Bill(db, "USD", 100m)); await f.Login(); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { invoiceId = (Guid?)null, channel }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Theory] [InlineData(" ")] [InlineData("InApp,Email")] [InlineData("InApp\0")]
    public async Task Malformed_channel_is_refused_without_audit_or_notification_mutation(string channel)
    { using var f = new Fixture(); await f.Seed("FinanceUser"); await f.Change(db => f.Bill(db, "INR", 100m)); await f.Login(); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { invoiceId = (Guid?)null, channel }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Anonymous_reminder_request_is_unauthorized_before_any_channel_write()
    { using var f = new Fixture(); await f.Seed("FinanceUser"); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { channel = "Email" }); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Foreign_academy_remains_forbidden_with_supported_channel()
    { using var f = new Fixture(); await f.Seed("FinanceUser"); await f.Login(); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Foreign}/fee-reminders", new { channel = "InApp" }); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Unprivileged_teacher_cannot_queue_a_supported_notification()
    { using var f = new Fixture(); await f.Seed("Teacher"); await f.Login(); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { channel = "InApp" }); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Fact] public async Task Unsupported_channel_is_explicitly_refused_even_without_matching_invoices()
    { using var f = new Fixture(); await f.Seed("FinanceUser"); await f.Login(); var before = await f.Snapshot(); using var response = await f.Client.PostAsJsonAsync($"/api/academies/{f.Academy}/fee-reminders", new { channel = "Email" }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(before, await f.Snapshot()); }
    [Theory] [InlineData("FinanceUser")] [InlineData("AcademyAdmin")] [InlineData("Owner")]
    public async Task Reminder_history_and_student_options_work_without_Communication_and_expose_only_owned_metadata(string role)
    {
        using var f = new Fixture(); await f.Seed(role); await f.Change(db => {
            db.Add(new Notification { AcademyId=f.Academy, RecipientId=f.Student, RecipientType="Student", Title="Fee payment reminder", Message="PRIVATE body must not be exposed", Channel="InApp", Status="Queued" });
            db.Add(new Notification { AcademyId=f.Foreign, RecipientId=Guid.NewGuid(), RecipientType="Student", Title="Fee payment reminder", Message="Foreign private body", Channel="InApp" });
            db.Add(new Notification { AcademyId=f.Academy, RecipientType="Student", Title="Unrelated private notification", Message="Other body", Channel="InApp" });
        }); await f.Login(); var before=await f.Snapshot();
        using var history=await f.Client.GetAsync($"/api/academies/{f.Academy}/fee-reminders/history"); Assert.Equal(HttpStatusCode.OK,history.StatusCode);
        using var json=JsonDocument.Parse(await history.Content.ReadAsStringAsync()); var item=Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(new[]{"channel","id","sentAtUtc","status","title"},item.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray()); Assert.Equal("Fee payment reminder",item.GetProperty("title").GetString()); Assert.Equal("InApp",item.GetProperty("channel").GetString());
        using var options=await f.Client.GetAsync($"/api/academies/{f.Academy}/invoices/student-options"); Assert.Equal(HttpStatusCode.OK,options.StatusCode);
        using var generic=await f.Client.GetAsync($"/api/academies/{f.Academy}/notifications"); Assert.Equal(HttpStatusCode.Forbidden,generic.StatusCode); Assert.Equal(before,await f.Snapshot());
    }
    [Fact] public async Task Reminder_history_cannot_cross_academy_scope()
    { using var f=new Fixture(); await f.Seed("FinanceUser"); await f.Login(); var before=await f.Snapshot(); using var r=await f.Client.GetAsync($"/api/academies/{f.Foreign}/fee-reminders/history"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode); Assert.Equal(before,await f.Snapshot()); }
    [Fact] public async Task Reminder_history_is_not_available_to_unprivileged_teacher()
    { using var f=new Fixture(); await f.Seed("Teacher"); await f.Login(); var before=await f.Snapshot(); using var r=await f.Client.GetAsync($"/api/academies/{f.Academy}/fee-reminders/history"); Assert.Equal(HttpStatusCode.Forbidden,r.StatusCode); Assert.Equal(before,await f.Snapshot()); }
    [Fact] public async Task Reminder_history_is_not_available_to_anonymous_callers()
    { using var f=new Fixture(); await f.Seed("FinanceUser"); using var r=await f.Client.GetAsync($"/api/academies/{f.Academy}/fee-reminders/history"); Assert.Equal(HttpStatusCode.Unauthorized,r.StatusCode); }
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
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { Id = Actor, UserName = "reminder-channel@example.invalid", Email = "reminder-channel@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic family actor", AcademyId = Academy, StudentId = role == "Student" ? Student : null, GuardianId = role == "Guardian" ? Guardian : null };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        public Invoice Bill(AcademyDeskDbContext db, string currency, decimal total, decimal adjusted = 0, Guid? student = null, Guid? academy = null) { var i = new Invoice { AcademyId = academy ?? Academy, StudentId = student ?? Student, InvoiceNumber = $"FAMILY-{Guid.NewGuid():N}", TotalAmount = total, AdjustedAmount = adjusted, Currency = currency, DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) }; db.Add(i); return i; }
        public async Task Change(Action<AcademyDeskDbContext> change) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); change(db); await db.SaveChangesAsync(); }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "reminder-channel@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task<JsonElement> Details() { using var r = await Client.GetAsync(DetailPath); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Document(Guid invoice) { using var r = await Client.GetAsync(DocPath(invoice)); Assert.Equal(HttpStatusCode.OK, r.StatusCode); return await r.Content.ReadAsStringAsync(); }
        public async Task<JsonElement> Queue(Guid invoice) { using var r = await Client.PostAsJsonAsync($"/api/academies/{Academy}/fee-reminders", new { invoiceId = invoice, channel = "InApp" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); return json.RootElement.Clone(); }
        public async Task<string> Snapshot(bool audit = true, bool notices = true) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = notices ? await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null, audit = audit ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null }); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"reminder-channel-{Guid.NewGuid():N}";
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
