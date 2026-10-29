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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingReceiptMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Single(x => x.EndsWith("_MeetingPreviewReceipts", StringComparison.Ordinal));
        var previous = migrations[Array.IndexOf(migrations, latest) - 1]; var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        var probe = new MeetingPreviewReceipt { AcademyId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(), IdempotencyKey = "migration-probe", RequestHash = new string('A',64), SourceVersion = new string('B',64), ProtectedEnvelope = "synthetic-migration-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        db.MeetingPreviewReceipts.Add(probe); await db.SaveChangesAsync();
        var rejected = false;
        try { await migrator.MigrateAsync(previous); } catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 51000) { rejected = true; }
        if (!rejected || await db.MeetingPreviewReceipts.CountAsync() != 1) throw new InvalidOperationException("Meeting migration allowed receipt evidence loss");
        db.MeetingPreviewReceipts.Remove(probe); await db.SaveChangesAsync(); // Exact synthetic fixture in run-owned database only.
        await migrator.MigrateAsync(previous); await migrator.MigrateAsync(latest);
        if (await db.MeetingPreviewReceipts.AnyAsync()) throw new InvalidOperationException("Empty receipt migration replay mismatch");
        Console.WriteLine("MEETING RECEIPT MIGRATION PASS: prior upgrade, populated downgrade refused with row retained, empty down/up succeeds.");
    }
    private sealed class MeetingReviewAuditFault : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (data.Context?.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == "MeetingDraftReviewedNoExecution") == true)
                throw new InvalidOperationException("Synthetic meeting audit failure");
            return ValueTask.FromResult(result);
        }
    }
    private sealed class MeetingFutureClock : TimeProvider { public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow.AddMinutes(6); }
    private static async Task VerifyMeetingReceiptsAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, batch, teacher, student;
        using (var s = factory.Services.CreateScope())
        {
            var db = s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var users = s.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!; if (!(await users.AddToRoleAsync(u,"AcademyAdmin")).Succeeded) throw new InvalidOperationException("Receipt fixture role reset failed");
        }
        var auth = await LoginAsync(client,"qa-admin-a@example.invalid","Synthetic!39Ab");
        await CreateAccessActorAsync(factory,"qa-receipt-other@example.invalid","AcademyAdmin",academy);
        var other = await LoginAsync(client,"qa-receipt-other@example.invalid","Synthetic!39Ab");
        var start = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(17),DateTimeKind.Unspecified),TimeSpan.FromMinutes(330));
        var request = new MeetingPreviewRequest("GoogleWorkspace",batch,teacher,"Synthetic reviewed draft",start,"Asia/Kolkata",60,[student],"Once","InviteesOnly","Disabled","Off");
        var checks = new List<object>(); var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        async Task<MeetingReceiptState?> Send(string label,HttpMethod method,string path,object? body,HttpStatusCode expected,string? key=null,string? bearer=null,HttpClient? transport=null)
        {
            await Task.Delay(700); using var call = new HttpRequestMessage(method,path); call.Headers.Authorization = new AuthenticationHeaderValue("Bearer",bearer??auth); if(key is not null)call.Headers.Add("Idempotency-Key",key); if(body is not null)call.Content=JsonContent.Create(body,options:options);
            using var response=await (transport??client).SendAsync(call); if(response.StatusCode!=expected)throw new InvalidOperationException($"MEETING RECEIPT {label}: expected {(int)expected}, got {(int)response.StatusCode}");
            MeetingReceiptState? result=null; if(expected is HttpStatusCode.OK or HttpStatusCode.Created) { result=await response.Content.ReadFromJsonAsync<MeetingReceiptState>(options); if(result is null||result.CanExecute)throw new InvalidOperationException("Executable receipt returned"); }
            checks.Add(new {label,status=(int)expected});Console.WriteLine($"MEETING RECEIPT CASE {label} PASS status={(int)expected}.");return result;
        }
        string Root(Guid a)=>$"/api/academies/{a}/meetings/preview-receipts";
        var state=(await Send("prepare",HttpMethod.Post,Root(academy),request,HttpStatusCode.Created,"receipt-sql-1"))!;
        var duplicate=(await Send("same-key-replay",HttpMethod.Post,Root(academy),request,HttpStatusCode.OK,"receipt-sql-1"))!; if(state.Id!=duplicate.Id)throw new InvalidOperationException("Receipt replay duplicated row");
        await Send("changed-key-body",HttpMethod.Post,Root(academy),request with{Title="Different"},HttpStatusCode.Conflict,"receipt-sql-1");
        await Send("owned-read",HttpMethod.Get,Root(academy)+$"/{state.Id}",null,HttpStatusCode.OK);
        await Send("other-admin-denied",HttpMethod.Get,Root(academy)+$"/{state.Id}",null,HttpStatusCode.NotFound,bearer:other);
        await Send("foreign-route-denied",HttpMethod.Get,Root(foreign)+$"/{state.Id}",null,HttpStatusCode.Forbidden);
        var review=new ReviewMeetingDraftRequest(0,state.RequestHash,state.SourceVersion);
        await Send("wrong-version",HttpMethod.Post,Root(academy)+$"/{state.Id}/review",review with{ExpectedVersion=9},HttpStatusCode.Conflict);
        var approved=(await Send("review-draft-only",HttpMethod.Post,Root(academy)+$"/{state.Id}/review",review,HttpStatusCode.OK))!; if(approved.Status!="ReviewedDraft"||approved.Version!=1)throw new InvalidOperationException("Receipt review state mismatch");
        await Send("review-replay",HttpMethod.Post,Root(academy)+$"/{state.Id}/review",review,HttpStatusCode.Conflict);
        var race=(await Send("race-prepare",HttpMethod.Post,Root(academy),request,HttpStatusCode.Created,"receipt-sql-race"))!;
        async Task<HttpStatusCode> Race(){using var call=new HttpRequestMessage(HttpMethod.Post,Root(academy)+$"/{race.Id}/review"){Content=JsonContent.Create(new ReviewMeetingDraftRequest(0,race.RequestHash,race.SourceVersion))};call.Headers.Authorization=new AuthenticationHeaderValue("Bearer",auth);using var response=await client.SendAsync(call);return response.StatusCode;}
        var statuses=await Task.WhenAll(Race(),Race());if(statuses.Count(x=>x==HttpStatusCode.OK)!=1||statuses.Count(x=>x==HttpStatusCode.Conflict)!=1)throw new InvalidOperationException("Competing reviews were not exactly200/409");
        using(var s=factory.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.AuditLogs.CountAsync(x=>x.EntityId==race.Id&&x.Action=="MeetingDraftReviewedNoExecution")!=1)throw new InvalidOperationException("Duplicate review audit");}
        checks.Add(new {label="simultaneous-reviews",status="200/409"});Console.WriteLine("MEETING RECEIPT CASE simultaneous-reviews PASS 200/409, exactly one review audit.");
        var fault=(await Send("fault-prepare",HttpMethod.Post,Root(academy),request,HttpStatusCode.Created,"receipt-sql-fault"))!;
        using(var failing=new QaApiFactory(manifest,isolatedDomainInterceptor:new MeetingReviewAuditFault()))
        using(var faultClient=failing.CreateClient(new(){AllowAutoRedirect=false}))
        {
            var rejected=false;try{await Send("audit-failure",HttpMethod.Post,Root(academy)+$"/{fault.Id}/review",new ReviewMeetingDraftRequest(0,fault.RequestHash,fault.SourceVersion),HttpStatusCode.InternalServerError,transport:faultClient);rejected=true;}catch(Exception ex)when(ex.Message.Contains("Synthetic meeting audit failure",StringComparison.Ordinal)){rejected=true;checks.Add(new{label="audit-failure",status="request failed"});}
            if(!rejected)throw new InvalidOperationException("Audit fault accepted");
        }
        using(var s=factory.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var stored=await db.MeetingPreviewReceipts.SingleAsync(x=>x.Id==fault.Id);if(stored.Status!="AwaitingReview"||stored.Version!=0||await db.AuditLogs.AnyAsync(x=>x.EntityId==fault.Id&&x.Action=="MeetingDraftReviewedNoExecution"))throw new InvalidOperationException("Audit failure partially committed review");}
        using(var future=new QaApiFactory(manifest,isolatedClock:new MeetingFutureClock()))using(var futureClient=future.CreateClient(new(){AllowAutoRedirect=false}))
            await Send("expired-receipt",HttpMethod.Get,Root(academy)+$"/{fault.Id}",null,HttpStatusCode.Gone,transport:futureClient);
        using(var s=factory.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.MeetingPreviewReceipts.SingleAsync(x=>x.Id==fault.Id)).Status="ReviewedDraft";await db.SaveChangesAsync();}
        await Send("tampered-status",HttpMethod.Get,Root(academy)+$"/{fault.Id}",null,HttpStatusCode.Conflict);
        var changed=(await Send("stale-source-prepare",HttpMethod.Post,Root(academy),request,HttpStatusCode.Created,"receipt-sql-source"))!;
        using(var s=factory.Services.CreateScope()){var db=s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();(await db.Students.SingleAsync(x=>x.Id==student)).Email="changed@example.invalid";await db.SaveChangesAsync();}
        await Send("stale-source-review",HttpMethod.Post,Root(academy)+$"/{changed.Id}/review",new ReviewMeetingDraftRequest(0,changed.RequestHash,changed.SourceVersion),HttpStatusCode.Conflict);
        using(var s=factory.Services.CreateScope())
        {
            var db=s.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();if(await db.Notifications.AnyAsync())throw new InvalidOperationException("Receipt workflow queued notification");
            var row=await db.MeetingPreviewReceipts.SingleAsync(x=>x.Id==changed.Id);if(row.ProtectedEnvelope.Contains("Synthetic reviewed draft",StringComparison.Ordinal)||row.ProtectedEnvelope.Contains(student.ToString(),StringComparison.Ordinal))throw new InvalidOperationException("Receipt payload unprotected");
            var evidence=Path.Combine("QA","EVIDENCE","meeting-receipt-sql-"+manifest.RunId.ToString("N"));Directory.CreateDirectory(evidence);
            var sources=new[]{"apps/api/Infrastructure/Meetings/MeetingReceiptService.cs","apps/api/Controllers/MeetingsController.cs","apps/api/Domain/Entities/MeetingPreviewReceipt.cs","QA/tools/SqlHarness/MeetingReceiptRegression.cs"}.ToDictionary(p=>p,p=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p))));
            await File.WriteAllTextAsync(Path.Combine(evidence,"result.json"),JsonSerializer.Serialize(new{checks,sources,approval="ReviewedDraft only; execution unavailable",storage="real SQL/Identity/HTTP; required audit rollback and concurrency tested"},new JsonSerializerOptions{WriteIndented=true}));
        }
        if(checks.Count!=17)throw new InvalidOperationException("Receipt case count mismatch "+checks.Count);
        Console.WriteLine("MEETING RECEIPT PASS: 17 HTTP/SQL cases; actor scope, replay, concurrency, expiry, integrity and audit rollback; no provider action.");
    }
}
