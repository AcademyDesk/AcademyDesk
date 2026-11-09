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

public sealed class SalesTrialIntegrityTests
{
    private static Lead Lead(Fixture f, string stage="New", bool linked=false) => new() { AcademyId=f.Academy, FullName="Synthetic Prospect", Stage=stage, ConvertedStudentId=linked?f.Student:null, Email="prospect@example.invalid" };
    private static string Base(Fixture f) => $"/api/academies/{f.Academy}/sales-marketing";
    [Theory] [InlineData("Sales")] [InlineData("Marketing")] [InlineData("FrontDesk")] [InlineData("Owner")] [InlineData("AcademyAdmin")] [InlineData("CustomSales")] [InlineData("PlatformOwner")]
    public async Task Authorized_teacher_options_expose_only_own_names_without_teacher_management(string role)
    {
        using var f=new Fixture(); await f.Seed(role); await f.Change(db=>{ db.Add(new Teacher{AcademyId=f.Academy,FirstName="Owned",LastName="Teacher",Email="private@example.invalid"});db.Add(new Teacher{AcademyId=f.Foreign,FirstName="Foreign",LastName="Teacher"}); }); await f.Login(); var before=await f.Snapshot();
        using var r=await f.Client.GetAsync(Base(f)+"/teacher-options"); Assert.Equal(HttpStatusCode.OK,r.StatusCode);using var j=JsonDocument.Parse(await r.Content.ReadAsStringAsync());var item=Assert.Single(j.RootElement.EnumerateArray());Assert.Equal(new[]{"firstName","id","lastName"},item.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());Assert.Equal("Owned",item.GetProperty("firstName").GetString());
        using var leads=await f.Client.GetAsync($"/api/academies/{f.Academy}/leads");Assert.Equal(HttpStatusCode.OK,leads.StatusCode);using var trials=await f.Client.GetAsync(Base(f)+"/trials");Assert.Equal(HttpStatusCode.OK,trials.StatusCode);
        if(role is "Sales" or "Marketing" or "FrontDesk"){using var denied=await f.Client.GetAsync($"/api/academies/{f.Academy}/teachers");Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode);}Assert.Equal(before,await f.Snapshot());
    }
    [Theory] [InlineData("New")] [InlineData("Contacted")] [InlineData("FollowUp")] [InlineData("Won")] [InlineData("Lost")] [InlineData("TrialBooked")]
    public async Task Unconverted_stage_retains_supported_booking_and_attributed_audit(string stage)
    { using var f=new Fixture();await f.Seed("Sales");Lead? lead=null;await f.Change(db=>{lead=Lead(f,stage);db.Add(lead);});await f.Login();using var r=await f.Client.PostAsJsonAsync(Base(f)+"/trials",new{leadId=lead!.Id,teacherId=(Guid?)null,scheduledAtUtc=new DateTime(2026,12,1,4,30,0,DateTimeKind.Utc),notes="  Trial note  "});Assert.Equal(HttpStatusCode.OK,r.StatusCode);await using var scope=f.Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=Assert.Single(await db.TrialClassBookings.ToListAsync());Assert.Equal("Trial note",row.Notes);Assert.Null(row.TeacherId);Assert.Equal("TrialBooked",(await db.Leads.SingleAsync()).Stage);Assert.Single(await db.AuditLogs.Where(x=>x.ActorUserId==f.Actor).ToListAsync()); }
    [Theory] [InlineData("Sales",true)] [InlineData("Sales",false)] [InlineData("Marketing",true)] [InlineData("Marketing",false)] [InlineData("FrontDesk",true)] [InlineData("FrontDesk",false)] [InlineData("PlatformOwner",false)] [InlineData("CustomSales",true)]
    public async Task Sales_booking_keeps_optional_owned_teacher(string role,bool assigned)
    {using var f=new Fixture();await f.Seed(role);Lead? lead=null;var teacher=Guid.NewGuid();await f.Change(db=>{lead=Lead(f);db.Add(lead);db.Add(new Teacher{Id=teacher,AcademyId=f.Academy,FirstName="Owned",LastName="Teacher"});});await f.Login();using var r=await f.Client.PostAsJsonAsync(Base(f)+"/trials",new{leadId=lead!.Id,teacherId=assigned?(Guid?)teacher:null,scheduledAtUtc=DateTime.UtcNow.AddDays(1)});Assert.Equal(HttpStatusCode.OK,r.StatusCode);await using var scope=f.Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();Assert.Equal(assigned?(Guid?)teacher:null,(await db.TrialClassBookings.SingleAsync()).TeacherId);}
    [Theory] [InlineData("Converted",true)] [InlineData("converted",false)] [InlineData(" Converted ",false)] [InlineData("TrialBooked",true)]
    public async Task Terminal_conversion_state_cannot_regress_through_trial_creation(string stage,bool linked)
    {using var f=new Fixture();await f.Seed("Sales");Lead? lead=null;await f.Change(db=>{lead=Lead(f,stage,linked);db.Add(lead);});await f.Login();var before=await f.Snapshot();using var r=await f.Client.PostAsJsonAsync(Base(f)+"/trials",new{leadId=lead!.Id,scheduledAtUtc=DateTime.UtcNow.AddDays(1)});Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);Assert.Equal(before,await f.Snapshot());}
    [Theory] [InlineData("Converted")] [InlineData("converted")] [InlineData("CONVERTED")]
    public async Task A_stage_choice_cannot_manufacture_conversion_without_a_student(string stage)
    {using var f=new Fixture();await f.Seed("Sales");Lead? lead=null;await f.Change(db=>{lead=Lead(f);db.Add(lead);});await f.Login();var before=await f.Snapshot();using var r=await f.Client.PatchAsJsonAsync($"/api/academies/{f.Academy}/leads/{lead!.Id}/stage",new{stage});Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Equal(before,await f.Snapshot());}
    [Theory] [InlineData("New")] [InlineData("Converted")]
    public async Task Explicit_conversion_creates_one_real_link_and_blocks_repeated_conversion_or_trial(string stage)
    {using var f=new Fixture();await f.Seed("Sales");Lead? lead=null;await f.Change(db=>{lead=Lead(f,stage);db.Add(lead);});await f.Login();var url=$"/api/academies/{f.Academy}/leads/{lead!.Id}/convert";using var r=await f.Client.PostAsJsonAsync(url,new{});Assert.Equal(HttpStatusCode.OK,r.StatusCode);await using(var scope=f.Factory.Services.CreateAsyncScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();Assert.Equal(2,await db.Students.CountAsync());Assert.NotNull((await db.Leads.SingleAsync()).ConvertedStudentId);Assert.Single(await db.AuditLogs.Where(x=>x.ActorUserId==f.Actor).ToListAsync());}var before=await f.Snapshot();using var again=await f.Client.PostAsJsonAsync(url,new{});Assert.Equal(HttpStatusCode.Conflict,again.StatusCode);using var trial=await f.Client.PostAsJsonAsync(Base(f)+"/trials",new{leadId=lead.Id,scheduledAtUtc=DateTime.UtcNow.AddDays(1)});Assert.Equal(HttpStatusCode.Conflict,trial.StatusCode);Assert.Equal(before,await f.Snapshot());}
    [Theory] [InlineData("lead")] [InlineData("teacher")] [InlineData("batch")]
    public async Task Invalid_or_foreign_trial_references_do_not_mutate_any_captured_rows(string field)
    {using var f=new Fixture();await f.Seed("Sales");Lead? lead=null;await f.Change(db=>{lead=Lead(f);db.Add(lead);});await f.Login();var before=await f.Snapshot();using var r=await f.Client.PostAsJsonAsync(Base(f)+"/trials",new{leadId=field=="lead"?Guid.NewGuid():lead!.Id,teacherId=field=="teacher"?(Guid?)Guid.NewGuid():null,batchId=field=="batch"?(Guid?)Guid.NewGuid():null,scheduledAtUtc=DateTime.UtcNow.AddDays(1)});Assert.Equal(HttpStatusCode.BadRequest,r.StatusCode);Assert.Equal(before,await f.Snapshot());}
    [Theory] [InlineData("Booked")] [InlineData("Completed")] [InlineData("Cancelled")] [InlineData("NoShow")]
    public async Task Existing_trial_history_can_update_status_without_regressing_its_converted_lead(string status)
    {using var f=new Fixture();await f.Seed("Sales");var id=Guid.NewGuid();await f.Change(db=>{var lead=Lead(f,"Converted",true);db.Add(lead);db.Add(new TrialClassBooking{Id=id,AcademyId=f.Academy,LeadId=lead.Id,ScheduledAtUtc=DateTime.UtcNow,Status="Booked"});});await f.Login();using var r=await f.Client.PatchAsJsonAsync(Base(f)+$"/trials/{id}/status",new{status});Assert.Equal(HttpStatusCode.OK,r.StatusCode);await using var scope=f.Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();Assert.Equal("Converted",(await db.Leads.SingleAsync()).Stage);Assert.Equal(f.Student,(await db.Leads.SingleAsync()).ConvertedStudentId);Assert.Equal(status,(await db.TrialClassBookings.SingleAsync()).Status);}
    [Theory] [InlineData("anonymous")] [InlineData("Teacher")] [InlineData("foreign")]
    public async Task Booking_access_remains_scoped_and_refused_requests_leave_rows_unchanged(string actor)
    {using var f=new Fixture();await f.Seed(actor=="Teacher"?"Teacher":"Sales");if(actor!="anonymous")await f.Login();var before=await f.Snapshot();var academy=actor=="foreign"?f.Foreign:f.Academy;using var r=await f.Client.GetAsync($"/api/academies/{academy}/sales-marketing/teacher-options");Assert.Equal(actor=="anonymous"?HttpStatusCode.Unauthorized:HttpStatusCode.Forbidden,r.StatusCode);Assert.Equal(before,await f.Snapshot());}
    [Theory]
    [InlineData("New")]
    [InlineData("Contacted")]
    [InlineData("TrialBooked")]
    [InlineData("FollowUp")]
    [InlineData("Won")]
    [InlineData("Lost")]
    public async Task Ordinary_stage_changes_still_work_without_creating_a_student(string stage)
    {
        using var f = new Fixture(); await f.Seed("Sales");
        Lead? lead = null; await f.Change(db => { lead = Lead(f); db.Add(lead); }); await f.Login();
        using var response = await f.Client.PatchAsJsonAsync($"/api/academies/{f.Academy}/leads/{lead!.Id}/stage", new { stage });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = f.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        Assert.Equal(stage, (await db.Leads.SingleAsync()).Stage); Assert.Equal(1, await db.Students.CountAsync());
        Assert.Single(await db.AuditLogs.Where(x => x.ActorUserId == f.Actor).ToListAsync());
    }

    [Theory]
    [InlineData("Converted", false)]
    [InlineData(" converted ", false)]
    [InlineData("TrialBooked", true)]
    public async Task Stage_edits_cannot_regress_legacy_or_linked_conversion(string stage, bool linked)
    {
        using var f = new Fixture(); await f.Seed("Sales");
        Lead? lead = null; await f.Change(db => { lead = Lead(f, stage, linked); db.Add(lead); }); await f.Login();
        var before = await f.Snapshot();
        using var response = await f.Client.PatchAsJsonAsync($"/api/academies/{f.Academy}/leads/{lead!.Id}/stage", new { stage = "New" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    [Theory]
    [InlineData("Teacher")]
    [InlineData("Student")]
    [InlineData("foreign")]
    [InlineData("module")]
    public async Task Trial_writes_preserve_role_tenant_and_subscription_refusals(string actor)
    {
        using var f = new Fixture(); await f.Seed(actor is "Teacher" or "Student" ? actor : "Sales");
        Lead? lead = null;
        await f.Change(db => { lead = Lead(f); db.Add(lead); if (actor == "module") db.Academies.Single(x => x.Id == f.Academy).EnabledModulesJson = "[\"Core\"]"; });
        await f.Login(); var before = await f.Snapshot(); var academy = actor == "foreign" ? f.Foreign : f.Academy;
        using var response = await f.Client.PostAsJsonAsync($"/api/academies/{academy}/sales-marketing/trials", new { leadId = lead!.Id, scheduledAtUtc = DateTime.UtcNow.AddDays(1) });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(before, await f.Snapshot());
    }

    private sealed class Fixture : IDisposable
    {
        public HttpFactory Factory = new(); public HttpClient Client { get; } public Guid Academy = Guid.NewGuid(), Foreign = Guid.NewGuid(), Student = Guid.NewGuid(), Guardian = Guid.NewGuid(), Actor = Guid.NewGuid();
        public Fixture() => Client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        public async Task Seed(string role = "Student", bool finance = true)
        {
            await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(new Academy { Id = Academy, Name = "Synthetic currency academy", EnabledModulesJson = "[\"Core\",\"Sales\"]" }); db.Add(new Academy { Id = Foreign, Name = "Foreign academy" });
            db.Add(new Student { Id = Student, AcademyId = Academy, FirstName = "Synthetic", LastName = "Student" }); db.Add(new Guardian { Id = Guardian, AcademyId = Academy, FirstName = "Synthetic", LastName = "Guardian" });
            db.Add(new StudentGuardian { AcademyId = Academy, StudentId = Student, GuardianId = Guardian, CanAccessPortal = true, CanViewFinance = finance }); await db.SaveChangesAsync();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(); Assert.True((await roles.CreateAsync(new ApplicationRole { Name = role, IsSystemRole = role != "CustomSales", PermissionsJson = role == "CustomSales" ? "[\"sales.manage\"]" : "[]" })).Succeeded);
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = new ApplicationUser { Id = Actor, UserName = "sales-trial@example.invalid", Email = "sales-trial@example.invalid", EmailConfirmed = true, DisplayName = "Synthetic family actor", AcademyId = Academy, IsPlatformOwner = role == "PlatformOwner", StudentId = role == "Student" ? Student : null, GuardianId = role == "Guardian" ? Guardian : null };
            Assert.True((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded); Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        }
        public async Task Change(Action<AcademyDeskDbContext> change) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); change(db); await db.SaveChangesAsync(); }
        public async Task Login() { using var r = await Client.PostAsJsonAsync("/api/auth/login", new { email = "sales-trial@example.invalid", password = "Synthetic!39Ab" }); Assert.Equal(HttpStatusCode.OK, r.StatusCode); using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync()); Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString()); }
        public async Task<string> Snapshot(bool audit = true, bool notices = true) { await using var scope = Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { leads = await db.Leads.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), trials = await db.TrialClassBookings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = notices ? await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null, audit = audit ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() : null }); }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); }
    }
    private sealed class HttpFactory : WebApplicationFactory<Program>
    {
        private readonly string database = $"sales-trial-{Guid.NewGuid():N}";
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
