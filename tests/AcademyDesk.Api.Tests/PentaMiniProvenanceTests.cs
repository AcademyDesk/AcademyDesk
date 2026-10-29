using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text.Json;

namespace AcademyDesk.Api.Tests;

public sealed class PentaMiniProvenanceTests
{
    [Fact]
    public void Missing_runtime_cannot_fabricate_a_planner_version()
    {
        var ev = PentaMiniLearningEvent.ForOutcome(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            "RESULT", "SearchLearners", null)!;
        Assert.Equal("unverified", ev.PromptVersion);
        Assert.Equal("unverified", ev.ModelRevision);
        Assert.False(ev.TrainingEligible);
    }
    [Fact]
    public void Reported_runtime_sets_planner_but_never_attests_weights_or_adds_authority()
    {
        var runtime = new MiniRuntimeIdentity("2.4.1", "planner-9.4", "0.1",
            new('a',64), new('b',64), new('c',64), new('d',64), new('e',64), new('f',64));
        var ev = PentaMiniLearningEvent.ForOutcome(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
            "RESULT", "SearchLearners", null, runtime)!;
        Assert.Equal("planner-9.4", ev.PromptVersion);
        Assert.Equal("unverified", ev.ModelRevision);
        Assert.False(ev.TrainingEligible);
        Assert.Empty(ev.OriginalArguments);
        Assert.False(PentaMiniTools.ValidPlan(new("0.1", "TOOL_REQUEST", "Synthetic",
            new("DeleteLearner", []), "WRITE", MiniState.Empty)));
    }

    [Theory]
    [InlineData("Created", false)]
    [InlineData("Claimed", false)]
    [InlineData("Replayed", false)]
    [InlineData("ERROR", false)]
    [InlineData("RESULT", true)]
    [InlineData("DeniedAfterPlanning", true)]
    [InlineData("ERROR", true)]
    public void Actual_audit_serialization_keeps_runtime_and_expected_weights_distinct(string kind, bool observed)
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        // Exercise metadata production only, no provider/authority/SQL behavior.
        var sut = new PentaMiniOrchestrator(db, null!, null!, null!, null!, null!, null!, null!,
            new ConfigurationBuilder().Build(), TimeProvider.System);
        var runtime = observed ? new MiniRuntimeIdentity("2.4.1", "planner-9.4", "0.1",
            new('a',64), new('b',64), new('c',64), new('d',64), new('e',64), new('f',64)) : null;
        typeof(PentaMiniOrchestrator).GetMethod("Audit", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(sut,
            [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), kind, "SearchLearners", 12L,
             kind == "ERROR" ? "RuntimeIdentityFailed" : null, "executor", null, runtime]);
        var record = db.AuditLogs.Local.Single();
        using var doc = JsonDocument.Parse(record.MetadataJson!);
        var data = doc.RootElement;
        Assert.Equal(observed ? "ResponseReported" : "NotObserved", data.GetProperty("runtimeProvenanceStatus").GetString());
        Assert.Equal(observed ? "2.4.1" : null, data.GetProperty("serviceVersion").GetString());
        Assert.Equal(observed ? "planner-9.4" : null, data.GetProperty("promptVersion").GetString());
        Assert.False(data.GetProperty("runtimeModelAttested").GetBoolean());
        Assert.Equal(PentaMiniLearningEvent.ExpectedModelRevision, data.GetProperty("expectedModelRevision").GetString());
        Assert.Empty(data.GetProperty("argumentKeys").EnumerateArray());
        if (observed)
            Assert.Equal(runtime!.ProviderSha256, data.GetProperty("runtimeProvenance").GetProperty("provider_sha256").GetString());
        else Assert.Equal(JsonValueKind.Null, data.GetProperty("runtimeProvenance").ValueKind);
        if (kind is "RESULT" or "ERROR" or "DeniedAfterPlanning")
        {
            var ev = data.GetProperty("learningEvent");
            Assert.Equal("unverified", ev.GetProperty("model_revision").GetString());
            Assert.Equal(observed ? "planner-9.4" : "unverified", ev.GetProperty("prompt_version").GetString());
            Assert.False(ev.GetProperty("training_eligible").GetBoolean());
        }
        Assert.DoesNotContain("planner-0.1", record.MetadataJson);
        Assert.DoesNotContain("user_message", record.MetadataJson);
    }
}
