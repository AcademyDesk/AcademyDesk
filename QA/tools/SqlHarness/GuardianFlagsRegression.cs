using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
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
    private static async Task VerifyGuardianFlagsAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign; Student child, other, foreignChild; Guardian parent;
        StudentGuardian link; Invoice invoice, otherInvoice, foreignInvoice; Certificate certificate;
        ClassSession session; Batch batch;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign = await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            batch = await db.Batches.SingleAsync(x=>x.AcademyId==academy);
            child = new() {AcademyId=academy,FirstName="Permission",LastName="Learner"};
            other = new() {AcademyId=academy,FirstName="Other",LastName="Child"};
            foreignChild = new() {AcademyId=foreign,FirstName="Foreign",LastName="Child"};
            parent = new() {AcademyId=academy,FirstName="Permission",LastName="Parent"};
            link = new() {AcademyId=academy,StudentId=child.Id,GuardianId=parent.Id,CanAccessPortal=true};
            invoice = new() {AcademyId=academy,StudentId=child.Id,InvoiceNumber="QA-FLAGS-OWN",TotalAmount=1000,AdjustedAmount=200};
            otherInvoice = new() {AcademyId=academy,StudentId=other.Id,InvoiceNumber="QA-FLAGS-OTHER",TotalAmount=9999};
            foreignInvoice = new() {AcademyId=foreign,StudentId=foreignChild.Id,InvoiceNumber="QA-FLAGS-FOREIGN",TotalAmount=8888};
            certificate = new() {AcademyId=academy,StudentId=child.Id,CertificateNumber="QA-FLAGS-CERT",VerificationCode="QA-FLAGS-VERIFY",Title="Permission award",Status="Issued"};
            session = new() {AcademyId=academy,BatchId=batch.Id,StartUtc=DateTime.UtcNow.AddDays(-2),EndUtc=DateTime.UtcNow.AddDays(-2).AddHours(1),DeliveryMode="Online",Status="Completed"};
            var excludedBatch = new Batch {AcademyId=academy,CourseId=batch.CourseId,Name="Unenrolled flags batch"};
            var excludedSession = new ClassSession {AcademyId=academy,BatchId=excludedBatch.Id,StartUtc=DateTime.UtcNow.AddDays(-3),EndUtc=DateTime.UtcNow.AddDays(-3).AddHours(1)};
            db.AddRange(child,other,foreignChild,parent,link,invoice,otherInvoice,foreignInvoice,certificate,session,excludedBatch,excludedSession,
                new Enrollment {AcademyId=academy,StudentId=child.Id,BatchId=batch.Id},
                new AttendanceRecord {AcademyId=academy,StudentId=child.Id,ClassSessionId=session.Id,Status="Present"});
            foreach (var (status, amount) in new[]{("Completed",100m),("Reconciled",200m),("Voided",900m)})
                db.Add(new Payment {AcademyId=academy,InvoiceId=invoice.Id,Amount=amount,Status=status});
            db.AddRange(
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-GENERAL",Url="https://example.invalid/qa/general"},
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-COMMON",Description="Shared class attachment",Url="https://example.invalid/qa/common",ClassSessionId=session.Id,BatchId=batch.Id},
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-PRIVATE",Description=null,Url="https://example.invalid/qa/private",ClassSessionId=session.Id,BatchId=batch.Id,StudentId=child.Id},
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-OTHER-PRIVATE",Url="https://example.invalid/qa/other-private",ClassSessionId=session.Id,BatchId=batch.Id,StudentId=other.Id},
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-UNPUBLISHED",Url="https://example.invalid/qa/unpublished",ClassSessionId=session.Id,BatchId=batch.Id,IsPublished=false},
                new LearningResource {AcademyId=academy,Title="QA-FLAGS-UNENROLLED",Url="https://example.invalid/qa/unenrolled",ClassSessionId=excludedSession.Id,BatchId=excludedBatch.Id},
                new LearningResource {AcademyId=foreign,Title="QA-FLAGS-FOREIGN-RESOURCE",Url="https://example.invalid/qa/foreign",StudentId=foreignChild.Id});
            await db.SaveChangesAsync();
        }
        var tokens = new Dictionary<string,string>();
        foreach(var actor in new[]{("parent","Guardian",academy,(Guid?)null,(Guid?)parent.Id),("learner","Student",academy,(Guid?)child.Id,(Guid?)null),("other","Student",academy,(Guid?)other.Id,(Guid?)null),("foreign","Student",foreign,(Guid?)foreignChild.Id,(Guid?)null)}) {
            var email="qa-flags-"+actor.Item1+"@example.invalid";
            var id=await CreateAccessActorAsync(factory,email,actor.Item2,actor.Item3,actor.Item4);
            using(var scope=factory.Services.CreateScope()) {
                var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(id.ToString()))!;
                user.GuardianId=actor.Item5;
                if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Guardian flags fixture Identity update failed");
            }
            tokens[actor.Item1]=await LoginAsync(client,email,"Synthetic!39Ab");
        }
        tokens["admin"]=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        tokens["teacher"]=await LoginAsync(client,"qa-teacher-a@example.invalid","Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=null;int count=0;
        string Details(Guid id)=>$"/api/portal/students/{id}";
        string InvoiceRoute(Guid id,Guid number)=>Details(id)+$"/invoices/{number}/download";
        var certificateRoute=Details(child.Id)+$"/certificates/{certificate.CertificateNumber}/download";
        async Task ChangeLink(Action<StudentGuardian> change) {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            change(await db.StudentGuardians.SingleAsync(x=>x.Id==link.Id));await db.SaveChangesAsync();
        }
        async Task<string> Snapshot() {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
                invoices=await db.Invoices.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),payments=await db.Payments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),resources=await db.LearningResources.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
                sessions=await db.ClassSessions.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),attendance=await db.AttendanceRecords.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),enrollments=await db.Enrollments.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),batches=await db.Batches.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),
                certificates=await db.Certificates.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        static string Digest(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        async Task<string> Send(string label,string path,HttpStatusCode status,string? actor) {
            await Task.Delay(650);var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Get,path);
            if(actor is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens[actor]);
            using var response=await client.SendAsync(request);var text=await response.Content.ReadAsStringAsync();var after=await Snapshot();
            Console.WriteLine("GUARDIANFLAGS EVIDENCE "+JsonSerializer.Serialize(new{label,path,status=(int)response.StatusCode,response=text,type=response.Content.Headers.ContentType?.MediaType,file=response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName,beforeDigest=Digest(before),afterDigest=Digest(after),bodyDigest=Digest(text)}));
            RequireFinanceStatus(response,status,label);if(before!=after)throw new InvalidOperationException("Permission GET modified captured SQL/Identity/audits: "+label);
            if(status!=HttpStatusCode.OK&&(text.Contains("QA-FLAGS-")||response.Content.Headers.ContentType?.MediaType=="text/html"))throw new InvalidOperationException("Denied response exposed protected content: "+label);
            if(status==HttpStatusCode.OK&&path.Contains("/invoices/")) {
                var file=(response.Content.Headers.ContentDisposition?.FileNameStar??response.Content.Headers.ContentDisposition?.FileName)?.Trim('"');
                if(response.Content.Headers.ContentType?.MediaType!="text/html"||file!=invoice.InvoiceNumber+".html"||!text.Contains(invoice.InvoiceNumber)||!text.Contains("Total: INR 1,000.00")||!text.Contains("Adjustments: INR 200.00")||!text.Contains("Paid: INR 300.00")||!text.Contains("Balance: INR 500.00"))throw new InvalidOperationException("Invoice HTML/financial contract changed: "+label);
            }
            return text;
        }
        void Pass(string label){count++;Console.WriteLine("GUARDIANFLAGS CASE "+label+" PASS.");}
        if(Environment.GetEnvironmentVariable("QA_GUARDIAN_FLAGS_BASELINE")=="1") {
            await ChangeLink(x=>{x.CanViewFinance=false;x.CanViewDocuments=false;x.CanViewAcademicProgress=true;});
            await Send("baseline-finance-download-bypass",InvoiceRoute(child.Id,invoice.Id),HttpStatusCode.OK,"parent");
            var text=await Send("baseline-nested-document-bypass",Details(child.Id),HttpStatusCode.OK,"parent");
            using var json=JsonDocument.Parse(text);var root=json.RootElement;
            if(root.GetProperty("invoices").GetArrayLength()!=0||root.GetProperty("resources").GetArrayLength()!=0||!root.GetProperty("classHistory").ToString().Contains("QA-FLAGS-PRIVATE"))throw new InvalidOperationException("Accepted nested bypass not reproduced");
            Console.WriteLine("GUARDIANFLAGS BASELINE: finance download and nested document disclosure reproduced; 2 unchanged-snapshot GETs; no repair pass cases.");return;
        }
        void AssertDetails(string label,string text,bool finance,bool documents,bool academic) {
            using var json=JsonDocument.Parse(text);var root=json.RootElement;
            if(root.GetProperty("name").GetString()!=child.FirstName+" "+child.LastName||root.GetProperty("invoices").GetArrayLength()!=(finance?1:0)||root.GetProperty("resources").GetArrayLength()!=(documents?3:0)||root.GetProperty("certificates").GetArrayLength()!=(documents?1:0)||root.GetProperty("classHistory").GetArrayLength()!=(academic?1:0))throw new InvalidOperationException("Permission projection mismatch: "+label);
            if(finance&&root.GetProperty("invoices")[0].GetProperty("balance").GetDecimal()!=500m)throw new InvalidOperationException("Finance balance projection changed");
            if(academic) {
                var history=root.GetProperty("classHistory")[0];
                if(history.GetProperty("sessionId").GetGuid()!=session.Id||history.GetProperty("batchName").GetString()!=batch.Name||history.GetProperty("status").GetString()!="Completed"||history.GetProperty("attendanceStatus").GetString()!="Present"||history.GetProperty("deliveryMode").GetString()!="Online"||history.GetProperty("startUtc").GetDateTime()!=session.StartUtc||history.GetProperty("endUtc").GetDateTime()!=session.EndUtc||history.GetProperty("resources").GetArrayLength()!=(documents?2:0))throw new InvalidOperationException("Class history metadata/resources mismatch: "+label);
                if(documents&&(!history.GetProperty("resources").ToString().Contains("QA-FLAGS-PRIVATE")||!history.GetProperty("resources").ToString().Contains("QA-FLAGS-COMMON")))throw new InvalidOperationException("Permitted attachment missing");
            }
            foreach(var marker in new[]{"QA-FLAGS-OTHER-PRIVATE","QA-FLAGS-UNPUBLISHED","QA-FLAGS-UNENROLLED","QA-FLAGS-FOREIGN"})if(text.Contains(marker))throw new InvalidOperationException("Out-of-scope disclosure: "+label);
            if(!documents&&(text.Contains("https://example.invalid/qa/")||text.Contains("QA-FLAGS-CERT")))throw new InvalidOperationException("Document restriction leaked bytes: "+label);
        }
        foreach(var finance in new[]{false,true})foreach(var documents in new[]{false,true})foreach(var academic in new[]{false,true}) {
            await ChangeLink(x=>{x.CanViewFinance=finance;x.CanViewDocuments=documents;x.CanViewAcademicProgress=academic;});
            var key=$"F{(finance?1:0)}D{(documents?1:0)}A{(academic?1:0)}";
            var text=await Send(key+"-details",Details(child.Id),HttpStatusCode.OK,"parent");AssertDetails(key,text,finance,documents,academic);Pass(key+"-details");
            await Send(key+"-invoice",InvoiceRoute(child.Id,invoice.Id),finance?HttpStatusCode.OK:HttpStatusCode.Forbidden,"parent");Pass(key+"-invoice");
            var cert=await Send(key+"-certificate",certificateRoute,documents?HttpStatusCode.OK:HttpStatusCode.Forbidden,"parent");
            if(documents&&!cert.Contains("<h2>Permission Learner</h2>"))throw new InvalidOperationException("Prior certificate learner identity regressed");Pass(key+"-certificate");
        }
        await ChangeLink(x=>{x.CanViewFinance=false;x.CanViewDocuments=false;x.CanViewAcademicProgress=false;});
        var own=await Send("learner-details-independent",Details(child.Id),HttpStatusCode.OK,"learner");AssertDetails("learner",own,true,true,true);Pass("learner-details-independent");
        await Send("learner-invoice-independent",InvoiceRoute(child.Id,invoice.Id),HttpStatusCode.OK,"learner");Pass("learner-invoice-independent");
        await ChangeLink(x=>{x.CanViewFinance=true;x.CanViewDocuments=true;x.CanViewAcademicProgress=true;});
        foreach(var actor in new string?[]{null,"other","foreign","admin","teacher"})foreach(var path in new[]{Details(child.Id),InvoiceRoute(child.Id,invoice.Id)}) {
            var label="denied-"+(actor??"anonymous")+(path.Contains("invoices")?"-invoice":"-details");await Send(label,path,actor is null?HttpStatusCode.Unauthorized:HttpStatusCode.Forbidden,actor);Pass(label);
        }
        foreach(var target in new[]{other.Id,foreignChild.Id,Guid.NewGuid()})foreach(var path in new[]{Details(target),InvoiceRoute(target,invoice.Id)}) {
            var label="wrong-child-"+target+(path.Contains("invoices")?"-invoice":"-details");await Send(label,path,HttpStatusCode.Forbidden,"parent");Pass(label);
        }
        foreach(var row in new[]{otherInvoice.Id,foreignInvoice.Id,Guid.NewGuid()}) {
            var label="scoped-missing-invoice-"+row;await Send(label,InvoiceRoute(child.Id,row),HttpStatusCode.NotFound,"parent");Pass(label);
        }
        await ChangeLink(x=>x.AccessRevokedAtUtc=DateTime.UtcNow);
        foreach(var path in new[]{Details(child.Id),InvoiceRoute(child.Id,invoice.Id)}) {var label="live-link-revoked-"+(path.Contains("invoices")?"invoice":"details");await Send(label,path,HttpStatusCode.Forbidden,"parent");Pass(label);}
        await ChangeLink(x=>{x.AccessRevokedAtUtc=null;x.CanAccessPortal=false;});
        foreach(var path in new[]{Details(child.Id),InvoiceRoute(child.Id,invoice.Id)}) {var label="portal-off-"+(path.Contains("invoices")?"invoice":"details");await Send(label,path,HttpStatusCode.Forbidden,"parent");Pass(label);}
        await ChangeLink(x=>{x.CanAccessPortal=true;x.CanViewFinance=false;x.CanViewDocuments=false;});
        await Send("live-finance-revoked",InvoiceRoute(child.Id,invoice.Id),HttpStatusCode.Forbidden,"parent");Pass("live-finance-revoked");
        var restricted=await Send("live-documents-revoked",Details(child.Id),HttpStatusCode.OK,"parent");AssertDetails("live-documents-revoked",restricted,false,false,true);Pass("live-documents-revoked");
        await ChangeLink(x=>{x.CanViewFinance=true;x.CanViewDocuments=true;});
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Students.SingleAsync(x=>x.Id==child.Id)).IsActive=false;await db.SaveChangesAsync();}
        foreach(var actor in new[]{"parent","learner"})foreach(var path in new[]{Details(child.Id),InvoiceRoute(child.Id,invoice.Id)}) {var label="inactive-"+actor+(path.Contains("invoices")?"-invoice":"-details");await Send(label,path,HttpStatusCode.Forbidden,actor);Pass(label);}
        Console.WriteLine($"GUARDIANFLAGS REGRESSION PASS: {count} real Identity/HTTP-SQL GET cases; all captured snapshots unchanged. Eight finance/document/academic combinations; existing certificate path retained; not browser/PDF/all-role/race acceptance.");
    }
}
