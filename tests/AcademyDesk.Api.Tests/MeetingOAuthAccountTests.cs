using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingOAuthAccountTests
{
    private const string Access = "synthetic-account-access", Refresh = "synthetic-account-refresh";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "Zoom", int revision = 1)
    {
        var profile = provider switch { "GoogleWorkspace" => "google-calendar-owned-web/1", "Microsoft365" => "microsoft-online-meetings-work/1", _ => "zoom-public-pkce/1" };
        return MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new(provider, revision, "synthetic-client",
            "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings", profile,
            provider == "Microsoft365" ? "019acc9a-6200-7000-8000-000000000001" : null)]).Resolve(provider)!;
    }
    private static string Account(string provider) => provider switch
    {
        "GoogleWorkspace" => "{\"sub\":\"synthetic-google-user\",\"email_verified\":true}",
        "Microsoft365" => "{\"id\":\"019acc9a-6200-7000-8000-000000000002\",\"userPrincipalName\":\"not-authority@example.invalid\"}",
        _ => "{\"id\":\"synthetic-zoom-user\",\"account_id\":\"synthetic-zoom-account\",\"status\":\"active\"}"
    };
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration registration)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, registration.Provider, 0, browser, registration.Fingerprint);
        Assert.Equal(201, prepared.StatusCode);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, registration.Provider, prepared.Material!.State, browser, registration.Fingerprint, "synthetic-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, registration.Provider, prepared.Material.State, browser, registration.Fingerprint);
        var started = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, registration, claim); Assert.Equal(201, started.StatusCode);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, registration, started.Permit!, new(200, "TokensReceivedAccountUnverified",
            Tokens: new(Access, Refresh, clock.Now.AddHours(1), registration.Flow!.RequiredTokenScopes)))).StatusCode);
        return (store, started.Permit!);
    }
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { Calls++; return send(request); }
    }
    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK, string type = "application/json")
        => new(status) { Content = new StringContent(json, Encoding.UTF8, type) };

    [Fact] public async Task External_stamp_rotation_cannot_consume_intent_through_a_tracked_identity()
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var registration = Registration();
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), new Clock());
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var prepared = await store.PrepareAsync(f.Actor, f.Academy.Id, "Zoom", 0, browser, registration.Fingerprint); Assert.Equal(201, prepared.StatusCode);
        var oldStamp = f.User.SecurityStamp;
        await using (var other = new IdentityDbContext(f.Services.GetRequiredService<DbContextOptions<IdentityDbContext>>()))
        { var user = await other.Users.SingleAsync(x => x.Id == f.User.Id); user.SecurityStamp = Guid.NewGuid().ToString(); await other.SaveChangesAsync(); }
        Assert.Equal(oldStamp, f.User.SecurityStamp); // Deliberately keep the stale tracked object.
        Assert.Equal(409, (await store.ClaimAsync(f.Actor, f.Academy.Id, "Zoom", prepared.Material!.State, browser, registration.Fingerprint)).StatusCode);
        Assert.Equal("Pending", (await f.Db.MeetingOAuthAttempts.SingleAsync()).Status); Assert.Equal(1, await f.Db.AuditLogs.CountAsync());
    }

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Private_probe_uses_fixed_endpoint_and_audited_current_tokens_not_connected(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(provider); var (store, permit) = await Setup(f, clock, registration);
        var handler = new Handler(async request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Equal(registration.Flow!.AccountEndpoint, request.RequestUri);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal(Access, request.Headers.Authorization.Parameter);
            Assert.Null(request.Content); Assert.Equal("", request.RequestUri!.Query); Assert.True(request.Headers.CacheControl!.NoStore);
            Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingOAuthQuarantinedTokensRead"));
            return Response(Account(provider));
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingOAuthAccountProbe(store, transport, clock).InspectAsync(f.Actor, f.Academy.Id, registration, permit);
        Assert.Equal(200, result.StatusCode); Assert.Equal("ProviderAccountObservedNotConnected", result.Code); Assert.NotNull(result.Observation);
        Assert.False(result.Connected); Assert.False(result.CanExecute); Assert.Equal(1, handler.Calls); Assert.Equal("{}", JsonSerializer.Serialize(result.Observation));
        Assert.DoesNotContain("synthetic-", JsonSerializer.Serialize(result)); Assert.DoesNotContain("synthetic-", result.Observation!.ToString());
        Assert.Equal(2, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingOAuthQuarantinedTokensRead"));
        Assert.Equal("TokensQuarantined", (await f.Db.MeetingOAuthExchanges.SingleAsync()).Status); Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
    }

    [Theory] [InlineData("anonymous")] [InlineData("foreign")] [InlineData("role")] [InlineData("stamp")] [InlineData("generation")]
    [InlineData("registration")] [InlineData("fence")] [InlineData("expiry")] [InlineData("tamper")]
    public async Task Current_context_denials_make_zero_network_calls(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(); var (store, permit) = await Setup(f, clock, registration);
        if (mode == "role") Assert.True((await f.Users.RemoveFromRoleAsync(f.User, "Owner")).Succeeded);
        if (mode == "stamp") Assert.True((await f.Users.UpdateSecurityStampAsync(f.User)).Succeeded);
        if (mode == "generation") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Version++; b.Generation++; await f.Db.SaveChangesAsync(); }
        if (mode == "registration") registration = Registration(revision: 2);
        if (mode == "fence") permit = permit with { Fence = Guid.NewGuid() };
        if (mode == "expiry") clock.Now = clock.Now.AddHours(1);
        if (mode == "tamper") { var row = await f.Db.MeetingOAuthExchanges.SingleAsync(); row.ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        var handler = new Handler(_ => Task.FromResult(Response(Account("Zoom")))); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingOAuthAccountProbe(store, transport, clock).InspectAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor,
            mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, registration, permit);
        Assert.Contains(result.StatusCode, new[] { 401, 403, 404, 409 }); Assert.Null(result.Observation); Assert.Equal(0, handler.Calls);
        Assert.Equal(0, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingOAuthQuarantinedTokensRead"));
    }

    [Theory] [InlineData("role")] [InlineData("stamp")] [InlineData("generation")] [InlineData("expiry")] [InlineData("disconnect")]
    public async Task Context_change_during_response_cannot_release_identity(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(); var (store, permit) = await Setup(f, clock, registration);
        var handler = new Handler(async _ =>
        {
            if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
            if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "generation") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Version++; b.Generation++; await f.Db.SaveChangesAsync(); }
            if (mode == "expiry") clock.Now = clock.Now.AddHours(1);
            if (mode == "disconnect") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, "Zoom", 2, true)).StatusCode);
            return Response(Account("Zoom"));
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingOAuthAccountProbe(store, transport, clock).InspectAsync(f.Actor, f.Academy.Id, registration, permit);
        Assert.Contains(result.StatusCode, new[] { 403, 409 }); Assert.Null(result.Observation); Assert.Equal(1, handler.Calls);
        Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingOAuthQuarantinedTokensRead"));
    }

    [Theory] [InlineData("redirect")] [InlineData("unauthorized")] [InlineData("429")] [InlineData("html")] [InlineData("encoding")]
    [InlineData("oversized")] [InlineData("network")] [InlineData("cancel")]
    public async Task Transport_failures_are_sanitized_never_retried(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(); var (store, permit) = await Setup(f, clock, registration);
        var handler = new Handler(_ =>
        {
            if (mode == "network") throw new HttpRequestException(Access);
            if (mode == "cancel") throw new OperationCanceledException(Refresh);
            var response = Response(mode == "oversized" ? new string('x', 65537) : Account("Zoom"),
                mode switch { "redirect" => HttpStatusCode.Redirect, "unauthorized" => HttpStatusCode.Unauthorized, "429" => HttpStatusCode.TooManyRequests, _ => HttpStatusCode.OK }, mode == "html" ? "text/html" : "application/json");
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip");
            response.Headers.Location = new Uri("https://untrusted.invalid/never-follow"); return Task.FromResult(response);
        });
        using var transport = new HttpMessageInvoker(handler); var result = await new MeetingOAuthAccountProbe(store, transport, clock).InspectAsync(f.Actor, f.Academy.Id, registration, permit);
        Assert.Equal(502, result.StatusCode); Assert.Null(result.Observation); Assert.Equal(1, handler.Calls); Assert.DoesNotContain(Access, JsonSerializer.Serialize(result));
    }

    [Theory]
    [InlineData("GoogleWorkspace", "{\"sub\":\"user-1\",\"email_verified\":true}", true)]
    [InlineData("GoogleWorkspace", "{\"email\":\"owner@example.invalid\",\"email_verified\":true}", false)]
    [InlineData("GoogleWorkspace", "{\"sub\":\"user-1\",\"email_verified\":false}", false)]
    [InlineData("GoogleWorkspace", "{\"sub\":\"user-1\",\"email_verified\":\"true\"}", false)]
    [InlineData("GoogleWorkspace", "{\"sub\":\"user-1\",\"SUB\":\"user-2\",\"email_verified\":true}", false)]
    [InlineData("GoogleWorkspace", "{\"sub\":\"user-1\",\"email_verified\":true,\"error\":null}", false)]
    [InlineData("Microsoft365", "{\"id\":\"019acc9a-6200-7000-8000-000000000002\"}", true)]
    [InlineData("Microsoft365", "{\"id\":\"00000000-0000-0000-0000-000000000000\"}", false)]
    [InlineData("Microsoft365", "{\"userPrincipalName\":\"owner@example.invalid\"}", false)]
    [InlineData("Microsoft365", "{\"id\":32}", false)]
    [InlineData("Zoom", "{\"id\":\"user-1\",\"account_id\":\"account-1\",\"status\":\"active\"}", true)]
    [InlineData("Zoom", "{\"id\":\"user-1\",\"account_id\":\"account-1\",\"status\":\"pending\"}", false)]
    [InlineData("Zoom", "{\"id\":\"user-1\",\"account_id\":\"account-1\",\"status\":\"inactive\"}", false)]
    [InlineData("Zoom", "{\"id\":\"user-1\",\"status\":\"active\"}", false)]
    [InlineData("Zoom", "{\"id\":\"bad\\nuser\",\"account_id\":\"account-1\",\"status\":\"active\"}", false)]
    [InlineData("Zoom", "[]", false)] [InlineData("Zoom", "null", false)] [InlineData("Zoom", "not-json", false)]
    public void Provider_identity_is_not_email_roles_tenant_or_a_connected_proof(string provider, string json, bool accepted)
    {
        var result = MeetingOAuthAccountProbe.Parse(Encoding.UTF8.GetBytes(json), Registration(provider), DateTimeOffset.UtcNow);
        Assert.Equal(accepted ? 200 : 502, result.StatusCode); Assert.False(result.Connected); Assert.False(result.CanExecute);
        Assert.Equal(accepted, result.Observation is not null);
    }

    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Local_disconnect_fences_credentials_and_retains_evidence_not_provider_revoked(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var registration = Registration(provider); var (store, permit) = await Setup(f, clock, registration);
        var encrypted = (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope;
        var result = await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, provider, 2, true);
        Assert.Equal(200, result.StatusCode); Assert.Equal("DisconnectedLocallyProviderRevocationRequired", result.Code);
        var binding = await f.Db.MeetingProviderBindings.SingleAsync(); Assert.Equal("Disconnected", binding.Status); Assert.Equal(3, binding.Version); Assert.Equal(2, binding.Generation);
        Assert.Equal(encrypted, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope); Assert.Equal(1, await f.Db.AuditLogs.CountAsync(x => x.Action == "MeetingDisconnectedLocallyRevocationRequired"));
        Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, registration, permit)).StatusCode);
        Assert.Equal(409, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, provider, 2, true)).StatusCode);
        var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        Assert.Equal(201, (await store.PrepareAsync(f.Actor, f.Academy.Id, provider, 3, browser, registration.Fingerprint)).StatusCode);
        Assert.Equal(3, binding.Generation); Assert.Equal(409, (await store.ReadQuarantinedTokensAsync(f.Actor, f.Academy.Id, registration, permit)).StatusCode);
    }

    [Theory] [InlineData("unconfirmed")] [InlineData("stale")] [InlineData("foreign")] [InlineData("role")] [InlineData("anonymous")]
    [InlineData("overflow")] [InlineData("provider")]
    public async Task Disconnect_denials_do_not_write(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var (store, _) = await Setup(f, clock, Registration());
        if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
        if (mode == "overflow") { var b = await f.Db.MeetingProviderBindings.SingleAsync(); b.Generation = int.MaxValue; await f.Db.SaveChangesAsync(); }
        var result = await store.DisconnectLocallyAsync(mode == "anonymous" ? new ClaimsPrincipal() : f.Actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id,
            mode == "provider" ? "arbitrary" : "Zoom", mode == "stale" ? 1 : 2, mode != "unconfirmed");
        Assert.Contains(result.StatusCode, new[] { 400, 401, 403, 409 }); Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status);
        Assert.Equal(6, await f.Db.AuditLogs.CountAsync());
    }
}
