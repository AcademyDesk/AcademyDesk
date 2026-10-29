using System.Net;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthDurabilityTests
{
    private const string Access = "synthetic-vault-access", Refresh = "synthetic-vault-refresh", Code = "synthetic-vault-code";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "Zoom", int revision = 1)
    {
        var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
        return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, revision, "synthetic-client",
            "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings", profile,
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    }
    private static MeetingOAuthAttemptStore Store(MeetingReadinessTests.Fixture f, Clock clock, IDataProtectionProvider keys) => new(f.Db, f.Users, f.Readiness, keys, clock);
    private static async Task<MeetingOAuthOutcome> Claim(MeetingReadinessTests.Fixture f, MeetingOAuthAttemptStore store, MeetingOAuthRegistration registration)
    {
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint);
        Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, Code, false)).StatusCode);
        return await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
    }
    private static MeetingOAuthExchangeOutcome Success(Clock clock, MeetingOAuthRegistration registration) => new(200, "TokensReceivedAccountUnverified",
        Tokens: new(Access, Refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes));

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task All_providers_have_one_durable_start_encrypted_quarantine_and_current_authorized_private_read(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var store = Store(f, clock, keys); var registration = Registration(provider);
        var claim = await Claim(f, store, registration); var started = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim);
        Assert.Equal(201, started.StatusCode); Assert.NotNull(started.Permit); Assert.Equal("{}", JsonSerializer.Serialize(started.Permit));
        Assert.Equal(409, (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim)).StatusCode);
        var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("Started", row.Status); Assert.DoesNotContain(Code, row.ProtectedEnvelope);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, started.Permit!, Success(clock, registration))).StatusCode);
        Assert.Equal("TokensQuarantined", row.Status); Assert.DoesNotContain(Access, row.ProtectedEnvelope); Assert.DoesNotContain(Refresh, row.ProtectedEnvelope);
        var read = await Store(f, clock, keys).ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, registration, started.Permit!);
        Assert.Equal(200, read.StatusCode); Assert.Equal(Access, read.Tokens!.AccessToken); Assert.Equal(Refresh, read.Tokens.RefreshToken);
        Assert.DoesNotContain(Access, JsonSerializer.Serialize(read)); Assert.DoesNotContain(Refresh, read.ToString()); Assert.False(read.CanRetry);
        Assert.Equal(409, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, started.Permit!, Success(clock, registration))).StatusCode);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal(7, await f.Db.AuditLogs.CountAsync());
        Assert.Empty(await f.Db.Notifications.ToListAsync()); Assert.Empty(await f.Db.MeetingOperations.ToListAsync());
    }

    [Theory] [InlineData("code")] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("stamp")]
    [InlineData("role")] [InlineData("registration")] [InlineData("expiry")] [InlineData("claim")]
    public async Task Invalid_start_never_persists_admission(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var store = Store(f, clock, new EphemeralDataProtectionProvider()); var registration = Registration();
        var claim = await Claim(f, store, registration); var actor = f.Actor;
        if (mode == "code") claim = claim with { AuthorizationCode = "substituted-code" };
        if (mode == "anonymous") actor = new(new ClaimsIdentity());
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "registration") registration = Registration(revision: 2);
        if (mode == "expiry") clock.Now = clock.Now.AddMinutes(5);
        if (mode == "claim") claim = claim with { StatusCode = 202 };
        var result = await store.BeginExchangeAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, registration, claim);
        Assert.Contains(result.StatusCode, new[] { 401, 403, 409 }); Assert.Null(result.Permit); Assert.Empty(await f.Db.MeetingOAuthExchanges.ToListAsync()); Assert.Equal(4, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("fence")] [InlineData("registration")] [InlineData("stamp")] [InlineData("role")]
    [InlineData("foreign")] [InlineData("anonymous")] [InlineData("generation")] [InlineData("deadline")]
    [InlineData("envelope")] [InlineData("keys")] [InlineData("tokens")] [InlineData("huge-expiry")]
    public async Task Invalid_completion_never_stores_or_reveals_tokens(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var store = Store(f, clock, keys); var registration = Registration();
        var claim = await Claim(f, store, registration); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim)).Permit!; var actor = f.Actor;
        var success = Success(clock, registration);
        if (mode == "fence") permit = permit with { Fence = Guid.NewGuid() };
        if (mode == "registration") registration = Registration(revision: 2);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "anonymous") actor = new(new ClaimsIdentity());
        if (mode == "generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Generation++; binding.Version++; await f.Db.SaveChangesAsync(); }
        if (mode == "deadline") clock.Now = clock.Now.AddSeconds(30);
        if (mode == "envelope") { var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); row.ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "keys") store = Store(f, clock, new EphemeralDataProtectionProvider());
        if (mode == "tokens") success = success with { Tokens = new("bad\ntoken", Refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes) };
        if (mode == "huge-expiry") success = success with { Tokens = new(Access, Refresh, DateTimeOffset.MaxValue, registration.Flow!.RequiredTokenScopes) };
        var result = await store.FinishExchangeAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, registration, permit, success);
        Assert.Contains(result.StatusCode, new[] { 401, 403, 404, 409 }); Assert.Null(result.Tokens);
        var saved = await f.Db.MeetingOAuthExchanges.SingleAsync(); Assert.Equal("Started", saved.Status); Assert.Equal(0, saved.Version); Assert.DoesNotContain(Access, saved.ProtectedEnvelope); Assert.Equal(5, await f.Db.AuditLogs.CountAsync());
    }

    [Fact] public async Task Restart_recovers_expired_exchange_as_unknown_never_restarts_or_returns_tokens()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var store = Store(f, clock, keys); var registration = Registration();
        var claim = await Claim(f, store, registration); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim)).Permit!;
        Assert.False(await store.RecoverExpiredExchangeAsync(f.Academy.Id, permit.Id)); clock.Now = clock.Now.AddSeconds(30);
        var restarted = Store(f, clock, keys); Assert.False(await restarted.RecoverExpiredExchangeAsync(Guid.NewGuid(), permit.Id));
        Assert.True(await restarted.RecoverExpiredExchangeAsync(f.Academy.Id, permit.Id)); Assert.False(await restarted.RecoverExpiredExchangeAsync(f.Academy.Id, permit.Id));
        Assert.Equal("OutcomeUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(409, (await restarted.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim)).StatusCode);
        Assert.Equal(409, (await restarted.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, permit, Success(clock, registration))).StatusCode);
        Assert.Equal(409, (await restarted.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, registration, permit)).StatusCode);
        Assert.Equal(6, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("stamp")] [InlineData("role")] [InlineData("generation")] [InlineData("keys")] [InlineData("expiry")] [InlineData("tamper")]
    public async Task Quarantined_credentials_do_not_survive_revocation_or_wrong_keys(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var keys = new EphemeralDataProtectionProvider(); var store = Store(f, clock, keys); var registration = Registration();
        var claim = await Claim(f, store, registration); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim)).Permit!;
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, permit, Success(clock, registration))).StatusCode);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "generation") { var binding = await f.Db.MeetingProviderBindings.SingleAsync(); binding.Generation++; binding.Version++; await f.Db.SaveChangesAsync(); }
        if (mode == "keys") store = Store(f, clock, new EphemeralDataProtectionProvider());
        if (mode == "expiry") clock.Now = clock.Now.AddHours(1);
        if (mode == "tamper") { var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); row.DeadlineUtc = row.DeadlineUtc.AddSeconds(1); await f.Db.SaveChangesAsync(); }
        var result = await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, registration, permit);
        Assert.Contains(result.StatusCode, new[] { 403, 409 }); Assert.Null(result.Tokens); Assert.Equal(6, await f.Db.AuditLogs.CountAsync());
    }

    private sealed class Handler(Func<Task> check, byte[]? response = null) : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; await check();
            if (response is null) throw new HttpRequestException("synthetic-timeout-no-retry");
            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(response) }; result.Content.Headers.ContentType = new("application/json"); return result;
        }
    }
    [Theory] [InlineData(true)] [InlineData(false)]
    public async Task Coordinator_commits_start_before_transport_and_never_reissues_after_result(bool success)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var store = Store(f, clock, new EphemeralDataProtectionProvider()); var registration = Registration(); var claim = await Claim(f, store, registration);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new { access_token = Access, refresh_token = Refresh, token_type = "Bearer", expires_in = 3600, scope = string.Join(' ', registration.Flow!.RequiredTokenScopes) });
        var handler = new Handler(async () => { Assert.Equal("Started", (await f.Db.MeetingOAuthExchanges.AsNoTracking().SingleAsync()).Status); Assert.True(await f.Db.AuditLogs.AnyAsync(x => x.Action == "MeetingOAuthExchangeStarted")); }, success ? bytes : null);
        using var transport = new HttpMessageInvoker(handler); var coordinator = new MeetingOAuthExchangeCoordinator(store, new(f.Db, f.Users, f.Readiness, transport, clock));
        var result = await coordinator.ExecuteAsync(f.Actor, f.Academy.Id, registration, claim, null);
        Assert.Equal(success ? 200 : 409, result.StatusCode); Assert.Null(result.Tokens); Assert.False(result.CanRetry);
        Assert.Equal(success ? "TokensQuarantined" : "OutcomeUnknown", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status);
        Assert.Equal(409, (await coordinator.ExecuteAsync(f.Actor, f.Academy.Id, registration, claim, null)).StatusCode); Assert.Equal(1, handler.Calls);
    }
}
