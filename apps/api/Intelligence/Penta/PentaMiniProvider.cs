using System.Net.Http.Json;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

// Local/private only. No credentials, automatic retries, redirects or public endpoint.
public sealed class PentaMiniProvider(HttpClient http, IConfiguration config, IHostEnvironment env) : IPentaProvider
{
    private Uri? Endpoint => (env.IsDevelopment() || env.IsEnvironment("Testing")) &&
        config.GetValue<bool>("Penta:Enabled") && config.GetValue<bool>("Penta:Mini:Enabled") &&
        Uri.TryCreate(config["Penta:Mini:BaseUrl"] ?? "http://127.0.0.1:8000/", UriKind.Absolute, out var uri) &&
        uri.Scheme == "http" && uri.Host == "127.0.0.1" && uri.AbsolutePath == "/" &&
        uri.UserInfo == "" && uri.Query == "" && uri.Fragment == "" ? uri : null;

    public async Task<MiniProviderOutcome> PlanAsync(MiniPlanRequest request, CancellationToken token)
    {
        if (Endpoint is not { } endpoint) return new(null, "Disabled");
        if (request.UserMessage is not { Length: > 0 and <= 2000 } || !PentaMiniTools.ValidState(request.State) ||
            !request.AvailableTools.SequenceEqual(PentaMiniTools.Available)) return new(null, "InvalidRequest");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(125));
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, "v1/chat")) { Content = JsonContent.Create(request) };
            using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode) return new(null, "Unavailable");
            var data = await BoundedJsonAsync(response.Content, deadline.Token);
            var plan = data.Deserialize<MiniPlan>();
            return PentaMiniTools.ValidPlan(plan) ? new(plan) : new(null, "SchemaFailed");
        }
        catch (OperationCanceledException) { return new(null, "Timeout"); }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException)
        { return new(null, ex is JsonException ? "SchemaFailed" : "Unavailable"); }
    }

    public async Task<string> ReadinessAsync(CancellationToken token)
    {
        if (Endpoint is not { } endpoint) return "Unavailable";
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            using var response = await http.GetAsync(new Uri(endpoint, "readiness"), HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode) return "Unavailable";
            var data = await BoundedJsonAsync(response.Content, deadline.Token);
            return data.TryGetProperty("inference_ready", out var ready) && ready.ValueKind == JsonValueKind.True ? "Available" : "Degraded";
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or IOException or JsonException)
        { return "Unavailable"; }
    }
    private static async Task<JsonElement> BoundedJsonAsync(HttpContent content, CancellationToken token)
    {
        const int max = 16 * 1024;
        if (content.Headers.ContentLength > max) throw new JsonException("Provider response too large.");
        await using var stream = await content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var bytes = new byte[2048];
        int read;
        while ((read = await stream.ReadAsync(bytes, token)) > 0)
        {
            if (buffer.Length + read > max) throw new JsonException("Provider response too large.");
            buffer.Write(bytes, 0, read);
        }
        using var document = JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        // System.Text.Json otherwise accepts duplicate authority-bearing keys.
        ValidateUnique(document.RootElement);
        return document.RootElement.Clone();
    }
    private static void ValidateUnique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            { if (!keys.Add(property.Name)) throw new JsonException("Duplicate key."); ValidateUnique(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) ValidateUnique(item);
    }
}
