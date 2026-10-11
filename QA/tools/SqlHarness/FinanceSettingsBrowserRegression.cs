using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyFinanceSettingsBrowserAsync(QaApiFactory factory, HttpClient client, Guid academy)
    {
        var cases = new List<object>(); var actors = new HashSet<Guid>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            if (!await db.AcademyFinanceSettings.AnyAsync(x => x.AcademyId == foreign))
            {
                db.AcademyFinanceSettings.Add(new AcademyFinanceSettings { AcademyId = foreign, TaxLabel = "Foreign sentinel", PayslipTemplateKey = "Compact" });
                await db.SaveChangesAsync();
            }
        }
        foreach (var width in new[] { 320, 1440 }) foreach (var theme in new[] { "light", "dark" })
            foreach (var page in new[] { "finance-policy", "finance-governance" })
            {
                var email = $"qa-settings-ui-{width}-{theme}-{page}@example.invalid";
                actors.Add(await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy));
                cases.Add(new { width, theme, page, email });
            }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                Settings = await db.AcademyFinanceSettings.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            });
        }
        var failures = new ConcurrentQueue<string>(); var statuses = new ConcurrentQueue<int>();
        var settingsPath = $"/api/academies/{academy}/finance-governance/settings";
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        // Production middleware partitions before authentication. Separate test
        // identities do not give separate limiter budgets; never disable it.
        Console.WriteLine("PASS: SETTINGS BROWSER pacing one unchanged production limiter window before linked acceptance.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        using var pacing = new SemaphoreSlim(1, 1);
        await using var bridge = builder.Build();
        bridge.Run(async context => {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var isSettingsWrite = context.Request.Method == "PUT" && context.Request.Path == settingsPath;
            var allowedLogin = context.Request.Method == "POST" && context.Request.Path == "/api/auth/login";
            if (context.Request.Method is not ("GET" or "OPTIONS") && !isSettingsWrite && !allowedLogin) { context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try { await Task.Delay(600, context.RequestAborted); }
            finally { pacing.Release(); }
            var before = isSettingsWrite ? await Snapshot() : null;
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers) {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            using var response = await client.SendAsync(request, context.RequestAborted);
            var bytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) failures.Enqueue("HTTP429 is not accepted.");
            if (isSettingsWrite)
            {
                statuses.Enqueue((int)response.StatusCode); var after = await Snapshot();
                if (!response.IsSuccessStatusCode && before != after) failures.Enqueue("Rejected browser request changed settings/audits/ledgers.");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var result = JsonSerializer.Deserialize<FinanceSettingsSummary>(bytes, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                    using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    var row = await db.AcademyFinanceSettings.AsNoTracking().SingleAsync(x => x.AcademyId == academy);
                    var stored = new FinanceSettingsSummary(row.TaxRegistrationNumber,row.TaxLabel,row.TaxRatePercent,row.DefaultPaymentTermsDays,row.TaxInclusivePricing,row.InvoiceLogoUrl,row.InvoiceAuthorityName,row.InvoiceAuthorityTitle,row.InvoiceSignatureUrl,row.InvoiceTemplateKey,row.PayslipTemplateKey);
                    if (stored != result with { SettingsVersion = null }) failures.Enqueue("Successful browser response differs from fresh SQL fields.");
                    using var oldState = JsonDocument.Parse(before!); using var newState = JsonDocument.Parse(after);
                    foreach (var field in new[] { "Invoices", "Payments", "Expenses" })
                        if (oldState.RootElement.GetProperty(field).GetRawText() != newState.RootElement.GetProperty(field).GetRawText()) failures.Enqueue("Browser settings changed unrelated ledger " + field);
                    var oldRows = oldState.RootElement.GetProperty("Settings").EnumerateArray().Where(x => x.GetProperty("AcademyId").GetGuid() != academy).Select(x => x.GetRawText()).ToArray();
                    var newRows = newState.RootElement.GetProperty("Settings").EnumerateArray().Where(x => x.GetProperty("AcademyId").GetGuid() != academy).Select(x => x.GetRawText()).ToArray();
                    if (!oldRows.SequenceEqual(newRows)) failures.Enqueue("Browser settings changed another academy.");
                    var oldAudits = oldState.RootElement.GetProperty("Audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
                    var addedAudits = newState.RootElement.GetProperty("Audits").EnumerateArray().Where(x => !oldAudits.Contains(x.GetProperty("Id").GetGuid())).ToArray();
                    if (oldRows.Length == 0) failures.Enqueue("Missing positive foreign settings sentinel.");
                    if (addedAudits.Length != 1 || addedAudits.Any(x => x.GetProperty("AcademyId").GetGuid() != academy || x.GetProperty("ActorUserId").ValueKind == JsonValueKind.Null || !actors.Contains(x.GetProperty("ActorUserId").GetGuid()) || x.GetProperty("Action").GetString() != "PUT FinanceGovernance" || !x.GetProperty("MetadataJson").GetString()!.Contains(settingsPath))) failures.Enqueue("Browser success audit actor/tenant/action/route/count differs.");
                }
            }
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers)) {
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            Console.WriteLine($"PASS: SETTINGS BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/finance-settings-live.cjs")); start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(academy.ToString()); start.ArgumentList.Add(JsonSerializer.Serialize(cases));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Settings browser did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Console.Write(await output); var errorText = await errors;
        await bridge.StopAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("Settings linked browser failed: " + errorText);
        if (!failures.IsEmpty || statuses.Count != 32 || statuses.Count(x => x == 200) != 24 || statuses.Count(x => x == 409) != 8) throw new InvalidOperationException("Settings linked SQL verification failed: " + string.Join("; ", failures));
        Console.WriteLine("PASS: SETTINGS BROWSER eight linked journeys;24 real successes/eight stale409; exact SQL fields/audits/foreign+ledger preservation; no429; physical devices NOT RUN.");
    }
}
