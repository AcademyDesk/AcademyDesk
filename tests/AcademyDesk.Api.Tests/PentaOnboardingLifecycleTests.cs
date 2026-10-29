using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingLifecycleTests
{
    private static readonly Guid Request = Guid.Parse("33333333-2222-3333-4444-555555555555");
    private static JsonObject Decision(string kind = "request_pause") => JsonSerializer.SerializeToNode(new {
        request_ref = Request.ToString("D"), scope = new { tenant_id = "synthetic", actor_id = "synthetic", connection_id = "synthetic",
            conversation_id = "synthetic", authority_version = 1, context_version = 1 }, base_revision = 1, catalog_revision = 1,
        kind, candidates = Array.Empty<object>(), questions = Array.Empty<string>(), invalidates_review = true })!.AsObject();

    [Theory]
    [InlineData("request_pause")]
    [InlineData("request_resume")]
    [InlineData("request_cancel")]
    public void Only_exact_non_mutating_lifecycle_decisions_are_parsed(string kind)
    {
        Assert.True(PentaOnboardingLifecycleInput.TryRead(JsonSerializer.SerializeToElement(Decision(kind)), Request, out var result));
        Assert.Equal(kind, result!.Kind);
        Assert.Equal(1, result.BaseRevision);
    }

    [Theory]
    [InlineData("request")]
    [InlineData("approval")]
    [InlineData("execute")]
    [InlineData("status")]
    [InlineData("revision")]
    [InlineData("catalog")]
    [InlineData("authority")]
    [InlineData("role")]
    [InlineData("candidate")]
    [InlineData("questions")]
    [InlineData("invalidate")]
    [InlineData("fraction")]
    [InlineData("scope-array")]
    [InlineData("oversize")]
    public void Untrusted_or_ambiguous_transition_shapes_are_denied(string mutation)
    {
        var value = Decision();
        switch (mutation) {
            case "request": value["request_ref"] = Guid.NewGuid().ToString("D"); break;
            case "approval": value["approved"] = true; break;
            case "execute": value["kind"] = "execute"; break;
            case "status": value["kind"] = "request_status"; break;
            case "revision": value["base_revision"] = 100; break;
            case "catalog": value["catalog_revision"] = 2; break;
            case "authority": value["scope"]!["authority_version"] = 2; break;
            case "role": value["scope"]!["role"] = "Owner"; break;
            case "candidate": value["candidates"] = new JsonArray(new JsonObject()); break;
            case "questions": value["questions"] = new JsonArray("StudentFirstName"); break;
            case "invalidate": value["invalidates_review"] = false; break;
            case "fraction": value["base_revision"] = 1.5; break;
            case "scope-array": value["scope"] = new JsonArray(); break;
            case "oversize": value["request_ref"] = new string('a', 3000); break;
        }
        Assert.False(PentaOnboardingLifecycleInput.TryRead(JsonSerializer.SerializeToElement(value), Request, out _));
    }

    [Fact]
    public void Duplicate_keys_and_empty_nonce_are_not_accepted()
    {
        var json = Decision().ToJsonString();
        using var document = JsonDocument.Parse(json[..^1] + ",\"base_revision\":2}");
        Assert.False(PentaOnboardingLifecycleInput.TryRead(document.RootElement, Request, out _));
        Assert.False(PentaOnboardingLifecycleInput.TryRead(JsonSerializer.SerializeToElement(Decision()), Guid.Empty, out _));
    }
}
