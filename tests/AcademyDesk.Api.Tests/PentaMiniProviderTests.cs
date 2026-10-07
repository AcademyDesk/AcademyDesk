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
    private sealed class Transport(string body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? Request { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Request = request.Content is null ? null : await request.Content.ReadAsStringAsync(token);
            return new(status) { Content = new StringContent(body) };
        }
    }
    private static PentaMiniProvider Provider(Transport transport, string env = "Testing", string url = "http://127.0.0.1:8000/", bool enabled = true) =>
        new(new HttpClient(transport), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = enabled.ToString(), ["Penta:Mini:BaseUrl"] = url
        }).Build(), new Environment(env));
    private static MiniPlanRequest Request => new(Guid.NewGuid().ToString("D"), "Show students with pending fees.", MiniState.Empty, PentaMiniTools.Available.ToArray());
    private static string Response => JsonSerializer.Serialize(new MiniPlan("0.1", "TOOL_REQUEST", "Request prepared.",
        new("SearchLearners", new() { ["balance_status"] = JsonSerializer.SerializeToElement("Pending") }), "READ", MiniState.Empty));

    [Fact]
    public async Task Wire_contract_is_minimal_and_accepts_real_tool_proposal()
    {
        var transport = new Transport(Response);
        var outcome = await Provider(transport).PlanAsync(Request, default);
        Assert.Equal("SearchLearners", outcome.Plan?.ToolCall?.Name);
        using var request = JsonDocument.Parse(transport.Request!);
        Assert.Equal(4, request.RootElement.EnumerateObject().Count());
        Assert.Equal(new[] { "SearchLearners", "GetLearner" }, request.RootElement.GetProperty("available_tools").EnumerateArray().Select(x => x.GetString()));
        Assert.False(request.RootElement.TryGetProperty("user_context", out _));
        Assert.False(request.RootElement.TryGetProperty("tenant_context", out _));
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
