using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyExpenseBrowserAsync(QaApiFactory factory, HttpClient client, Guid academy, Guid foreignBranch)
    {
        var cases = new List<object>();
        foreach (var width in new[] { 320, 1440 }) foreach (var theme in new[] { "light", "dark" })
        {
            var email = $"qa-expense-ui-{width}-{theme}@example.invalid";
            await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
            cases.Add(new { width, theme, email });
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        var initial = await Snapshot(); var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        // Start browser acceptance in a fresh production fixed-window interval,
        // rather than spending the preceding 66 API cases' remaining IP budget.
        Console.WriteLine("PASS: EXPENSE BROWSER pacing one 60-second production limiter window; limiter configuration unchanged.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        await using var bridge = builder.Build();
        bridge.Run(async context => {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var allowedPost = context.Request.Method == "POST" && (context.Request.Path == "/api/auth/login" || context.Request.Path == $"/api/academies/{academy}/expenses");
            if (context.Request.Method is not ("GET" or "OPTIONS") && !allowedPost) { context.Response.StatusCode = 405; return; }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers) {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) { Console.WriteLine("FAIL: EXPENSE BROWSER HTTP 429; not accepted."); throw new InvalidOperationException("Expense browser rate-limited; not accepted."); }
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers)) {
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            Console.WriteLine($"PASS: EXPENSE BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/expenses-feedback-live.cjs")); start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(academy.ToString());
        start.ArgumentList.Add(foreignBranch.ToString()); start.ArgumentList.Add(JsonSerializer.Serialize(cases));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Expense browser did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        var result = await output; var errorText = await errors;
        // The browser emits only fixture labels/status/evidence paths, never bearer tokens.
        Console.Write(result);
        if (process.ExitCode != 0) throw new InvalidOperationException("Expense linked browser failed: " + errorText);
        await bridge.StopAsync();
        using var before = JsonDocument.Parse(initial); using var final = JsonDocument.Parse(await Snapshot());
        foreach (var field in new[] { "Branches", "Invoices", "Payments" }) if (before.RootElement.GetProperty(field).GetRawText() != final.RootElement.GetProperty(field).GetRawText()) throw new InvalidOperationException("Expense browser changed unrelated " + field);
        var oldRows = before.RootElement.GetProperty("Expenses").EnumerateArray().ToArray(); var newRows = final.RootElement.GetProperty("Expenses").EnumerateArray().ToArray();
        foreach (var row in oldRows) if (newRows.Single(x => x.GetProperty("Id").GetGuid() == row.GetProperty("Id").GetGuid()).GetRawText() != row.GetRawText()) throw new InvalidOperationException("Expense browser changed existing/sentinel expense.");
        var added = newRows.Where(x => !oldRows.Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid())).ToArray();
        if (added.Length != 5 || added.Any(x => x.GetProperty("AcademyId").GetGuid() != academy ||
            (x.GetProperty("Description").GetString() == "UI exact expense" ? x.GetProperty("Amount").GetDecimal() != 9999999999999999.99m : !x.GetProperty("Description").GetString()!.StartsWith("UI expense ") || x.GetProperty("Amount").GetDecimal() != 2.75m) ||
            x.GetProperty("Currency").GetString() != "INR" || x.GetProperty("Category").GetString() != "General" || x.GetProperty("Status").GetString() != "Recorded" || x.GetProperty("BranchId").ValueKind != JsonValueKind.Null) || added.Count(x => x.GetProperty("Description").GetString() == "UI exact expense") != 1) throw new InvalidOperationException("Expense browser fresh SQL writes differ.");
        var oldAudits = before.RootElement.GetProperty("Audits").EnumerateArray().ToArray(); var newAudits = final.RootElement.GetProperty("Audits").EnumerateArray().Where(x => !oldAudits.Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid())).ToArray();
        if (newAudits.Length != 5 || newAudits.Any(x => x.GetProperty("Action").GetString() != "POST Expenses" || x.GetProperty("AcademyId").GetGuid() != academy || x.GetProperty("ActorUserId").ValueKind == JsonValueKind.Null)) throw new InvalidOperationException("Expense browser audit differs.");
        Console.WriteLine("PASS: EXPENSE BROWSER five real authenticated form saves including maximum exact decimal, four invalid-branch no-writes, exact fresh SQL/audits, all sentinels/unrelated ledgers preserved; no 429.");
    }
}
