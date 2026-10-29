using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingTeamsLicenseTests
{
    private const string Tenant = "019acc9a-6200-7000-8000-000000000001", ObjectId = "019acc9a-6200-7000-8000-000000000002", Client = "019acc9a-6200-7000-8000-000000000003", Access = "synthetic-teams-license-access";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
    private static MeetingOAuthRegistration Registration(bool identity = true) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Microsoft365", 1, Client,
        "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", identity ? MeetingOAuthFlowProfile.MicrosoftIdentityProfile : "microsoft-online-meetings-work/1", Tenant)]).Resolve("Microsoft365")!;
    private static string Plan(string status = "Success", string applies = "User", string? id = null, string name = "TEAMS1") => JsonSerializer.Serialize(new { servicePlanId = id ?? MeetingTeamsLicenseProbe.TeamsPlan, servicePlanName = name, provisioningStatus = status, appliesTo = applies });
    private static string Evidence(string? plan = null) => "{\"value\":[{\"servicePlans\":[" + (plan ?? Plan()) + "]}]}";
    private sealed class Handler(string keys = "{}", string? evidence = null, string mode = "ok", Func<Task>? during = null) : HttpMessageHandler
    {
        internal int LicenseCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            if (request.RequestUri!.AbsoluteUri == MeetingMicrosoftIdentityVerifier.KeysEndpoint(Registration()))
            { Assert.Null(request.Headers.Authorization); return new(HttpStatusCode.OK) { Content = new StringContent(keys, Encoding.UTF8, "application/json") }; }
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal(Access, request.Headers.Authorization.Parameter);
            if (request.RequestUri.AbsoluteUri == "https://graph.microsoft.com/v1.0/me") return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { id = ObjectId }), Encoding.UTF8, "application/json") };
            Assert.Equal(MeetingTeamsLicenseProbe.Endpoint, request.RequestUri.AbsoluteUri); Assert.DoesNotContain(Access, request.RequestUri.AbsoluteUri); Assert.Null(request.Content); Assert.True(request.Headers.CacheControl!.NoStore); LicenseCalls++;
            if (during is not null) await during(); if (mode == "network") throw new HttpRequestException(); if (mode == "timeout") throw new TaskCanceledException();
            var status = mode switch { "forbidden" => HttpStatusCode.Forbidden, "unauthorized" => HttpStatusCode.Unauthorized, "redirect" => HttpStatusCode.Redirect, "throttled" => HttpStatusCode.TooManyRequests, "error" => HttpStatusCode.ServiceUnavailable, _ => HttpStatusCode.OK };
            var result = new HttpResponseMessage(status) { Content = new StringContent(mode == "oversize" ? new string('x', 131073) : evidence ?? Evidence(), Encoding.UTF8, mode == "mime" ? "text/html" : "application/json") };
            if (mode == "encoding") result.Content.Headers.ContentEncoding.Add("gzip"); return result;
        }
    }
    [Theory] [InlineData("success")] [InlineData("disabled")] [InlineData("error")] [InlineData("pending-input")] [InlineData("pending-activation")] [InlineData("pending-provisioning")]
    [InlineData("company")] [InlineData("unknown-id-name-only")] [InlineData("wrong-name")] [InlineData("empty")] [InlineData("empty-plans")]
    [InlineData("null")] [InlineData("array")] [InlineData("malformed")] [InlineData("missing-value")] [InlineData("value-type")] [InlineData("missing-plans")] [InlineData("plan-number")]
    [InlineData("duplicate-plan")] [InlineData("duplicate-root")] [InlineData("duplicate-plan-field")] [InlineData("invalid-guid")] [InlineData("status-type")]
    [InlineData("unknown-status")] [InlineData("unknown-applies")] [InlineData("next-link")] [InlineData("delta-link")] [InlineData("provider-error")]
    [InlineData("late-malformed-license")] [InlineData("too-many-licenses")] [InlineData("too-many-plans")] [InlineData("oversize")]
    [InlineData("same-plan-different-licenses")] [InlineData("mixed-disabled-success")]
    public void Only_exact_provisioned_user_plan_in_complete_bounded_response_is_observed(string mode)
    {
        var json = mode switch {
            "disabled" => Evidence(Plan("Disabled")), "error" => Evidence(Plan("Error")), "pending-input" => Evidence(Plan("PendingInput")), "pending-activation" => Evidence(Plan("PendingActivation")),
            "pending-provisioning" => Evidence(Plan("PendingProvisioning")), "company" => Evidence(Plan(applies: "Company")), "unknown-id-name-only" => Evidence(Plan(id: ObjectId)), "wrong-name" => Evidence(Plan(name: "TEAMS_PREMIUM")),
            "empty" => "{\"value\":[]}", "empty-plans" => Evidence(""), "null" => "null", "array" => "[]", "malformed" => "{", "missing-value" => "{}", "value-type" => "{\"value\":true}",
            "missing-plans" => "{\"value\":[{}]}", "plan-number" => Evidence("123"), "duplicate-plan" => Evidence(Plan() + "," + Plan()), "duplicate-root" => "{\"value\":[],\"Value\":[]}",
            "duplicate-plan-field" => Evidence(Plan()[..^1] + ",\"provisioningStatus\":\"Success\"}"), "invalid-guid" => Evidence(Plan(id: "not-guid")),
            "status-type" => Evidence().Replace("\"Success\"", "true"), "unknown-status" => Evidence(Plan("Enabled")), "unknown-applies" => Evidence(Plan(applies: "Any")),
            "next-link" => Evidence()[..^1] + ",\"@odata.nextLink\":\"https://attacker.invalid\"}", "delta-link" => Evidence()[..^1] + ",\"@odata.deltaLink\":null}",
            "provider-error" => Evidence()[..^1] + ",\"error\":{\"message\":\"private\"}}", "late-malformed-license" => Evidence()[..^2] + ",{}]}",
            "too-many-licenses" => "{\"value\":[" + string.Join(',', Enumerable.Repeat("{\"servicePlans\":[]}", 129)) + "]}",
            "too-many-plans" => Evidence(string.Join(',', Enumerable.Range(0, 257).Select(_ => Plan(id: Guid.NewGuid().ToString("D"))))), "oversize" => new string('x', 131073),
            "same-plan-different-licenses" => "{\"value\":[{\"servicePlans\":[" + Plan() + "]},{\"servicePlans\":[" + Plan() + "]}]}",
            "mixed-disabled-success" => "{\"value\":[{\"servicePlans\":[" + Plan("Disabled") + "]},{\"servicePlans\":[" + Plan() + "]}]}", _ => Evidence() };
        var result = MeetingTeamsLicenseProbe.Parse(Encoding.UTF8.GetBytes(json));
        var positive = mode is "success" or "same-plan-different-licenses" or "mixed-disabled-success";
        var notObserved = mode is "disabled" or "error" or "pending-input" or "pending-activation" or "pending-provisioning" or "company" or "unknown-id-name-only" or "empty" or "empty-plans";
        Assert.Equal(positive ? 200 : notObserved ? 409 : 502, result.StatusCode); Assert.Equal(positive, result.TeamsPlanProvisioned); Assert.False(result.ProviderVerified); Assert.False(result.CanExecute); Assert.False(result.Connected);
        Assert.DoesNotContain(MeetingTeamsLicenseProbe.TeamsPlan, JsonSerializer.Serialize(result)); Assert.DoesNotContain("servicePlans", JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("ok")] [InlineData("forbidden")] [InlineData("unauthorized")] [InlineData("redirect")] [InlineData("throttled")] [InlineData("error")]
    [InlineData("network")] [InlineData("timeout")] [InlineData("mime")] [InlineData("encoding")] [InlineData("oversize")]
    public async Task Existing_scope_read_is_fixed_header_only_bounded_and_never_retries_or_assigns_permissions(string mode)
    {
        using var handler = new Handler(mode: mode); using var transport = new HttpMessageInvoker(handler); var result = await new MeetingTeamsLicenseProbe(transport).InspectAsync(Access);
        Assert.Equal(mode == "ok" ? 200 : 502, result.StatusCode); Assert.Equal(1, handler.LicenseCalls); Assert.False(result.CanRetry); Assert.False(result.CanExecute); Assert.DoesNotContain(Access, JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("ok")] [InlineData("old-profile")] [InlineData("no-token")] [InlineData("policy")] [InlineData("forbidden")] [InlineData("disabled")]
    [InlineData("pagination")] [InlineData("role")] [InlineData("stamp")] [InlineData("disconnect")] [InlineData("refresh")] [InlineData("expired")]
    [InlineData("foreign")] [InlineData("other-actor")] [InlineData("anonymous")] [InlineData("source-cipher")]
    public async Task License_observation_requires_current_signed_identity_and_transactional_private_host_context(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); using var rsa = RSA.Create(2048); var clock = new Clock(); var reg = Registration(mode != "old-profile"); var p = rsa.ExportParameters(false);
        var keys = JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", use = "sig", kid = "synthetic-license-key", n = Encode(p.Modulus!), e = Encode(p.Exponent!), issuer = MeetingMicrosoftIdentityVerifier.IssuerTemplate } } });
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = Encode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint); Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-license-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        var head = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", typ = "JWT", kid = "synthetic-license-key" }));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = MeetingMicrosoftIdentityVerifier.Issuer(reg), aud = Client, tid = Tenant, oid = ObjectId, sub = "pairwise-license-subject", ver = "2.0", nonce = intent.Material.IdentityNonce,
            iat = clock.Now.ToUnixTimeSeconds(), nbf = clock.Now.AddSeconds(-30).ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds() }));
        var data = head + "." + payload; var jwt = data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-license-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode is "old-profile" or "no-token" ? null : jwt)))).StatusCode);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new(ObjectId, null, clock.Now))).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var policies = mode == "policy" ? MeetingAccountPolicyRegistry.Disabled : MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, ObjectId, null, Tenant, clock.Now.AddMinutes(-1), clock.Now.AddHours(3))]);
        if (mode == "source-cipher") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt"; await f.Db.SaveChangesAsync(); }
        async Task During()
        {
            if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "disconnect") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true)).StatusCode);
            if (mode == "refresh") Assert.Equal(201, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
            if (mode == "expired") clock.Now = clock.Now.AddHours(2);
        }
        var evidence = mode == "disabled" ? Evidence(Plan("Disabled")) : mode == "pagination" ? Evidence()[..^1] + ",\"@odata.nextLink\":\"https://attacker.invalid\"}" : Evidence();
        using var handler = new Handler(keys, evidence, mode, During); using var transport = new HttpMessageInvoker(handler);
        var who = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var result = await store.CheckTeamsLicenseAsync(who, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, review.Id, policies, new(transport, clock), transport, new(transport));
        Assert.Equal(mode == "ok", result.TeamsPlanProvisioned); Assert.False(result.CanExecute); Assert.False(result.ProviderVerified); Assert.False(result.Connected);
        var audits = await f.Db.AuditLogs.Where(x => x.Action == "MeetingTeamsPlanObservedExecutionQualificationRequired").ToArrayAsync();
        if (mode == "ok") { Assert.Equal(503, result.StatusCode); var audit = Assert.Single(audits); Assert.DoesNotContain(ObjectId, audit.MetadataJson!); Assert.DoesNotContain(Tenant, audit.MetadataJson!); Assert.DoesNotContain(jwt, audit.MetadataJson!); Assert.DoesNotContain(Access, audit.MetadataJson!); Assert.Equal(1, handler.LicenseCalls); }
        else { Assert.Empty(audits); if (mode is "old-profile" or "no-token" or "policy" or "foreign" or "other-actor" or "anonymous" or "source-cipher") Assert.Equal(0, handler.LicenseCalls); }
    }
}
