using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAssessmentAccessAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student, batch, assessment; var count = 0;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academy = a.Id;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            a.EnabledModulesJson = "[\"Core\",\"AcademicGovernance\",\"TeacherClassroom\"]";
            var row = new Assessment { AcademyId = academy, BatchId = batch, Title = "Academic access fixture", MaxScore = 100 };
            assessment = row.Id; db.AddRange(row, new Enrollment { AcademyId = academy, BatchId = batch, StudentId = student }); await db.SaveChangesAsync();
        }
        var tokens = new Dictionary<string,string>();
        foreach (var actor in new[] {
            (Role:"AcademyAdmin",Email:"qa-admin-a@example.invalid",Allowed:true,Permissions:"[]"),
            (Role:"Owner",Email:"qa-academic-owner@example.invalid",Allowed:true,Permissions:"[]"),
            (Role:"QA-Academic",Email:"qa-academic-custom@example.invalid",Allowed:true,Permissions:"[\"academics.manage\"]"),
            (Role:"QA-NoAcademic",Email:"qa-academic-none@example.invalid",Allowed:false,Permissions:"[]"),
            (Role:"FinanceUser",Email:"qa-academic-finance@example.invalid",Allowed:false,Permissions:"[]"),
            (Role:"Teacher",Email:"qa-teacher-a@example.invalid",Allowed:false,Permissions:"[]"),
            (Role:"Student",Email:"qa-academic-student@example.invalid",Allowed:false,Permissions:"[]"),
            (Role:"Guardian",Email:"qa-academic-guardian@example.invalid",Allowed:false,Permissions:"[]")
        }) {
            if (actor.Role is not ("AcademyAdmin" or "Teacher")) await CreateAccessActorAsync(factory, actor.Email, actor.Role, academy, actor.Role == "Student" ? student : null, actor.Permissions);
            tokens[actor.Role] = await LoginAsync(client, actor.Email, "Synthetic!39Ab");
        }
        client.DefaultRequestHeaders.Authorization = null;
        string Results(Guid? id = null, Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/assessments/{id ?? assessment}/results";
        async Task<string> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Results = await db.AssessmentResults.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Assessments = await db.Assessments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<JsonElement?> Check(string label, string? token, string method, string route, HttpStatusCode expected, Guid? resultAssessment = null, object? payload = null) {
            await Task.Delay(550); var before = await Snapshot();
            using var request = new HttpRequestMessage(new HttpMethod(method),route);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",token);
            if (method == "POST") request.Content = JsonContent.Create(payload ?? new { studentId = student, score = 80m, grade = (string?)null, isGradeManual = false, remarks = label, isPublished = false });
            using var response = await client.SendAsync(request); RequireFinanceStatus(response,expected,label);
            if ((int)expected >= 400 || method == "GET") { if (before != await Snapshot()) throw new InvalidOperationException("Denied/read academic request changed rows: " + label); }
            JsonElement? body = null;
            if ((int)expected < 400) {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); body = json.RootElement.Clone();
                if (resultAssessment.HasValue) {
                    using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    var saved = await db.AssessmentResults.AsNoTracking().Where(x => x.AcademyId == academy && x.AssessmentId == resultAssessment && x.StudentId == student).ToListAsync();
                    if (method == "POST") {
                        if (saved.Count != 1 || saved[0].Score != 80 || saved[0].Remarks != label || saved[0].IsPublished || body.Value.GetProperty("studentId").GetGuid() != student || body.Value.GetProperty("score").GetDecimal() != saved[0].Score) throw new InvalidOperationException("Academic result SQL/response mismatch: " + label);
                    } else {
                        var rows = body.Value.EnumerateArray().ToArray();
                        if (rows.Length != saved.Count || rows.Any(x => x.GetProperty("studentId").GetGuid() != student || x.GetProperty("score").GetDecimal() != saved.Single().Score)) throw new InvalidOperationException("Academic result GET differs from scoped SQL: " + label);
                    }
                }
            }
            count++; Console.WriteLine($"ACADEMICACCESS CASE {label} PASS."); return body;
        }
        async Task Pair(string label,string? token,HttpStatusCode expected,string? route=null) {
            await Check(label+"-save",token,"POST",route ?? Results(),expected,assessment);
            await Check(label+"-read",token,"GET",route ?? Results(),expected,assessment);
        }
        foreach (var actor in tokens) await Pair("role-"+actor.Key,actor.Value,actor.Key is "AcademyAdmin" or "Owner" or "QA-Academic" ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        await Pair("anonymous",null,HttpStatusCode.Unauthorized);
        await Pair("foreign-route",tokens["QA-Academic"],HttpStatusCode.Forbidden,Results(tenant:foreign));
        var grantUser = await CreateAccessActorAsync(factory,"qa-academic-grant@example.invalid","QA-AcademicGrant",academy);
        var grantToken = await LoginAsync(client,"qa-academic-grant@example.invalid","Synthetic!39Ab");
        await Pair("grant-absent",grantToken,HttpStatusCode.Forbidden);
        Guid grantId;
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var grant = new AccessGrant { AcademyId=foreign, UserId=grantUser, GrantedByUserId=grantUser, PermissionsJson="[\"academics.manage\"]", ExpiresAtUtc=DateTimeOffset.UtcNow.AddHours(1), Reason="Synthetic academic fixture" };
            identity.Add(grant); await identity.SaveChangesAsync(); grantId=grant.Id;
        }
        await Pair("grant-foreign-academy",grantToken,HttpStatusCode.Forbidden);
        foreach (var state in new[] {"valid","expired","revoked","permanent","no-expiry","revoked-permanent"}) {
            using (var scope = factory.Services.CreateScope()) {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var grant = await identity.AccessGrants.SingleAsync(x=>x.Id==grantId);
                grant.AcademyId=academy; grant.IsPermanent=state is "permanent" or "revoked-permanent";
                grant.ExpiresAtUtc=state=="no-expiry" ? null : DateTimeOffset.UtcNow.AddHours(state is "expired" or "permanent" ? -1 : 1);
                grant.RevokedAtUtc=state is "revoked" or "revoked-permanent" ? DateTimeOffset.UtcNow : null; await identity.SaveChangesAsync();
            }
            await Pair("grant-"+state+"-same-token",grantToken,state is "valid" or "permanent" ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        }
        foreach (var modules in new[] {"[\"Core\"]","[\"AcademicGovernance\"]","[]","invalid-json"}) {
            using (var scope = factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson=modules; await db.SaveChangesAsync(); }
            foreach (var role in new[] {"AcademyAdmin","Owner","QA-Academic"}) await Pair("modules-"+modules+"-"+role,tokens[role],modules=="[\"AcademicGovernance\"]" ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
        }
        using (var scope = factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var a=await db.Academies.SingleAsync(x=>x.Id==academy); a.EnabledModulesJson="[\"Core\",\"AcademicGovernance\",\"TeacherClassroom\"]"; a.IsActive=false; await db.SaveChangesAsync(); }
        await Pair("inactive-academy",tokens["QA-Academic"],HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x=>x.Id==academy)).IsActive=true; await db.SaveChangesAsync();
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user=await users.FindByEmailAsync("qa-academic-custom@example.invalid") ?? throw new InvalidOperationException("Missing custom actor"); user.IsActive=false; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Inactive fixture failed");
        }
        await Pair("inactive-user-existing-token",tokens["QA-Academic"],HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user=await users.FindByEmailAsync("qa-academic-custom@example.invalid") ?? throw new InvalidOperationException("Missing actor"); user.IsActive=true; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Restore actor failed"); }
        await Pair("reactivated-user-existing-token",tokens["QA-Academic"],HttpStatusCode.OK);
        await Check("teacher-own-save",tokens["Teacher"],"POST",$"/api/teacher/assessments/{assessment}/results",HttpStatusCode.OK,assessment);
        await Check("teacher-own-read",tokens["Teacher"],"GET",$"/api/teacher/assessments/{assessment}/results",HttpStatusCode.OK,assessment);
        // Existing page dependencies are evaluated individually, not broadened.
        foreach (var route in new[] {"batches","students","enrollments","grading-schemes/active"}) await Check("lookup-still-denied-"+route,tokens["QA-Academic"],"GET",$"/api/academies/{academy}/{route}",HttpStatusCode.Forbidden);
        foreach (var role in new[] {"AcademyAdmin","QA-Academic"}) await Check("setup-list-"+role,tokens[role],"GET",$"/api/academies/{academy}/assessments",HttpStatusCode.OK);
        var created=await Check("delegated-setup-create",tokens["QA-Academic"],"POST",$"/api/academies/{academy}/assessments",HttpStatusCode.Created,payload:new {batchId=batch,title="Delegated API workflow",maxScore=100m,isPublished=false});
        var createdId=created!.Value.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope()) { var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); if (!await db.Assessments.AnyAsync(x=>x.Id==createdId&&x.AcademyId==academy&&x.BatchId==batch&&x.Title=="Delegated API workflow")) throw new InvalidOperationException("Delegated setup SQL mismatch"); }
        await Check("delegated-created-result-save",tokens["QA-Academic"],"POST",Results(createdId),HttpStatusCode.OK,createdId);
        await Check("delegated-created-result-read",tokens["QA-Academic"],"GET",Results(createdId),HttpStatusCode.OK,createdId);
        if (count != 77) throw new InvalidOperationException("Academic access case count changed: "+count);
        Console.WriteLine("ACADEMICACCESS REGRESSION PASS:77 cases; role/grant/module/activity/tenant matrix, denied no-write, setup-to-results SQL parity, Teacher own control; four generic page lookups still denied, scoped lookup repair next.");
    }
}
