using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    private static async Task VerifyAnnouncementAudienceAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy,foreign,student,teacher,guardian,revoked; StudentGuardian liveLink; int count=0;
        var baseline=Environment.GetEnvironmentVariable("QA_ANNOUNCEMENT_AUDIENCE_BASELINE")=="1";
        using(var scope=factory.Services.CreateScope()) {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
            foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            student=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            teacher=await db.Teachers.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            var parent=new Guardian {AcademyId=academy,FirstName="Announcement",LastName="Parent"};
            var denied=new Guardian {AcademyId=academy,FirstName="Revoked",LastName="Parent"};guardian=parent.Id;revoked=denied.Id;
            liveLink=new() {AcademyId=academy,StudentId=student,GuardianId=guardian,CanAccessPortal=true};
            db.AddRange(parent,denied,liveLink,new StudentGuardian {AcademyId=academy,StudentId=student,GuardianId=revoked,CanAccessPortal=true,AccessRevokedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
        }
        var tokens=new Dictionary<string,string>();var ids=new Dictionary<string,Guid>();
        var specs=new[]{
            ("owner","Owner",(Guid?)null,(Guid?)null,(Guid?)null),
            ("manager","Manager",(Guid?)null,(Guid?)null,(Guid?)null),
            ("finance","FinanceUser",(Guid?)null,(Guid?)null,(Guid?)null),
            ("unlinked","QA-Announcement",(Guid?)null,(Guid?)null,(Guid?)null),
            ("guardian","Guardian",(Guid?)null,(Guid?)null,(Guid?)guardian),
            ("revoked-guardian","Guardian",(Guid?)null,(Guid?)null,(Guid?)revoked),
            ("student","Student",(Guid?)student,(Guid?)null,(Guid?)null),
            ("unlinked-student","Student",(Guid?)null,(Guid?)null,(Guid?)null),
            ("unlinked-teacher","Teacher",(Guid?)null,(Guid?)null,(Guid?)null),
            ("dual-student-teacher","Student",(Guid?)student,(Guid?)teacher,(Guid?)null),
            ("dual-guardian-student","Guardian",(Guid?)student,(Guid?)null,(Guid?)guardian),
            ("dual-guardian-teacher","Guardian",(Guid?)null,(Guid?)teacher,(Guid?)guardian),
            ("dual-guardian-admin","AcademyAdmin",(Guid?)null,(Guid?)null,(Guid?)guardian),
            ("dual-admin-student","AcademyAdmin",(Guid?)student,(Guid?)null,(Guid?)null),
            ("platform","PlatformOwner",(Guid?)null,(Guid?)null,(Guid?)null)};
        foreach(var (name,role,s,t,g) in specs) {
            var email="qa-announcement-"+name+"@example.invalid";var id=await CreateAccessActorAsync(factory,email,role,academy,s,role=="QA-Announcement"?"[\"communications.manage\"]":"[]");ids[name]=id;
            using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(id.ToString()))!;user.TeacherId=t;user.GuardianId=g;if(name=="platform"){user.AcademyId=null;user.IsPlatformOwner=true;}if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("Announcement identity fixture failed");}
            tokens[name]=await LoginAsync(client,email,"Synthetic!39Ab");
        }
        foreach(var (name,email) in new[]{("admin","qa-admin-a@example.invalid"),("teacher","qa-teacher-a@example.invalid"),("foreign-admin","qa-admin-b@example.invalid")})tokens[name]=await LoginAsync(client,email,"Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization=null;
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();return JsonSerializer.Serialize(new{notifications=await db.Notifications.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),receipts=await db.NotificationReadReceipts.AsNoTracking().OrderBy(x=>x.NotificationId).ThenBy(x=>x.UserId).ToListAsync(),platformAudits=await db.PlatformAuditEntries.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),teachers=await db.Teachers.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),guardians=await db.Guardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),links=await db.StudentGuardians.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),roles=await identity.Roles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),userRoles=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToListAsync()});}
        static string Digest(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        void Pass(string label){count++;Console.WriteLine("ANNOUNCEMENTAUDIENCE CASE "+label+" PASS.");}
        var beforePublish=await Snapshot();Guid published;
        using(var request=new HttpRequestMessage(HttpMethod.Post,"/api/platform/announcements")){request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens["platform"]);request.Content=JsonContent.Create(new{academyId=academy,title=" QA-ADMIN-OWNER ",message=" Private academy administrator message ",displayHours=4});using var response=await client.SendAsync(request);RequireFinanceStatus(response,HttpStatusCode.OK,"owner-publish");using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());published=json.RootElement.GetProperty("id").GetGuid();}
        var afterPublish=await Snapshot();
        using(var before=JsonDocument.Parse(beforePublish))using(var after=JsonDocument.Parse(afterPublish)){
            var b=before.RootElement;var a=after.RootElement;var fresh=a.GetProperty("notifications").EnumerateArray().Where(x=>x.GetProperty("Id").GetGuid()==published).Single();
            if(a.GetProperty("notifications").GetArrayLength()!=b.GetProperty("notifications").GetArrayLength()+1||fresh.GetProperty("AcademyId").GetGuid()!=academy||fresh.GetProperty("RecipientId").ValueKind!=JsonValueKind.Null||fresh.GetProperty("RecipientType").GetString()!="Academy"||fresh.GetProperty("Title").GetString()!="QA-ADMIN-OWNER"||fresh.GetProperty("Message").GetString()!="Private academy administrator message"||fresh.GetProperty("Status").GetString()!="Sent"||fresh.GetProperty("Channel").GetString()!="InApp")throw new InvalidOperationException("Owner publication readback differs");
            using var metadata=JsonDocument.Parse(fresh.GetProperty("VariablesJson").GetString()!);if(metadata.RootElement.GetProperty("audiences").GetString()!="Admin"||metadata.RootElement.GetProperty("important").GetString()!="true"||metadata.RootElement.GetProperty("expiresAtUtc").GetDateTime()<=DateTime.UtcNow)throw new InvalidOperationException("Owner audience metadata differs");
            if(a.GetProperty("platformAudits").GetArrayLength()!=b.GetProperty("platformAudits").GetArrayLength()+1||!a.GetProperty("platformAudits").EnumerateArray().Any(x=>x.GetProperty("EntityId").GetGuid()==published&&x.GetProperty("ActorUserId").GetGuid()==ids["platform"]&&x.GetProperty("Action").GetString()=="Academy admin announcement published"))throw new InvalidOperationException("Owner publication audit missing");
            foreach(var property in b.EnumerateObject().Where(x=>x.Name!="notifications"&&x.Name!="platformAudits"))if(property.Value.GetRawText()!=a.GetProperty(property.Name).GetRawText())throw new InvalidOperationException("Owner publication changed unrelated captured rows");
        }
        Console.WriteLine("ANNOUNCEMENTAUDIENCE PUBLICATION "+JsonSerializer.Serialize(new{published,academy,actor=ids["platform"],beforeDigest=Digest(beforePublish),afterDigest=Digest(afterPublish),persistedAdminAudience=true,exactOneNotificationAndPlatformAudit=true}));if(!baseline)Pass("owner-publication-readback-audit");
        var rows=new List<Notification>();
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();rows.Add(await db.Notifications.AsNoTracking().SingleAsync(x=>x.Id==published));}
        string Metadata(string? audience,DateTime? start=null,DateTime? expiry=null,string important="true") {var map=new Dictionary<string,string>{{"important",important},{"startsAtUtc",(start??DateTime.UtcNow.AddHours(-1)).ToString("O")},{"expiresAtUtc",(expiry??DateTime.UtcNow.AddHours(3)).ToString("O")}};if(audience is not null)map["audiences"]=audience;return JsonSerializer.Serialize(map);}
        async Task Read(string label,string? actor,HttpStatusCode status,IEnumerable<Notification> expected) {
            await Task.Delay(650);var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Get,"/api/portal/announcements");if(actor is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens[actor]);using var response=await client.SendAsync(request);var text=await response.Content.ReadAsStringAsync();var after=await Snapshot();
            Console.WriteLine("ANNOUNCEMENTAUDIENCE EVIDENCE "+JsonSerializer.Serialize(new{label,actor,status=(int)response.StatusCode,response=text,beforeDigest=Digest(before),afterDigest=Digest(after),bodyDigest=Digest(text)}));RequireFinanceStatus(response,status,label);if(before!=after)throw new InvalidOperationException("Announcement read mutated captured rows: "+label);
            if(status==HttpStatusCode.OK){using var json=JsonDocument.Parse(text);var actual=json.RootElement.EnumerateArray().ToArray();var wanted=expected.OrderByDescending(x=>x.CreatedAtUtc).ToArray();if(actual.Length!=wanted.Length)throw new InvalidOperationException("Announcement audience count: "+label);for(int i=0;i<actual.Length;i++){var x=actual[i];var e=wanted[i];if(x.EnumerateObject().Count()!=4||x.GetProperty("id").GetGuid()!=e.Id||x.GetProperty("title").GetString()!=e.Title||x.GetProperty("message").GetString()!=e.Message||x.GetProperty("createdAtUtc").GetDateTime()!=e.CreatedAtUtc)throw new InvalidOperationException("Exact announcement payload/order mismatch: "+label);}}
            else if(text.Contains("QA-ADMIN-OWNER"))throw new InvalidOperationException("Forbidden audience bytes");
            if(!baseline)Pass(label);
        }
        if(baseline){await Read("baseline-admin-control","admin",HttpStatusCode.OK,rows);foreach(var actor in new[]{"guardian","unlinked","manager"})await Read("baseline-admin-disclosure-"+actor,actor,HttpStatusCode.OK,rows);Console.WriteLine("ANNOUNCEMENTAUDIENCE BASELINE: three non-administrator disclosures reproduced after real owner publication; admin positive control; four unchanged GETs; no passing repair cases.");return;}
        var ownBase=DateTime.UtcNow.AddMinutes(-1);Notification foreignRow;
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();foreach(var (label,audience) in new[]{("Student","Student"),("Teacher","Teacher"),("Both","Both"),("Case"," aDmIn , Teacher "),("Legacy",(string?)null)}){var row=new Notification{AcademyId=academy,RecipientType="Academy",Title="QA-AUDIENCE-"+label,Message="Synthetic message "+label,Status="Sent",VariablesJson=Metadata(audience),CreatedAtUtc=ownBase.AddSeconds(-rows.Count)};rows.Add(row);db.Add(row);}foreignRow=new(){AcademyId=foreign,RecipientType="Academy",Title="QA-FOREIGN-ADMIN",Message="Other tenant private message",Status="Sent",VariablesJson=Metadata("Admin")};db.Add(foreignRow);await db.SaveChangesAsync();}
        Notification[] For(string audience)=>rows.Where(x=>{using var json=JsonDocument.Parse(x.VariablesJson!);return !json.RootElement.TryGetProperty("audiences",out var target)||target.GetString()!.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Any(x=>x.Equals(audience,StringComparison.OrdinalIgnoreCase)||x=="Both");}).ToArray();
        foreach(var actor in new[]{"admin","owner","dual-guardian-admin"})await Read("role-"+actor,actor,HttpStatusCode.OK,For("Admin"));
        foreach(var actor in new[]{"student","dual-student-teacher","dual-guardian-student","dual-admin-student"})await Read("role-"+actor,actor,HttpStatusCode.OK,For("Student"));
        foreach(var actor in new[]{"teacher","dual-guardian-teacher"})await Read("role-"+actor,actor,HttpStatusCode.OK,For("Teacher"));
        foreach(var actor in new[]{"guardian","revoked-guardian","manager","finance","unlinked","unlinked-student","unlinked-teacher"})await Read("role-"+actor,actor,HttpStatusCode.OK,[]);
        await Read("foreign-admin-scoped","foreign-admin",HttpStatusCode.OK,[foreignRow]);await Read("anonymous",null,HttpStatusCode.Unauthorized,[]);await Read("no-academy-platform","platform",HttpStatusCode.Forbidden,[]);
        var both=rows.Single(x=>x.Title=="QA-AUDIENCE-Both");
        foreach(var variant in new[]{"future","expired","cancelled","not-important","malformed","targeted","wrong-recipient"}) {
            using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.Notifications.SingleAsync(x=>x.Id==both.Id);row.Status=variant=="cancelled"?"Cancelled":"Sent";row.RecipientId=variant=="targeted"?student:null;row.RecipientType=variant=="wrong-recipient"?"Student":"Academy";row.VariablesJson=variant=="malformed"?"{broken":Metadata("Both",variant=="future"?DateTime.UtcNow.AddDays(1):null,variant=="expired"?DateTime.UtcNow.AddDays(-1):null,variant=="not-important"?"false":"true");await db.SaveChangesAsync();}
            foreach(var (actor,audience) in new[]{("admin","Admin"),("student","Student"),("teacher","Teacher")})await Read("metadata-"+variant+"-"+actor,actor,HttpStatusCode.OK,For(audience).Where(x=>x.Id!=both.Id));
        }
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var row=await db.Notifications.SingleAsync(x=>x.Id==both.Id);row.Status="Sent";row.RecipientId=null;row.RecipientType="Academy";row.VariablesJson=both.VariablesJson;await db.SaveChangesAsync();}
        async Task Role(string name,string role,bool add){using var scope=factory.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(ids[name].ToString()))!;var result=add?await users.AddToRoleAsync(user,role):await users.RemoveFromRoleAsync(user,role);if(!result.Succeeded)throw new InvalidOperationException("Live role fixture failed");}
        await Role("dual-guardian-admin","AcademyAdmin",false);await Read("admin-role-revoked-same-token","dual-guardian-admin",HttpStatusCode.OK,[]);
        await Role("dual-guardian-admin","AcademyAdmin",true);await Read("admin-role-restored-same-token","dual-guardian-admin",HttpStatusCode.OK,For("Admin"));
        await Role("unlinked","Owner",true);await Read("owner-role-granted-same-token","unlinked",HttpStatusCode.OK,For("Admin"));await Role("unlinked","Owner",false);await Read("owner-role-removed-same-token","unlinked",HttpStatusCode.OK,[]);
        foreach(var portalOff in new[]{false,true}){using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var l=await db.StudentGuardians.SingleAsync(x=>x.Id==liveLink.Id);l.AccessRevokedAtUtc=portalOff?null:DateTime.UtcNow;l.CanAccessPortal=!portalOff;await db.SaveChangesAsync();}await Read(portalOff?"guardian-portal-off":"guardian-link-revoked","guardian",HttpStatusCode.OK,[]);}
        Console.WriteLine($"ANNOUNCEMENTAUDIENCE REGRESSION PASS: {count} real HTTP-SQL cases; one owned publication with exact persisted row/platform audit; all GET captured snapshots unchanged. Live administrative roles, preserved link precedence, metadata and tenant isolation; not browser/full-portal/all-lifecycle/race acceptance.");
    }
}
