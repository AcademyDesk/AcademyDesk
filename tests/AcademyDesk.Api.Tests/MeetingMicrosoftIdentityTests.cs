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

public sealed class MeetingMicrosoftIdentityTests
{
    private const string Tenant = "019acc9a-6200-7000-8000-000000000001", ObjectId = "019acc9a-6200-7000-8000-000000000002", Client = "019acc9a-6200-7000-8000-000000000003";
    private const string Access = "synthetic-opaque-graph-access";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(bool identity = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new("Microsoft365", revision, Client, "https://api.academy.invalid/api/meetings/oauth/Microsoft365/callback", "https://app.academy.invalid/communication-settings", identity ? MeetingOAuthFlowProfile.MicrosoftIdentityProfile : "microsoft-online-meetings-work/1", Tenant)]).Resolve("Microsoft365")!;
    private static string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
    private static Dictionary<string, object> Claims(Clock clock, string nonce) => new() { ["iss"] = "https://login.microsoftonline.com/" + Tenant + "/v2.0", ["aud"] = Client,
        ["tid"] = Tenant, ["ver"] = "2.0", ["oid"] = ObjectId, ["sub"] = "pairwise-application-subject-not-graph-id", ["nonce"] = nonce,
        ["iat"] = clock.Now.ToUnixTimeSeconds(), ["nbf"] = clock.Now.AddSeconds(-30).ToUnixTimeSeconds(), ["exp"] = clock.Now.AddHours(1).ToUnixTimeSeconds(),
        ["roles"] = new[] { "Owner", "PlatformOwner" }, ["email"] = "not-authority@example.invalid" };
    private static Dictionary<string, object> Key(RSA rsa, string? issuer = null) { var p = rsa.ExportParameters(false); return new() { ["kty"] = "RSA", ["use"] = "sig", ["kid"] = "synthetic-key",
        ["n"] = Encode(p.Modulus!), ["e"] = Encode(p.Exponent!), ["issuer"] = issuer ?? MeetingMicrosoftIdentityVerifier.IssuerTemplate,
        ["x5t"] = "ignored-public-metadata", ["x5c"] = new[] { "ignored-public-certificate-metadata" } }; }
    private static string Sign(RSA rsa, string payload, Dictionary<string, object>? header = null)
    {
        var data = Encode(JsonSerializer.SerializeToUtf8Bytes(header ?? new() { ["alg"] = "RS256", ["typ"] = "JWT", ["kid"] = "synthetic-key" })) + "." + Encode(Encoding.UTF8.GetBytes(payload));
        return data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
    private sealed class Handler(string keys, string account = ObjectId, Func<Task>? duringKeys = null, Func<Task>? duringAccount = null, string mode = "ok") : HttpMessageHandler
    {
        internal int KeyCalls, AccountCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Empty(request.RequestUri!.Query);
            if (request.RequestUri.AbsoluteUri == MeetingMicrosoftIdentityVerifier.KeysEndpoint(Registration()))
            {
                KeyCalls++; Assert.Null(request.Headers.Authorization); if (duringKeys is not null) await duringKeys();
                if (mode == "timeout") throw new TaskCanceledException(); if (mode == "network") throw new HttpRequestException();
                var result = new HttpResponseMessage(mode == "redirect" ? HttpStatusCode.Redirect : mode == "status" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
                { Content = new StringContent(mode == "oversize" ? new string('x', 131073) : keys, Encoding.UTF8, mode == "mime" ? "text/html" : "application/json") };
                if (mode == "encoding") result.Content.Headers.ContentEncoding.Add("gzip"); return result;
            }
            Assert.Equal("https://graph.microsoft.com/v1.0/me", request.RequestUri.AbsoluteUri); Assert.Equal(Access, request.Headers.Authorization!.Parameter); Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            AccountCalls++; if (duringAccount is not null) await duringAccount();
            return new(mode == "account-forbidden" ? HttpStatusCode.Forbidden : HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { id = account }), Encoding.UTF8, "application/json") };
        }
    }
    [Fact] public void New_identity_profile_is_distinct_scoped_to_work_directory_and_requires_canonical_application_id()
    {
        var old = Registration(false); var current = Registration(); Assert.NotEqual(old.Fingerprint, current.Fingerprint);
        Assert.DoesNotContain("openid", old.Flow!.RequestedScopes); Assert.Contains("openid", current.Flow!.RequestedScopes); Assert.Contains("profile", current.Flow.RequestedScopes);
        Assert.Equal(old.Flow.RequiredTokenScopes, current.Flow.RequiredTokenScopes); Assert.Equal("User.Read", current.Flow.NormalizeScope("https://graph.microsoft.com/User.Read")); Assert.True(MeetingOAuthAttemptStore.SupportsRefresh(current));
        foreach (var client in new[] { "synthetic-not-guid", Guid.Empty.ToString(), Client.ToUpperInvariant() })
        {
            var r = MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid", [new("Microsoft365", 1, client, current.Callback.AbsoluteUri, current.WorkspaceReturn.AbsoluteUri, MeetingOAuthFlowProfile.MicrosoftIdentityProfile, Tenant)]);
            if (client != Client) Assert.False(r.IsValid);
        }
        Assert.Null(MeetingOAuthFlowProfile.Resolve("Microsoft365", MeetingOAuthFlowProfile.MicrosoftIdentityProfile, "9188040d-6c67-4c5b-b112-36a304b66dad"));
    }
    [Theory] [InlineData("absent")] [InlineData("valid")] [InlineData("malformed")] [InlineData("null")]
    public void Identity_token_is_only_private_quarantine_and_missing_evidence_never_means_verified(string mode)
    {
        var reg = Registration(); var fields = new Dictionary<string, object?> { ["access_token"] = Access, ["refresh_token"] = "synthetic-refresh", ["token_type"] = "Bearer", ["expires_in"] = 3600, ["scope"] = string.Join(' ', reg.Flow!.RequiredTokenScopes) };
        if (mode != "absent") fields["id_token"] = mode == "valid" ? "e30.e30.c2ln" : mode == "malformed" ? "bad" : null;
        var result = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), reg.Flow, DateTimeOffset.UtcNow);
        Assert.Equal(mode is "absent" or "valid" ? 200 : 502, result.StatusCode);
        if (mode == "valid") { Assert.Equal("e30.e30.c2ln", result.Tokens!.IdentityToken); Assert.DoesNotContain("e30.e30.c2ln", JsonSerializer.Serialize(result)); Assert.Null(MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), Registration(false).Flow!, DateTimeOffset.UtcNow).Tokens!.IdentityToken); }
    }
    [Theory] [InlineData("template")] [InlineData("exact")]
    public async Task Valid_signed_v2_directory_object_identity_does_not_require_optional_access_hash_or_grant_permissions(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); var reg = Registration();
        using var handler = new Handler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa, mode == "exact" ? MeetingMicrosoftIdentityVerifier.Issuer(reg) : null) } })); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingMicrosoftIdentityVerifier(transport, clock).VerifyAsync(reg, Sign(rsa, JsonSerializer.Serialize(Claims(clock, nonce))), ObjectId, nonce);
        Assert.Equal(200, result.StatusCode); Assert.True(result.IdentityVerified); Assert.False(result.DirectoryAccountMatched); Assert.False(result.ProviderVerified); Assert.False(result.CanExecute); Assert.Equal(1, handler.KeyCalls); Assert.Equal(0, handler.AccountCalls);
    }
    [Theory] [InlineData("issuer")] [InlineData("tenant")] [InlineData("audience")] [InlineData("aud-array")] [InlineData("azp")] [InlineData("object")]
    [InlineData("subject")] [InlineData("nonce")] [InlineData("version")] [InlineData("expiry")] [InlineData("iat-future")] [InlineData("iat-stale")]
    [InlineData("long-expiry")] [InlineData("nbf")] [InlineData("missing-nbf")] [InlineData("string-exp")] [InlineData("missing-exp")] [InlineData("duplicate-claim")]
    [InlineData("alg")] [InlineData("typ")] [InlineData("jku")] [InlineData("jwk")] [InlineData("x5u")] [InlineData("crit")] [InlineData("b64")]
    [InlineData("bad-signature")] [InlineData("unknown-kid")] [InlineData("key-issuer")] [InlineData("missing-key-issuer")] [InlineData("key-private")]
    [InlineData("key-use")] [InlineData("key-duplicate")] [InlineData("key-weak")] [InlineData("key-alg")] [InlineData("keys-malformed")]
    public async Task Hostile_or_mismatched_identity_and_key_material_is_rejected(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); var reg = Registration(); var claims = Claims(clock, nonce); var key = Key(rsa);
        var header = new Dictionary<string, object> { ["alg"] = "RS256", ["typ"] = "JWT", ["kid"] = "synthetic-key" };
        switch (mode)
        {
            case "issuer": claims["iss"] = "https://attacker.invalid/" + Tenant + "/v2.0"; break; case "tenant": claims["tid"] = "019acc9a-6200-7000-8000-000000000099"; break;
            case "audience": claims["aud"] = Tenant; break; case "aud-array": claims["aud"] = new[] { Client, Tenant }; break; case "azp": claims["azp"] = Tenant; break;
            case "object": claims["oid"] = Tenant; break; case "subject": claims.Remove("sub"); break; case "nonce": claims["nonce"] = Encode(RandomNumberGenerator.GetBytes(32)); break;
            case "version": claims["ver"] = "1.0"; break; case "expiry": claims["exp"] = clock.Now.ToUnixTimeSeconds(); break;
            case "iat-future": claims["iat"] = clock.Now.AddMinutes(3).ToUnixTimeSeconds(); break; case "iat-stale": claims["iat"] = clock.Now.AddHours(-3).ToUnixTimeSeconds(); break;
            case "long-expiry": claims["exp"] = clock.Now.AddHours(3).ToUnixTimeSeconds(); break; case "nbf": claims["nbf"] = clock.Now.AddMinutes(1).ToUnixTimeSeconds(); break;
            case "missing-nbf": claims.Remove("nbf"); break; case "string-exp": claims["exp"] = "9999999999"; break; case "missing-exp": claims.Remove("exp"); break;
            case "alg": header["alg"] = "HS256"; break; case "typ": header["typ"] = "at+jwt"; break; case "jku": case "jwk": case "x5u": case "crit": case "b64": header[mode] = "https://attacker.invalid"; break;
            case "unknown-kid": header["kid"] = "unknown"; break; case "key-issuer": key["issuer"] = "https://login.microsoftonline.com/" + ObjectId + "/v2.0"; break;
            case "missing-key-issuer": key.Remove("issuer"); break; case "key-private": key["d"] = "private"; break; case "key-use": key["use"] = "enc"; break;
            case "key-weak": key["n"] = Encode(new byte[128]); break; case "key-alg": key["alg"] = "HS256"; break;
        }
        var payload = JsonSerializer.Serialize(claims); if (mode == "duplicate-claim") payload = payload[..^1] + ",\"tid\":\"" + Tenant + "\"}";
        var jwt = Sign(rsa, payload, header); if (mode == "bad-signature") { using var other = RSA.Create(2048); jwt = Sign(other, payload, header); }
        var keys = JsonSerializer.Serialize(new { keys = mode == "key-duplicate" ? new[] { key, key } : new[] { key } }); if (mode == "keys-malformed") keys = "{";
        using var handler = new Handler(keys); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingMicrosoftIdentityVerifier(transport, clock).VerifyAsync(reg, jwt, ObjectId, nonce);
        Assert.Equal(502, result.StatusCode); Assert.False(result.IdentityVerified); Assert.False(result.CanExecute); Assert.Equal(0, handler.AccountCalls); Assert.DoesNotContain(jwt, JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("status")] [InlineData("redirect")] [InlineData("timeout")] [InlineData("network")] [InlineData("mime")] [InlineData("encoding")] [InlineData("oversize")]
    public async Task Public_key_transport_is_fixed_bounded_and_does_not_retry_or_send_credentials(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); using var handler = new Handler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } }), mode: mode); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingMicrosoftIdentityVerifier(transport, clock).VerifyAsync(Registration(), Sign(rsa, JsonSerializer.Serialize(Claims(clock, nonce))), ObjectId, nonce);
        Assert.Equal(502, result.StatusCode); Assert.False(result.IdentityVerified); Assert.Equal(1, handler.KeyCalls); Assert.Equal(0, handler.AccountCalls);
    }
    [Theory] [InlineData("ok")] [InlineData("old-profile")] [InlineData("no-token")] [InlineData("policy")] [InlineData("wrong-account")] [InlineData("account-forbidden")]
    [InlineData("role-at-keys")] [InlineData("role-at-account")] [InlineData("stamp-at-account")] [InlineData("disconnect-at-account")] [InlineData("refresh-at-account")]
    [InlineData("expiry-at-account")] [InlineData("foreign")] [InlineData("other-actor")] [InlineData("anonymous")] [InlineData("source-cipher")] [InlineData("intent-cipher")] [InlineData("review-cipher")]
    public async Task Host_requires_exact_signed_directory_object_and_current_Graph_account_without_model_role_authority(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); using var rsa = RSA.Create(2048); var clock = new Clock(); var reg = Registration(mode != "old-profile");
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = Encode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.True(MeetingOAuthRegistrationRegistry.ValidRandom(intent.Material!.IdentityNonce));
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint, "synthetic-ms-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); Assert.Equal(intent.Material.IdentityNonce, claim.Material!.IdentityNonce);
        var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!; var jwt = Sign(rsa, JsonSerializer.Serialize(Claims(clock, intent.Material.IdentityNonce!)));
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-ms-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode is "old-profile" or "no-token" ? null : jwt)))).StatusCode);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new(ObjectId, null, clock.Now))).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var policies = mode == "policy" ? MeetingAccountPolicyRegistry.Disabled : MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, ObjectId, null, Tenant, clock.Now.AddMinutes(-1), clock.Now.AddHours(3))]);
        if (mode == "source-cipher") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "intent-cipher") (await f.Db.MeetingOAuthAttempts.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "review-cipher") (await f.Db.MeetingAccountReviews.SingleAsync()).ProtectedEnvelope = "corrupt"; await f.Db.SaveChangesAsync();
        async Task DuringKeys() { if (mode == "role-at-keys") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); }
        async Task DuringAccount()
        {
            if (mode == "role-at-account") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp-at-account") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "disconnect-at-account") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true)).StatusCode);
            if (mode == "refresh-at-account") Assert.Equal(201, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
            if (mode == "expiry-at-account") clock.Now = clock.Now.AddHours(2);
        }
        using var handler = new Handler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } }), mode == "wrong-account" ? Tenant : ObjectId, DuringKeys, DuringAccount, mode); using var transport = new HttpMessageInvoker(handler);
        var who = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var result = await store.CheckMicrosoftIdentityAsync(who, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, review.Id, policies, new(transport, clock), transport);
        Assert.Equal(mode == "ok", result.IdentityVerified); Assert.Equal(mode == "ok", result.DirectoryAccountMatched); Assert.False(result.ProviderVerified); Assert.False(result.Connected); Assert.False(result.CanExecute);
        var audits = await f.Db.AuditLogs.Where(x => x.Action == "MeetingMicrosoftIdentityVerifiedEntitlementRequired").ToArrayAsync();
        if (mode == "ok") { Assert.Equal(503, result.StatusCode); var audit = Assert.Single(audits); Assert.DoesNotContain(ObjectId, audit.MetadataJson!); Assert.DoesNotContain(Tenant, audit.MetadataJson!); Assert.DoesNotContain(jwt, audit.MetadataJson!); Assert.DoesNotContain(Access, audit.MetadataJson!); Assert.Equal(1, handler.AccountCalls); }
        else { Assert.Empty(audits); if (mode is "old-profile" or "no-token" or "policy" or "role-at-keys" or "foreign" or "other-actor" or "anonymous" or "source-cipher" or "intent-cipher" or "review-cipher") Assert.Equal(0, handler.AccountCalls); }
        Assert.DoesNotContain(jwt, JsonSerializer.Serialize(result)); Assert.DoesNotContain(jwt, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope);
    }
}
