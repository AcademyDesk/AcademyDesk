using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.DataProtection;

namespace AcademyDesk.Api.Tests;

public sealed class PentaEnrollmentLifecycleBrowserSessionTests
{
    private static PentaMiniSession Session() => new() { Id = Guid.NewGuid(), AcademyId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(),
        Version = 0, ExpiresAtUtc = new DateTime(2027, 1, 1, 10, 0, 0, DateTimeKind.Utc), ProtectedState = "retained Mini state" };
    private const string Stamp = "synthetic-current-identity-stamp";

    [Fact]
    public void Scoped_start_key_is_stable_and_does_not_reuse_raw_request_id()
    {
        var academy = Guid.NewGuid(); var actor = Guid.NewGuid(); var request = Guid.NewGuid();
        var key = PentaEnrollmentLifecycleBrowserSession.Key(academy, actor, request);
        Assert.Equal(key, PentaEnrollmentLifecycleBrowserSession.Key(academy, actor, request));
        Assert.NotEqual(Guid.Empty, key); Assert.NotEqual(request, key);
        Assert.NotEqual(key, PentaEnrollmentLifecycleBrowserSession.Key(Guid.NewGuid(), actor, request));
        Assert.NotEqual(key, PentaEnrollmentLifecycleBrowserSession.Key(academy, Guid.NewGuid(), request));
        Assert.NotEqual(key, PentaEnrollmentLifecycleBrowserSession.Key(academy, actor, Guid.NewGuid()));
    }
    [Theory][InlineData("academy")][InlineData("actor")][InlineData("request")]
    public void Empty_start_scope_is_refused(string field) => Assert.Throws<ArgumentException>(() =>
        PentaEnrollmentLifecycleBrowserSession.Key(field == "academy" ? Guid.Empty : Guid.NewGuid(),
            field == "actor" ? Guid.Empty : Guid.NewGuid(), field == "request" ? Guid.Empty : Guid.NewGuid()));

    [Fact]
    public void Real_protection_roundtrip_retains_separate_Mini_state()
    {
        var session = Session(); var binding = new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider());
        session.ProtectedOnboardingState = binding.Seal(session, Stamp);
        Assert.True(binding.Matches(session, Stamp)); Assert.Equal("retained Mini state", session.ProtectedState);
        Assert.DoesNotContain(Stamp, session.ProtectedOnboardingState);
        Assert.False(binding.Matches(session, "a different current stamp"));
        Assert.False(new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider()).Matches(session, Stamp));
    }
    [Theory][InlineData("id")][InlineData("academy")][InlineData("actor")][InlineData("version")][InlineData("expiry")]
    [InlineData("pending")][InlineData("cursor")][InlineData("empty-cursor")][InlineData("empty-id")][InlineData("empty-academy")][InlineData("empty-actor")]
    public void Scope_and_ciphertext_tampering_never_match(string field)
    {
        var session = Session(); var binding = new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider());
        session.ProtectedOnboardingState = binding.Seal(session, Stamp);
        switch (field)
        {
            case "id": session.Id = Guid.NewGuid(); break;
            case "academy": session.AcademyId = Guid.NewGuid(); break;
            case "actor": session.ActorUserId = Guid.NewGuid(); break;
            case "version": session.Version = 1; break;
            case "expiry": session.ExpiresAtUtc = session.ExpiresAtUtc.AddTicks(1); break;
            case "pending": session.PendingRequestId = Guid.NewGuid(); break;
            case "cursor": session.ProtectedOnboardingState += "corrupted"; break;
            case "empty-cursor": session.ProtectedOnboardingState = null; break;
            case "empty-id": session.Id = Guid.Empty; break;
            case "empty-academy": session.AcademyId = Guid.Empty; break;
            case "empty-actor": session.ActorUserId = Guid.Empty; break;
        }
        Assert.False(binding.Matches(session, Stamp));
    }
    [Theory][InlineData("academy.payment-collection.v1")][InlineData("academy.invoice-issuance.v1")][InlineData("academy.enrollment-create.v1")][InlineData("academy.enrollment-browser-session.v1")][InlineData("academy.onboarding.v1")]
    public void An_otherwise_identical_cursor_from_another_purpose_never_matches(string purpose)
    {
        var provider = new EphemeralDataProtectionProvider(); var session = Session();
        session.ProtectedOnboardingState = provider.CreateProtector(purpose, session.Id.ToString("D"), session.AcademyId.ToString("D"),
            session.ActorUserId.ToString("D"), FormattableString.Invariant($"{session.Version}:{session.ExpiresAtUtc.Ticks}")).Protect(Stamp);
        Assert.False(new PentaEnrollmentLifecycleBrowserSession(provider).Matches(session, Stamp));
    }
    [Fact]
    public void SQL_unspecified_kind_preserves_UTC_clock_ticks_in_binding()
    {
        var session = Session(); var binding = new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider());
        session.ProtectedOnboardingState = binding.Seal(session, Stamp);
        session.ExpiresAtUtc = DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Unspecified);
        Assert.True(binding.Matches(session, Stamp));
    }
    [Theory][InlineData("")][InlineData(null)]
    public void Missing_security_stamp_is_never_a_valid_binding(string? stamp)
    {
        var binding = new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider()); var session = Session();
        Assert.Throws<ArgumentException>(() => binding.Seal(session, stamp!)); Assert.False(binding.Matches(session, stamp!));
    }
    [Fact]
    public void Legacy_fixture_session_is_not_upgraded_or_modified_by_a_failed_read()
    {
        var session = Session(); var binding = new PentaEnrollmentLifecycleBrowserSession(new EphemeralDataProtectionProvider());
        Assert.False(binding.Matches(session, Stamp)); Assert.Null(session.ProtectedOnboardingState);
        Assert.Equal("retained Mini state", session.ProtectedState);
    }
    [Fact]
    public void Creation_and_lifecycle_sessions_and_start_keys_never_interchange()
    {
        var provider=new EphemeralDataProtectionProvider();var session=Session();var create=new PentaEnrollmentBrowserSession(provider);var lifecycle=new PentaEnrollmentLifecycleBrowserSession(provider);
        var request=Guid.NewGuid();Assert.NotEqual(PentaEnrollmentBrowserSession.Key(session.AcademyId,session.ActorUserId,request),PentaEnrollmentLifecycleBrowserSession.Key(session.AcademyId,session.ActorUserId,request));
        session.ProtectedOnboardingState=create.Seal(session,Stamp);Assert.True(create.Matches(session,Stamp));Assert.False(lifecycle.Matches(session,Stamp));
        session.ProtectedOnboardingState=lifecycle.Seal(session,Stamp);Assert.True(lifecycle.Matches(session,Stamp));Assert.False(create.Matches(session,Stamp));
    }
    [Theory][InlineData(null,false)][InlineData("",false)][InlineData(" ",false)][InlineData("M",false)]
    [InlineData("Mi\nra",false)][InlineData("Mi\u007fra",false)][InlineData("Mi\u0085ra",false)]
    [InlineData(" Mira ",true)][InlineData("राम",true)][InlineData("Piano",true)]
    public void Bounded_search_normalization_is_literal_and_control_free(string? input,bool expected)
    {
        Assert.Equal(expected,PentaEnrollmentCreationWorkflow.LifecycleSearchQuery(input,out var query));
        if(expected)Assert.Equal(input!.Trim(),query);
        Assert.False(PentaEnrollmentCreationWorkflow.LifecycleSearchQuery(new string('a',81),out _));
        Assert.True(PentaEnrollmentCreationWorkflow.LifecycleSearchQuery(new string('a',80),out _));
    }
    [Fact]
    public void Search_contract_contains_only_minimal_source_context()
    {
        var row=new PentaEnrollmentLifecycleRow(Guid.NewGuid(),Guid.NewGuid(),"Synthetic student",Guid.NewGuid(),"Historical batch","Paused",new DateOnly(2027,1,1),null,"/enrollments");
        using var json=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(row,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        Assert.Equal(new[]{"batchId","batchName","endDate","sourceId","sourcePath","startDate","status","studentId","studentName"},json.RootElement.EnumerateObject().Select(x=>x.Name).OrderBy(x=>x).ToArray());
    }

}
