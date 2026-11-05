using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingPreparationTests
{
    private static readonly Guid Request = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static JsonObject Decision() => JsonSerializer.SerializeToNode(new {
        request_ref = Request.ToString("D"), scope = new { tenant_id = "synthetic", actor_id = "synthetic", connection_id = "synthetic",
            conversation_id = "synthetic", authority_version = 1, context_version = 1 }, base_revision = 1, catalog_revision = 1,
        kind = "host_prepare", candidates = new[] { new { tool_key = PentaOnboardingDraftService.Tool,
            edit = new { mode = "replace", set = new { }, remove = Array.Empty<string>() } } },
        questions = Array.Empty<string>(), invalidates_review = true })!.AsObject();
    [Fact]
    public void Exact_host_preparation_shape_is_read_without_approving_or_executing()
    {
        Assert.True(PentaOnboardingPreparationInput.TryRead(JsonSerializer.SerializeToElement(Decision()), Request, out var input));
        Assert.Equal(1, input!.BaseRevision);
        using var document = JsonDocument.Parse(input.Payload);
        Assert.Empty(document.RootElement.GetProperty("fields").EnumerateObject());
    }
    [Theory]
    [InlineData("request")]
    [InlineData("approval")]
    [InlineData("kind")]
    [InlineData("revision")]
    [InlineData("catalog")]
    [InlineData("authority")]
    [InlineData("role")]
    [InlineData("tool")]
    [InlineData("refine")]
    [InlineData("questions")]
    [InlineData("invalidate")]
    [InlineData("credentials")]
    public void Unknown_authority_or_non_preparation_shapes_are_denied(string mutation)
    {
        var value = Decision();
        switch (mutation) {
            case "request": value["request_ref"] = Guid.NewGuid().ToString("D"); break;
            case "approval": value["approved"] = true; break;
            case "kind": value["kind"] = "execute"; break;
            case "revision": value["base_revision"] = 100; break;
            case "catalog": value["catalog_revision"] = 2; break;
            case "authority": value["scope"]!["authority_version"] = 2; break;
            case "role": value["scope"]!["role"] = "Owner"; break;
            case "tool": value["candidates"]![0]!["tool_key"] = "ExecuteSQL"; break;
            case "refine": value["candidates"]![0]!["edit"]!["mode"] = "refine"; break;
            case "questions": value["questions"] = new JsonArray("StudentFirstName"); break;
            case "invalidate": value["invalidates_review"] = false; break;
            case "credentials": value["candidates"]![0]!["edit"]!["set"]!["StudentTemporaryPassword"] = JsonSerializer.SerializeToNode(new { kind = "text", value = "synthetic-only" }); break;
        }
        Assert.False(PentaOnboardingPreparationInput.TryRead(JsonSerializer.SerializeToElement(value), Request, out _));
    }
    [Fact]
    public void Duplicate_top_level_decision_keys_are_not_last_value_wins()
    {
        var json = Decision().ToJsonString();
        using var document = JsonDocument.Parse(json[..^1] + ",\"base_revision\":2}");
        Assert.False(PentaOnboardingPreparationInput.TryRead(document.RootElement, Request, out _));
    }
    [Theory]
    [InlineData("2000-01-01", false)]
    [InlineData("2015-01-01", true)]
    public void Preparation_normalizes_domain_defaults_and_optional_blanks_before_review(string dob, bool minor)
    {
        var payload = JsonSerializer.Serialize(new { tool_key = PentaOnboardingDraftService.Tool, tool_revision = 1,
            fields = new Dictionary<string,object> { ["StudentFirstName"] = new { kind = "text", value = " Synthetic " },
                ["DateOfBirth"] = new { kind = "date", value = dob }, ["StudentEmail"] = new { kind = "text", value = "  " },
                ["AllowFinance"] = new { kind = "boolean", value = false } } });
        var canonical = PentaOnboardingFields.NormalizeForCore(payload, new DateOnly(2026,10,18));
        using var document = JsonDocument.Parse(canonical); var fields = document.RootElement.GetProperty("fields");
        Assert.Equal("Synthetic", fields.GetProperty("StudentFirstName").GetProperty("value").GetString());
        Assert.False(fields.TryGetProperty("StudentEmail", out _));
        Assert.Equal("2026-10-18", fields.GetProperty("AdmissionDate").GetProperty("value").GetString());
        Assert.False(fields.GetProperty("AllowFinance").GetProperty("value").GetBoolean());
        Assert.Equal(minor, fields.GetProperty("AllowParentPortalAccess").GetProperty("value").GetBoolean());
        Assert.Equal(minor, fields.TryGetProperty("Relationship", out _));
        Assert.Equal(canonical, PentaOnboardingFields.NormalizeForCore(canonical, new DateOnly(2026,10,19)));
    }
}
