using Microsoft.Extensions.Configuration;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.DataProtection;

namespace AcademyDesk.Api.Tests;

public sealed class PentaClassSessionBrowserSessionTests
{
    private static PentaMiniSession Session() => new() { Id = Guid.NewGuid(), AcademyId = Guid.NewGuid(), ActorUserId = Guid.NewGuid(),
        Version = 0, ExpiresAtUtc = new DateTime(2027, 1, 1, 10, 0, 0, DateTimeKind.Utc), ProtectedState = "retained Mini state" };
    private const string Stamp = "synthetic-current-identity-stamp";

    [Fact]
    public void Scoped_start_key_is_stable_and_does_not_reuse_raw_request_id()
    {
        var academy = Guid.NewGuid(); var actor = Guid.NewGuid(); var request = Guid.NewGuid();
        var key = PentaClassSessionBrowserSession.Key(academy, actor, request);
        Assert.Equal(key, PentaClassSessionBrowserSession.Key(academy, actor, request));
        Assert.NotEqual(Guid.Empty, key); Assert.NotEqual(request, key);
        Assert.NotEqual(key, PentaClassSessionBrowserSession.Key(Guid.NewGuid(), actor, request));
        Assert.NotEqual(key, PentaClassSessionBrowserSession.Key(academy, Guid.NewGuid(), request));
        Assert.NotEqual(key, PentaClassSessionBrowserSession.Key(academy, actor, Guid.NewGuid()));
    }
    [Theory][InlineData("academy")][InlineData("actor")][InlineData("request")]
    public void Empty_start_scope_is_refused(string field) => Assert.Throws<ArgumentException>(() =>
        PentaClassSessionBrowserSession.Key(field == "academy" ? Guid.Empty : Guid.NewGuid(),
            field == "actor" ? Guid.Empty : Guid.NewGuid(), field == "request" ? Guid.Empty : Guid.NewGuid()));

    [Fact]
    public void Real_protection_roundtrip_retains_separate_Mini_state()
    {
        var session = Session(); var binding = new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider());
        session.ProtectedOnboardingState = binding.Seal(session, Stamp);
        Assert.True(binding.Matches(session, Stamp)); Assert.Equal("retained Mini state", session.ProtectedState);
        Assert.DoesNotContain(Stamp, session.ProtectedOnboardingState);
        Assert.False(binding.Matches(session, "a different current stamp"));
        Assert.False(new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider()).Matches(session, Stamp));
    }
    [Theory][InlineData("id")][InlineData("academy")][InlineData("actor")][InlineData("version")][InlineData("expiry")]
    [InlineData("pending")][InlineData("cursor")][InlineData("empty-cursor")][InlineData("empty-id")][InlineData("empty-academy")][InlineData("empty-actor")]
    public void Scope_and_ciphertext_tampering_never_match(string field)
    {
        var session = Session(); var binding = new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider());
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
        Assert.False(new PentaClassSessionBrowserSession(provider).Matches(session, Stamp));
    }
    [Fact]
    public void SQL_unspecified_kind_preserves_UTC_clock_ticks_in_binding()
    {
        var session = Session(); var binding = new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider());
        session.ProtectedOnboardingState = binding.Seal(session, Stamp);
        session.ExpiresAtUtc = DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Unspecified);
        Assert.True(binding.Matches(session, Stamp));
    }
    [Theory][InlineData("")][InlineData(null)]
    public void Missing_security_stamp_is_never_a_valid_binding(string? stamp)
    {
        var binding = new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider()); var session = Session();
        Assert.Throws<ArgumentException>(() => binding.Seal(session, stamp!)); Assert.False(binding.Matches(session, stamp!));
    }
    [Fact]
    public void Legacy_fixture_session_is_not_upgraded_or_modified_by_a_failed_read()
    {
        var session = Session(); var binding = new PentaClassSessionBrowserSession(new EphemeralDataProtectionProvider());
        Assert.False(binding.Matches(session, Stamp)); Assert.Null(session.ProtectedOnboardingState);
        Assert.Equal("retained Mini state", session.ProtectedState);
    }
    [Fact]
    public void Enrollment_creation_and_lifecycle_purposes_and_keys_never_interchange()
    {
        var provider=new EphemeralDataProtectionProvider();var session=Session();var binding=new PentaClassSessionBrowserSession(provider);
        var creation=new PentaEnrollmentBrowserSession(provider);var lifecycle=new PentaEnrollmentLifecycleBrowserSession(provider);var request=Guid.NewGuid();
        Assert.NotEqual(PentaClassSessionBrowserSession.Key(session.AcademyId,session.ActorUserId,request),PentaEnrollmentBrowserSession.Key(session.AcademyId,session.ActorUserId,request));
        Assert.NotEqual(PentaClassSessionBrowserSession.Key(session.AcademyId,session.ActorUserId,request),PentaEnrollmentLifecycleBrowserSession.Key(session.AcademyId,session.ActorUserId,request));
        session.ProtectedOnboardingState=creation.Seal(session,Stamp);Assert.False(binding.Matches(session,Stamp));
        session.ProtectedOnboardingState=lifecycle.Seal(session,Stamp);Assert.False(binding.Matches(session,Stamp));
        session.ProtectedOnboardingState=binding.Seal(session,Stamp);Assert.False(creation.Matches(session,Stamp));Assert.False(lifecycle.Matches(session,Stamp));
    }
    [Fact]
    public void Browser_response_marks_SQL_clock_ticks_UTC_without_changing_storage_or_hash()
    {
        var t=new DateTime(2027,1,1,10,1,2,DateTimeKind.Unspecified);
        var plan=new AcademyDesk.Api.Controllers.ClassSessionSummary(Guid.NewGuid(),Guid.NewGuid(),null,null,t,t.AddHours(1),"InPerson","Synthetic studio","Scheduled");
        var receipt=new PentaClassSessionReceipt(Guid.NewGuid(),Guid.NewGuid(),plan.Id,t);
        var d=new PentaClassSessionDraftView(Guid.NewGuid(),1,0,"Succeeded",t,plan,new string('a',64),Guid.NewGuid(),receipt,plan,true);
        var outcome=PentaClassSessionSchedulingWorkflow.BrowserView(new(200,d));var v=outcome.Draft!;
        Assert.Equal(DateTimeKind.Utc,v.ExpiresAtUtc.Kind);Assert.Equal(t.Ticks,v.ExpiresAtUtc.Ticks);
        Assert.Equal(DateTimeKind.Utc,v.Plan.StartUtc.Kind);Assert.Equal(plan.StartUtc.Ticks,v.Plan.StartUtc.Ticks);Assert.Equal(DateTimeKind.Utc,v.Plan.EndUtc.Kind);Assert.Equal(plan.EndUtc.Ticks,v.Plan.EndUtc.Ticks);
        Assert.Equal(DateTimeKind.Utc,v.Source!.StartUtc.Kind);Assert.Equal(DateTimeKind.Utc,v.Source.EndUtc.Kind);Assert.Equal(DateTimeKind.Utc,v.Receipt!.CompletedAtUtc.Kind);
        Assert.Equal(d.PayloadHash,v.PayloadHash);Assert.Equal(d.ApprovalId,v.ApprovalId);Assert.Equal(d.FollowsBatchTeacher,v.FollowsBatchTeacher);
        Assert.Equal(DateTimeKind.Unspecified,d.ExpiresAtUtc.Kind);Assert.Equal(DateTimeKind.Unspecified,d.Plan.StartUtc.Kind);Assert.Equal(DateTimeKind.Unspecified,d.Receipt!.CompletedAtUtc.Kind);
        var json=System.Text.Json.JsonSerializer.Serialize(v,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));Assert.Contains("2027-01-01T10:01:02Z",json);
        Assert.Null(PentaClassSessionSchedulingWorkflow.BrowserView(new(404)).Draft);
        Assert.Null(PentaClassSessionSchedulingWorkflow.BrowserView(new(200,d with{Receipt=null,Source=null})).Draft!.Source);
    }
    [Theory][InlineData(null,false)][InlineData("",false)][InlineData("x",false)][InlineData("mu\nsic",false)][InlineData("mu\u0085sic",false)][InlineData("  संगीत %_  ",true)]
    public void Search_normalization_is_bounded_literal_and_control_free(string? input,bool valid)
    {Assert.Equal(valid,PentaClassSessionSchedulingWorkflow.BrowserQuery(input,out var q));if(valid)Assert.Equal(input!.Trim(),q);}
    [Fact]
    public void Lookup_rows_have_exact_minimal_JSON_schema_and_no_private_locations_or_contacts()
    {
        var options=new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web);
        using var batch=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new PentaClassBrowserBatch(Guid.NewGuid(),"Music",null,null,"/sessions"),options));
        Assert.Equal(new[]{"sourceId","name","teacherId","branchId","sourcePath"},batch.RootElement.EnumerateObject().Select(p=>p.Name));
        using var source=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(new PentaClassBrowserSource(Guid.NewGuid(),Guid.NewGuid(),"Music",DateTime.UtcNow,DateTime.UtcNow.AddHours(1),"Scheduled","/sessions"),options));
        Assert.Equal(new[]{"sourceId","batchId","batchName","startUtc","endUtc","status","sourcePath"},source.RootElement.EnumerateObject().Select(p=>p.Name));
    }
    [Theory][InlineData("Development")][InlineData("Production")]
    public async Task Non_Testing_environment_never_uses_a_dependency_or_opens_a_store(string environment)
    {
        var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Penta:Enabled"]="true",["Penta:ClassSessionSchedulingEnabled"]="true",["Penta:ClassSessionReschedulingEnabled"]="true",["Penta:ClassSchedulingBrowserContractsEnabled"]="true"}).Build();
        var host=new TestEnvironment{EnvironmentName=environment};var workflow=new PentaClassSessionSchedulingWorkflow(null!,null!,null!,null!,null!,null!,config,host,TimeProvider.System);
        Assert.False(workflow.BrowserEnabled);Assert.Equal(404,(await workflow.BrowserReadinessAsync(new(),Guid.NewGuid(),default)).HttpStatus);Assert.Equal(404,(await workflow.BrowserStartAsync(new(),Guid.NewGuid(),Guid.NewGuid(),true,default)).HttpStatus);
    }
    private sealed class TestEnvironment:Microsoft.Extensions.Hosting.IHostEnvironment
    {
        public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Synthetic class browser availability";public string ContentRootPath{get;set;}=Path.GetTempPath();public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider{get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
