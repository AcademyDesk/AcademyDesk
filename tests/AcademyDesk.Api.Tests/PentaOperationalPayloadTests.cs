using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOperationalPayloadTests
{
    private const string Empty = """{"tool_key":"PrepareProfile","tool_revision":1,"fields":{}}""";
    private static string Field(string value) =>
        "{\"tool_key\":\"PrepareProfile\",\"tool_revision\":1,\"fields\":{\"note\":" + value + "}}";

    public static IEnumerable<object[]> Vectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "penta-operational-payload-vectors.json")));
        foreach (var row in document.RootElement.EnumerateArray())
            yield return [row.GetProperty("id").GetString()!, row.GetProperty("payload").GetRawText(),
                row.GetProperty("canonical").GetString()!, row.GetProperty("sha256").GetString()!];
    }

    [Theory, MemberData(nameof(Vectors))]
    public void Matches_independent_Python_canonical_bytes_and_SHA256(string id, string payload, string canonical, string hash)
    {
        Assert.NotEmpty(id);
        Assert.Equal(canonical, PentaOperationalPayload.Canonicalize(payload));
        Assert.Equal(hash, PentaOperationalPayload.Digest(payload));
        Assert.Equal(hash, PentaOperationalPayload.Digest(canonical));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":0,\"fields\":{}}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1.0,\"fields\":{}}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":true,\"fields\":{}}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1,\"fields\":{},\"approved\":true}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1,\"fields\":{},\"fields\":{}}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1,\"fields\":{\"x\":{},\"x\":{}}}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1,\"fields\":null}")]
    [InlineData("{\"tool_key\":\"Read\",\"tool_revision\":1,\"fields\":{\"bad-key\":{\"kind\":\"text\",\"value\":\"x\"}}}")]
    [InlineData("{\"tool_key\":\"Read\\n\",\"tool_revision\":1,\"fields\":{}}")]
    public void Rejects_invalid_closed_envelope_without_echoing_input(string payload)
    {
        Assert.Equal("Invalid PENTA operational payload.", Assert.Throws<ArgumentException>(
            () => PentaOperationalPayload.Canonicalize(payload)).Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"kind\":\"text\",\"value\":null}")]
    [InlineData("{\"kind\":\"text\",\"value\":5}")]
    [InlineData("{\"kind\":\"text\",\"value\":\"x\",\"extra\":true}")]
    [InlineData("{\"kind\":\"text\",\"kind\":\"text\",\"value\":\"x\"}")]
    [InlineData("{\"kind\":\"sql\",\"value\":\"x\"}")]
    [InlineData("{\"kind\":\"integer\",\"value\":1.5}")]
    [InlineData("{\"kind\":\"integer\",\"value\":1e0}")]
    [InlineData("{\"kind\":\"integer\",\"value\":9007199254740992}")]
    [InlineData("{\"kind\":\"integer\",\"value\":-9007199254740992}")]
    [InlineData("{\"kind\":\"decimal\",\"value\":1.5}")]
    [InlineData("{\"kind\":\"decimal\",\"value\":\"01.50\"}")]
    [InlineData("{\"kind\":\"decimal\",\"value\":\"1.1234567\"}")]
    [InlineData("{\"kind\":\"decimal\",\"value\":\"NaN\"}")]
    [InlineData("{\"kind\":\"boolean\",\"value\":\"true\"}")]
    [InlineData("{\"kind\":\"date\",\"value\":\"2025-02-29\"}")]
    [InlineData("{\"kind\":\"date\",\"value\":\"2024-2-1\"}")]
    [InlineData("{\"kind\":\"reference\",\"value\":\"Display label\"}")]
    [InlineData("{\"kind\":\"reference\",\"value\":\"id\\n\"}")]
    [InlineData("{\"kind\":\"rows\",\"value\":[{\"x\":{\"kind\":\"rows\",\"value\":[]}}]}")]
    [InlineData("{\"kind\":\"text\",\"value\":\"\\ud800\"}")]
    public void Rejects_malformed_typed_values(string value) =>
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(Field(value)));

    [Fact]
    public void Limits_fields_rows_columns_text_and_total_input()
    {
        var root = JsonNode.Parse(Empty)!;
        for (var i = 0; i < 41; i++) root["fields"]!["f" + i] = new JsonObject { ["kind"] = "text", ["value"] = "x" };
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(root.ToJsonString()));
        var row = new JsonObject();
        for (var i = 0; i < 17; i++) row["f" + i] = new JsonObject { ["kind"] = "integer", ["value"] = 1 };
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(Field(
            new JsonObject { ["kind"] = "rows", ["value"] = new JsonArray(row) }.ToJsonString())));
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(Field(
            "{\"kind\":\"rows\",\"value\":[" + string.Join(',', Enumerable.Repeat("{}", 101)) + "]}")));
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(Field(
            JsonSerializer.Serialize(new { kind = "text", value = new string('x', 2001) }))));
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(new string(' ', PentaOperationalPayload.MaximumJsonCharacters + 1)));
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(null!));
    }

    [Fact]
    public void Unicode_scalar_limits_match_Python_without_normalizing_user_text()
    {
        var emoji = string.Concat(Enumerable.Repeat("😀", 2000));
        Assert.NotEmpty(PentaOperationalPayload.Digest(Field(JsonSerializer.Serialize(new { kind = "text", value = emoji }))));
        Assert.Throws<ArgumentException>(() => PentaOperationalPayload.Digest(Field(JsonSerializer.Serialize(new { kind = "text", value = emoji + "x" }))));
        Assert.NotEqual(PentaOperationalPayload.Digest(Field(JsonSerializer.Serialize(new { kind = "text", value = "é" }))),
            PentaOperationalPayload.Digest(Field(JsonSerializer.Serialize(new { kind = "text", value = "e\u0301" }))));
    }

    [Fact]
    public void Changes_to_tool_revision_fields_decimal_scale_and_row_order_change_binding()
    {
        Assert.NotEqual(PentaOperationalPayload.Digest(Empty), PentaOperationalPayload.Digest(Empty.Replace("PrepareProfile", "AnotherTool")));
        Assert.NotEqual(PentaOperationalPayload.Digest(Empty), PentaOperationalPayload.Digest(Empty.Replace(":1,", ":2,")));
        Assert.NotEqual(PentaOperationalPayload.Digest(Field("{\"kind\":\"decimal\",\"value\":\"1.0\"}")),
            PentaOperationalPayload.Digest(Field("{\"kind\":\"decimal\",\"value\":\"1.00\"}")));
        var rows = "{\"kind\":\"rows\",\"value\":[{\"id\":{\"kind\":\"integer\",\"value\":1}},{\"id\":{\"kind\":\"integer\",\"value\":2}}]}";
        var reversed = JsonNode.Parse(rows)!;
        var array = reversed["value"]!.AsArray();
        var first = array[0]!.DeepClone(); var second = array[1]!.DeepClone();
        array[0] = second; array[1] = first;
        Assert.NotEqual(PentaOperationalPayload.Digest(Field(rows)), PentaOperationalPayload.Digest(Field(reversed.ToJsonString())));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NOT-A-DIGEST")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    public void Missing_malformed_or_wrong_digest_never_matches(string? digest) =>
        Assert.False(PentaOperationalPayload.MatchesDigest(Empty, digest));

    [Fact]
    public void Exact_fingerprint_is_not_tool_permission_or_approval()
    {
        var hash = PentaOperationalPayload.Digest(Empty);
        Assert.True(PentaOperationalPayload.MatchesDigest(Empty, hash));
        Assert.False(PentaOperationalPayload.MatchesDigest(Empty.Replace("PrepareProfile", "ChangedTool"), hash));
        Assert.False(PentaOperationalPayload.MatchesDigest("null", hash));
        // The adapter does not register operations or relax the active READ-only protocol.
        Assert.Equal(new[] { "SearchLearners", "GetLearner" }, PentaMiniTools.Available);
        Assert.False(PentaMiniTools.ValidPlan(new("0.1", "TOOL_REQUEST", "Synthetic",
            new("PrepareProfile", []), "WRITE", MiniState.Empty)));
    }
}
