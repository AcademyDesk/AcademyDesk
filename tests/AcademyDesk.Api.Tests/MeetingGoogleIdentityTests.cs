using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingGoogleIdentityTests
{
    private const string Subject = "synthetic-google-identity", Access = "synthetic-identity-access";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new("GoogleWorkspace", revision, "synthetic-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
    private static string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
    private static Dictionary<string, object> Claims(Clock clock, string nonce) => new()
    { ["iss"] = "https://accounts.google.com", ["aud"] = "synthetic-client", ["sub"] = Subject, ["iat"] = clock.Now.ToUnixTimeSeconds(),
        ["exp"] = clock.Now.AddHours(1).ToUnixTimeSeconds(), ["nonce"] = nonce, ["at_hash"] = Encode(SHA256.HashData(Encoding.ASCII.GetBytes(Access)).AsSpan(0, 16).ToArray()) };
    private static string Sign(RSA rsa, string payload, Dictionary<string, object>? header = null)
    {
        var data = Encode(JsonSerializer.SerializeToUtf8Bytes(header ?? new() { ["alg"] = "RS256", ["kid"] = "synthetic-key", ["typ"] = "JWT" })) + "." + Encode(Encoding.UTF8.GetBytes(payload));
        return data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
    private static Dictionary<string, object> Key(RSA rsa)
    { var p = rsa.ExportParameters(false); return new() { ["kty"] = "RSA", ["alg"] = "RS256", ["use"] = "sig", ["kid"] = "synthetic-key", ["n"] = Encode(p.Modulus!), ["e"] = Encode(p.Exponent!) }; }
    private sealed class KeysHandler(string keys, Func<Task>? before = null, string mode = "ok") : HttpMessageHandler
    {
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Assert.Equal(MeetingGoogleIdentityVerifier.KeysEndpoint, request.RequestUri!.AbsoluteUri); Assert.Null(request.Headers.Authorization);
            Assert.Empty(request.RequestUri.Query); Assert.Equal(HttpMethod.Get, request.Method);
            if (before is not null) await before();
            if (mode == "timeout") throw new TaskCanceledException(); if (mode == "network") throw new HttpRequestException();
            var response = new HttpResponseMessage(mode == "redirect" ? HttpStatusCode.Redirect : mode == "status" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent(mode == "oversize" ? new string('x', 65537) : keys, Encoding.UTF8, mode == "mime" ? "text/html" : "application/json") };
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return response;
        }
    }
    [Theory] [InlineData("absent")] [InlineData("valid")] [InlineData("malformed")] [InlineData("null")] [InlineData("number")]
    public void Exchange_only_quarantines_bounded_identity_tokens_and_never_serializes_them(string mode)
    {
        var clock = new Clock(); var reg = Registration();
        var fields = new Dictionary<string, object?> { ["access_token"] = Access, ["refresh_token"] = "synthetic-refresh", ["token_type"] = "Bearer", ["expires_in"] = 3600, ["scope"] = string.Join(' ', reg.Flow!.RequiredTokenScopes) };
        if (mode != "absent") fields["id_token"] = mode switch { "valid" => "eyJhbGciOiJSUzI1NiJ9.e30.c2ln", "malformed" => "not-a-jwt", "number" => 123, _ => null };
        var result = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), reg.Flow, clock.Now);
        Assert.Equal(mode is "valid" or "absent" ? 200 : 502, result.StatusCode);
        if (mode == "valid") { Assert.Equal(fields["id_token"], result.Tokens!.IdentityToken); Assert.DoesNotContain((string)fields["id_token"]!, JsonSerializer.Serialize(result)); Assert.Equal("{}", JsonSerializer.Serialize(result.Tokens)); }
    }
    [Theory] [InlineData("https://accounts.google.com")] [InlineData("accounts.google.com")]
    public async Task Signed_identity_matches_exact_nonce_audience_subject_and_access_but_grants_no_permissions(string issuer)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); var claims = Claims(clock, nonce); claims["iss"] = issuer; claims["azp"] = "synthetic-client";
        using var handler = new KeysHandler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } })); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingGoogleIdentityVerifier(transport, clock).VerifyAsync(Registration(), Sign(rsa, JsonSerializer.Serialize(claims)), Subject, nonce, Access);
        Assert.Equal(200, result.StatusCode); Assert.True(result.IdentityVerified); Assert.False(result.ProviderVerified); Assert.False(result.Connected); Assert.False(result.CanExecute); Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain(Subject, JsonSerializer.Serialize(result)); Assert.DoesNotContain(nonce, result.ToString());
    }
    [Theory] [InlineData("issuer")] [InlineData("audience")] [InlineData("audience-array")] [InlineData("azp")] [InlineData("subject")] [InlineData("nonce")]
    [InlineData("expired")] [InlineData("iat-future")] [InlineData("iat-stale")] [InlineData("long-lifetime")] [InlineData("missing-exp")] [InlineData("exp-string")]
    [InlineData("missing-iat")] [InlineData("nbf")] [InlineData("at-hash")] [InlineData("missing-at-hash")] [InlineData("duplicate-claim")]
    [InlineData("alg-none")] [InlineData("alg-hs")] [InlineData("jku")] [InlineData("jwk")] [InlineData("x5u")] [InlineData("crit")] [InlineData("b64")] [InlineData("typ")]
    [InlineData("unknown-kid")] [InlineData("bad-signature")] [InlineData("duplicate-header")] [InlineData("bad-compact")]
    public async Task Hostile_or_mismatched_signed_evidence_fails_closed(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); var claims = Claims(clock, nonce);
        var header = new Dictionary<string, object> { ["alg"] = "RS256", ["kid"] = "synthetic-key", ["typ"] = "JWT" };
        switch (mode)
        {
            case "issuer": claims["iss"] = "https://attacker.invalid"; break; case "audience": claims["aud"] = "other-client"; break;
            case "audience-array": claims["aud"] = new[] { "synthetic-client", "other-client" }; break; case "azp": claims["azp"] = "other-client"; break;
            case "subject": claims["sub"] = "another-user"; break; case "nonce": claims["nonce"] = Encode(RandomNumberGenerator.GetBytes(32)); break;
            case "expired": claims["exp"] = clock.Now.ToUnixTimeSeconds(); break; case "iat-future": claims["iat"] = clock.Now.AddMinutes(2).ToUnixTimeSeconds(); break;
            case "iat-stale": claims["iat"] = clock.Now.AddHours(-3).ToUnixTimeSeconds(); break; case "long-lifetime": claims["exp"] = clock.Now.AddHours(3).ToUnixTimeSeconds(); break;
            case "missing-exp": claims.Remove("exp"); break; case "exp-string": claims["exp"] = "9999999999"; break; case "missing-iat": claims.Remove("iat"); break;
            case "nbf": claims["nbf"] = clock.Now.AddMinutes(1).ToUnixTimeSeconds(); break; case "at-hash": claims["at_hash"] = "wrong"; break;
            case "missing-at-hash": claims.Remove("at_hash"); break; case "alg-none": header["alg"] = "none"; break; case "alg-hs": header["alg"] = "HS256"; break;
            case "jku": case "jwk": case "x5u": case "crit": case "b64": header[mode] = "https://attacker.invalid"; break;
            case "typ": header["typ"] = "at+jwt"; break; case "unknown-kid": header["kid"] = "unknown"; break;
        }
        var payload = JsonSerializer.Serialize(claims); if (mode == "duplicate-claim") payload = payload[..^1] + ",\"sub\":\"" + Subject + "\"}";
        var jwt = Sign(rsa, payload, header);
        if (mode == "bad-signature") { using var other = RSA.Create(2048); jwt = Sign(other, payload, header); }
        if (mode == "duplicate-header") jwt = Encode(Encoding.UTF8.GetBytes("{\"alg\":\"RS256\",\"kid\":\"synthetic-key\",\"kid\":\"synthetic-key\"}")) + "." + jwt.Split('.')[1] + "." + jwt.Split('.')[2];
        if (mode == "bad-compact") jwt = "not.a-valid.jwt=";
        using var handler = new KeysHandler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } })); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingGoogleIdentityVerifier(transport, clock).VerifyAsync(Registration(), jwt, Subject, nonce, Access);
        Assert.Equal(502, result.StatusCode); Assert.False(result.IdentityVerified); Assert.False(result.CanExecute); Assert.DoesNotContain(jwt, JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("status")] [InlineData("redirect")] [InlineData("timeout")] [InlineData("network")] [InlineData("mime")] [InlineData("encoding")] [InlineData("oversize")]
    [InlineData("malformed")] [InlineData("duplicate-id")] [InlineData("private-key")] [InlineData("weak-key")] [InlineData("bad-exponent")]
    [InlineData("wrong-use")] [InlineData("wrong-alg")] [InlineData("key-ops")] [InlineData("duplicate-root")] [InlineData("too-many")]
    public async Task Untrusted_or_unavailable_keys_never_validate_a_token(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32)); var key = Key(rsa);
        switch (mode) { case "private-key": key["d"] = "synthetic-private"; break; case "weak-key": key["n"] = Encode(new byte[128]); break;
            case "bad-exponent": key["e"] = "Aw"; break; case "wrong-use": key["use"] = "enc"; break; case "wrong-alg": key["alg"] = "HS256"; break; case "key-ops": key["key_ops"] = new[] { "sign" }; break; }
        var keys = JsonSerializer.Serialize(new { keys = mode == "duplicate-id" ? new[] { key, key } : mode == "too-many" ? Enumerable.Repeat(key, 17).ToArray() : new[] { key } });
        if (mode == "malformed") keys = "{"; if (mode == "duplicate-root") keys = "{\"keys\":[],\"keys\":[]}";
        using var handler = new KeysHandler(keys, mode: mode); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingGoogleIdentityVerifier(transport, clock).VerifyAsync(Registration(), Sign(rsa, JsonSerializer.Serialize(Claims(clock, nonce))), Subject, nonce, Access);
        Assert.Equal(502, result.StatusCode); Assert.False(result.IdentityVerified); Assert.Equal(1, handler.Calls);
    }
    [Fact] public async Task Token_expiring_while_keys_are_in_flight_is_rejected()
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var nonce = Encode(RandomNumberGenerator.GetBytes(32));
        using var handler = new KeysHandler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } }), () => { clock.Now = clock.Now.AddHours(2); return Task.CompletedTask; });
        using var transport = new HttpMessageInvoker(handler); var jwt = Sign(rsa, JsonSerializer.Serialize(Claims(clock, nonce)));
        Assert.False((await new MeetingGoogleIdentityVerifier(transport, clock).VerifyAsync(Registration(), jwt, Subject, nonce, Access)).IdentityVerified);
    }
    [Theory] [InlineData("ok")] [InlineData("no-token")] [InlineData("wrong-nonce")] [InlineData("policy")] [InlineData("expired-review")]
    [InlineData("revoked-during-keys")] [InlineData("stamp-during-keys")] [InlineData("disconnect-during-keys")] [InlineData("refresh-during-keys")]
    [InlineData("token-cipher")] [InlineData("intent-cipher")] [InlineData("review-cipher")]
    public async Task Host_store_binds_encrypted_source_current_authority_and_private_audit_without_enabling_connection(string mode)
    {
        using var rsa = RSA.Create(2048); var clock = new Clock(); var reg = Registration(); await using var f = await MeetingReadinessTests.Fixture.Create();
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock);
        var browser = Encode(RandomNumberGenerator.GetBytes(32)); var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.True(MeetingOAuthRegistrationRegistry.ValidRandom(intent.Material!.IdentityNonce));
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint, "synthetic-identity-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint);
        Assert.Equal(intent.Material.IdentityNonce, claim.Material!.IdentityNonce);
        var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        var jwt = Sign(rsa, JsonSerializer.Serialize(Claims(clock, mode == "wrong-nonce" ? Encode(RandomNumberGenerator.GetBytes(32)) : intent.Material.IdentityNonce!)));
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode == "no-token" ? null : jwt)))).StatusCode);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new(Subject, null, clock.Now))).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var policies = MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, Subject, null, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(1))]);
        if (mode == "policy") policies = MeetingAccountPolicyRegistry.Disabled; if (mode == "expired-review") clock.Now = clock.Now.AddMinutes(5);
        if (mode == "token-cipher") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "intent-cipher") (await f.Db.MeetingOAuthAttempts.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "review-cipher") (await f.Db.MeetingAccountReviews.SingleAsync()).ProtectedEnvelope = "corrupt";
        await f.Db.SaveChangesAsync();
        async Task During()
        {
            if (mode == "revoked-during-keys") await f.Users.RemoveFromRoleAsync(f.User, "Owner");
            if (mode == "stamp-during-keys") await f.Users.UpdateSecurityStampAsync(f.User);
            if (mode == "disconnect-during-keys") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true)).StatusCode);
            if (mode == "refresh-during-keys") Assert.Equal(201, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
        }
        using var handler = new KeysHandler(JsonSerializer.Serialize(new { keys = new[] { Key(rsa) } }), During); using var transport = new HttpMessageInvoker(handler);
        var result = await store.CheckGoogleIdentityAsync(f.Actor, f.Academy.Id, reg, review.Id, policies, new(transport, clock));
        Assert.False(result.Connected); Assert.False(result.ProviderVerified); Assert.False(result.CanExecute);
        Assert.Equal(mode == "ok", result.IdentityVerified);
        var audits = await f.Db.AuditLogs.Where(x => x.Action == "MeetingGoogleIdentityVerifiedEntitlementRequired").ToArrayAsync();
        if (mode == "ok") { Assert.Equal(503, result.StatusCode); var audit = Assert.Single(audits); Assert.DoesNotContain(Subject, audit.MetadataJson!); Assert.DoesNotContain(jwt, audit.MetadataJson!); Assert.DoesNotContain(Access, audit.MetadataJson!); }
        else Assert.Empty(audits);
        Assert.DoesNotContain(jwt, JsonSerializer.Serialize(result)); Assert.DoesNotContain(jwt, (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope);
    }
}
