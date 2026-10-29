using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingAdmissionAsync(QaApiFactory factory,HttpClient client,QaRunManifest manifest)
    {
        Guid academy,foreign,actorId,studentId;MeetingReceiptState reviewed,unreviewed;Guid unknownOutcome,pending,foreignOperation,inconsistent;
        var auth=await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        var other=await LoginAsync(client,"qa-receipt-other@example.invalid","Synthetic!39Ab");
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();foreign=await db.Academies.Where(x=>x.Name=="Synthetic Academy B").Select(x=>x.Id).SingleAsync();
            actorId=(await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;studentId=await db.Students.Where(x=>x.AcademyId==academy).Select(x=>x.Id).SingleAsync();
            var batch=await db.Batches.SingleAsync(x=>x.AcademyId==academy);
            var request=new MeetingPreviewRequest("GoogleWorkspace",batch.Id,batch.TeacherId!.Value,"Synthetic private admission",new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(3).AddHours(17),DateTimeKind.Unspecified),TimeSpan.FromMinutes(330)),"Asia/Kolkata",60,[studentId],"Once","InviteesOnly","Disabled","Off");
            var receiptService=scope.ServiceProvider.GetRequiredService<MeetingReceiptService>();
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,actorId.ToString())},"synthetic-fixture"));
            unreviewed=(await receiptService.PrepareAsync(principal,academy,request,"admission-unreviewed",default)).Receipt??throw new InvalidOperationException("Admission receipt setup failed");
            var prepared=(await receiptService.PrepareAsync(principal,academy,request,"admission-reviewed",default)).Receipt??throw new InvalidOperationException("Admission receipt setup failed");
            reviewed=(await receiptService.ReviewAsync(principal,academy,prepared.Id,new(0,prepared.RequestHash,prepared.SourceVersion),default)).Receipt??throw new InvalidOperationException("Admission review setup failed");
            // Synthetic stored-state fixtures are NOT operations created by this endpoint.
            MeetingOperation AddOperation(Guid ownerAcademy,string status,bool withBox=true)
            {
                var row=SyntheticOperation(ownerAcademy);row.ActorUserId=actorId;row.Status=status;row.ProtectedEnvelope="private-status-payload";db.Add(row);
                if(withBox)db.Add(new MeetingOutbox{AcademyId=ownerAcademy,OperationId=row.Id});return row;
            }
            unknownOutcome=AddOperation(academy,"AwaitingReview").Id;pending=AddOperation(academy,"PendingDispatch").Id;
            foreignOperation=AddOperation(foreign,"Held").Id;inconsistent=AddOperation(academy,"Held",false).Id;await db.SaveChangesAsync();
        }
        var options=new JsonSerializerOptions(JsonSerializerDefaults.Web);var checks=new List<object>();
        string Root(Guid id)=>$"/api/academies/{id}/meetings";
        MeetingAdmissionRequest Body(MeetingReceiptState receipt)=>new(receipt.Id,receipt.Version,receipt.RequestHash,receipt.SourceVersion);
        async Task<string> Snapshot()
        {
            using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new{operations=await db.MeetingOperations.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),outbox=await db.MeetingOutbox.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),receipts=await db.MeetingPreviewReceipts.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audits=await db.AuditLogs.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),notifications=await db.Notifications.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),bindings=await db.MeetingProviderBindings.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),attempts=await db.MeetingOAuthAttempts.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),callbacks=await db.MeetingOAuthCallbacks.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),exchanges=await db.MeetingOAuthExchanges.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),accountReviews=await db.MeetingAccountReviews.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),revocations=await db.MeetingCredentialRevocations.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});
        }
        async Task<HttpStatusCode> Raw(HttpMethod method,string path,object? body,string? bearer,string? key,HttpClient? transport=null)
        {
            using var call=new HttpRequestMessage(method,path);if(bearer is not null)call.Headers.Authorization=new AuthenticationHeaderValue("Bearer",bearer);if(key is not null)call.Headers.Add("Idempotency-Key",key);
            if(method==HttpMethod.Post)call.Content=JsonContent.Create(body,options:options);
            using var response=await (transport??client).SendAsync(call);var text=await response.Content.ReadAsStringAsync();
            if(response.StatusCode==HttpStatusCode.OK)
            {
                var state=JsonSerializer.Deserialize<MeetingOperationStatus>(text,options)??throw new InvalidOperationException("Missing operation status");
                if(state.CanExecute||state.CanRetry||state.UpdatedAtUtc.Kind!=DateTimeKind.Utc||state.ExternalOutcome is not("Unknown" or "NotConfirmed")||text.Contains("private",StringComparison.OrdinalIgnoreCase)||text.Contains("leaseId",StringComparison.OrdinalIgnoreCase)||text.Contains("protectedEnvelope",StringComparison.OrdinalIgnoreCase)||response.Headers.CacheControl?.NoStore!=true)throw new InvalidOperationException("Unsafe/cached operation status or missing UTC timestamp");
                if(state.Id==unknownOutcome&&(state.Status!="AwaitingReview"||state.ExternalOutcome!="Unknown"||!state.RequiresReview))throw new InvalidOperationException("Unknown outcome reported incorrectly");
            }
            if(response.StatusCode==HttpStatusCode.ServiceUnavailable&&(!text.Contains("MeetingExecutionUnavailable",StringComparison.Ordinal)||!text.Contains("Nothing was queued",StringComparison.Ordinal)||response.Headers.CacheControl?.NoStore!=true))throw new InvalidOperationException("Unavailable response inaccurate");
            return response.StatusCode;
        }
        async Task Check(string label,HttpMethod method,string path,object? body,HttpStatusCode expected,string? bearer=null,string? key="creation-key",HttpClient? transport=null,bool anonymous=false)
        {
            await Task.Delay(700);var before=await Snapshot();var status=await Raw(method,path,body,anonymous?null:bearer??auth,key,transport);
            if(status!=expected||before!=await Snapshot())throw new InvalidOperationException($"MEETING ADMISSION {label}: expected {(int)expected}, got {(int)status}, or unexpected SQL write");
            checks.Add(new{label,status=(int)status});Console.WriteLine($"MEETING ADMISSION CASE {label} PASS status={(int)status}; no SQL writes.");
        }
        await Check("unreviewed-not-approved",HttpMethod.Post,Root(academy),Body(unreviewed),HttpStatusCode.Conflict);
        await Check("invalid-key",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.BadRequest,key:"short");
        await Check("missing-receipt",HttpMethod.Post,Root(academy),Body(reviewed) with{ReceiptId=Guid.NewGuid()},HttpStatusCode.NotFound);
        await Check("client-approved-rejected",HttpMethod.Post,Root(academy),new{reviewed.Id,approved=true},HttpStatusCode.BadRequest);
        await Check("model-academy-rejected",HttpMethod.Post,Root(academy),new{academyId=academy},HttpStatusCode.BadRequest);
        await Check("sql-field-rejected",HttpMethod.Post,Root(academy),new{sql="not executable"},HttpStatusCode.BadRequest);
        await Check("null-body",HttpMethod.Post,Root(academy),null,HttpStatusCode.BadRequest);
        await Check("malformed-hash",HttpMethod.Post,Root(academy),Body(reviewed) with{RequestHash="bad"},HttpStatusCode.BadRequest);
        await Check("stale-version",HttpMethod.Post,Root(academy),Body(reviewed) with{ExpectedVersion=0},HttpStatusCode.Conflict);
        await Check("reviewed-still-unavailable",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.ServiceUnavailable);
        await Check("same-key-no-queue-replay",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.ServiceUnavailable);
        await Task.Delay(700);var beforeRace=await Snapshot();var race=await Task.WhenAll(Raw(HttpMethod.Post,Root(academy),Body(reviewed),auth,"concurrent-key"),Raw(HttpMethod.Post,Root(academy),Body(reviewed),auth,"concurrent-key"));
        if(race.Any(x=>x!=HttpStatusCode.ServiceUnavailable)||beforeRace!=await Snapshot())throw new InvalidOperationException("Unavailable concurrent admission queued work");checks.Add(new{label="concurrent-unavailable",status="503/503"});Console.WriteLine("MEETING ADMISSION CASE concurrent-unavailable PASS503/503; no SQL writes.");
        await Check("own-unknown-status",HttpMethod.Get,Root(academy)+$"/operations/{unknownOutcome}",null,HttpStatusCode.OK);
        await Check("own-pending-status",HttpMethod.Get,Root(academy)+$"/operations/{pending}",null,HttpStatusCode.OK);
        await Check("other-admin-status-denied",HttpMethod.Get,Root(academy)+$"/operations/{unknownOutcome}",null,HttpStatusCode.NotFound,bearer:other);
        await Check("foreign-status-route",HttpMethod.Get,Root(foreign)+$"/operations/{foreignOperation}",null,HttpStatusCode.Forbidden);
        await Check("foreign-id-own-route",HttpMethod.Get,Root(academy)+$"/operations/{foreignOperation}",null,HttpStatusCode.NotFound);
        await Check("other-admin-receipt-denied",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.NotFound,bearer:other);
        await Check("anonymous-status",HttpMethod.Get,Root(academy)+$"/operations/{pending}",null,HttpStatusCode.Unauthorized,anonymous:true);
        await Check("anonymous-admission",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Unauthorized,anonymous:true);
        foreach(var role in new[]{"Teacher","Student"})
        {
            var email="qa-admission-"+role.ToLowerInvariant()+"@example.invalid";await CreateAccessActorAsync(factory,email,role,academy);var ticket=await LoginAsync(client,email,"Synthetic!39Ab");
            await Check(role+"-status-denied",HttpMethod.Get,Root(academy)+$"/operations/{pending}",null,HttpStatusCode.Forbidden,bearer:ticket);
            await Check(role+"-admission-denied",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Forbidden,bearer:ticket);
        }
        await CreateAccessActorAsync(factory,"qa-admission-platform@example.invalid","PlatformOwner",foreign);var platform=await LoginAsync(client,"qa-admission-platform@example.invalid","Synthetic!39Ab");
        await Check("platform-foreign-status-denied",HttpMethod.Get,Root(academy)+$"/operations/{pending}",null,HttpStatusCode.Forbidden,bearer:platform);
        await Check("platform-foreign-admission-denied",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Forbidden,bearer:platform);
        await Check("missing-operation",HttpMethod.Get,Root(academy)+$"/operations/{Guid.NewGuid()}",null,HttpStatusCode.NotFound);
        await Check("inconsistent-operation",HttpMethod.Get,Root(academy)+$"/operations/{inconsistent}",null,HttpStatusCode.Conflict);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Students.SingleAsync(x=>x.Id==studentId)).Email="admission-changed@example.invalid";await db.SaveChangesAsync();}
        await Check("source-changed-admission",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Conflict);
        using(var future=new QaApiFactory(manifest,isolatedClock:new MeetingFutureClock()))using(var futureClient=future.CreateClient(new(){AllowAutoRedirect=false}))
            await Check("expired-admission",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Gone,transport:futureClient);
        using(var scope=factory.Services.CreateScope()){var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByIdAsync(actorId.ToString()))!;if(!(await users.RemoveFromRoleAsync(user,"AcademyAdmin")).Succeeded)throw new InvalidOperationException("Admission role revocation failed");}
        await Check("revoked-old-ticket-status",HttpMethod.Get,Root(academy)+$"/operations/{pending}",null,HttpStatusCode.Forbidden);
        await Check("revoked-old-ticket-admission",HttpMethod.Post,Root(academy),Body(reviewed),HttpStatusCode.Forbidden);
        if(checks.Count!=32)throw new InvalidOperationException("Admission case count mismatch "+checks.Count);
        var evidence=Path.Combine("QA","EVIDENCE","meeting-admission-sql-"+manifest.RunId.ToString("N"));Directory.CreateDirectory(evidence);
        var sources=new[]{"apps/api/Infrastructure/Meetings/MeetingOperationService.cs","apps/api/Controllers/MeetingsController.cs","apps/api/Program.cs","QA/tools/SqlHarness/MeetingAdmissionRegression.cs"}.ToDictionary(p=>p,p=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence,"result.json"),JsonSerializer.Serialize(new{checks,sources,scope="Real HTTP/Identity/SQL denial-only admission and actor-owned status; fixtures are not approved operations"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("MEETING ADMISSION PASS:32realHTTP/SQL cases; all denials/status reads leave SQL unchanged, no429 or provider calls.");
    }
}
