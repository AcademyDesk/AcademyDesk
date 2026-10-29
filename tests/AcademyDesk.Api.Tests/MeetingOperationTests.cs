using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOperationTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Fixture : IAsyncDisposable
    {
        public readonly AcademyDeskDbContext Db = new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        public readonly Clock Clock = new();
        public readonly MeetingOperation Row = new() { AcademyId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(), ReceiptId = Guid.NewGuid(), Provider = "GoogleWorkspace", IdempotencyKey = "synthetic-only", RequestHash = new string('A',64), SourceVersion = new string('B',64), ProtectedEnvelope = "test-only-not-an-approval" };
        public MeetingOperationStore Store => new(Db, Clock);
        public async Task Seed(string status = "PendingDispatch")
        {
            Row.Status = status; Db.AddRange(Row, new MeetingOutbox { AcademyId = Row.AcademyId, OperationId = Row.Id }); await Db.SaveChangesAsync();
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    [Fact] public async Task Held_default_cannot_be_claimed_or_dispatch()
    {
        await using var f = new Fixture(); await f.Seed("Held");
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        Assert.False(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,0,Guid.NewGuid()));
        Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }
    [Fact] public async Task Claim_dispatch_unknown_never_requeues()
    {
        await using var f=new Fixture(); await f.Seed(); var lease=Guid.NewGuid();
        Assert.True(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,lease,TimeSpan.FromMinutes(1)));
        Assert.True(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,1,lease));
        Assert.True(await f.Store.TryRecordUncertainAsync(f.Row.AcademyId,f.Row.Id,2,lease));
        Assert.Equal("AwaitingReview",f.Row.Status); Assert.Equal(3,f.Row.Version);
        Assert.Null((await f.Db.MeetingOutbox.SingleAsync()).LeaseId);
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,3,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        Assert.Equal(3,await f.Db.AuditLogs.CountAsync());
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task Crash_before_dispatch_can_recover_but_possible_send_is_quarantined(bool dispatched)
    {
        await using var f=new Fixture();await f.Seed();var lease=Guid.NewGuid();
        Assert.True(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,lease,TimeSpan.FromSeconds(1)));
        if(dispatched)Assert.True(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,1,lease));
        f.Clock.Now=f.Clock.Now.AddSeconds(1);
        Assert.False(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,f.Row.Version,lease));
        Assert.True(await f.Store.TryRecoverExpiredAsync(f.Row.AcademyId,f.Row.Id,f.Row.Version));
        Assert.Equal(dispatched?"AwaitingReview":"PendingDispatch",f.Row.Status);
        Assert.False(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,f.Row.Version,lease));
    }
    [Theory] [InlineData(-1)] [InlineData(1)] [InlineData(int.MaxValue)]
    public async Task Incorrect_versions_have_no_effect(int version)
    {
        await using var f=new Fixture();await f.Seed();
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,version,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        Assert.Equal(0,f.Row.Version);Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }
    [Theory] [InlineData(0)] [InlineData(121)]
    public async Task Unbounded_leases_are_rejected(int seconds)
    {
        await using var f=new Fixture();await f.Seed();
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,Guid.NewGuid(),TimeSpan.FromSeconds(seconds)));
        Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }
    [Fact] public async Task Wrong_tenant_lease_and_stale_version_cannot_advance()
    {
        await using var f=new Fixture();await f.Seed();var lease=Guid.NewGuid();
        Assert.False(await f.Store.TryClaimAsync(Guid.NewGuid(),f.Row.Id,0,lease,TimeSpan.FromMinutes(1)));
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,Guid.Empty,TimeSpan.FromMinutes(1)));
        Assert.True(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,lease,TimeSpan.FromMinutes(1)));
        Assert.False(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,1,Guid.NewGuid()));
        Assert.False(await f.Store.TryBeginDispatchAsync(f.Row.AcademyId,f.Row.Id,0,lease));
        Assert.False(await f.Store.TryRecoverExpiredAsync(f.Row.AcademyId,f.Row.Id,1));
        Assert.Equal("Leased",f.Row.Status);Assert.Single(await f.Db.AuditLogs.ToListAsync());
    }
    [Fact] public async Task Missing_or_inconsistent_outbox_fails_closed()
    {
        await using var f=new Fixture();f.Db.Add(f.Row);await f.Db.SaveChangesAsync();
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        f.Db.Add(new MeetingOutbox {AcademyId=f.Row.AcademyId,OperationId=f.Row.Id,Version=3});await f.Db.SaveChangesAsync();
        Assert.False(await f.Store.TryClaimAsync(f.Row.AcademyId,f.Row.Id,0,Guid.NewGuid(),TimeSpan.FromMinutes(1)));
        Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }
}
