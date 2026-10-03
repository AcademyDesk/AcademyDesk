using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAssessmentOptionsAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, empty, batch, student, schemeId; var count=0;
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a=await db.Academies.SingleAsync(x=>x.Name=="Synthetic Academy A");academy=a.Id;
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            batch=await db.Batches.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            student=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            var foreignStudent=await db.Students.Where(x=>x.AcademyId==foreign).Select(x=>x.Id).SingleAsync();
            a.EnabledModulesJson="[\"Core\",\"AcademicGovernance\"]";
            (await db.Academies.SingleAsync(x=>x.Id==foreign)).EnabledModulesJson="[\"Core\",\"AcademicGovernance\"]";
            var emptyAcademy=new Academy {Name="Synthetic Empty Options",EnabledModulesJson="[\"AcademicGovernance\"]"};empty=emptyAcademy.Id;db.Add(emptyAcademy);
            var zeta=new Batch {AcademyId=academy,Name="Zeta options batch",CourseId=Guid.NewGuid(),IsActive=false};
            var foreignBatch=new Batch {AcademyId=foreign,Name="Foreign options batch",CourseId=Guid.NewGuid()};db.AddRange(zeta,foreignBatch);
            var extra=new Student {AcademyId=academy,FirstName="Zed",LastName="Alpha",IsActive=false,Email="private-qa@example.invalid",Phone="QA private"};
            var nonactive=new Student {AcademyId=academy,FirstName="Nonactive",LastName="Excluded"};
            var orphan=new Student {AcademyId=academy,FirstName="Unenrolled",LastName="Excluded"};db.AddRange(extra,nonactive,orphan);
            db.AddRange(new Enrollment {AcademyId=academy,BatchId=batch,StudentId=student},new Enrollment {AcademyId=academy,BatchId=batch,StudentId=student},
                new Enrollment {AcademyId=academy,BatchId=zeta.Id,StudentId=student},new Enrollment {AcademyId=academy,BatchId=zeta.Id,StudentId=extra.Id},
                new Enrollment {AcademyId=academy,BatchId=batch,StudentId=foreignStudent},new Enrollment {AcademyId=academy,BatchId=foreignBatch.Id,StudentId=orphan.Id},
                new Enrollment {AcademyId=foreign,BatchId=batch,StudentId=orphan.Id},new Enrollment {AcademyId=foreign,BatchId=foreignBatch.Id,StudentId=foreignStudent});
            foreach(var status in new[]{"Paused","Completed","Waitlisted","Withdrawn","Cancelled","Transferred"})db.Add(new Enrollment {AcademyId=academy,BatchId=batch,StudentId=nonactive.Id,Status=status});
            var scheme=new GradingScheme {AcademyId=academy,Name="Alpha options scheme",PassingPercent=50,BandsJson="[\"Synthetic private bands\"]"};schemeId=scheme.Id;
            db.AddRange(scheme,new GradingScheme {AcademyId=academy,Name="Zeta options scheme",PassingPercent=75},new GradingScheme {AcademyId=academy,Name="Inactive options scheme",IsActive=false},new GradingScheme {AcademyId=foreign,Name="Foreign options scheme"});await db.SaveChangesAsync();
        }
        var tokens=new Dictionary<string,string>();
        foreach(var actor in new[] {
            (Role:"AcademyAdmin",Email:"qa-admin-a@example.invalid",Permissions:"[]"),
            (Role:"Owner",Email:"qa-options-owner@example.invalid",Permissions:"[]"),
            (Role:"QA-OptionsAcademic",Email:"qa-options-academic@example.invalid",Permissions:"[\"academics.manage\"]"),
            (Role:"QA-OptionsNone",Email:"qa-options-none@example.invalid",Permissions:"[]"),
            (Role:"FinanceUser",Email:"qa-options-finance@example.invalid",Permissions:"[]"),
            (Role:"Teacher",Email:"qa-teacher-a@example.invalid",Permissions:"[]"),
            (Role:"Student",Email:"qa-options-student@example.invalid",Permissions:"[]"),
            (Role:"Guardian",Email:"qa-options-guardian@example.invalid",Permissions:"[]")
        }) {
            if(actor.Role is not ("AcademyAdmin" or "Teacher"))await CreateAccessActorAsync(factory,actor.Email,actor.Role,academy,actor.Role=="Student"?student:null,actor.Permissions);
            tokens[actor.Role]=await LoginAsync(client,actor.Email,"Synthetic!39Ab");
        }
        client.DefaultRequestHeaders.Authorization=null;
        string Route(Guid? tenant=null)=>$"/api/academies/{tenant??academy}/assessments/options";
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {Batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Schemes=await db.GradingSchemes.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Assessments=await db.Assessments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Results=await db.AssessmentResults.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),Audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        static void Fields(JsonElement element,params string[] names) {
            if(!element.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(names.OrderBy(x=>x,StringComparer.Ordinal)))throw new InvalidOperationException("Options exposed unexpected/missing fields");
        }
        async Task CheckProjection(JsonElement body,Guid tenant) {
            Fields(body,"batches","students","enrollments","gradingSchemes");
            foreach(var row in body.GetProperty("batches").EnumerateArray())Fields(row,"id","name");
            foreach(var row in body.GetProperty("students").EnumerateArray())Fields(row,"id","firstName","lastName");
            foreach(var row in body.GetProperty("enrollments").EnumerateArray())Fields(row,"studentId","batchId","status");
            foreach(var row in body.GetProperty("gradingSchemes").EnumerateArray())Fields(row,"id","name","passingPercent");
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var batches=await db.Batches.AsNoTracking().Where(x=>x.AcademyId==tenant).OrderBy(x=>x.Name).ThenBy(x=>x.Id).Select(x=>new AssessmentBatchOption(x.Id,x.Name)).ToListAsync();
            var allStudents=await db.Students.AsNoTracking().Where(x=>x.AcademyId==tenant).ToListAsync();
            var rows=await db.Enrollments.AsNoTracking().Where(x=>x.AcademyId==tenant&&x.Status=="Active").ToListAsync();
            var roster=rows.Where(x=>batches.Any(b=>b.Id==x.BatchId)&&allStudents.Any(s=>s.Id==x.StudentId)).Select(x=>new AssessmentEnrollmentOption(x.StudentId,x.BatchId,"Active")).Distinct().ToArray();
            var students=allStudents.Where(x=>roster.Any(e=>e.StudentId==x.Id)).OrderBy(x=>x.LastName).ThenBy(x=>x.FirstName).ThenBy(x=>x.Id).Select(x=>new AssessmentStudentOption(x.Id,x.FirstName,x.LastName)).ToArray();
            var schemes=await db.GradingSchemes.AsNoTracking().Where(x=>x.AcademyId==tenant&&x.IsActive).OrderBy(x=>x.Name).ThenBy(x=>x.Id).Select(x=>new AssessmentSchemeOption(x.Id,x.Name,x.PassingPercent)).ToListAsync();
            var actual=body.Deserialize<AssessmentOptions>(new JsonSerializerOptions(JsonSerializerDefaults.Web))??throw new InvalidOperationException("Missing options body");
            if(!actual.Batches.SequenceEqual(batches)||!actual.Students.SequenceEqual(students)||!actual.GradingSchemes.SequenceEqual(schemes)||!actual.Enrollments.OrderBy(x=>x.BatchId).ThenBy(x=>x.StudentId).SequenceEqual(roster.OrderBy(x=>x.BatchId).ThenBy(x=>x.StudentId)))throw new InvalidOperationException("Options projection differs from fresh independently filtered SQL");
            if(tenant==academy&&(actual.Batches.Count!=2||actual.Students.Count!=2||actual.Enrollments.Count!=3||actual.GradingSchemes.Count!=2))throw new InvalidOperationException("Owned fixture counts changed/foreign or nonactive roster included");
            if(tenant==empty&&(actual.Batches.Count+actual.Students.Count+actual.Enrollments.Count+actual.GradingSchemes.Count!=0))throw new InvalidOperationException("Empty tenant leaked data");
        }
        async Task<JsonElement?> Read(string label,string? token,HttpStatusCode expected,Guid? tenant=null,string? path=null,string method="GET") {
            await Task.Delay(550);var before=await Snapshot();using var request=new HttpRequestMessage(new HttpMethod(method),path??Route(tenant));
            if(token is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
            using var response=await client.SendAsync(request);RequireFinanceStatus(response,expected,label);
            if(before!=await Snapshot())throw new InvalidOperationException("Read/denied options request changed rows: "+label);
            JsonElement? body=null;if(expected==HttpStatusCode.OK){using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());body=json.RootElement.Clone();await CheckProjection(body.Value,tenant??academy);}
            count++;Console.WriteLine($"ACADEMICOPTIONS CASE {label} PASS.");return body;
        }
        foreach(var actor in tokens)await Read("role-"+actor.Key,actor.Value,actor.Key is "AcademyAdmin" or "Owner" or "QA-OptionsAcademic"?HttpStatusCode.OK:HttpStatusCode.Forbidden);
        await Read("anonymous",null,HttpStatusCode.Unauthorized);
        foreach(var role in new[]{"AcademyAdmin","QA-OptionsAcademic"})await Read("foreign-route-"+role,tokens[role],HttpStatusCode.Forbidden,foreign);
        var foreignToken=await LoginAsync(client,"qa-admin-b@example.invalid","Synthetic!39Ab");await Read("foreign-academy-own-options",foreignToken,HttpStatusCode.OK,foreign);
        await CreateAccessActorAsync(factory,"qa-options-empty@example.invalid","AcademyAdmin",empty);var emptyToken=await LoginAsync(client,"qa-options-empty@example.invalid","Synthetic!39Ab");await Read("empty-academy",emptyToken,HttpStatusCode.OK,empty);
        var grantUser=await CreateAccessActorAsync(factory,"qa-options-grant@example.invalid","QA-OptionsGrant",academy);var grantToken=await LoginAsync(client,"qa-options-grant@example.invalid","Synthetic!39Ab");
        await Read("grant-absent",grantToken,HttpStatusCode.Forbidden);Guid grantId;
        using(var scope=factory.Services.CreateScope()){var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();var grant=new AccessGrant {AcademyId=foreign,UserId=grantUser,GrantedByUserId=grantUser,PermissionsJson="[\"academics.manage\"]",ExpiresAtUtc=DateTimeOffset.UtcNow.AddHours(1),Reason="Synthetic options fixture"};identity.Add(grant);await identity.SaveChangesAsync();grantId=grant.Id;}
        await Read("grant-foreign-academy",grantToken,HttpStatusCode.Forbidden);
        foreach(var state in new[]{"valid","expired","revoked","permanent","no-expiry","revoked-permanent"}) {
            using(var scope=factory.Services.CreateScope()){var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();var grant=await identity.AccessGrants.SingleAsync(x=>x.Id==grantId);grant.AcademyId=academy;grant.IsPermanent=state is "permanent" or "revoked-permanent";grant.ExpiresAtUtc=state=="no-expiry"?null:DateTimeOffset.UtcNow.AddHours(state is "expired" or "permanent"?-1:1);grant.RevokedAtUtc=state is "revoked" or "revoked-permanent"?DateTimeOffset.UtcNow:null;await identity.SaveChangesAsync();}
            await Read("grant-"+state+"-same-token",grantToken,state is "valid" or "permanent"?HttpStatusCode.OK:HttpStatusCode.Forbidden);
        }
        foreach(var modules in new[]{"[\"Core\"]","[\"AcademicGovernance\"]","[]","invalid-json"}) {
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Academies.SingleAsync(x=>x.Id==academy)).EnabledModulesJson=modules;await db.SaveChangesAsync();}
            foreach(var role in new[]{"AcademyAdmin","QA-OptionsAcademic"})await Read("modules-"+modules+"-"+role,tokens[role],modules=="[\"AcademicGovernance\"]"?HttpStatusCode.OK:HttpStatusCode.Forbidden);
        }
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var a=await db.Academies.SingleAsync(x=>x.Id==academy);a.EnabledModulesJson="[\"Core\",\"AcademicGovernance\"]";a.IsActive=false;await db.SaveChangesAsync();}
        await Read("inactive-academy",tokens["QA-OptionsAcademic"],HttpStatusCode.Forbidden);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Academies.SingleAsync(x=>x.Id==academy)).IsActive=true;await db.SaveChangesAsync();var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=await users.FindByEmailAsync("qa-options-academic@example.invalid")??throw new InvalidOperationException("Missing actor");user.IsActive=false;if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Inactive fixture failed");}
        await Read("inactive-user-existing-token",tokens["QA-OptionsAcademic"],HttpStatusCode.Forbidden);
        using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=await users.FindByEmailAsync("qa-options-academic@example.invalid")??throw new InvalidOperationException("Missing actor");user.IsActive=true;if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Restore actor failed");}
        await Read("reactivated-user-existing-token",tokens["QA-OptionsAcademic"],HttpStatusCode.OK);
        foreach(var path in new[]{"batches","students","enrollments","grading-schemes/active"})await Read("generic-management-still-denied-"+path,tokens["QA-OptionsAcademic"],HttpStatusCode.Forbidden,path:$"/api/academies/{academy}/{path}");
        await Read("options-post-method-denied",tokens["QA-OptionsAcademic"],HttpStatusCode.MethodNotAllowed,method:"POST");
        // Use actual returned selector IDs for the delegated setup -> grade -> fresh readback workflow.
        using var optionsRequest=new HttpRequestMessage(HttpMethod.Get,Route());optionsRequest.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["QA-OptionsAcademic"]);
        using var optionsResponse=await client.SendAsync(optionsRequest);RequireFinanceStatus(optionsResponse,HttpStatusCode.OK,"workflow-options");using var optionsJson=JsonDocument.Parse(await optionsResponse.Content.ReadAsStringAsync());
        var optionBatch=optionsJson.RootElement.GetProperty("batches").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==batch).GetProperty("id").GetGuid();
        var optionStudent=optionsJson.RootElement.GetProperty("enrollments").EnumerateArray().Single(x=>x.GetProperty("batchId").GetGuid()==batch&&x.GetProperty("studentId").GetGuid()==student).GetProperty("studentId").GetGuid();
        var optionScheme=optionsJson.RootElement.GetProperty("gradingSchemes").EnumerateArray().Single(x=>x.GetProperty("id").GetGuid()==schemeId).GetProperty("id").GetGuid();
        using var create=new HttpRequestMessage(HttpMethod.Post,$"/api/academies/{academy}/assessments");create.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["QA-OptionsAcademic"]);create.Content=JsonContent.Create(new {batchId=optionBatch,title="Delegated lookup workflow",maxScore=100m,gradingSchemeId=optionScheme,isPublished=false});
        using var created=await client.SendAsync(create);RequireFinanceStatus(created,HttpStatusCode.Created,"options-create");using var createdBody=JsonDocument.Parse(await created.Content.ReadAsStringAsync());var assessmentId=createdBody.RootElement.GetProperty("id").GetGuid();
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(!await db.Assessments.AnyAsync(x=>x.Id==assessmentId&&x.AcademyId==academy&&x.BatchId==optionBatch&&x.GradingSchemeId==optionScheme))throw new InvalidOperationException("Option-linked assessment SQL mismatch");}
        count++;Console.WriteLine("ACADEMICOPTIONS CASE delegated-option-linked-create PASS.");
        using var save=new HttpRequestMessage(HttpMethod.Post,$"/api/academies/{academy}/assessments/{assessmentId}/results");save.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["QA-OptionsAcademic"]);save.Content=JsonContent.Create(new {studentId=optionStudent,score=80m,isGradeManual=false,isPublished=false});
        using var saved=await client.SendAsync(save);RequireFinanceStatus(saved,HttpStatusCode.OK,"options-result");using var savedBody=JsonDocument.Parse(await saved.Content.ReadAsStringAsync());
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var result=await db.AssessmentResults.AsNoTracking().SingleAsync(x=>x.AssessmentId==assessmentId&&x.StudentId==optionStudent&&x.AcademyId==academy);if(result.Score!=80||result.Grade!="Pass"||result.IsGradeManual!=false||savedBody.RootElement.GetProperty("grade").GetString()!=result.Grade)throw new InvalidOperationException("Option-linked result SQL/grade mismatch");}
        count++;Console.WriteLine("ACADEMICOPTIONS CASE delegated-option-linked-result-save PASS.");
        using var read=new HttpRequestMessage(HttpMethod.Get,$"/api/academies/{academy}/assessments/{assessmentId}/results");read.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["QA-OptionsAcademic"]);
        using var listed=await client.SendAsync(read);RequireFinanceStatus(listed,HttpStatusCode.OK,"options-readback");using var listedBody=JsonDocument.Parse(await listed.Content.ReadAsStringAsync());if(listedBody.RootElement.GetArrayLength()!=1||listedBody.RootElement[0].GetProperty("studentId").GetGuid()!=optionStudent||listedBody.RootElement[0].GetProperty("grade").GetString()!="Pass")throw new InvalidOperationException("Option-linked result readback mismatch");
        count++;Console.WriteLine("ACADEMICOPTIONS CASE delegated-option-linked-result-readback PASS.");
        if(count!=40)throw new InvalidOperationException("Options case count changed: "+count);
        Console.WriteLine("ACADEMICOPTIONS REGRESSION PASS:40 cases; minimal owned projection/fresh SQL, deduplication and inactive membership exclusions, read-only role/grant/module/activity/tenant gates, generic management remains denied, delegated option-linked grading workflow; browser/device and historical eligibility policy pending.");
    }
}
