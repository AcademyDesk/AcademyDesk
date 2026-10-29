using System.Data.Common;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // All operations below are explicit synthetic persistence fixtures, NOT approved
    // provider work or an alternate HTTP/admission/authorization path.
    private static MeetingOperation SyntheticOperation(Guid academy, string? key = null) => new()
    {
        AcademyId=academy, ActorUserId=Guid.NewGuid(), ReceiptId=Guid.NewGuid(), Provider="GoogleWorkspace",
        IdempotencyKey=key??Guid.NewGuid().ToString("N"), RequestHash=new string('A',64),
        SourceVersion=new string('B',64), ProtectedEnvelope="synthetic-only-no-approval", Status="PendingDispatch"
    };
    private static async Task VerifyMeetingOperationMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations=db.Database.GetMigrations().ToArray();var latest=migrations.Single(x=>x.EndsWith("_MeetingOperationOutbox",StringComparison.Ordinal));
        var previous=migrations[Array.IndexOf(migrations,latest)-1];var migrator=db.GetService<IMigrator>();
        await migrator.MigrateAsync(latest);
        var probe=SyntheticOperation(Guid.NewGuid());var box=new MeetingOutbox{AcademyId=probe.AcademyId,OperationId=probe.Id};
        db.AddRange(probe,box);await db.SaveChangesAsync();var blocked=false;
        try{await migrator.MigrateAsync(previous);}catch(SqlException ex)when(ex.Number==51000){blocked=true;}
        if(!blocked||await db.MeetingOperations.CountAsync()!=1||await db.MeetingOutbox.CountAsync()!=1)throw new InvalidOperationException("Meeting operation evidence lost on downgrade");
        db.Remove(box);await db.SaveChangesAsync();db.Remove(probe);await db.SaveChangesAsync(); // Exact probe in owned QA database only.
        await migrator.MigrateAsync(previous);await migrator.MigrateAsync(latest);
        Console.WriteLine("MEETING OPERATION MIGRATION PASS: upgrade, populated-down refusal with both rows retained, empty down/up.");
    }
    private sealed class OperationClock : TimeProvider
    {
        public DateTimeOffset Now=DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private sealed class ClaimBarrier : SaveChangesInterceptor
    {
        private int arrivals;
        private readonly TaskCompletionSource<bool> ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,InterceptionResult<int> result,CancellationToken cancellationToken=default)
        {
            if(Interlocked.Increment(ref arrivals)==2)ready.TrySetResult(true);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(15),cancellationToken);return result;
        }
    }
    private sealed class OperationCommitFault : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,TransactionEventData eventData,InterceptionResult result,CancellationToken cancellationToken=default)
            => throw new InvalidOperationException("Synthetic operation commit failure after SQL writes");
    }
    private static async Task VerifyMeetingOperationsAsync(QaApiFactory factory,QaRunManifest manifest)
    {
        string connection;Guid academy;
        using(var scope=factory.Services.CreateScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();connection=db.Database.GetConnectionString()!;
            academy=await db.Academies.Where(x=>x.Name=="Synthetic Academy A").Select(x=>x.Id).SingleAsync();
        }
        var parsed=new SqlConnectionStringBuilder(connection);
        if(parsed.DataSource!=manifest.SqlServer||parsed.InitialCatalog!=manifest.Database||parsed.UserID!=manifest.RuntimeLogin)throw new InvalidOperationException("Operation test requires exact guarded runtime SQL target");
        AcademyDeskDbContext Open(IInterceptor? interceptor=null)
        {
            var options=new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection);
            if(interceptor is not null)options.AddInterceptors(interceptor);
            return new(options.Options);
        }
        var checks=new List<string>();void Check(string label,bool success){if(!success)throw new InvalidOperationException("MEETING OPERATION "+label);checks.Add(label);Console.WriteLine("MEETING OPERATION CASE "+label+" PASS");}
        async Task<MeetingOperation> Seed(string status="PendingDispatch")
        {
            await using var db=Open();var row=SyntheticOperation(academy);row.Status=status;
            db.AddRange(row,new MeetingOutbox{AcademyId=academy,OperationId=row.Id});await db.SaveChangesAsync();return row;
        }
        var clock=new OperationClock();var held=await Seed("Held");
        await using(var db=Open())Check("held-not-dispatchable",!await new MeetingOperationStore(db,clock).TryClaimAsync(academy,held.Id,0,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        var row=await Seed();var barrier=new ClaimBarrier();var leaseA=Guid.NewGuid();var leaseB=Guid.NewGuid();
        async Task<bool> Claim(Guid lease){await using var db=Open(barrier);return await new MeetingOperationStore(db,clock).TryClaimAsync(academy,row.Id,0,lease,TimeSpan.FromSeconds(10));}
        var winners=await Task.WhenAll(Claim(leaseA),Claim(leaseB));Check("simultaneous-claim-one-winner",winners.Count(x=>x)==1);
        Guid lease;
        await using(var db=Open())
        {
            var saved=await db.MeetingOperations.SingleAsync(x=>x.Id==row.Id);var box=await db.MeetingOutbox.SingleAsync(x=>x.OperationId==row.Id);lease=box.LeaseId!.Value;
            Check("restart-durable-fence",saved.Status=="Leased"&&saved.Version==1&&box.Version==1);
            Check("one-claim-audit",await db.AuditLogs.CountAsync(x=>x.EntityId==row.Id&&x.Action=="MeetingOperationLeased")==1);
            Check("foreign-scope-no-transition",!await new MeetingOperationStore(db,clock).TryBeginDispatchAsync(Guid.NewGuid(),row.Id,1,lease));
            Check("wrong-lease-no-transition",!await new MeetingOperationStore(db,clock).TryBeginDispatchAsync(academy,row.Id,1,Guid.NewGuid()));
        }
        clock.Now=clock.Now.AddSeconds(10);
        await using(var db=Open())
        {
            var store=new MeetingOperationStore(db,clock);
            Check("expired-worker-cannot-dispatch",!await store.TryBeginDispatchAsync(academy,row.Id,1,lease));
            Check("pre-dispatch-crash-recovered",await store.TryRecoverExpiredAsync(academy,row.Id,1));
            Check("fresh-worker-claim",await store.TryClaimAsync(academy,row.Id,2,Guid.NewGuid(),TimeSpan.FromSeconds(10)));
            Check("old-worker-fenced-even-new-version",!await store.TryBeginDispatchAsync(academy,row.Id,3,lease));
            var active=(await db.MeetingOutbox.SingleAsync(x=>x.OperationId==row.Id)).LeaseId!.Value;
            Check("durable-dispatch-marker",await store.TryBeginDispatchAsync(academy,row.Id,3,active));
        }
        clock.Now=clock.Now.AddSeconds(10);
        await using(var db=Open())
        {
            var store=new MeetingOperationStore(db,clock);Check("possible-send-crash-quarantined",await store.TryRecoverExpiredAsync(academy,row.Id,4));
            Check("unknown-outcome-never-requeued",(await db.MeetingOperations.SingleAsync(x=>x.Id==row.Id)).Status=="AwaitingReview"&&!await store.TryClaimAsync(academy,row.Id,5,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        }
        var uncertain=await Seed();var currentLease=Guid.NewGuid();
        await using(var db=Open())
        {
            var store=new MeetingOperationStore(db,clock);if(!await store.TryClaimAsync(academy,uncertain.Id,0,currentLease,TimeSpan.FromMinutes(1))||!await store.TryBeginDispatchAsync(academy,uncertain.Id,1,currentLease))throw new InvalidOperationException("Uncertain fixture failed");
            Check("explicit-timeout-quarantined",await store.TryRecordUncertainAsync(academy,uncertain.Id,2,currentLease)&&(await db.MeetingOperations.SingleAsync(x=>x.Id==uncertain.Id)).Status=="AwaitingReview");
        }
        var fault=await Seed();var failed=false;
        await using(var db=Open(new OperationCommitFault()))
        {try{await new MeetingOperationStore(db,clock).TryClaimAsync(academy,fault.Id,0,Guid.NewGuid(),TimeSpan.FromMinutes(1));}catch(InvalidOperationException ex)when(ex.Message=="Synthetic operation commit failure after SQL writes"){failed=true;}}
        await using(var db=Open())
        {
            var saved=await db.MeetingOperations.SingleAsync(x=>x.Id==fault.Id);var box=await db.MeetingOutbox.SingleAsync(x=>x.OperationId==fault.Id);
            Check("commit-failure-rolls-back-both-rows-and-audit",failed&&saved.Status=="PendingDispatch"&&saved.Version==0&&box.Version==0&&box.LeaseId is null&&!await db.AuditLogs.AnyAsync(x=>x.EntityId==fault.Id));
        }
        async Task RejectConstraint(string label,Action<AcademyDeskDbContext> stage,int number)
        {
            var rejected=false;await using(var db=Open()){stage(db);try{await db.SaveChangesAsync();}catch(DbUpdateException ex)when(ex.InnerException is SqlException sql&&sql.Number==number){rejected=true;}}
            Check(label,rejected);
        }
        await RejectConstraint("duplicate-key-cross-actor-rejected",db=>{var duplicate=SyntheticOperation(academy,held.IdempotencyKey);db.AddRange(duplicate,new MeetingOutbox{AcademyId=academy,OperationId=duplicate.Id});},2601);
        await RejectConstraint("receipt-cannot-admit-two-operations",db=>{var duplicate=SyntheticOperation(academy);duplicate.ReceiptId=held.ReceiptId;db.Add(duplicate);},2601);
        await RejectConstraint("cross-tenant-outbox-foreign-key",db=>db.Add(new MeetingOutbox{AcademyId=Guid.NewGuid(),OperationId=held.Id}),547);
        await RejectConstraint("invalid-status-refused",db=>{var invalid=SyntheticOperation(academy);invalid.Status="Created";db.Add(invalid);},547);
        await using(var db=Open())Check("no-notification-side-effect",!await db.Notifications.AnyAsync());
        if(checks.Count!=20)throw new InvalidOperationException("Operation test count mismatch "+checks.Count);
        var evidence=Path.Combine("QA","EVIDENCE","meeting-operation-sql-"+manifest.RunId.ToString("N"));Directory.CreateDirectory(evidence);
        var sources=new[]{"apps/api/Infrastructure/Meetings/MeetingOperationStore.cs","apps/api/Domain/Entities/MeetingOperation.cs","apps/api/Data/AcademyDeskDbContext.cs","QA/tools/SqlHarness/MeetingOperationRegression.cs"}.ToDictionary(p=>p,p=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence,"result.json"),JsonSerializer.Serialize(new{checks,sources,scope="Internal persistence with synthetic seeded operations, NOT HTTP admission, permission or provider execution"},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine("MEETING OPERATION PASS: 20 real SQL cases; no production admission, worker or provider dispatch.");
    }
}
