using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingOperationTests
{
    private static JsonObject Input(bool approval) {
        var value = JsonSerializer.SerializeToNode(new { operation_ref = "onboarding:synthetic:2", revision = 2, payload_hash = new string('a',64) })!.AsObject();
        if (approval) value["approval_ref"] = Guid.NewGuid().ToString("D");
        return value;
    }
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Exact_operation_binding_is_parsed_without_granting_authority(bool approval)
    {
        Assert.True(PentaOnboardingOperationBinding.TryRead(JsonSerializer.SerializeToElement(Input(approval)),approval,out var result));
        Assert.Equal(2,result!.Revision);
        Assert.Equal(approval,result.ApprovalId is not null);
    }
    [Theory]
    [InlineData("unknown")]
    [InlineData("role")]
    [InlineData("revision")]
    [InlineData("fraction")]
    [InlineData("hash")]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("shape")]
    [InlineData("null")]
    [InlineData("reference")]
    [InlineData("size")]
    public void Malformed_or_forged_confirmation_fields_fail_closed(string change)
    {
        var value = Input(true);
        switch (change) {
            case "unknown": value["approved"] = true; break;
            case "role": value["role"] = "Owner"; break;
            case "revision": value["revision"] = 101; break;
            case "fraction": value["revision"] = 1.5; break;
            case "hash": value["payload_hash"] = new string('A',64); break;
            case "missing": value.Remove("approval_ref"); break;
            case "empty": value["approval_ref"] = Guid.Empty.ToString("D"); break;
            case "shape": value["approval_ref"] = Guid.NewGuid().ToString("N"); break;
            case "null": value["approval_ref"] = null; break;
            case "reference": value["operation_ref"] = "https://outside.invalid/execute"; break;
            case "size": value["operation_ref"] = new string('a',1100); break;
        }
        Assert.False(PentaOnboardingOperationBinding.TryRead(JsonSerializer.SerializeToElement(value),true,out _));
    }
    [Fact]
    public void Duplicate_keys_and_approval_on_read_challenge_are_rejected()
    {
        var json = Input(true).ToJsonString();
        using var document = JsonDocument.Parse(json[..^1]+",\"revision\":2}");
        Assert.False(PentaOnboardingOperationBinding.TryRead(document.RootElement,true,out _));
        Assert.False(PentaOnboardingOperationBinding.TryRead(JsonSerializer.SerializeToElement(Input(true)),false,out _));
    }
}
