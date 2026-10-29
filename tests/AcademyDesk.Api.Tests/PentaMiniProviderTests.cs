using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaMiniProviderTests
{
    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "QA";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class Transport(string body, HttpStatusCode status = HttpStatusCode.OK, string? runtime = "default", bool duplicate = false, bool changing = false) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Request { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Request = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
            var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
            if (runtime is not null)
                response.Headers.TryAddWithoutValidation(MiniRuntimeIdentity.HeaderName,
                    duplicate ? new[] { runtime, runtime } : new[] { changing ? Runtime.Replace("2.4.1", $"2.4.{Calls}") : runtime == "default" ? Runtime : runtime });
            return response;
        }
    }
    private static PentaMiniProvider Provider(Transport transport, string env = "Testing", string url = "http://127.0.0.1:8000/", bool enabled = true) =>
        new(new HttpClient(transport), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = enabled.ToString(), ["Penta:Mini:BaseUrl"] = url
        }).Build(), new Environment(env));
    private static MiniPlanRequest Request => new(Guid.NewGuid().ToString("D"), "Show students with pending fees.", MiniState.Empty, PentaMiniTools.Available.ToArray());
    private static string Response => JsonSerializer.Serialize(new MiniPlan("0.1", "TOOL_REQUEST", "Request prepared.",
        new("SearchLearners", new() { ["balance_status"] = JsonSerializer.SerializeToElement("Pending") }), "READ", MiniState.Empty));
    private static string Runtime => JsonSerializer.Serialize(new MiniRuntimeIdentity("2.4.1", "planner-9.4", "0.1",
        new('a', 64), new('b', 64), new('c', 64), new('d', 64), new('e', 64), new('f', 64)));

    [Fact]
    public async Task Wire_contract_is_minimal_and_accepts_real_tool_proposal()
    {
        var transport = new Transport(Response);
        var outcome = await Provider(transport).PlanAsync(Request, default);
        Assert.Equal("SearchLearners", outcome.Plan?.ToolCall?.Name);
        Assert.Equal("2.4.1", outcome.Runtime?.ServiceVersion);
        Assert.Equal("planner-9.4", outcome.Runtime?.PromptVersion);
        using var request = JsonDocument.Parse(transport.Request!);
        Assert.Equal(4, request.RootElement.EnumerateObject().Count());
        Assert.Equal(new[] { "SearchLearners", "GetLearner" }, request.RootElement.GetProperty("available_tools").EnumerateArray().Select(x => x.GetString()));
        Assert.False(request.RootElement.TryGetProperty("user_context", out _));
        Assert.False(request.RootElement.TryGetProperty("tenant_context", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task Missing_or_invalid_runtime_never_prepares_a_tool(string? identity)
    {
        var transport = new Transport(Response, runtime: identity);
        var outcome = await Provider(transport).PlanAsync(Request, default);
        Assert.Null(outcome.Plan); Assert.Null(outcome.Runtime);
        Assert.Equal("RuntimeIdentityFailed", outcome.Error);
        Assert.Equal(1, transport.Calls);
    }
    [Fact]
    public async Task Oversized_duplicate_unknown_mismatched_or_unsafe_runtime_is_rejected()
    {
        foreach (var runtime in new[] { new string('x', 1600), Runtime.Replace("2.4.1", "<private>"),
            Runtime.Replace("planner-9.4", "private prompt"), Runtime.Replace("\"0.1\"", "\"9.0\""),
            Runtime.Replace(new string('a',64), "short"), Runtime.Replace("{", "{\"extra\":\"private\","),
            Runtime.Replace("{", "{\"service_version\":\"1.0.0\","), Runtime.Replace("2.4.1", "") })
        {
            var outcome = await Provider(new Transport(Response, runtime: runtime)).PlanAsync(Request, default);
            Assert.Null(outcome.Plan); Assert.Null(outcome.Runtime);
            Assert.Equal("RuntimeIdentityFailed", outcome.Error);
        }
        Assert.Null((await Provider(new Transport(Response, runtime: Runtime, duplicate: true)).PlanAsync(Request, default)).Plan);
    }
    [Fact]
    public async Task Provenance_is_bound_to_each_response_not_cached_or_preflighted()
    {
        var transport = new Transport(Response, changing: true);
        var provider = Provider(transport);
        var first = await provider.PlanAsync(Request, default);
        var second = await provider.PlanAsync(Request, default);
        Assert.Equal("2.4.1", first.Runtime?.ServiceVersion);
        Assert.Equal("2.4.2", second.Runtime?.ServiceVersion);
        Assert.NotNull(first.Plan); Assert.NotNull(second.Plan);
        Assert.Equal(2, transport.Calls);
    }
    [Theory]
    [InlineData("Production", "http://127.0.0.1:8000/", true)]
    [InlineData("Staging", "http://127.0.0.1:8000/", true)]
    [InlineData("Testing", "https://example.com/", true)]
    [InlineData("Testing", "http://127.0.0.1:8000/redirect", true)]
    [InlineData("Testing", "http://127.0.0.1:8000/", false)]
    public async Task Gate_rejects_public_or_disabled_transport(string env, string url, bool enabled)
    {
        var transport = new Transport(Response);
        Assert.Null((await Provider(transport, env, url, enabled).PlanAsync(Request, default)).Plan);
        Assert.Equal(0, transport.Calls);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("not json")]
    [InlineData("{\"kind\":\"TOOL_REQUEST\",\"kind\":\"REFUSAL\"}")]
    [InlineData("{\"protocol_version\":\"9\"}")]
    public async Task Invalid_schema_is_safe_and_not_retried(string body)
    {
        var transport = new Transport(body);
        Assert.Equal("SchemaFailed", (await Provider(transport).PlanAsync(Request, default)).Error);
        Assert.Equal(1, transport.Calls);
    }
    [Fact]
    public void Invalid_tool_arguments_fail_strict_registry()
    {
        Assert.False(PentaMiniTools.TrySearch(new("SearchLearners", new() { ["tenant_id"] = JsonSerializer.SerializeToElement("other") }), out _));
        Assert.False(PentaMiniTools.TrySearch(new("SearchLearners", new() { ["sort_by"] = JsonSerializer.SerializeToElement("amount") }), out _));
        Assert.False(PentaMiniTools.TrySearch(new("SearchLearners", new() { ["name"] = JsonSerializer.SerializeToElement(10) }), out _));
        Assert.False(PentaMiniTools.ValidState(new(null, [Guid.NewGuid().ToString("D"), "invented"], [], null)));
    }
    [Theory]
    [InlineData("Meera")]
    [InlineData("AD-M001")]
    [InlineData("AD-M002")]
    public void Generic_search_accepts_names_and_record_codes_as_untrusted_filters(string hint)
    {
        Assert.True(PentaMiniTools.TrySearch(new("SearchLearners", new() { ["name"] = JsonSerializer.SerializeToElement(hint) }), out var state));
        Assert.Equal(hint, state.Filters["name"]);
        Assert.Empty(state.CurrentResultIds);
        Assert.Null(state.CurrentLearnerId);
        Assert.True(PentaMiniTools.ValidState(state));
    }
    [Fact]
    public async Task Oversized_and_http_failure_are_bounded()
    {
        Assert.Equal("SchemaFailed", (await Provider(new Transport(new string('x', 17000))).PlanAsync(Request, default)).Error);
        var transport = new Transport("private engine detail", HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Unavailable", (await Provider(transport).PlanAsync(Request, default)).Error);
        Assert.Equal(1, transport.Calls);
        Assert.Equal("Unavailable", await Provider(new Transport("{}", HttpStatusCode.ServiceUnavailable)).ReadinessAsync(default));
        Assert.Equal("Available", await Provider(new Transport("{\"inference_ready\":true}")).ReadinessAsync(default));
        Assert.Equal("Degraded", await Provider(new Transport("{}" )).ReadinessAsync(default));
    }
}
