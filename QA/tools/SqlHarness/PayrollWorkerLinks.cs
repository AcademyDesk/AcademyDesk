using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollWorkerLinksAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_BATCH1") == "baseline";
        Guid academy, foreignTeacher, teacher, staff, foreignStaff, teacherUser;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            teacher=await db.Teachers.Where(x=>x.AcademyId==academy&&x.FirstName=="Synthetic").Select(x=>x.Id).SingleAsync();
            var foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            var foreignRow=new Teacher{AcademyId=foreign,FirstName="Synthetic",LastName="Foreign payroll control"};db.Teachers.Add(foreignRow);await db.SaveChangesAsync();foreignTeacher=foreignRow.Id;
            staff=await identity.Users.Where(x=>x.Email=="qa-admin-a@example.invalid").Select(x=>x.Id).SingleAsync();
            foreignStaff=await identity.Users.Where(x=>x.Email=="qa-admin-b@example.invalid").Select(x=>x.Id).SingleAsync();
            teacherUser=await identity.Users.Where(x=>x.Email=="qa-teacher-a@example.invalid").Select(x=>x.Id).SingleAsync();
        }
        var token=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");client.DefaultRequestHeaders.Authorization=null;
        var root=$"/api/academies/{academy}/payroll";var unknown=Guid.NewGuid();var rejected=0;var reproduced=0;var positive=0;
        async Task<string> State()
        {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {Profiles=(await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x=>x.Id).ToArray(),Payouts=(await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x=>x.Id).ToArray(),Audits=(await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x=>x.Id).ToArray(),Invoices=(await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x=>x.Id).ToArray(),Payments=(await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x=>x.Id).ToArray()});
        }
        var valid=new SavePayrollProfileRequest("Teacher",teacher,null,"Synthetic Link Original","Monthly",1000m,null,null,new DateOnly(2026,10,1));
        var cases=new[] {
            ("teacher-missing",valid with {TeacherId=null}), ("teacher-empty",valid with {TeacherId=Guid.Empty}),
            ("teacher-unknown",valid with {TeacherId=unknown}), ("teacher-foreign",valid with {TeacherId=foreignTeacher}),
            ("teacher-both",valid with {StaffUserId=staff.ToString()}),
            ("staff-missing",valid with {WorkerType="Staff",TeacherId=null,StaffUserId=null}),
            ("staff-unknown",valid with {WorkerType="Staff",TeacherId=null,StaffUserId=unknown.ToString()}),
            ("staff-foreign",valid with {WorkerType="Staff",TeacherId=null,StaffUserId=foreignStaff.ToString()}),
            ("staff-teacher-user",valid with {WorkerType="Staff",TeacherId=null,StaffUserId=teacherUser.ToString()}),
            ("staff-malformed",valid with {WorkerType="Staff",TeacherId=null,StaffUserId="not-an-identity-id"}),
            ("staff-both",valid with {WorkerType="Staff",StaffUserId=staff.ToString()}) };
        async Task<HttpResponseMessage> Send(HttpMethod method,string route,object body)
        {await Task.Delay(650);using var request=new HttpRequestMessage(method,route){Content=JsonContent.Create(body)};request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);var response=await client.SendAsync(request);if(response.StatusCode==HttpStatusCode.TooManyRequests)throw new InvalidOperationException("Worker link cannot pass with429.");return response;}
        foreach(var method in new[]{HttpMethod.Post,HttpMethod.Put})foreach(var (name,body) in cases)
        {
            Guid id=Guid.Empty;
            if(method==HttpMethod.Put){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=new PayrollProfile{AcademyId=academy,WorkerType="Teacher",TeacherId=teacher,WorkerName=valid.WorkerName,MonthlyAmount=1000m,EffectiveFrom=valid.EffectiveFrom!.Value};db.PayrollProfiles.Add(row);await db.SaveChangesAsync();id=row.Id;}
            var before=await State();using var response=await Send(method,root+"/profiles"+(method==HttpMethod.Put?"/"+id:""),body);
            if(response.StatusCode==HttpStatusCode.BadRequest){if(before!=await State())throw new InvalidOperationException("Rejected link changed five-table state.");rejected++;Console.WriteLine($"PAYROLL LINK PASS {method}/{name}:400, exact full no-write.");}
            else if(baseline&&response.StatusCode==(method==HttpMethod.Post?HttpStatusCode.Created:HttpStatusCode.OK)){reproduced++;Console.WriteLine($"PAYROLL LINK FAIL / REPRODUCED {method}/{name}: {(int)response.StatusCode}, invalid link stored; NOT ACCEPTED.");}
            else throw new InvalidOperationException($"Payroll invalid link accepted {method}/{name}: {(int)response.StatusCode}.");
        }
        foreach(var worker in new[]{valid,valid with {WorkerType="Staff",TeacherId=null,StaffUserId=staff.ToString(),WorkerName="Synthetic Link Staff"}})
        {
            using var create=await Send(HttpMethod.Post,root+"/profiles",worker);RequireFinanceStatus(create,HttpStatusCode.Created,"valid link create");var row=await create.Content.ReadFromJsonAsync<PayrollProfileSummary>()??throw new InvalidOperationException("No profile.");
            using var update=await Send(HttpMethod.Put,root+"/profiles/"+row.Id,worker with {WorkerName=worker.WorkerName+" Edited"});RequireFinanceStatus(update,HttpStatusCode.OK,"valid link update");
            using var scope=factory.Services.CreateScope();var stored=await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollProfiles.AsNoTracking().SingleAsync(x=>x.Id==row.Id);
            if(stored.TeacherId!=worker.TeacherId||stored.StaffUserId!=worker.StaffUserId||stored.WorkerType!=worker.WorkerType||stored.WorkerName!=worker.WorkerName+" Edited")throw new InvalidOperationException("Valid worker link roundtrip differs.");positive+=2;
        }
        if(baseline?reproduced==0:rejected!=22||reproduced!=0)throw new InvalidOperationException("Worker links acceptance incomplete.");
        Console.WriteLine($"PAYROLL LINK {(baseline?"BASELINE NOT ACCEPTED":"GUARDED PASS")}: {rejected}/22 invalid rejected, {reproduced} defects reproduced, {positive}/4 valid create/update controls; no429; issue remains OPEN for wider gates.");
    }
}
