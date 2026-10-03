using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCertificateFamilyAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign; Student learner, other, foreignLearner; Certificate issued, foreignCertificate;
        var guardians = new List<Guardian>(); var links = new List<StudentGuardian>();
        var rows = new List<Certificate>();
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign = await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            learner = new() { AcademyId=academy, FirstName="Learner <&", LastName="O'Neil" };
            other = new() { AcademyId=academy, FirstName="Other", LastName="Learner" };
            foreignLearner = new() { AcademyId=foreign, FirstName="Foreign", LastName="Learner" };
            db.AddRange(learner,other,foreignLearner);
            for (var i=0;i<3;i++) {
                var guardian = new Guardian { AcademyId=academy,FirstName="Parent",LastName=i.ToString() };
                var link = new StudentGuardian { AcademyId=academy,StudentId=learner.Id,GuardianId=guardian.Id,CanAccessPortal=true,CanViewDocuments=i!=2 };
                guardians.Add(guardian);links.Add(link);db.AddRange(guardian,link);
            }
            foreach(var status in new[]{"Issued","Revoked","Replaced","issued","iSsUeD"}) {
                var row=new Certificate { AcademyId=academy,StudentId=learner.Id,BatchId=null,CertificateNumber="QA-FAMILY-"+status.ToUpperInvariant()+"-"+rows.Count,VerificationCode="QA-FAMILY-VERIFY-"+rows.Count,Title="Award <script> & \"quoted\"",Status=status,IssuedDate=new(2020,2,29),Notes=null };
                rows.Add(row);db.Add(row);
            }
            issued=rows[0]; foreignCertificate=new() { AcademyId=foreign,StudentId=foreignLearner.Id,CertificateNumber="QA-FAMILY-FOREIGN",VerificationCode="QA-FAMILY-FOREIGN",Title="Foreign award",Status="Issued" };
            db.Add(foreignCertificate);await db.SaveChangesAsync();
        }
        var tokens=new Dictionary<string,string>();
        foreach(var item in new[]{("learner", "Student",academy,(Guid?)learner.Id,(Guid?)null),("other", "Student",academy,(Guid?)other.Id,(Guid?)null),("foreign", "Student",foreign,(Guid?)foreignLearner.Id,(Guid?)null),("parent1","Guardian",academy,(Guid?)null,(Guid?)guardians[0].Id),("parent2","Guardian",academy,(Guid?)null,(Guid?)guardians[1].Id),("restricted","Guardian",academy,(Guid?)null,(Guid?)guardians[2].Id)}) {
            var email="qa-family-"+item.Item1+"@example.invalid";
            var id=await CreateAccessActorAsync(factory,email,item.Item2,item.Item3,item.Item4);
            using(var scope=factory.Services.CreateScope()) {
                var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(id.ToString()))!;
                user.DisplayName="Account "+item.Item1+" <not learner>";user.GuardianId=item.Item5;
                if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Family actor fixture update failed");
            }
            tokens[item.Item1]=await LoginAsync(client,email,"Synthetic!39Ab");
        }
        tokens["admin"]=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        tokens["teacher"]=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=null;var count=0;
        string Route(Guid student,string number)=>$"/api/portal/students/{student}/certificates/{Uri.EscapeDataString(number)}/download";
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new{certificates=await db.Certificates.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        static string Digest(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<(string Text,string? Type,string? File)> Send(string label,string path,HttpStatusCode expected,string? actor) {
            await Task.Delay(650);var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Get,path);
            if(actor is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens[actor]);
            using var response=await client.SendAsync(request);var bytes=await response.Content.ReadAsByteArrayAsync();var text=Encoding.UTF8.GetString(bytes);var after=await Snapshot();
            Console.WriteLine("CERTFAMILY EVIDENCE "+JsonSerializer.Serialize(new{label,path,status=(int)response.StatusCode,response=text,type=response.Content.Headers.ContentType?.MediaType,file=response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName,beforeDigest=Digest(before),afterDigest=Digest(after),bodyDigest=Digest(text)}));
            RequireFinanceStatus(response,expected,label);if(before!=after)throw new InvalidOperationException("Family read changed SQL/Identity/audit: "+label);
            return(text,response.Content.Headers.ContentType?.MediaType,(response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName)?.Trim('"'));
        }
        void Pass(string label){count++;Console.WriteLine("CERTFAMILY CASE "+label+" PASS.");}
        if(Environment.GetEnvironmentVariable("QA_CERTIFICATE_FAMILY_BASELINE")=="1") {
            var wrong=await Send("baseline-guardian-name",Route(learner.Id,issued.CertificateNumber),HttpStatusCode.OK,"parent1");
            if(!wrong.Text.Contains(WebUtility.HtmlEncode("Account parent1 <not learner>"))||wrong.Text.Contains(WebUtility.HtmlEncode(learner.FirstName+" "+learner.LastName)))throw new InvalidOperationException("Accepted guardian-name defect not reproduced");
            var leak=await Send("baseline-restricted-documents",Route(learner.Id,issued.CertificateNumber),HttpStatusCode.OK,"restricted");
            if(!leak.Text.Contains(issued.CertificateNumber))throw new InvalidOperationException("Accepted document-permission bypass not reproduced");
            Console.WriteLine("CERTFAMILY BASELINE: wrong guardian identity and restricted-document disclosure reproduced; 2 GETs with unchanged snapshots; no passing repair cases.");return;
        }
        async Task Download(string label,string actor,Certificate row) {
            var result=await Send(label,Route(learner.Id,row.CertificateNumber),HttpStatusCode.OK,actor);
            if(result.Type!="text/html"||result.File!=row.CertificateNumber+".html"||!result.Text.Contains("<h2>"+WebUtility.HtmlEncode(learner.FirstName+" "+learner.LastName)+"</h2>")||!result.Text.Contains(WebUtility.HtmlEncode(row.Title))||!result.Text.Contains(row.CertificateNumber)||!result.Text.Contains("29 Feb 2020")||result.Text.Contains("Account ")||result.Text.Contains("<script>"))throw new InvalidOperationException("Complete family HTML/recipient/encoding contract: "+label);
            Pass(label);
        }
        foreach(var actor in new[]{"learner","parent1","parent2"})await Download("identity-"+actor,actor,issued);
        foreach(var row in rows.Skip(3))await Download("status-case-"+row.Status,"learner",row);
        async Task Denied(string label,string? actor,Guid target,string number,HttpStatusCode status) {
            var result=await Send(label,Route(target,number),status,actor);if(result.Text.Contains("This certifies that")||result.Type=="text/html")throw new InvalidOperationException("Denied response disclosed certificate bytes: "+label);Pass(label);
        }
        await Denied("anonymous",null,learner.Id,issued.CertificateNumber,HttpStatusCode.Unauthorized);
        foreach(var actor in new[]{"other","foreign","admin","teacher","restricted"})await Denied("deny-"+actor,actor,learner.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        await Denied("wrong-child-parent","parent1",other.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        await Denied("foreign-child-parent","parent1",foreignLearner.Id,foreignCertificate.CertificateNumber,HttpStatusCode.Forbidden);
        await Denied("foreign-number","learner",learner.Id,foreignCertificate.CertificateNumber,HttpStatusCode.NotFound);
        await Denied("missing-number","learner",learner.Id,"QA-MISSING",HttpStatusCode.NotFound);
        foreach(var row in rows.Skip(1).Take(2))await Denied("status-"+row.Status,"parent1",learner.Id,row.CertificateNumber,HttpStatusCode.NotFound);
        await Denied("missing-child","learner",Guid.NewGuid(),issued.CertificateNumber,HttpStatusCode.Forbidden);
        async Task ChangeLink(Action<StudentGuardian> change) {using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();change(await db.StudentGuardians.SingleAsync(x=>x.Id==links[0].Id));await db.SaveChangesAsync();}
        await ChangeLink(x=>x.CanViewDocuments=false);await Denied("permission-revoked-after-login","parent1",learner.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        await Download("other-parent-still-permitted","parent2",issued);
        await ChangeLink(x=>{x.CanViewDocuments=true;x.AccessRevokedAtUtc=DateTime.UtcNow;});await Denied("link-revoked-after-login","parent1",learner.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        await ChangeLink(x=>{x.AccessRevokedAtUtc=null;x.CanAccessPortal=false;});await Denied("portal-flag-off","parent1",learner.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        await ChangeLink(x=>{x.CanAccessPortal=true;x.CanViewFinance=false;x.CanViewAcademicProgress=false;});await Download("documents-only-permission","parent1",issued);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Students.SingleAsync(x=>x.Id==learner.Id)).IsActive=false;await db.SaveChangesAsync();}
        foreach(var actor in new[]{"learner","parent1"})await Denied("inactive-child-"+actor,actor,learner.Id,issued.CertificateNumber,HttpStatusCode.Forbidden);
        Console.WriteLine($"CERTFAMILY REGRESSION PASS: {count} cases; all GET responses compared with fresh unchanged SQL/Identity/audit snapshots; synthetic fixture edits are not endpoint writes. HTML download, not PDF/browser/device acceptance.");
    }
}
