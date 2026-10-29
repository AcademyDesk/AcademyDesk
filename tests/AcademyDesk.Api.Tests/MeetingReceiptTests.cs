using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingReceiptTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static async Task<(MeetingReadinessTests.Fixture F, MeetingPreviewRequest Request, MeetingReceiptService Service, Clock Clock)> Seed()
    {
        var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock();
        var t = new Teacher { AcademyId = f.Academy.Id, FirstName = "Synthetic", LastName = "Teacher", Email = "teacher@example.invalid" };
        var b = new Batch { AcademyId = f.Academy.Id, Name = "Online", TeacherId = t.Id, DeliveryMode = "Online" };
        var s = new Student { AcademyId = f.Academy.Id, FirstName = "Synthetic", LastName = "Student", Email = "student@example.invalid" };
        f.Db.AddRange(t, b, s, new Enrollment { AcademyId = f.Academy.Id, StudentId = s.Id, BatchId = b.Id }); await f.Db.SaveChangesAsync();
        var r = new MeetingPreviewRequest("GoogleWorkspace", b.Id, t.Id, "Private synthetic title", new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(2).AddHours(17), DateTimeKind.Unspecified), TimeSpan.FromMinutes(330)), "Asia/Kolkata", 60, [s.Id], "Once", "InviteesOnly", "Disabled", "Off");
        var service = new MeetingReceiptService(f.Db, f.Users, f.Readiness, new MeetingPreviewService(f.Db, f.Users, f.Readiness, clock), new EphemeralDataProtectionProvider(), clock);
        return (f, r, service, clock);
    }
    [Fact] public async Task Durable_actor_bound_encrypted_receipt_and_review_never_enable_execution()
    {
        var (f, r, service, _) = await Seed(); await using var owned = f;
        var made = await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default); Assert.Equal(201, made.StatusCode);
        var state = made.Receipt!; Assert.False(state.CanExecute); Assert.Equal("AwaitingReview", state.Status);
        var row = await f.Db.MeetingPreviewReceipts.SingleAsync(); Assert.DoesNotContain(r.Title!, row.ProtectedEnvelope); Assert.DoesNotContain(r.StudentIds![0].ToString(), row.ProtectedEnvelope);
        Assert.Equal(200, (await service.GetAsync(f.Actor, f.Academy.Id, row.Id, default)).StatusCode);
        var reviewed = await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, new(0, state.RequestHash, state.SourceVersion), default);
        Assert.Equal(200, reviewed.StatusCode); Assert.Equal("ReviewedDraft", reviewed.Receipt!.Status); Assert.Equal(1, reviewed.Receipt.Version); Assert.False(reviewed.Receipt.CanExecute);
        Assert.Equal(2, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync());
    }
    [Fact] public async Task Same_key_same_request_is_idempotent_different_request_conflicts()
    {
        var (f, r, service, _) = await Seed(); await using var owned = f;
        var first = await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default);
        var second = await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default); Assert.Equal(200, second.StatusCode); Assert.Equal(first.Receipt!.Id, second.Receipt!.Id);
        Assert.Equal(409, (await service.PrepareAsync(f.Actor, f.Academy.Id, r with { Title = "Changed" }, "receipt-key-1", default)).StatusCode);
        Assert.Single(await f.Db.MeetingPreviewReceipts.ToListAsync()); Assert.Single(await f.Db.AuditLogs.ToListAsync());
    }
    [Theory] [InlineData(null)] [InlineData("short")] [InlineData("key with spaces")] [InlineData("key:invalid")]
    public async Task Invalid_key_does_not_create_state(string? key)
    { var (f, r, service, _) = await Seed(); await using var owned = f; Assert.Equal(400, (await service.PrepareAsync(f.Actor, f.Academy.Id, r, key, default)).StatusCode); Assert.Empty(await f.Db.MeetingPreviewReceipts.ToListAsync()); }
    [Theory] [InlineData("version")] [InlineData("request")] [InlineData("source")]
    public async Task Review_requires_exact_displayed_version_and_hashes(string mode)
    {
        var (f, r, service, _) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!;
        var review = new ReviewMeetingDraftRequest(mode == "version" ? 9 : 0, mode == "request" ? "forged" : state.RequestHash, mode == "source" ? "forged" : state.SourceVersion);
        Assert.Equal(409, (await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, review, default)).StatusCode); Assert.Equal("AwaitingReview", (await f.Db.MeetingPreviewReceipts.SingleAsync()).Status);
    }
    [Fact] public async Task Approval_replay_is_conflict_and_creates_no_second_review_audit()
    {
        var (f, r, service, _) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!; var body = new ReviewMeetingDraftRequest(0, state.RequestHash, state.SourceVersion);
        Assert.Equal(200, (await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, body, default)).StatusCode); Assert.Equal(409, (await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, body, default)).StatusCode); Assert.Equal(2, await f.Db.AuditLogs.CountAsync());
    }
    [Fact] public async Task Expiry_blocks_read_review_and_same_key_refresh()
    {
        var (f, r, service, clock) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!; clock.Now = clock.Now.AddMinutes(6);
        Assert.Equal(410, (await service.GetAsync(f.Actor, f.Academy.Id, state.Id, default)).StatusCode);
        Assert.Equal(410, (await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, new(0, state.RequestHash, state.SourceVersion), default)).StatusCode);
        Assert.Equal(410, (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).StatusCode);
    }
    [Theory] [InlineData("payload")] [InlineData("status")] [InlineData("expiry")] [InlineData("hash")]
    public async Task Tampered_durable_state_fails_integrity(string kind)
    {
        var (f, r, service, _) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!; var row = await f.Db.MeetingPreviewReceipts.SingleAsync();
        switch(kind) { case "payload": row.ProtectedEnvelope = "tampered"; break; case "status": row.Status = "ReviewedDraft"; break; case "expiry": row.ExpiresAtUtc = row.ExpiresAtUtc.AddHours(1); break; case "hash": row.SourceVersion = new string('0',64); break; } await f.Db.SaveChangesAsync();
        Assert.Equal(409, (await service.GetAsync(f.Actor, f.Academy.Id, state.Id, default)).StatusCode);
    }
    [Fact] public async Task Changed_source_or_revoked_authority_cannot_be_reviewed()
    {
        var (f, r, service, _) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!;
        (await f.Db.Students.SingleAsync()).Email = "changed@example.invalid"; await f.Db.SaveChangesAsync();
        Assert.Equal(409, (await service.ReviewAsync(f.Actor, f.Academy.Id, state.Id, new(0, state.RequestHash, state.SourceVersion), default)).StatusCode);
        await f.Users.RemoveFromRoleAsync(f.User, "Owner"); Assert.Equal(403, (await service.GetAsync(f.Actor, f.Academy.Id, state.Id, default)).StatusCode);
    }
    [Fact] public async Task Other_admin_same_academy_cannot_read_initiators_receipt()
    {
        var (f, r, service, _) = await Seed(); await using var owned = f; var state = (await service.PrepareAsync(f.Actor, f.Academy.Id, r, "receipt-key-1", default)).Receipt!;
        var user = new ApplicationUser { UserName = "other-synthetic", AcademyId = f.Academy.Id, IsActive = true }; Assert.True((await f.Users.CreateAsync(user)).Succeeded); Assert.True((await f.Users.AddToRoleAsync(user,"Owner")).Succeeded);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()) },"test"));
        Assert.Equal(404, (await service.GetAsync(actor, f.Academy.Id, state.Id, default)).StatusCode); Assert.Equal(403, (await service.GetAsync(f.Actor, Guid.NewGuid(), state.Id, default)).StatusCode);
    }
    [Fact] public void Review_does_not_accept_execution_or_approval_flags()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ReviewMeetingDraftRequest>("{\"approved\":true}",new JsonSerializerOptions(JsonSerializerDefaults.Web)));
}
