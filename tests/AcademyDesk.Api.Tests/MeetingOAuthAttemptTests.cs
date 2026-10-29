using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthAttemptTests
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static string RandomBinding() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    private static readonly string Registration = new('A', 64);
    private static MeetingOAuthAttemptStore Store(MeetingReadinessTests.Fixture f, Clock clock, IDataProtectionProvider? protection = null)
        => new(f.Db, f.Users, f.Readiness, protection ?? new EphemeralDataProtectionProvider(), clock);

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Each_provider_has_private_state_PKCE_and_one_time_claim_but_never_connects(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var store = Store(f, clock); var browser = RandomBinding();
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, provider, 0, browser, Registration);
        Assert.Equal(201, prepared.StatusCode); var material = prepared.Material!;
        Assert.Equal(43, material.State.Length); Assert.Equal(43, material.Challenge.Length); Assert.Null(material.Verifier);
        var saved = await f.Db.MeetingOAuthAttempts.SingleAsync();
        Assert.DoesNotContain(material.State, saved.ProtectedEnvelope); Assert.DoesNotContain(browser, saved.ProtectedEnvelope);
        Assert.DoesNotContain(material.State, JsonSerializer.Serialize(prepared)); Assert.DoesNotContain(material.State, prepared.ToString());
        var claimed = await store.ClaimAsync(f.Actor, f.Academy.Id, provider, material.State, browser, Registration);
        Assert.Equal(200, claimed.StatusCode); Assert.NotNull(claimed.Material!.Verifier);
        Assert.Equal(material.Challenge, WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(claimed.Material.Verifier))));
        Assert.DoesNotContain(claimed.Material.Verifier, JsonSerializer.Serialize(claimed)); Assert.DoesNotContain(claimed.Material.Verifier, claimed.Material.ToString());
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(409, (await store.ClaimAsync(f.Actor, f.Academy.Id, provider, material.State, browser, Registration)).StatusCode);
        Assert.Equal(2, await f.Db.AuditLogs.CountAsync()); Assert.Empty(await f.Db.MeetingOperations.ToListAsync()); Assert.Empty(await f.Db.Notifications.ToListAsync());
    }

    [Fact] public async Task Three_providers_keep_independent_bindings_and_generations()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var store = Store(f, new Clock()); var browser = RandomBinding();
        var states = new Dictionary<string, string>();
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365", "Zoom" })
            states[provider] = (await store.PrepareAsync(f.Actor, f.Academy.Id, provider, 0, browser, Registration)).Material!.State;
        Assert.Equal(3, await f.Db.MeetingProviderBindings.CountAsync());
        await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 1, browser, Registration);
        Assert.Equal("ConnectionGenerationChanged", (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", states["Zoom"], browser, Registration)).Code);
        foreach (var provider in new[] { "GoogleWorkspace", "Microsoft365" }) Assert.Equal(200, (await store.ClaimAsync(f.Actor, f.Academy.Id, provider, states[provider], browser, Registration)).StatusCode);
    }

    [Theory] [InlineData("browser")] [InlineData("registration")] [InlineData("stamp")] [InlineData("expiry")]
    public async Task Changed_context_or_expiry_never_consumes_the_intent(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var store = Store(f, clock); var browser = RandomBinding();
        var state = (await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration)).Material!.State;
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "expiry") clock.Now = clock.Now.AddMinutes(5);
        var result = await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", state, mode == "browser" ? RandomBinding() : browser, mode == "registration" ? new string('B', 64) : Registration);
        Assert.Equal(mode == "expiry" ? 410 : 409, result.StatusCode);
        Assert.Equal("Pending", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status); Assert.Single(await f.Db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("payload")] [InlineData("provider")] [InlineData("expiry")] [InlineData("generation")] [InlineData("status")]
    public async Task Attempt_tampering_fails_closed(string field)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var store = Store(f, new Clock()); var browser = RandomBinding();
        var state = (await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration)).Material!.State;
        var row = await f.Db.MeetingOAuthAttempts.SingleAsync();
        if (field == "payload") row.ProtectedEnvelope = "forged";
        // The new callback FK makes provider/actor alternate-key fields immutable
        // in EF. Replace only this disposable adversarial fixture row to simulate
        // datastore tampering; retain the original protected payload and assertions.
        if (field == "provider") { f.Db.Remove(row); await f.Db.SaveChangesAsync(); row.Provider = "GoogleWorkspace"; f.Db.Add(row); }
        if (field == "expiry") row.ExpiresAtUtc = row.ExpiresAtUtc.AddMinutes(5);
        if (field == "generation") row.Generation++;
        if (field == "status") row.Status = "Claimed";
        await f.Db.SaveChangesAsync();
        Assert.Equal(409, (await store.ClaimAsync(f.Actor, f.Academy.Id, row.Provider, state, browser, Registration)).StatusCode);
        Assert.Single(await f.Db.AuditLogs.ToListAsync());
    }

    [Theory] [InlineData("Teacher")] [InlineData("Student")] [InlineData("Manager")] [InlineData("PlatformOwner")]
    public async Task Current_role_cannot_be_supplied_by_claims(string role)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(role); var store = Store(f, new Clock());
        Assert.Equal(403, (await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, RandomBinding(), Registration)).StatusCode);
        Assert.Equal(403, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", RandomBinding(), RandomBinding(), Registration)).StatusCode);
        Assert.Empty(await f.Db.MeetingProviderBindings.ToListAsync());
    }

    [Fact] public async Task Revocation_and_anonymous_foreign_wrong_actor_provider_and_state_are_rejected()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var store = Store(f, new Clock()); var browser = RandomBinding();
        var state = (await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration)).Material!.State;
        Assert.Equal(401, (await store.ClaimAsync(new ClaimsPrincipal(new ClaimsIdentity()), f.Academy.Id, "Zoom", state, browser, Registration)).StatusCode);
        Assert.Equal(403, (await store.ClaimAsync(f.Actor, Guid.NewGuid(), "Zoom", state, browser, Registration)).StatusCode);
        Assert.Equal(404, (await store.ClaimAsync(f.Actor, f.Academy.Id, "GoogleWorkspace", state, browser, Registration)).StatusCode);
        Assert.Equal(404, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", RandomBinding(), browser, Registration)).StatusCode);
        var row = await f.Db.MeetingOAuthAttempts.SingleAsync(); f.Db.Remove(row); await f.Db.SaveChangesAsync();
        row.ActorUserId = Guid.NewGuid(); f.Db.Add(row); await f.Db.SaveChangesAsync();
        Assert.Equal(404, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", state, browser, Registration)).StatusCode);
        Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        Assert.Equal(403, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", state, browser, Registration)).StatusCode);
    }

    [Theory] [InlineData("provider")] [InlineData("browser")] [InlineData("registration")] [InlineData("version")]
    public async Task Invalid_preparation_arguments_never_create_state(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var store = Store(f, new Clock());
        Assert.Equal(400, (await store.PrepareAsync(f.Actor, f.Academy.Id, mode == "provider" ? "https://untrusted.invalid" : "Zoom", mode == "version" ? -1 : 0, mode == "browser" ? "short" : RandomBinding(), mode == "registration" ? "bad" : Registration)).StatusCode);
        Assert.Empty(await f.Db.MeetingOAuthAttempts.ToListAsync()); Assert.Empty(await f.Db.MeetingProviderBindings.ToListAsync()); Assert.Empty(await f.Db.AuditLogs.ToListAsync());
    }

    [Fact] public async Task Stale_prepare_does_not_invalidate_current_intent()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var store = Store(f, new Clock()); var browser = RandomBinding();
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration);
        Assert.Equal(409, (await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration)).StatusCode);
        Assert.Equal(200, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", prepared.Material!.State, browser, Registration)).StatusCode);
    }

    [Fact] public async Task Missing_identity_security_stamp_and_new_protection_keys_fail_closed()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var browser = RandomBinding();
        var first = Store(f, clock); var prepared = await first.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, Registration);
        Assert.Equal(409, (await Store(f, clock).ClaimAsync(f.Actor, f.Academy.Id, "Zoom", prepared.Material!.State, browser, Registration)).StatusCode);
        // Corrupt only the disposable fixture; UserManager correctly refuses to write a missing stamp.
        f.User.SecurityStamp = null; await f.Services.GetRequiredService<IdentityDbContext>().SaveChangesAsync();
        Assert.Equal(403, (await first.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", prepared.Material.State, browser, Registration)).StatusCode);
        Assert.Equal(403, (await first.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 1, browser, Registration)).StatusCode);
    }
}
