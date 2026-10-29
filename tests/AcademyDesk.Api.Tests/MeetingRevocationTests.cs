using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingRevocationTests
{
    private const string Access = "synthetic-revoke-access", Refresh = "synthetic-revoke-refresh";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace", int revision = 1)
    {
        var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
        return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, revision, "synthetic-client",
            "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings", profile,
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    }
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration registration)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint); Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-revoke-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
        var start = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim); Assert.Equal(201, start.StatusCode);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, start.Permit!, new(200, "TokensReceivedAccountUnverified",
            Tokens: new(Access, Refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode);
        return (store, start.Permit!);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Calls++; return response(request); }
    }
    private static HttpResponseMessage Empty() => new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };

    [Theory] [InlineData("GoogleWorkspace", 200, 1)] [InlineData("Microsoft365", 202, 0)] [InlineData("Zoom", 202, 0)]
    public async Task Provider_specific_revocation_commits_disconnect_and_audits_before_any_fake_call(string provider, int expected, int calls)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, permit) = await Setup(f, clock, reg);
        var ciphertext = (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope;
        var handler = new Handler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method); Assert.Equal("https://oauth2.googleapis.com/revoke", request.RequestUri!.AbsoluteUri); Assert.Null(request.Headers.Authorization);
            Assert.Equal("application/x-www-form-urlencoded", request.Content!.Headers.ContentType!.MediaType); Assert.Equal("token=" + Refresh, await request.Content.ReadAsStringAsync());
            Assert.True(request.Headers.CacheControl!.NoStore); Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
            Assert.Equal(8, await f.Db.AuditLogs.CountAsync()); Assert.Equal("Started", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status); return Empty();
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingProviderRevocation(store, transport).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true);
        Assert.Equal(expected, result.StatusCode); Assert.Null(result.Material); Assert.False(result.Connected); Assert.False(result.CanRetry); Assert.Equal(calls, handler.Calls);
        var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); Assert.Equal(provider == "GoogleWorkspace" ? "ProviderAcknowledged" : "ManualRequired", row.Status);
        Assert.DoesNotContain(Access, row.ProtectedEnvelope); Assert.DoesNotContain(Refresh, row.ProtectedEnvelope); Assert.DoesNotContain(Refresh, JsonSerializer.Serialize(result));
        Assert.Equal(ciphertext, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope); Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, reg, permit)).StatusCode);
        Assert.Equal(409, (await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true)).StatusCode);
    }

    [Theory] [InlineData("unconfirmed")] [InlineData("project-consent")] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")]
    [InlineData("stamp")] [InlineData("registration")] [InlineData("version")] [InlineData("fence")] [InlineData("generation")]
    public async Task Denied_revocation_does_not_disconnect_or_release_credentials(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
        if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "registration") reg = Registration(revision: 2);
        if (mode == "fence") permit = permit with { Fence = Guid.NewGuid() };
        if (mode == "generation") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Version++; b.Generation++; await f.Db.SaveChangesAsync(); }
        var result = await store.BeginRevocationAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id,
            reg, permit, mode == "version" ? 1 : 2, mode != "unconfirmed", mode != "project-consent");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409 }); Assert.Null(result.Material); Assert.Empty(await f.Db.MeetingCredentialRevocations.ToListAsync());
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal(6, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("redirect")] [InlineData("400")] [InlineData("429")] [InlineData("body")] [InlineData("encoding")] [InlineData("network")] [InlineData("cancel")]
    public async Task Provider_errors_or_ambiguous_acknowledgement_are_unknown_never_retried(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true); Assert.Equal(201, start.StatusCode);
        Assert.Equal("{}", JsonSerializer.Serialize(start.Material)); Assert.DoesNotContain(Refresh, start.ToString());
        var handler = new Handler(_ =>
        {
            if (mode == "network") throw new HttpRequestException(Refresh);
            if (mode == "cancel") throw new OperationCanceledException(Refresh);
            var response = Empty(); response.StatusCode = mode switch { "redirect" => HttpStatusCode.Redirect, "400" => HttpStatusCode.BadRequest, "429" => HttpStatusCode.TooManyRequests, _ => HttpStatusCode.OK };
            if (mode == "body") response.Content = new StringContent("{\"error\":\"" + Refresh + "\"}", Encoding.UTF8, "application/json");
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return Task.FromResult(response);
        });
        using var transport = new HttpMessageInvoker(handler); var service = new MeetingProviderRevocation(store, transport);
        var result = await service.SendStartedAsync(f.Actor, f.Academy.Id, reg, start.Material!); Assert.Equal(409, result.StatusCode); Assert.Equal("OutcomeUnknown", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
        Assert.Equal(409, (await service.SendStartedAsync(f.Actor, f.Academy.Id, reg, start.Material!)).StatusCode); Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain(Refresh, JsonSerializer.Serialize(result)); Assert.False(result.CanRetry);
    }

    [Theory] [InlineData("role")] [InlineData("stamp")] [InlineData("reconnect")] [InlineData("fence")] [InlineData("registration")] [InlineData("tamper")]
    public async Task Changed_context_cannot_send_or_record_provider_success(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true); var material = start.Material!;
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
        if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "reconnect") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Version++; b.Generation++; b.Status = "AuthorizationPending"; await f.Db.SaveChangesAsync(); }
        if (mode == "fence") material = new(material.Id, Guid.NewGuid(), Refresh);
        if (mode == "registration") reg = Registration(revision: 2);
        if (mode == "tamper") { var row = await f.Db.MeetingCredentialRevocations.SingleAsync(); row.ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        var handler = new Handler(_ => Task.FromResult(Empty())); using var transport = new HttpMessageInvoker(handler);
        Assert.Equal(409, (await new MeetingProviderRevocation(store, transport).SendStartedAsync(f.Actor, f.Academy.Id, reg, material)).StatusCode);
        Assert.Equal(0, handler.Calls); Assert.Contains((await store.FinishRevocationAsync(f.Actor, f.Academy.Id, reg, material, true)).StatusCode, new[] { 403, 409 });
        Assert.Equal("Started", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
    }

    [Fact] public async Task Interrupted_revocation_recovers_only_to_unknown_without_a_new_send_permit()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true); var row = await f.Db.MeetingCredentialRevocations.SingleAsync();
        Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, row.Id)); clock.Now = clock.Now.AddSeconds(30);
        Assert.False(await store.RecoverRevocationAsync(Guid.NewGuid(), row.Id)); Assert.True(await store.RecoverRevocationAsync(f.Academy.Id, row.Id)); Assert.False(await store.RecoverRevocationAsync(f.Academy.Id, row.Id));
        Assert.Equal("OutcomeUnknown", row.Status); Assert.Equal(409, (await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true)).StatusCode);
        Assert.Equal(409, (await store.FinishRevocationAsync(f.Actor, f.Academy.Id, reg, start.Material!, true)).StatusCode);
        Assert.DoesNotContain(Refresh, row.ProtectedEnvelope); Assert.Equal(9, await f.Db.AuditLogs.CountAsync());
    }
    [Fact] public async Task Expired_access_does_not_falsely_mean_refresh_grant_already_revoked()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg); clock.Now = clock.Now.AddHours(1);
        Assert.Equal(201, (await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true)).StatusCode);
    }
    [Fact] public async Task Unresolved_project_revocation_blocks_reconnect_even_after_unknown_recovery()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var start = await store.BeginRevocationAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, reg.Fingerprint)).Code);
        clock.Now = clock.Now.AddSeconds(30); Assert.True(await store.RecoverRevocationAsync(f.Academy.Id, start.Material!.Id));
        Assert.Equal("ProviderRevocationNeedsReview", (await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 3, browser, reg.Fingerprint)).Code);
        Assert.Equal(1, await f.Db.MeetingOAuthAttempts.CountAsync()); Assert.Equal("Disconnected", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }
    [Fact] public async Task Revocation_during_response_denies_success_and_retains_unknown_recovery_evidence()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit) = await Setup(f, clock, reg);
        var handler = new Handler(async _ => { await f.Users.RemoveFromRoleAsync(f.User, "Owner"); return Empty(); }); using var transport = new HttpMessageInvoker(handler);
        Assert.Equal(403, (await new MeetingProviderRevocation(store, transport).ExecuteAsync(f.Actor, f.Academy.Id, reg, permit, 2, true, true)).StatusCode);
        Assert.Equal(1, handler.Calls); Assert.Equal("Started", (await f.Db.MeetingCredentialRevocations.SingleAsync()).Status);
    }
}
