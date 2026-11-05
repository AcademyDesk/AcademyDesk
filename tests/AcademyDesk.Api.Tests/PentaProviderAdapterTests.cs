using System.Net;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaProviderAdapterTests
{
    private sealed class LocalEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "PentaProviderAdapterTests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            return reply(request, token);
        }
    }

    private static IConfiguration Settings(bool enabled = true, bool approved = true,
        bool pilot = true, string? model = "synthetic-model", string? key = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = pilot.ToString(),
            ["Penta:Provider:Enabled"] = enabled.ToString(),
            ["Penta:Provider:SyntheticProbeApproved"] = approved.ToString(),
            ["Penta:Provider:Model"] = model,
            ["Penta:Provider:ApiKey"] = key ?? new string('x', 24)
        }).Build();

    private static PentaOpenAiProbeAdapter Adapter(FakeHandler handler, IConfiguration config,
        string environment = "Testing") => new(new HttpClient(handler), config,
        new LocalEnvironment(environment));

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private const string GoodResponse = """
        {"status":"completed","output":[{"type":"message","role":"assistant","content":[{"type":"output_text","text":"{\"status\":\"ok\"}"}]}],"usage":{"input_tokens":33,"output_tokens":8}}
        """;

    [Theory]
    [InlineData("Production", true, true, true)]
    [InlineData("Testing", false, true, true)]
    [InlineData("Testing", true, false, true)]
    [InlineData("Testing", true, true, false)]
    public async Task Independent_gates_disable_transport(string environment,
        bool enabled, bool approved, bool pilot)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(GoodResponse)));
        var adapter = Adapter(handler, Settings(enabled, approved, pilot), environment);
        Assert.Equal("Disabled", (await adapter.ProbeAsync(CancellationToken.None)).Status);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(null, "synthetic-model")]
    [InlineData(" ", "synthetic-model")]
    [InlineData("placeholder", null)]
    [InlineData("placeholder", "bad model")]
    public async Task Missing_or_invalid_configuration_never_sends(string? key, string? model)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(GoodResponse)));
        var configuration = Settings(model: model, key: key ?? " ");
        var adapter = Adapter(handler, configuration);
        Assert.Equal("ConfigurationUnavailable", (await adapter.ProbeAsync(CancellationToken.None)).Status);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Fixed_synthetic_request_is_stateless_tool_free_and_usage_bounded()
    {
        var handler = new FakeHandler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://api.openai.com/v1/responses", request.RequestUri!.AbsoluteUri);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = body.RootElement;
            Assert.Equal("synthetic-model", root.GetProperty("model").GetString());
            Assert.False(root.GetProperty("store").GetBoolean());
            Assert.Equal(64, root.GetProperty("max_output_tokens").GetInt32());
            Assert.Equal(0, root.GetProperty("tools").GetArrayLength());
            Assert.Equal("none", root.GetProperty("tool_choice").GetString());
            var format = root.GetProperty("text").GetProperty("format");
            Assert.Equal("json_schema", format.GetProperty("type").GetString());
            Assert.True(format.GetProperty("strict").GetBoolean());
            Assert.False(format.GetProperty("schema").GetProperty("additionalProperties").GetBoolean());
            Assert.Equal("ok", format.GetProperty("schema").GetProperty("properties")
                .GetProperty("status").GetProperty("enum")[0].GetString());
            Assert.False(root.TryGetProperty("conversation", out _));
            Assert.False(root.TryGetProperty("previous_response_id", out _));
            Assert.False(root.TryGetProperty("background", out _));
            Assert.Contains("synthetic", root.GetProperty("input").GetString(), StringComparison.Ordinal);
            Assert.DoesNotContain("academyId", await request.Content.ReadAsStringAsync(token), StringComparison.Ordinal);
            return Json(GoodResponse);
        });
        var adapter = Adapter(handler, Settings());
        Assert.Equal("openai", adapter.ProviderName);
        Assert.Equal("synthetic-model", adapter.ModelName);
        var result = await adapter.ProbeAsync(CancellationToken.None);
        Assert.Equal(new PentaProviderProbeResult("Succeeded", 33, 8), result);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "ConfigurationUnavailable")]
    [InlineData(HttpStatusCode.Forbidden, "ConfigurationUnavailable")]
    [InlineData(HttpStatusCode.TooManyRequests, "RateLimited")]
    [InlineData(HttpStatusCode.BadRequest, "Rejected")]
    [InlineData(HttpStatusCode.RequestTimeout, "OutcomeUnknown")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "OutcomeUnknown")]
    public async Task Http_failures_have_safe_status_and_no_retry(HttpStatusCode code, string expected)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json("{\"secret\":\"do not echo\"}", code)));
        var result = await Adapter(handler, Settings()).ProbeAsync(CancellationToken.None);
        Assert.Equal(expected, result.Status);
        Assert.Null(result.InputTokens);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("{", "InvalidResponse")]
    [InlineData("{\"status\":\"incomplete\"}", "Incomplete")]
    [InlineData("{\"status\":\"failed\"}", "OutcomeUnknown")]
    [InlineData("{\"status\":3}", "InvalidResponse")]
    [InlineData("{\"status\":\"completed\",\"output\":[],\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "InvalidResponse")]
    [InlineData("{\"status\":\"completed\",\"output\":[{\"type\":\"function_call\"}],\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "InvalidResponse")]
    [InlineData("{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"refusal\",\"refusal\":\"No\"}]}],\"usage\":{\"input_tokens\":1,\"output_tokens\":1}}", "Refused")]
    public async Task Malformed_incomplete_tool_and_refusal_results_fail_closed(string body, string expected)
    {
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(body)));
        Assert.Equal(expected, (await Adapter(handler, Settings()).ProbeAsync(CancellationToken.None)).Status);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Oversized_response_and_transport_failure_never_retry_or_return_content()
    {
        var large = new string('x', 17 * 1024);
        var handler = new FakeHandler((_, _) => Task.FromResult(Json(large)));
        Assert.Equal("InvalidResponse", (await Adapter(handler, Settings()).ProbeAsync(CancellationToken.None)).Status);
        Assert.Equal(1, handler.Calls);

        var failed = new FakeHandler((_, _) => throw new HttpRequestException("synthetic transport failure"));
        Assert.Equal("OutcomeUnknown", (await Adapter(failed, Settings()).ProbeAsync(CancellationToken.None)).Status);
        Assert.Equal(1, failed.Calls);
    }
}
