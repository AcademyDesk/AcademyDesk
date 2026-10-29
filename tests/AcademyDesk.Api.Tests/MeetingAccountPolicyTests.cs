using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Meetings;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MeetingAccountPolicyTests
{
    private const string DirectoryId = "019acc9a-6200-7000-8000-000000000001", MicrosoftSubject = "019acc9a-6200-7000-8000-000000000002";
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static MeetingOAuthRegistration Registration(string provider = "GoogleWorkspace", int revision = 1) => MeetingOAuthRegistrationRegistry.Create("https://api.academy.invalid", "https://app.academy.invalid",
        [new(provider, revision, "synthetic-client", "https://api.academy.invalid" + MeetingOAuthRegistrationRegistry.CallbackPath(provider), "https://app.academy.invalid/communication-settings",
            provider == "GoogleWorkspace" ? "google-calendar-owned-web/1" : provider == "Microsoft365" ? "microsoft-online-meetings-work/1" : "zoom-public-pkce/1", provider == "Microsoft365" ? DirectoryId : null)]).Resolve(provider)!;
    private static MeetingAccountPolicyInput Input(Guid academy, Clock clock, MeetingOAuthRegistration reg) => new(academy, reg.Provider, 1, reg.Fingerprint,
        reg.Provider == "Microsoft365" ? MicrosoftSubject : "synthetic-policy-subject", reg.Provider == "Zoom" ? "synthetic-policy-account" : null,
        reg.Flow!.DirectoryTenant, clock.Now.AddMinutes(-1), clock.Now.AddHours(1));
    private static async Task<(MeetingOAuthAttemptStore Store, MeetingOAuthExchangePermit Permit, MeetingAccountReviewState Review)> Setup(MeetingReadinessTests.Fixture f, Clock clock, MeetingOAuthRegistration reg, bool confirm = true)
    {
        var store = new MeetingOAuthAttemptStore(f.Db, f.Users, f.Readiness, new EphemeralDataProtectionProvider(), clock); var browser = WebEncoders.Base64UrlEncode(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var intent = await store.PrepareAsync(f.Actor, f.Academy.Id, reg.Provider, 0, browser, reg.Fingerprint);
        Assert.Equal(202, (await store.CaptureAsync(f.Academy.Id, reg.Provider, intent.Material!.State, browser, reg.Fingerprint, "synthetic-policy-code", false)).StatusCode);
        var claim = await store.ResumeAsync(f.Actor, f.Academy.Id, reg.Provider, intent.Material.State, browser, reg.Fingerprint); var exchange = await store.BeginExchangeAsync(f.Actor, f.Academy.Id, reg, claim);
        Assert.Equal(200, (await store.FinishExchangeAsync(f.Actor, f.Academy.Id, reg, exchange.Permit!, new(200, "TokensReceivedAccountUnverified", Tokens: new("synthetic-policy-access", "synthetic-policy-refresh", clock.Now.AddHours(1), reg.Flow!.RequiredTokenScopes)))).StatusCode);
        var input = Input(f.Academy.Id, clock, reg); var review = (await store.PrepareAccountReviewAsync(f.Actor, f.Academy.Id, reg, exchange.Permit!, new(input.Subject, input.Account, clock.Now))).Review!;
        if (confirm) Assert.Equal(200, (await store.ConfirmAccountReviewAsync(f.Actor, f.Academy.Id, reg, review.Id, 0, review.ReviewHash, true)).StatusCode);
        return (store, exchange.Permit!, review);
    }
    [Theory] [InlineData("GoogleWorkspace")] [InlineData("Microsoft365")] [InlineData("Zoom")]
    public async Task Policy_match_is_private_audited_and_still_unavailable_without_provider_evidence(string provider)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(provider); var (store, _, review) = await Setup(f, clock, reg);
        var input = Input(f.Academy.Id, clock, reg); var inputs = new[] { input }; var registry = MeetingAccountPolicyRegistry.Create(inputs); Assert.True(registry.IsValid);
        var p = registry.Resolve(f.Academy.Id, reg, clock.Now)!; inputs[0] = input with { Subject = "modified-outside-snapshot" };
        Assert.Equal("{}", JsonSerializer.Serialize(input)); Assert.DoesNotContain(input.Subject, p.ToString()); Assert.DoesNotContain(input.Subject, registry.ToString());
        var outcome = await store.CheckAccountPolicyAsync(f.Actor, f.Academy.Id, reg, review.Id, registry);
        Assert.Equal(503, outcome.StatusCode); Assert.Equal("IndependentProviderEvidenceRequired", outcome.Code); Assert.True(outcome.AccountPolicyMatched); Assert.Equal(p.Fingerprint, outcome.PolicyFingerprint);
        Assert.False(outcome.ProviderVerified); Assert.False(outcome.Connected); Assert.False(outcome.CanExecute); Assert.False(outcome.CanRetry);
        var audit = await f.Db.AuditLogs.SingleAsync(x => x.Action == "MeetingAccountPolicyMatchedEvidenceRequired"); Assert.Contains(p.Fingerprint, audit.MetadataJson!); Assert.DoesNotContain(input.Subject, audit.MetadataJson!); Assert.DoesNotContain("synthetic-policy-access", audit.MetadataJson!);
        Assert.Equal("VerificationRequired", (await f.Db.MeetingProviderBindings.SingleAsync()).Status); Assert.Equal("ConfirmedNotConnected", (await f.Db.MeetingAccountReviews.SingleAsync()).Status);
        Assert.DoesNotContain(input.Subject, JsonSerializer.Serialize(outcome));
    }
    [Theory] [InlineData("empty")] [InlineData("null")] [InlineData("academy")] [InlineData("provider")] [InlineData("revision")] [InlineData("registration")]
    [InlineData("subject")] [InlineData("google-account")] [InlineData("directory")] [InlineData("time-order")] [InlineData("time-offset")]
    [InlineData("duplicate-academy-provider")] [InlineData("cross-academy-account")] [InlineData("zoom-no-account")] [InlineData("ms-no-directory")] [InlineData("ms-invalid-subject")]
    [InlineData("too-many")]
    public void Malformed_ambiguous_or_shared_account_snapshot_is_fully_disabled(string mode)
    {
        var clock = new Clock(); var reg = Registration(); var input = Input(Guid.NewGuid(), clock, reg);
        input = mode switch { "academy" => input with { Academy = Guid.Empty }, "provider" => input with { Provider = "model-provider" }, "revision" => input with { Revision = 0 },
            "registration" => input with { Registration = "bad" }, "subject" => input with { Subject = "https://untrusted.invalid" }, "google-account" => input with { Account = "bad-added-account" },
            "directory" => input with { Directory = DirectoryId }, "time-order" => input with { ValidUntil = input.ValidFrom }, "time-offset" => input with { ValidFrom = input.ValidFrom.ToOffset(TimeSpan.FromHours(1)) },
            "zoom-no-account" => input with { Provider = "Zoom" }, "ms-no-directory" => input with { Provider = "Microsoft365", Subject = MicrosoftSubject },
            "ms-invalid-subject" => input with { Provider = "Microsoft365", Directory = DirectoryId }, _ => input };
        MeetingAccountPolicyInput[]? inputs = mode switch { "null" => null, "empty" => [], "duplicate-academy-provider" => [input, input with { Revision = 2, Subject = "another-subject" }],
            "cross-academy-account" => [input, input with { Academy = Guid.NewGuid() }], "too-many" => Enumerable.Repeat(input, 1025).ToArray(), _ => [input] };
        var registry = MeetingAccountPolicyRegistry.Create(inputs); Assert.False(registry.IsValid); Assert.Null(registry.Resolve(input.Academy, reg, clock.Now));
    }
    [Theory] [InlineData("revision")] [InlineData("registration")] [InlineData("subject")] [InlineData("expiry")]
    public void Policy_fingerprint_binds_revision_registration_account_and_lifetime(string mode)
    {
        var clock = new Clock(); var reg = Registration(); var input = Input(Guid.NewGuid(), clock, reg); var original = MeetingAccountPolicyRegistry.Create([input]).Resolve(input.Academy, reg, clock.Now)!;
        var next = mode switch { "revision" => input with { Revision = 2 }, "registration" => input with { Registration = Registration(revision: 2).Fingerprint }, "subject" => input with { Subject = "synthetic-new-subject" }, _ => input with { ValidUntil = input.ValidUntil.AddMinutes(1) } };
        Assert.NotEqual(original.Fingerprint, new MeetingAccountPolicy(next).Fingerprint);
    }
    [Theory] [InlineData("disabled")] [InlineData("policy-foreign")] [InlineData("policy-expired")] [InlineData("policy-future")]
    [InlineData("policy-registration")] [InlineData("policy-subject")] [InlineData("unconfirmed")] [InlineData("review-expired")] [InlineData("role")]
    [InlineData("stamp")] [InlineData("source-cipher")] [InlineData("review-cipher")] [InlineData("disconnected")] [InlineData("refreshed")]
    [InlineData("anonymous")] [InlineData("foreign")] [InlineData("missing")] [InlineData("other-actor")]
    public async Task Unqualified_or_stale_context_cannot_produce_a_policy_match_or_audit(string mode)
    {
        await using var f = await MeetingReadinessTests.Fixture.Create(); var clock = new Clock(); var reg = Registration(); var (store, permit, review) = await Setup(f, clock, reg, mode != "unconfirmed");
        var input = Input(f.Academy.Id, clock, reg);
        input = mode switch { "policy-foreign" => input with { Academy = Guid.NewGuid() }, "policy-expired" => input with { ValidFrom = clock.Now.AddHours(-1), ValidUntil = clock.Now }, "policy-future" => input with { ValidFrom = clock.Now.AddMinutes(1) },
            "policy-registration" => input with { Registration = Registration(revision: 2).Fingerprint }, "policy-subject" => input with { Subject = "synthetic-other-subject" }, _ => input };
        var registry = mode == "disabled" ? MeetingAccountPolicyRegistry.Disabled : MeetingAccountPolicyRegistry.Create([input]);
        if (mode == "review-expired") clock.Now = clock.Now.AddMinutes(5); if (mode == "role") await f.Users.RemoveFromRoleAsync(f.User, "Owner"); if (mode == "stamp") await f.Users.UpdateSecurityStampAsync(f.User);
        if (mode == "source-cipher") { (await f.Db.MeetingOAuthExchanges.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "review-cipher") { (await f.Db.MeetingAccountReviews.SingleAsync()).ProtectedEnvelope = "forged"; await f.Db.SaveChangesAsync(); }
        if (mode == "disconnected") await store.DisconnectLocallyAsync(f.Actor, f.Academy.Id, reg.Provider, 2, true);
        if (mode == "refreshed") Assert.Equal(201, (await store.BeginRefreshAsync(f.Actor, f.Academy.Id, reg, permit, 1)).StatusCode);
        var actor = mode == "anonymous" ? new ClaimsPrincipal() : mode == "other-actor" ? new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) }, "synthetic")) : f.Actor;
        var before = await f.Db.AuditLogs.CountAsync(); var result = await store.CheckAccountPolicyAsync(actor, mode == "foreign" ? Guid.NewGuid() : f.Academy.Id, reg, mode == "missing" ? Guid.NewGuid() : review.Id, registry);
        Assert.Contains(result.StatusCode, new[] { 401, 403, 404, 409, 410, 503 }); Assert.False(result.AccountPolicyMatched); Assert.Null(result.PolicyFingerprint); Assert.False(result.CanExecute); Assert.Equal(before, await f.Db.AuditLogs.CountAsync());
    }
    [Fact] public void Same_zoom_account_with_different_users_cannot_be_assigned_to_two_academies()
    {
        var clock = new Clock(); var reg = Registration("Zoom"); var input = Input(Guid.NewGuid(), clock, reg);
        Assert.False(MeetingAccountPolicyRegistry.Create([input, input with { Academy = Guid.NewGuid(), Subject = "synthetic-other-zoom-user" }]).IsValid);
    }
    [Fact] public void Directory_configuration_mismatch_is_not_treated_as_directory_proof()
    {
        var clock = new Clock(); var reg = Registration("Microsoft365"); var input = Input(Guid.NewGuid(), clock, reg) with { Directory = "019acc9a-6200-7000-8000-000000000003" };
        var registry = MeetingAccountPolicyRegistry.Create([input]); Assert.True(registry.IsValid); Assert.Null(registry.Resolve(input.Academy, reg, clock.Now));
    }
}
