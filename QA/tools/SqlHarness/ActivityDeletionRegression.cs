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
    private static async Task VerifyActivityDeletionAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy,foreign,student,ownerId;int fixtureIndex=0,count=0;
        var baseline=Environment.GetEnvironmentVariable("QA_ACTIVITY_DELETION_BASELINE")=="1";
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();student=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();}
        ownerId=await CreateAccessActorAsync(factory,"qa-delete-platform@example.invalid","PlatformOwner",academy);
        using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var u=(await users.FindByIdAsync(ownerId.ToString()))!;u.IsPlatformOwner=true;u.AcademyId=null;u.DisplayName="Synthetic deletion owner";if(!(await users.UpdateAsync(u)).Succeeded)throw new InvalidOperationException("Deletion owner fixture failed");}
        var tokens=new Dictionary<string,string>{{"platform",await LoginAsync(client,"qa-delete-platform@example.invalid","Synthetic!39Ab")}};
        foreach(var (name,email) in new[]{("admin","qa-admin-a@example.invalid"),("foreign-admin","qa-admin-b@example.invalid"),("teacher","qa-teacher-a@example.invalid")})tokens[name]=await LoginAsync(client,email,"Synthetic!39Ab");
        foreach(var role in new[]{"Owner","Manager","FinanceUser","Guardian","Student","QA-DeleteDelegate","PlatformOwner"}){
            var email="qa-delete-"+role.ToLowerInvariant()+"@example.invalid";await CreateAccessActorAsync(factory,email,role,academy,role=="Student"?student:null,role=="QA-DeleteDelegate"?"[\"platform.manage\"]":"[]");tokens[role]=await LoginAsync(client,email,"Synthetic!39Ab");
        }
        client.DefaultRequestHeaders.Authorization=null;
        async Task<(DateTime From,DateTime To)> Seed(){fixtureIndex++;var from=new DateTime(2001,1,1,0,0,0,DateTimeKind.Utc).AddDays(fixtureIndex*2);var to=from.AddHours(1);var times=new[]{from.AddTicks(-1),from,from.AddMinutes(20),to.AddTicks(-1),to,to.AddTicks(1)};
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            for(int i=0;i<times.Length;i++){db.Add(new PlatformAuditEntry {ActorUserId=i%2==0?ownerId:null,ActorName="Synthetic historical actor",Action="QA-DELETE-P-"+fixtureIndex+"-"+i,EntityType="QA-Synthetic",EntityId=Guid.NewGuid(),MetadataJson="{\"sentinel\":\"preserve original row\"}",OccurredAtUtc=times[i]});db.Add(new AuditLog {AcademyId=i%2==0?academy:foreign,ActorUserId=i%2==0?ownerId:null,Action="QA-DELETE-A-"+fixtureIndex+"-"+i,EntityType="QA-Synthetic",EntityId=Guid.NewGuid(),MetadataJson=null,IpAddress="127.0.0.1",OccurredAtUtc=times[i]});}await db.SaveChangesAsync();return(from,to);}
        async Task<string> Snapshot(){using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var identity=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();return JsonSerializer.Serialize(new{platform=await db.PlatformAuditEntries.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),admin=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),students=await db.Students.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),notifications=await db.Notifications.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),users=await identity.Users.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),roles=await identity.Roles.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),memberships=await identity.UserRoles.AsNoTracking().OrderBy(x=>x.UserId).ThenBy(x=>x.RoleId).ToListAsync()});}
        static string Digest(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        void Pass(string label){count++;Console.WriteLine("ACTIVITYDELETE CASE "+label+" PASS.");}
        string Body(DateTime from,DateTime to,string? target,bool omit=false){var map=new Dictionary<string,object?>{{"fromUtc",from.ToString("O")},{"toUtc",to.ToString("O")}};if(!omit)map["scope"]=target;return JsonSerializer.Serialize(map);}
        async Task Request(string label,string? actor,string body,HttpStatusCode status,bool changes,DateTime from,DateTime to,string? target,bool expectedBoth=false){
            await Task.Delay(650);var before=await Snapshot();using var request=new HttpRequestMessage(HttpMethod.Delete,"/api/platform/activity-logs");if(actor is not null)request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",tokens[actor]);request.Content=new StringContent(body,Encoding.UTF8,"application/json");using var response=await client.SendAsync(request);var text=await response.Content.ReadAsStringAsync();var after=await Snapshot();RequireFinanceStatus(response,status,label);
            var deletedPlatform=new List<Guid>();var deletedAdmin=new List<Guid>();Guid? addedAudit=null;
            if(!changes){if(before!=after)throw new InvalidOperationException("Rejected deletion changed captured logs/data/Identity: "+label);}
            else {
                using var bDoc=JsonDocument.Parse(before);using var aDoc=JsonDocument.Parse(after);var b=bDoc.RootElement;var a=aDoc.RootElement;
                bool Matches(JsonElement row)=>row.GetProperty("OccurredAtUtc").GetDateTime()>=from&&row.GetProperty("OccurredAtUtc").GetDateTime()<to;
                var includePlatform=expectedBoth||target is null||target.Equals("PlatformOwner",StringComparison.OrdinalIgnoreCase);
                var includeAdmin=expectedBoth||target is null||target.Equals("AcademyAdmin",StringComparison.OrdinalIgnoreCase);
                foreach(var (store,include,deleted) in new[]{("platform",includePlatform,deletedPlatform),("admin",includeAdmin,deletedAdmin)}){
                    var old=b.GetProperty(store).EnumerateArray().ToArray();var now=a.GetProperty(store).EnumerateArray().ToDictionary(x=>x.GetProperty("Id").GetGuid());
                    foreach(var row in old){var id=row.GetProperty("Id").GetGuid();if(include&&Matches(row)){deleted.Add(id);if(now.ContainsKey(id))throw new InvalidOperationException("Selected audit row survived: "+label);}else if(!now.TryGetValue(id,out var saved)||row.GetRawText()!=saved.GetRawText())throw new InvalidOperationException("Out-of-scope/boundary row changed: "+label);}
                    var existing=old.Select(x=>x.GetProperty("Id").GetGuid()).ToHashSet();var extra=now.Where(x=>!existing.Contains(x.Key)).Select(x=>x.Value).ToArray();
                    if(store=="admin"){if(extra.Length!=0)throw new InvalidOperationException("Unexpected academy audit addition");}
                    else {
                        if(extra.Length!=1)throw new InvalidOperationException("Expected exact one deletion audit: "+label);var audit=extra[0];addedAudit=audit.GetProperty("Id").GetGuid();
                        if(audit.GetProperty("ActorUserId").GetGuid()!=ownerId||audit.GetProperty("ActorName").GetString()!="Synthetic deletion owner"||audit.GetProperty("Action").GetString()!="Activity logs deleted"||audit.GetProperty("EntityType").GetString()!="ActivityLog"||audit.GetProperty("EntityId").ValueKind!=JsonValueKind.Null)throw new InvalidOperationException("Deletion actor/audit identity mismatch");
                        using var meta=JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!);var m=meta.RootElement;
                        if(m.GetProperty("Scope").GetString()!=target||m.GetProperty("FromUtc").GetDateTime()!=from||m.GetProperty("ToUtc").GetDateTime()!=to||m.GetProperty("PlatformLogs").GetInt32()!=deletedPlatform.Count)throw new InvalidOperationException("Deletion metadata scope/date/platform count mismatch");
                    }
                }
                var auditRow=a.GetProperty("platform").EnumerateArray().Single(x=>x.GetProperty("Id").GetGuid()==addedAudit);using(var meta=JsonDocument.Parse(auditRow.GetProperty("MetadataJson").GetString()!))if(meta.RootElement.GetProperty("AdminLogs").GetInt32()!=deletedAdmin.Count)throw new InvalidOperationException("Deletion admin audit count mismatch");
                using var output=JsonDocument.Parse(text);if(output.RootElement.EnumerateObject().Count()!=1||output.RootElement.GetProperty("deleted").GetInt32()!=deletedPlatform.Count+deletedAdmin.Count)throw new InvalidOperationException("Deletion count response mismatch");
                foreach(var field in b.EnumerateObject().Where(x=>x.Name!="platform"&&x.Name!="admin"))if(field.Value.GetRawText()!=a.GetProperty(field.Name).GetRawText())throw new InvalidOperationException("Deletion touched unrelated captured collection");
            }
            Console.WriteLine("ACTIVITYDELETE EVIDENCE "+JsonSerializer.Serialize(new{label,actor,requestBody=body,status=(int)response.StatusCode,response=text,beforeDigest=Digest(before),afterDigest=Digest(after),bodyDigest=Digest(text),deletedPlatform,deletedAdmin,addedAudit,exactSnapshotVerified=true}));if(!baseline)Pass(label);
        }
        if(baseline){foreach(var target in new[]{"AcademyAdmn",""," PlatformOwner "}){var f=await Seed();await Request("baseline-unsafe-scope-"+fixtureIndex,"platform",Body(f.From,f.To,target),HttpStatusCode.OK,true,f.From,f.To,target,true);}Console.WriteLine("ACTIVITYDELETE BASELINE: three invalid nonnull scope requests each deleted both owned stores; exact selected IDs/survivors/success audit verified. No passing repair cases; no real logs targeted.");return;}
        foreach(var target in new[]{"AcademyAdmn",""," ","\t","All","Both","Owner","Admin"," PlatformOwner ","AcademyAdmin ","PlatformOwner,AcademyAdmin"}){var f=await Seed();await Request("invalid-scope-"+fixtureIndex,"platform",Body(f.From,f.To,target),HttpStatusCode.BadRequest,false,f.From,f.To,target);}
        foreach(var target in new string?[]{"PlatformOwner","AcademyAdmin","platformowner","aCaDeMyAdMiN",null}){var f=await Seed();await Request("valid-scope-"+(target??"null-all"),"platform",Body(f.From,f.To,target),HttpStatusCode.OK,true,f.From,f.To,target);await Request("repeat-empty-"+(target??"null-all"),"platform",Body(f.From,f.To,target),HttpStatusCode.OK,true,f.From,f.To,target);}
        var omitted=await Seed();await Request("legacy-omitted-scope-all","platform",Body(omitted.From,omitted.To,null,true),HttpStatusCode.OK,true,omitted.From,omitted.To,null);
        var empty=await Seed();await Request("empty-range-retains-all","platform",Body(empty.To.AddHours(2),empty.To.AddHours(3),null),HttpStatusCode.OK,true,empty.To.AddHours(2),empty.To.AddHours(3),null);
        foreach(var equal in new[]{true,false}){var f=await Seed();var to=equal?f.From:f.From.AddTicks(-1);await Request(equal?"equal-dates":"reversed-dates","platform",Body(f.From,to,null),HttpStatusCode.BadRequest,false,f.From,to,null);}
        foreach(var actor in new string?[]{null,"admin","foreign-admin","teacher","Owner","Manager","FinanceUser","Guardian","Student","QA-DeleteDelegate","PlatformOwner"}){var f=await Seed();await Request("denied-"+(actor??"anonymous"),actor,Body(f.From,f.To,null),actor is null?HttpStatusCode.Unauthorized:HttpStatusCode.Forbidden,false,f.From,f.To,null);}
        foreach(var body in new[]{"{broken","null","", "{\"fromUtc\":\"invalid\",\"toUtc\":\"2001-01-03T01:00:00Z\",\"scope\":null}","{\"fromUtc\":\"2001-01-03T00:00:00Z\",\"toUtc\":\"2001-01-03T01:00:00Z\",\"scope\":123}","{\"fromUtc\":\"2001-01-03T00:00:00Z\",\"toUtc\":\"2001-01-03T01:00:00Z\",\"scope\":[]}"}){var f=await Seed();await Request("invalid-json-"+fixtureIndex,"platform",body,HttpStatusCode.BadRequest,false,f.From,f.To,null);}
        Console.WriteLine($"ACTIVITYDELETE REGRESSION PASS: {count} real Identity/HTTP-SQL cases; exact selected IDs and full surviving rows, one success audit per accepted request, unchanged rejection snapshots. Only owned synthetic logs deleted; not browser/cancel/critical/race acceptance.");
    }
}
