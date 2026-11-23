using System.Net;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaTaskPlannerOutcome(JsonElement? Proposal, string? Error = null,
    MiniRuntimeIdentity? Runtime = null, string? TaskSourceSha256 = null, string? TaskPromptSha256 = null);
public interface IPentaTaskPlanner
{
    Task<PentaTaskPlannerOutcome> PlanAsync(JsonElement hostRequest, CancellationToken token);
}

/// <summary>Separate opt-in private task planner; no redirects/retry/public fallback.</summary>
public sealed class PentaTaskPlannerProvider(HttpClient http, IConfiguration config, IHostEnvironment environment) : IPentaTaskPlanner
{
    private Uri? Endpoint => (environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
        config.GetValue<bool>("Penta:Enabled") && config.GetValue<bool>("Penta:Tasks:Enabled") &&
        Uri.TryCreate(config["Penta:Tasks:BaseUrl"], UriKind.Absolute, out var uri) && uri.Scheme == "http" &&
        uri.Host == "127.0.0.1" && uri.AbsolutePath == "/" && uri.UserInfo == "" && uri.Query == "" && uri.Fragment == "" ? uri : null;

    public async Task<PentaTaskPlannerOutcome> PlanAsync(JsonElement hostRequest, CancellationToken token)
    {
        if (Endpoint is not { } endpoint) return new(null, "Disabled");
        var json = JsonSerializer.Serialize(hostRequest);
        if (Encoding.UTF8.GetByteCount(json) > 16 * 1024 || hostRequest.ValueKind != JsonValueKind.Object ||
            !hostRequest.TryGetProperty("contract", out var contract) || contract.GetString() != PentaTaskDialogue.Contract)
            return new(null, "RequestBound");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(125));
        MiniRuntimeIdentity? runtime = null;
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "v1/academy-tasks/plan")) {
                Content = new StringContent(json, Encoding.UTF8, "application/json") };
            message.Headers.Accept.ParseAdd("application/json"); message.Headers.AcceptEncoding.ParseAdd("identity");
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode != HttpStatusCode.OK) return new(null, "Unavailable");
            runtime = MiniRuntimeIdentity.FromHeaders(response.Headers);
            var source = HashHeader(response, "X-Penta-Task-Source");
            var prompt = HashHeader(response, "X-Penta-Task-Prompt");
            if (runtime is null || source is null || prompt is null ||
                !response.Headers.TryGetValues("X-Penta-Task-Contract", out var versions) ||
                !versions.SequenceEqual(new[] { PentaTaskDialogue.Contract })) return new(null, "RuntimeIdentityFailed");
            var proposal = await PentaMiniProvider.BoundedJsonAsync(response.Content, deadline.Token, 32 * 1024);
            return new(proposal, Runtime: runtime, TaskSourceSha256: source, TaskPromptSha256: prompt);
        }
        catch (OperationCanceledException) { return new(null, "Timeout", runtime); }
        catch (Exception error) when (error is HttpRequestException or IOException or JsonException or InvalidOperationException)
        { return new(null, error is JsonException ? "SchemaFailed" : "Unavailable", runtime); }
    }

    private static string? HashHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var values)) return null;
        var items = values.Take(2).ToArray();
        return items.Length == 1 && items[0].Length == 64 && items[0].All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f') ? items[0] : null;
    }
}
