using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaProviderProbeResult(string Status, int? InputTokens = null,
    int? OutputTokens = null);

/// <summary>Only a fixed synthetic probe is supported. No academy or user content is accepted.</summary>
public interface IPentaModelProvider
{
    Task<PentaProviderProbeResult> ProbeAsync(CancellationToken token);
}

/// <summary>
/// Candidate OpenAI Responses transport, with no production caller. Both PENTA's
/// local pilot and this independent probe gate must be enabled by an operator.
/// </summary>
public sealed class PentaOpenAiProbeAdapter(HttpClient http, IConfiguration configuration,
    IHostEnvironment environment) : IPentaModelProvider
{
    private const int MaximumResponseBytes = 16 * 1024;
    private const int MaximumOutputTokens = 64;
    private static readonly Uri Endpoint = new("https://api.openai.com/v1/responses");

    public async Task<PentaProviderProbeResult> ProbeAsync(CancellationToken token)
    {
        if ((!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) ||
            !configuration.GetValue<bool>("Penta:Enabled") ||
            !configuration.GetValue<bool>("Penta:Provider:Enabled") ||
            !configuration.GetValue<bool>("Penta:Provider:SyntheticProbeApproved"))
            return new("Disabled");

        var key = configuration["Penta:Provider:ApiKey"];
        var model = configuration["Penta:Provider:Model"];
        if (string.IsNullOrWhiteSpace(key) || key.Any(char.IsWhiteSpace) ||
            string.IsNullOrWhiteSpace(model) || model.Length > 100 ||
            model.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch is not '-' and not '_' and not '.'))
            return new("ConfigurationUnavailable");

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = JsonContent.Create(new
        {
            model,
            input = "Return the required synthetic probe status. No academy information is provided.",
            instructions = "This is a synthetic connectivity probe. Return only the required JSON status.",
            store = false,
            max_output_tokens = MaximumOutputTokens,
            tools = Array.Empty<object>(),
            tool_choice = "none",
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "penta_synthetic_probe_v1",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new { status = new { type = "string", @enum = new[] { "ok" } } },
                        required = new[] { "status" },
                        additionalProperties = false
                    }
                }
            }
        });

        // No blind retry: a timeout or transport failure may have consumed tokens.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            using var response = await http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) return new("RateLimited");
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return new("ConfigurationUnavailable");
            if (response.StatusCode == HttpStatusCode.RequestTimeout) return new("OutcomeUnknown");
            if ((int)response.StatusCode >= 500) return new("OutcomeUnknown");
            if (!response.IsSuccessStatusCode) return new("Rejected");
            if (response.Content.Headers.ContentLength > MaximumResponseBytes)
                return new("InvalidResponse");

            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var bytes = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(bytes, deadline.Token)) > 0)
            {
                if (buffer.Length + read > MaximumResponseBytes) return new("InvalidResponse");
                buffer.Write(bytes, 0, read);
            }
            buffer.Position = 0;
            using var document = await JsonDocument.ParseAsync(buffer,
                cancellationToken: deadline.Token);
            return Parse(document.RootElement);
        }
        catch (OperationCanceledException)
        {
            return new("OutcomeUnknown");
        }
        catch (HttpRequestException)
        {
            return new("OutcomeUnknown");
        }
        catch (IOException)
        {
            return new("OutcomeUnknown");
        }
        catch (JsonException)
        {
            return new("InvalidResponse");
        }
    }

    private static PentaProviderProbeResult Parse(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String)
            return new("InvalidResponse");
        if (status.GetString() == "incomplete") return new("Incomplete");
        if (status.GetString() == "failed") return new("OutcomeUnknown");
        if (status.GetString() != "completed" ||
            !root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object ||
            !ReadTokens(usage, "input_tokens", out var input) ||
            !ReadTokens(usage, "output_tokens", out var outputTokens) || input > 2048 ||
            outputTokens > MaximumOutputTokens)
            return new("InvalidResponse");

        var messages = 0;
        foreach (var item in output.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("type", out var itemType) ||
                itemType.ValueKind != JsonValueKind.String) return new("InvalidResponse");
            if (itemType.GetString() == "reasoning") continue;
            if (itemType.GetString() != "message" || ++messages > 1 ||
                !item.TryGetProperty("role", out var role) ||
                role.ValueKind != JsonValueKind.String || role.GetString() != "assistant" ||
                !item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array || content.GetArrayLength() != 1)
                return new("InvalidResponse");
            var part = content[0];
            if (part.ValueKind == JsonValueKind.Object &&
                part.TryGetProperty("type", out var refusal) &&
                refusal.ValueKind == JsonValueKind.String && refusal.GetString() == "refusal")
                return new("Refused");
            if (part.ValueKind != JsonValueKind.Object ||
                !part.TryGetProperty("type", out var partType) ||
                partType.ValueKind != JsonValueKind.String || partType.GetString() != "output_text" ||
                !part.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String ||
                text.GetString() is not { Length: <= 128 } payload)
                return new("InvalidResponse");
            try
            {
                using var parsed = JsonDocument.Parse(payload);
                var body = parsed.RootElement;
                if (body.ValueKind != JsonValueKind.Object ||
                    body.EnumerateObject().Count() != 1 ||
                    !body.TryGetProperty("status", out var probeStatus) ||
                    probeStatus.ValueKind != JsonValueKind.String ||
                    probeStatus.GetString() != "ok") return new("InvalidResponse");
            }
            catch (JsonException)
            {
                return new("InvalidResponse");
            }
        }
        return messages == 1 ? new("Succeeded", input, outputTokens) : new("InvalidResponse");
    }

    private static bool ReadTokens(JsonElement usage, string key, out int count)
    {
        count = 0;
        return usage.TryGetProperty(key, out var value) &&
               value.ValueKind == JsonValueKind.Number &&
               value.TryGetInt32(out count) && count >= 0;
    }
}
