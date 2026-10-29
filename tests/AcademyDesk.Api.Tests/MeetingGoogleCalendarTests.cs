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

public sealed class MeetingGoogleCalendarTests
{
    private const string Access = "synthetic-calendar-access", Subject = "synthetic-calendar-subject", CalendarId = "synthetic-private-calendar@example.invalid";
    private const string Metadata = "{\"kind\":\"calendar#calendar\",\"id\":\"" + CalendarId + "\",\"conferenceProperties\":{\"allowedConferenceSolutionTypes\":[\"hangoutsMeet\"]}}";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(bool metadata = true, int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new("GoogleWorkspace", revision, "synthetic-client", "https://api.academy.invalid/api/meetings/oauth/GoogleWorkspace/callback", "https://app.academy.invalid/communication-settings", metadata ? MeetingOAuthFlowProfile.GoogleMetadataProfile : "google-calendar-owned-web/1")]).Resolve("GoogleWorkspace")!;
    private static string Encode(byte[] bytes) => WebEncoders.Base64UrlEncode(bytes);
    private static string Keys(RSA rsa) { var p = rsa.ExportParameters(false); return JsonSerializer.Serialize(new { keys = new[] { new { kty = "RSA", alg = "RS256", use = "sig", kid = "synthetic-key", n = Encode(p.Modulus!), e = Encode(p.Exponent!) } } }); }
    private static string Jwt(RSA rsa, Clock clock, string nonce)
    {
        var head = Encode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "RS256", kid = "synthetic-key", typ = "JWT" }));
        var payload = Encode(JsonSerializer.SerializeToUtf8Bytes(new { iss = "https://accounts.google.com", aud = "synthetic-client", sub = Subject, iat = clock.Now.ToUnixTimeSeconds(), exp = clock.Now.AddHours(1).ToUnixTimeSeconds(), nonce,
            at_hash = Encode(SHA256.HashData(Encoding.ASCII.GetBytes(Access)).AsSpan(0, 16).ToArray()) }));
        var data = head + "." + payload; return data + "." + Encode(rsa.SignData(Encoding.ASCII.GetBytes(data), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }
    private sealed class Handler(string keys = "{}", string metadata = Metadata, string mode = "ok", Func<Task>? duringCalendar = null) : HttpMessageHandler
    {
        internal int KeyCalls, CalendarCalls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            if (request.RequestUri!.AbsoluteUri == MeetingGoogleIdentityVerifier.KeysEndpoint)
            { KeyCalls++; Assert.Null(request.Headers.Authorization); return new(HttpStatusCode.OK) { Content = new StringContent(keys, Encoding.UTF8, "application/json") }; }
            Assert.Equal(MeetingGoogleCalendarProbe.Endpoint, request.RequestUri.AbsoluteUri); Assert.Null(request.Content);
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal(Access, request.Headers.Authorization.Parameter);
            Assert.DoesNotContain(Access, request.RequestUri.AbsoluteUri); Assert.True(request.Headers.CacheControl!.NoStore); CalendarCalls++;
            if (duringCalendar is not null) await duringCalendar();
            if (mode == "network") throw new HttpRequestException("synthetic"); if (mode == "timeout") throw new TaskCanceledException();
            var status = mode switch { "unauthorized" => HttpStatusCode.Unauthorized, "forbidden" => HttpStatusCode.Forbidden, "redirect" => HttpStatusCode.Redirect,
                "throttled" => HttpStatusCode.TooManyRequests, "error" => HttpStatusCode.ServiceUnavailable, _ => HttpStatusCode.OK };
            var response = new HttpResponseMessage(status) { Content = new StringContent(mode == "oversize" ? new string('x', 32769) : metadata, Encoding.UTF8, mode == "mime" ? "text/html" : "application/json") };
            if (mode == "encoding") response.Content.Headers.ContentEncoding.Add("gzip"); return response;
        }
    }
    [Fact] public void New_profile_pins_narrow_metadata_scope_without_broadening_or_reusing_old_registration()
    {
        var old = Registration(false); var current = Registration();
        Assert.NotEqual(old.Fingerprint, current.Fingerprint); Assert.True(current.Flow!.IsGoogleIdentityProfile);
        Assert.DoesNotContain(MeetingOAuthFlowProfile.GoogleMetadataScope, old.Flow!.RequestedScopes);
        Assert.Contains(MeetingOAuthFlowProfile.GoogleMetadataScope, current.Flow.RequestedScopes); Assert.Contains(MeetingOAuthFlowProfile.GoogleMetadataScope, current.Flow.RequiredTokenScopes);
        Assert.DoesNotContain("https://www.googleapis.com/auth/calendar", current.Flow.RequestedScopes);
        Assert.DoesNotContain("https://www.googleapis.com/auth/calendar.readonly", current.Flow.RequestedScopes);
        Assert.Equal("email", current.Flow.NormalizeScope("https://www.googleapis.com/auth/userinfo.email"));
        Assert.Equal(old.Flow.AuthorizeEndpoint, current.Flow.AuthorizeEndpoint); Assert.Equal(old.Flow.TokenEndpoint, current.Flow.TokenEndpoint);
        Assert.True(MeetingOAuthAttemptStore.SupportsRefresh(current));
    }
    [Fact] public void New_profile_requires_both_granted_scopes_and_quarantines_optional_identity_token()
    {
        var reg = Registration(); var fields = new Dictionary<string, object> { ["access_token"] = Access, ["refresh_token"] = "synthetic-refresh", ["token_type"] = "Bearer", ["expires_in"] = 3600, ["scope"] = string.Join(' ', reg.Flow!.RequiredTokenScopes), ["id_token"] = "e30.e30.c2ln" };
        var accepted = MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), reg.Flow, DateTimeOffset.UtcNow);
        Assert.Equal(200, accepted.StatusCode); Assert.Equal(fields["id_token"], accepted.Tokens!.IdentityToken);
        fields["scope"] = "https://www.googleapis.com/auth/calendar.events.owned";
        Assert.Equal(502, MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), reg.Flow, DateTimeOffset.UtcNow).StatusCode);
        fields["scope"] = string.Join(' ', reg.Flow.RequiredTokenScopes);
        Assert.Equal(502, MeetingOAuthCodeExchange.Parse(JsonSerializer.SerializeToUtf8Bytes(fields), Registration(false).Flow!, DateTimeOffset.UtcNow).StatusCode);
    }
    [Theory] [InlineData("valid")] [InlineData("empty-solutions")] [InlineData("missing-properties")] [InlineData("missing-solutions")] [InlineData("legacy-only")] [InlineData("wrong-case")]
    [InlineData("null")] [InlineData("array")] [InlineData("malformed")] [InlineData("kind")] [InlineData("missing-id")] [InlineData("id-number")] [InlineData("id-space")] [InlineData("id-too-long")]
    [InlineData("duplicate-root")] [InlineData("duplicate-properties")] [InlineData("solutions-type")] [InlineData("solution-number")] [InlineData("duplicate-solutions")]
    [InlineData("solution-url")] [InlineData("properties-null")] [InlineData("error")] [InlineData("oversize")] [InlineData("too-many")]
    public void Metadata_is_bounded_unambiguous_and_never_permission_or_a_raw_calendar_disclosure(string mode)
    {
        var json = mode switch {
            "empty-solutions" => Metadata.Replace("\"hangoutsMeet\"", ""), "missing-properties" => "{\"kind\":\"calendar#calendar\",\"id\":\"safe\"}",
            "missing-solutions" => Metadata.Replace("\"allowedConferenceSolutionTypes\":[\"hangoutsMeet\"]", ""), "legacy-only" => Metadata.Replace("hangoutsMeet", "eventHangout"),
            "wrong-case" => Metadata.Replace("hangoutsMeet", "HangoutsMeet"), "null" => "null", "array" => "[]", "malformed" => "{", "kind" => Metadata.Replace("calendar#calendar", "calendar#event"),
            "missing-id" => Metadata.Replace("\"id\":\"" + CalendarId + "\",", ""), "id-number" => Metadata.Replace("\"" + CalendarId + "\"", "12"),
            "id-space" => Metadata.Replace(CalendarId, "unsafe calendar"), "id-too-long" => Metadata.Replace(CalendarId, new string('x', 1025)),
            "duplicate-root" => Metadata[..^1] + ",\"id\":\"another\"}", "duplicate-properties" => Metadata.Replace("\"allowedConferenceSolutionTypes\":[\"hangoutsMeet\"]", "\"allowedConferenceSolutionTypes\":[],\"allowedConferenceSolutionTypes\":[\"hangoutsMeet\"]"),
            "solutions-type" => Metadata.Replace("[\"hangoutsMeet\"]", "true"), "solution-number" => Metadata.Replace("[\"hangoutsMeet\"]", "[12]"),
            "duplicate-solutions" => Metadata.Replace("[\"hangoutsMeet\"]", "[\"hangoutsMeet\",\"hangoutsMeet\"]"), "solution-url" => Metadata.Replace("hangoutsMeet", "https://attacker.invalid"),
            "properties-null" => Metadata.Replace("{\"allowedConferenceSolutionTypes\":[\"hangoutsMeet\"]}", "null"), "error" => Metadata[..^1] + ",\"error\":{\"message\":\"private\"}}",
            "oversize" => new string('x', 32769), "too-many" => Metadata.Replace("[\"hangoutsMeet\"]", JsonSerializer.Serialize(Enumerable.Range(0, 17).Select(x => "type" + x))), _ => Metadata };
        var result = MeetingGoogleCalendarProbe.Parse(Encoding.UTF8.GetBytes(json));
        Assert.Equal(mode == "valid", result.MeetSupported); Assert.False(result.CanExecute); Assert.False(result.ProviderVerified); Assert.False(result.Connected);
        Assert.Equal(mode == "valid" ? 200 : mode is "empty-solutions" or "missing-properties" or "missing-solutions" or "legacy-only" or "wrong-case" ? 409 : 502, result.StatusCode);
        Assert.DoesNotContain(CalendarId, JsonSerializer.Serialize(result)); Assert.DoesNotContain(CalendarId, result.ToString());
        if (mode != "valid") Assert.Null(result.CalendarFingerprint); else Assert.Equal(64, result.CalendarFingerprint!.Length);
    }
    [Theory] [InlineData("ok")] [InlineData("unauthorized")] [InlineData("forbidden")] [InlineData("redirect")] [InlineData("throttled")]
    [InlineData("error")] [InlineData("network")] [InlineData("timeout")] [InlineData("mime")] [InlineData("encoding")] [InlineData("oversize")]
    public async Task Fixed_bearer_header_read_does_not_retry_redirect_expose_or_enable(string mode)
    {
        using var handler = new Handler(mode: mode); using var transport = new HttpMessageInvoker(handler);
        var result = await new MeetingGoogleCalendarProbe(transport).InspectAsync(Access);
        Assert.Equal(mode == "ok" ? 200 : 502, result.StatusCode); Assert.Equal(1, handler.CalendarCalls); Assert.Equal(0, handler.KeyCalls);
        Assert.False(result.CanExecute); Assert.DoesNotContain(Access, JsonSerializer.Serialize(result));
    }
    [Theory] [InlineData("ok")] [InlineData("old-profile")] [InlineData("wrong-policy")] [InlineData("no-identity")] [InlineData("no-meet")]
    [InlineData("expired")] [InlineData("disconnect")] [InlineData("refresh")] [InlineData("role")] [InlineData("stamp")]
    [InlineData("foreign")] [InlineData("other-actor")] [InlineData("anonymous")] [InlineData("source-cipher")] [InlineData("review-cipher")]
    public async Task Capability_check_requires_signed_identity_current_host_authority_and_exact_source_after_provider_read(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); using var rsa = RSA.Create(2048); var clock = new Clock(); var reg = Registration(mode != "old-profile");
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = Encode(RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-calendar-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var permit = (await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim)).Permit!;
        var jwt = Jwt(rsa, clock, intent.Material.IdentityNonce!);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, permit, new(200, "TokensReceivedAccountUnverified", Tokens: new(Access, "synthetic-calendar-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes, mode == "no-identity" ? null : jwt)))).StatusCode);
        var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, permit, new(Subject, null, clock.Now))).Review!;
        Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        var policies = mode == "wrong-policy" ? MeetingAccountPolicyRegistry.Disabled : MeetingAccountPolicyRegistry.Create([new(f.Academy.Id, reg.Provider, 1, reg.Fingerprint, Subject, null, null, clock.Now.AddMinutes(-1), clock.Now.AddHours(1))]);
        if (mode == "source-cipher") (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "corrupt";
        if (mode == "review-cipher") (await f.Db.MeetingAccountReviews.SingleAsync()).ProtectedEnvelope = "corrupt"; await f.Db.SaveChangesAsync();
        async Task During()
        {
            if (mode == "expired") clock.Now = clock.Now.AddHours(2);
            if (mode == "disconnect") Assert.Equal(200, (await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true)).StatusCode);
            if (mode == "refresh") Assert.Equal(201, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
            if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        }
        using var handler = new Handler(Keys(rsa), mode == "no-meet" ? Metadata.Replace("hangoutsMeet", "eventHangout") : Metadata, duringCalendar: During); using var transport = new HttpMessageInvoker(handler);
        var who = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var result = await store.CheckGoogleCalendarAsync(who, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, review.Id, policies, new(transport, clock), new(transport));
        Assert.Equal(mode == "ok", result.MeetSupported); Assert.False(result.Connected); Assert.False(result.ProviderVerified); Assert.False(result.CanExecute);
        Assert.DoesNotContain(CalendarId, JsonSerializer.Serialize(result)); Assert.DoesNotContain(jwt, JsonSerializer.Serialize(result)); Assert.Null(result.CalendarFingerprint);
        var audits = await f.Db.AuditLogs.Where(x => x.Action == "MeetingGoogleMeetAdvertisedExecutionQualificationRequired").ToArrayAsync();
        if (mode == "ok") { Assert.Equal(503, result.StatusCode); var audit = Assert.Single(audits); Assert.DoesNotContain(CalendarId, audit.MetadataJson!); Assert.DoesNotContain(jwt, audit.MetadataJson!); Assert.DoesNotContain(Access, audit.MetadataJson!); Assert.Equal(1, handler.CalendarCalls); }
        else { Assert.Empty(audits); if (mode is "old-profile" or "wrong-policy" or "no-identity" or "foreign" or "other-actor" or "anonymous" or "source-cipher" or "review-cipher") Assert.Equal(0, handler.CalendarCalls); }
    }
}
