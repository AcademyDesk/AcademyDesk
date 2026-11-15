using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Tests.Infrastructure;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task<HttpClient> PentaBrowserSignIn(QaApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
        return client;
    }

    private static async Task<T> PentaBrowserRead<T>(Task<HttpResponseMessage> request, HttpStatusCode status, string label) where T : class
    {
        using var response = await request;
        RequireFinanceStatus(response, status, label);
        PentaRequire(response.Headers.CacheControl?.NoStore == true, "Browser response may be cached: " + label);
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Missing browser response: " + label);
    }

    private static async Task PentaBrowserStatus(Task<HttpResponseMessage> request, HttpStatusCode status, string label)
    {
        using var response = await request;
        RequireFinanceStatus(response, status, label);
    }

    private static Task<HttpResponseMessage> PentaBrowserPrepare(HttpClient client, string route, string tool, object fields) =>
        client.PutAsJsonAsync(route, new { contextVersion = 0, payload = new { tool_key = tool, tool_revision = 1, fields } },
            new JsonSerializerOptions());

    private static Task<HttpResponseMessage> PentaBrowserApprove(HttpClient client, string route, long revision, string payloadHash, string intent) =>
        client.PostAsJsonAsync(route + "/approve", new { revision, payloadHash, intent });

    private static Task<HttpResponseMessage> PentaBrowserExecute(HttpClient client, string route, long revision, string payloadHash, Guid? approvalId) =>
        client.PostAsJsonAsync(route + "/execute", new { revision, payloadHash, approvalId });
}
