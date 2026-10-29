using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOperationalBindingsTests
{
    private static JsonNode Fixture(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory, "Fixtures", "penta-" + name + ".json")))!;
    private static readonly PentaOperationalScope Scope = new("synthetic-academy", "synthetic-admin", "fixture", "fixture-conversation", 1, 1);
    private static PentaOperationalExpectation Expected => new(Scope, "fixture-operation", "fixture-prepare",
        "PrepareStudentOnboarding", 1, "fixture-draft", 3, "cd0392819903dc700bb9ba45b2bc035e7cc9f2d048370447dd5969d6cbb4cf01");
    private static DateTimeOffset Now => DateTimeOffset.Parse("2026-10-16T09:00:00Z");

    [Fact]
    public void Actual_Python_preparation_and_receipt_examples_bind_without_granting_approval()
    {
        var operation = PentaOperationalBindings.ReadPreparation(Fixture("prepared-operation").ToJsonString(), Expected);
        Assert.Equal("awaiting_approval", operation.Status);
        Assert.False(PentaOperationalBindings.HasCurrentApprovalClaim(operation, Now));
        Assert.Equal(Expected.PayloadHash, PentaOperationalPayload.Digest(operation.CanonicalPayload));
        var receipt = PentaOperationalBindings.ReadReceipt(Fixture("committed-receipt").ToJsonString(), Expected);
        Assert.Equal("succeeded", receipt.Status);
        Assert.Equal("fixture-audit", receipt.AuditRef);
        Assert.Equal(new[] { "fixture-student" }, receipt.SourceRefs);
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("actor_id")]
    [InlineData("connection_id")]
    [InlineData("conversation_id")]
    [InlineData("authority_version")]
    [InlineData("context_version")]
    public void Wrong_or_stale_scope_rejected_on_preparation_and_receipt(string key)
    {
        foreach (var name in new[] { "prepared-operation", "committed-receipt" })
        {
            var value = Fixture(name);
            value["scope"]![key] = key.EndsWith("version") ? JsonValue.Create(2) : JsonValue.Create("other");
            Assert.Throws<ArgumentException>(() => name == "prepared-operation"
                ? (object)PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected)
                : PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected));
        }
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("request_ref")]
    [InlineData("tool_key")]
    [InlineData("draft_ref")]
    [InlineData("payload_hash")]
    [InlineData("tool_revision")]
    [InlineData("draft_revision")]
    public void Foreign_or_stale_preparation_rejected(string key)
    {
        var value = Fixture("prepared-operation");
        value[key] = key.EndsWith("revision") ? JsonValue.Create(9) : JsonValue.Create("other");
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected));
    }

    [Theory]
    [InlineData("operation_ref")]
    [InlineData("payload_hash")]
    [InlineData("draft_revision")]
    public void Foreign_or_stale_receipt_rejected(string key)
    {
        var value = Fixture("committed-receipt");
        value[key] = key.EndsWith("revision") ? JsonValue.Create(9) : JsonValue.Create("other");
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected));
    }

    [Fact]
    public void Canonical_payload_tampering_rejected_even_with_unchanged_hash()
    {
        var value = Fixture("prepared-operation");
        value["canonical_fields"]!["firstName"]!["value"] = "Changed";
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected));
    }

    [Theory]
    [InlineData("receipt_ref")]
    [InlineData("audit_ref")]
    public void Success_requires_non_null_receipt_and_audit_claims(string key)
    {
        var value = Fixture("committed-receipt"); value[key] = null;
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected));
    }

    [Theory]
    [InlineData("denied")]
    [InlineData("unknown")]
    [InlineData("arbitrary")]
    public void Success_with_error_is_not_accepted(string code)
    {
        var value = Fixture("committed-receipt"); value["error_code"] = code;
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected));
    }

    [Theory]
    [InlineData("prepared")]
    [InlineData("awaiting_approval")]
    [InlineData("approved")]
    [InlineData("executing")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    [InlineData("unknown")]
    public void Non_success_status_is_preserved_not_rewritten_to_success(string state)
    {
        var value = Fixture("committed-receipt"); value["status"] = state;
        value["receipt_ref"] = null; value["audit_ref"] = null;
        value["error_code"] = state == "unknown" ? "unknown" : "none";
        var parsed = PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected);
        Assert.Equal(state, parsed.Status); Assert.Null(parsed.AuditRef);
    }

    [Fact]
    public void Unknown_outcome_must_be_explicit()
    {
        var value = Fixture("committed-receipt"); value["status"] = "unknown";
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadReceipt(value.ToJsonString(), Expected));
    }

    [Theory]
    [InlineData("2026-10-16T10:00:00")]
    [InlineData("not-a-date")]
    public void Approval_expiry_needs_explicit_timezone(string date)
    {
        var value = Fixture("prepared-operation"); value["approval_expires_at"] = date;
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected));
    }

    [Fact]
    public void Current_claim_is_not_durable_approval_and_expired_claim_cannot_be_current()
    {
        var value = Fixture("prepared-operation"); value["status"] = "approved";
        var operation = PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected);
        Assert.True(PentaOperationalBindings.HasCurrentApprovalClaim(operation, Now));
        Assert.False(PentaOperationalBindings.HasCurrentApprovalClaim(operation, operation.ApprovalExpiresAt!.Value));
        Assert.False(PentaOperationalBindings.HasCurrentApprovalClaim(operation, Now.AddDays(1)));
        value["approval_ref"] = null;
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected));
    }

    [Fact]
    public void Duplicate_unknown_missing_fields_and_oversized_documents_fail_closed()
    {
        foreach (var name in new[] { "prepared-operation", "committed-receipt" })
        {
            var value = Fixture(name); value["override"] = true;
            foreach (var json in new[] { value.ToJsonString(), "{}", "null", "{\"status\":\"prepared\",\"status\":\"approved\"}", new string(' ', 262145) })
                Assert.Throws<ArgumentException>(() => name == "prepared-operation"
                    ? (object)PentaOperationalBindings.ReadPreparation(json, Expected)
                    : PentaOperationalBindings.ReadReceipt(json, Expected));
        }
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("password")]
    [InlineData("academyId")]
    [InlineData("approval")]
    public void Protected_fields_are_rejected_even_if_fingerprint_is_self_consistent(string field)
    {
        var value = Fixture("prepared-operation");
        value["canonical_fields"]![field] = new JsonObject { ["kind"] = "text", ["value"] = "synthetic" };
        var payload = new JsonObject { ["tool_key"] = Expected.ToolKey, ["tool_revision"] = 1, ["fields"] = value["canonical_fields"]!.DeepClone() };
        var hash = PentaOperationalPayload.Digest(payload.ToJsonString()); value["payload_hash"] = hash;
        Assert.Throws<ArgumentException>(() => PentaOperationalBindings.ReadPreparation(value.ToJsonString(), Expected with { PayloadHash = hash }));
    }
}
