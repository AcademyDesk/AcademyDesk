using System.Net;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaQueryPlannerOutcome(PentaQueryPlan? Plan, string? Error = null, MiniRuntimeIdentity? Runtime = null,
    string? QuerySourceSha256 = null, string? QueryPromptSha256 = null);
public interface IPentaQueryPlanner
{
    Task<PentaQueryPlannerOutcome> PlanAsync(JsonElement request, CancellationToken token);
    Task<bool> ReadyAsync(CancellationToken token);
}

public sealed class PentaQueryPlanner(HttpClient http, IConfiguration config, IHostEnvironment environment) : IPentaQueryPlanner
{
    private Uri? Endpoint => (environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
        config.GetValue<bool>("Penta:Enabled") && config.GetValue<bool>("Penta:Queries:Enabled") &&
        Uri.TryCreate(config["Penta:Queries:BaseUrl"], UriKind.Absolute, out var uri) && uri.Scheme == "http" &&
        uri.Host == "127.0.0.1" && uri.AbsolutePath == "/" && uri.UserInfo == "" && uri.Query == "" && uri.Fragment == "" ? uri : null;

    public async Task<bool> ReadyAsync(CancellationToken token)
    {
        if (Endpoint is not { } endpoint) return false;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try {
            using var response = await http.GetAsync(new Uri(endpoint, "v1/academy-query/readiness"), HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return false;
            var value = await PentaMiniProvider.BoundedJsonAsync(response.Content, deadline.Token, 1024);
            return value.ValueKind == JsonValueKind.Object && value.TryGetProperty("contract", out var contract) &&
                contract.GetString() == PentaQueryContract.Version && value.TryGetProperty("readOnly", out var read) && read.ValueKind == JsonValueKind.True;
        } catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or JsonException or InvalidOperationException) { return false; }
    }

    public async Task<PentaQueryPlannerOutcome> PlanAsync(JsonElement request, CancellationToken token)
    {
        if (Endpoint is not { } endpoint) return new(null, "Disabled");
        var json = request.GetRawText();
        if (Encoding.UTF8.GetByteCount(json) > 16 * 1024 || !request.TryGetProperty("contract", out var contract) || contract.GetString() != PentaQueryContract.Version)
            return new(null, "RequestBound");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(125));
        try {
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "v1/academy-query/plan")) {
                Content = new StringContent(json, Encoding.UTF8, "application/json") };
            message.Headers.Accept.ParseAdd("application/json"); message.Headers.AcceptEncoding.ParseAdd("identity");
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(null, "Unavailable");
            var runtime = MiniRuntimeIdentity.FromHeaders(response.Headers);
            var source = Hash(response, "X-Penta-Query-Source"); var prompt = Hash(response, "X-Penta-Query-Prompt");
            if (runtime is null || !Header(response, "X-Penta-Query-Contract", PentaQueryContract.Version) ||
                source is null || prompt is null) return new(null, "RuntimeIdentityFailed");
            var value = await PentaMiniProvider.BoundedJsonAsync(response.Content, deadline.Token, 4096);
            var plan = value.Deserialize<PentaQueryPlan>();
            return PentaQueryContract.ValidPlan(plan) ? new(plan, Runtime: runtime, QuerySourceSha256: source, QueryPromptSha256: prompt)
                : new(null, "SchemaFailed", runtime, source, prompt);
        } catch (Exception ex) when (ex is HttpRequestException or IOException or OperationCanceledException or JsonException or InvalidOperationException) {
            return new(null, ex is OperationCanceledException ? "Timeout" : "Unavailable");
        }
    }
    private static bool Header(HttpResponseMessage response, string key, string expected) =>
        response.Headers.TryGetValues(key, out var values) && values.SequenceEqual(new[] { expected });
    private static string? Hash(HttpResponseMessage response, string key) => response.Headers.TryGetValues(key, out var values) &&
        values.Count() == 1 && values.Single().Length == 64 && values.Single().All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ? values.Single() : null;
}
