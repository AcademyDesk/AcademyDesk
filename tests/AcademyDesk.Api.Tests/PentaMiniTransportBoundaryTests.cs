using System.Net;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

// No model, SQL or external service. Exercise the actual application adapter.
public sealed class PentaMiniTransportBoundaryTests
{
    private sealed class Host(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "MiniTransportBoundary";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return respond(request, token); }
    }
    private static PentaMiniProvider Provider(Handler handler, string environment = "Testing", bool enabled = true, bool miniEnabled = true) =>
        new(new HttpClient(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:Enabled"] = enabled.ToString(), ["Penta:Mini:Enabled"] = miniEnabled.ToString(),
            ["Penta:Mini:BaseUrl"] = "http://127.0.0.1:8000/"
        }).Build(), new Host(environment));
    private static MiniPlanRequest Input => new(Guid.NewGuid().ToString("D"), "Which learner?", MiniState.Empty, PentaMiniTools.Available);
    private static string Plan => JsonSerializer.Serialize(new MiniPlan("0.1", "CLARIFICATION_REQUIRED", "Which learner?", null, null, MiniState.Empty));
    private static string Runtime => JsonSerializer.Serialize(new MiniRuntimeIdentity("2.4.1", "planner-9.4", "0.1",
        new('a',64), new('b',64), new('c',64), new('d',64), new('e',64), new('f',64)));
    private static HttpResponseMessage Reply(string body, int status = 200, string? media = "application/json", string[]? encodings = null)
    {
        var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body, Encoding.UTF8) };
        response.Content.Headers.ContentType = media is null ? null : System.Net.Http.Headers.MediaTypeHeaderValue.Parse(media);
        if (encodings is not null) foreach (var encoding in encodings) response.Content.Headers.ContentEncoding.Add(encoding);
        response.Headers.TryAddWithoutValidation(MiniRuntimeIdentity.HeaderName, Runtime);
        return response;
    }
    private static Handler Fixed(string body, int status = 200, string? media = "application/json", string[]? encodings = null) =>
        new((_, _) => Task.FromResult(Reply(body, status, media, encodings)));

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[{}]")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("\"private-provider-text\"")]
    public async Task Readiness_nonobject_JSON_is_unavailable_not_an_exception(string body)
    {
        var handler = Fixed(body);
        Assert.Equal("Unavailable", await Provider(handler).ReadinessAsync(default));
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData("{\"inference_ready\":true}", "Available")]
    [InlineData("{\"inference_ready\":false}", "Degraded")]
    [InlineData("{}", "Degraded")]
    [InlineData("{\"inference_ready\":\"true\"}", "Degraded")]
    [InlineData("{\"inference_ready\":null}", "Degraded")]
    public async Task Readiness_requires_boolean_true_and_preserves_degraded_states(string body, string expected)
    {
        var handler = Fixed(body); Assert.Equal(expected, await Provider(handler).ReadinessAsync(default)); Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(201)][InlineData(202)][InlineData(203)][InlineData(204)]
    [InlineData(205)][InlineData(206)][InlineData(207)][InlineData(226)]
    [InlineData(301)][InlineData(302)][InlineData(307)][InlineData(308)]
    [InlineData(401)][InlineData(403)][InlineData(429)][InlineData(500)][InlineData(503)]
    public async Task Only_completed_200_responses_are_accepted_without_retry(int status)
    {
        var plan = Fixed(Plan, status); var ready = Fixed("{\"inference_ready\":true}", status);
        var outcome = await Provider(plan).PlanAsync(Input, default);
        Assert.Null(outcome.Plan); Assert.Equal("Unavailable", outcome.Error);
        Assert.Equal("Unavailable", await Provider(ready).ReadinessAsync(default));
        Assert.Equal(1, plan.Calls); Assert.Equal(1, ready.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    [InlineData("application/problem+json")]
    [InlineData("application/json; charset=utf-16")]
    public async Task Non_JSON_or_unsupported_charset_cannot_report_ready_or_prepare_a_plan(string? media)
    {
        var plan = Fixed(Plan, media: media); var ready = Fixed("{\"inference_ready\":true}", media: media);
        var outcome = await Provider(plan).PlanAsync(Input, default);
        Assert.Null(outcome.Plan); Assert.Equal("SchemaFailed", outcome.Error);
        Assert.Equal("Unavailable", await Provider(ready).ReadinessAsync(default));
        Assert.Equal(1, plan.Calls); Assert.Equal(1, ready.Calls);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("Application/JSON; charset=\"UTF-8\"")]
    public async Task Normal_JSON_is_accepted_and_explicit_identity_requests_are_sent(string media)
    {
        var handler = new Handler((request, _) => {
            Assert.Equal("127.0.0.1", request.RequestUri!.Host);
            Assert.Contains(request.Headers.Accept, h => h.MediaType == "application/json");
            Assert.Contains(request.Headers.AcceptEncoding, h => h.Value == "identity");
            return Task.FromResult(Reply(request.Method == HttpMethod.Post ? Plan : "{\"inference_ready\":true}", media: media));
        });
        var provider = Provider(handler);
        Assert.NotNull((await provider.PlanAsync(Input, default)).Plan);
        Assert.Equal("Available", await provider.ReadinessAsync(default)); Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData("gzip")][InlineData("br")][InlineData("deflate")]
    public async Task Encoded_response_is_refused_even_when_its_body_looks_like_JSON(string encoding)
    {
        var plan = Fixed(Plan, encodings: [encoding]); var ready = Fixed("{\"inference_ready\":true}", encodings: [encoding]);
        Assert.Equal("SchemaFailed", (await Provider(plan).PlanAsync(Input, default)).Error);
        Assert.Equal("Unavailable", await Provider(ready).ReadinessAsync(default));
        Assert.Equal(1, plan.Calls); Assert.Equal(1, ready.Calls);
    }
    [Fact]
    public async Task Explicit_identity_response_is_compatible_but_ambiguous_encoding_list_is_not()
    {
        Assert.Equal("Available", await Provider(Fixed("{\"inference_ready\":true}", encodings: ["identity"])).ReadinessAsync(default));
        Assert.Equal("Unavailable", await Provider(Fixed("{\"inference_ready\":true}", encodings: ["identity", "identity"])).ReadinessAsync(default));
    }

    [Theory]
    [InlineData("")][InlineData("{")]
    [InlineData("{\"inference_ready\":false,\"inference_ready\":true}")]
    [InlineData("{\"inference_ready\":true,\"extra\":{\"scope\":1,\"scope\":2}}")]
    public async Task Malformed_or_duplicate_JSON_never_becomes_ready(string body)
    {
        var handler = Fixed(body); Assert.Equal("Unavailable", await Provider(handler).ReadinessAsync(default)); Assert.Equal(1, handler.Calls);
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task Declared_and_streamed_body_limits_remain_enforced(bool misleadingLength)
    {
        var handler = new Handler((_, _) => {
            var r = Reply(new string(' ', 17 * 1024) + "{\"inference_ready\":true}");
            if (misleadingLength) r.Content.Headers.ContentLength = 1;
            return Task.FromResult(r);
        });
        Assert.Equal("Unavailable", await Provider(handler).ReadinessAsync(default)); Assert.Equal(1, handler.Calls);
    }
    [Fact]
    public async Task Excess_JSON_nesting_is_unavailable()
    {
        var body = "{\"inference_ready\":true,\"nested\":" + new string('[', 18) + "0" + new string(']', 18) + "}";
        Assert.Equal("Unavailable", await Provider(Fixed(body)).ReadinessAsync(default));
    }

    [Theory]
    [InlineData("Production", true, true)][InlineData("Staging", true, true)]
    [InlineData("Testing", false, true)][InlineData("Development", true, false)]
    public async Task Readiness_and_planning_keep_environment_and_enable_gates(string environment, bool enabled, bool miniEnabled)
    {
        var handler = Fixed(Plan); var provider = Provider(handler, environment, enabled, miniEnabled);
        Assert.Equal("Unavailable", await provider.ReadinessAsync(default));
        Assert.Equal("Disabled", (await provider.PlanAsync(Input, default)).Error); Assert.Equal(0, handler.Calls);
    }
    [Fact]
    public async Task Provider_failure_is_sanitized_and_never_retried()
    {
        var handler = new Handler((_, _) => throw new HttpRequestException("Synthetic private provider detail"));
        var provider = Provider(handler);
        Assert.Equal("Unavailable", await provider.ReadinessAsync(default));
        var result = await provider.PlanAsync(Input, default);
        Assert.Equal("Unavailable", result.Error); Assert.Null(result.Plan); Assert.Equal(2, handler.Calls);
    }
    [Fact]
    public async Task Caller_cancellation_during_send_has_safe_status_and_no_retry()
    {
        foreach (var planning in new[] { false, true })
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new Handler(async (_, token) => { entered.SetResult(); await Task.Delay(Timeout.Infinite, token); return Reply("{}"); });
            using var cancel = new CancellationTokenSource(); var provider = Provider(handler);
            async Task<string?> Check() => planning
                ? (await provider.PlanAsync(Input, cancel.Token)).Error
                : await provider.ReadinessAsync(cancel.Token);
            var pending = Check();
            await entered.Task; cancel.Cancel(); Assert.Equal(planning ? "Timeout" : "Unavailable", await pending); Assert.Equal(1, handler.Calls);
        }
    }
    [Fact]
    public async Task Explicit_subsequent_check_can_recover_without_a_hidden_retry_or_cached_result()
    {
        var handler = new Handler((_, _) => Task.FromResult(Reply("{\"inference_ready\":true}", media: "text/html")));
        Assert.Equal("Unavailable", await Provider(handler).ReadinessAsync(default)); Assert.Equal(1, handler.Calls);
        var count = 0;
        var sequence = new Handler((_, _) => Task.FromResult(Reply("{\"inference_ready\":true}", status: ++count == 1 ? 503 : 200)));
        var provider = Provider(sequence); Assert.Equal("Unavailable", await provider.ReadinessAsync(default)); Assert.Equal(1, sequence.Calls);
        Assert.Equal("Available", await provider.ReadinessAsync(default)); Assert.Equal(2, sequence.Calls);
    }
}
