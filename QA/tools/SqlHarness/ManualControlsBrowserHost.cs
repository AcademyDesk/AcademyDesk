using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task ServeManualControlsBrowserAsync(QaRunManifest manifest, ManualFixture fixture)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Distinct loopback browser and API origins are required.");

        var api = $"http://127.0.0.1:{port}";
        var browserOrigin = origin.GetLeftPart(UriPartial.Authority);
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = browserOrigin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned browser backend preflight failed.");

        const string approvalReason = "Live approval without a note";
        const string namedReason = "Live approval with a note";
        const string rejectionReason = "Live rejection with a reason";
        var approval = await ManualProposalAsync(factory, fixture, approvalReason);
        var named = await ManualProposalAsync(factory, fixture, namedReason);
        var rejection = await ManualProposalAsync(factory, fixture, rejectionReason);
        var metadata = new {
            run, api, origin = browserOrigin, academy = fixture.Academy, batch = fixture.Batch, foreignBatch = fixture.ForeignBatch,
            proposals = new {
                approval = new { id = approval.Adjustment, reason = approvalReason },
                named = new { id = named.Adjustment, reason = namedReason },
                rejection = new { id = rejection.Adjustment, reason = rejectionReason }
            }
        };
        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "manual-browser-fixture.json"), JsonSerializer.Serialize(metadata));

        string[] phases = ["finance-approve-cancel", "finance-reject-cancel", "finance-reject-empty", "finance-reject-whitespace",
            "finance-approved-empty", "finance-approved-note", "finance-rejected", "teacher-homework", "teacher-lesson",
            "teacher-assessment-blank", "teacher-assessment-dated", "teacher-denied", "teacher-unconfirmed"];
        var state = await ManualStateAsync(factory);
        var completed = 0;
        var failed = false;
        using var checkLock = new SemaphoreSlim(1);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(api);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
        await using var bridge = builder.Build();

        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
            {
                context.Response.StatusCode = 400;
                return;
            }
            if (context.Request.Path == "/__qa/manual-check")
            {
                if (context.Request.Method != "POST" || context.Request.ContentLength is not (> 0 and <= 2048))
                {
                    context.Response.StatusCode = 400;
                    return;
                }
                await checkLock.WaitAsync(context.RequestAborted);
                try
                {
                    var requested = await context.Request.ReadFromJsonAsync<JsonObject>(context.RequestAborted);
                    var phase = requested?["phase"]?.GetValue<string>();
                    if (requested?["run"]?.GetValue<string>() != run || failed || completed >= phases.Length || phase != phases[completed])
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }
                    var actual = await ManualStateAsync(factory);
                    var checkedState = actual.DeepClone().AsObject();
                    switch (phase)
                    {
                        case "finance-approved-empty":
                            ManualFinanceDelta(state, checkedState, fixture, approval.Invoice, approval.Adjustment, new JsonObject { ["approve"] = true, ["notes"] = "" });
                            break;
                        case "finance-approved-note":
                            ManualFinanceDelta(state, checkedState, fixture, named.Invoice, named.Adjustment, new JsonObject { ["approve"] = true, ["notes"] = "Live approval note" });
                            break;
                        case "finance-rejected":
                            ManualFinanceDelta(state, checkedState, fixture, rejection.Invoice, rejection.Adjustment, new JsonObject { ["approve"] = false, ["notes"] = "Live rejection reason" });
                            break;
                        case "teacher-homework":
                        case "teacher-lesson":
                        case "teacher-assessment-blank":
                        case "teacher-assessment-dated":
                        case "teacher-unconfirmed":
                            if (!Guid.TryParse(requested?["savedId"]?.GetValue<string>(), out var saved)) throw new InvalidOperationException("Real response row ID required.");
                            var route = "/api/teacher/assessments";
                            var body = new JsonObject { ["title"] = "Live " + phase };
                            if (phase == "teacher-homework")
                            {
                                route = "/api/teacher/assignments";
                                body["isPublished"] = true;
                                body["description"] = "";
                                body["type"] = "Homework";
                            }
                            else if (phase == "teacher-lesson")
                            {
                                route = "/api/teacher/lesson-plans";
                                body["objectives"] = "";
                            }
                            else
                            {
                                body["isPublished"] = false;
                                body["maxScore"] = 12.5m;
                                body["type"] = "Performance";
                                body["scheduledAtUtc"] = phase == "teacher-assessment-dated" ? JsonValue.Create(new DateTime(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc)) : null;
                            }
                            ManualTeacherDelta(state, checkedState, fixture, route, body, saved);
                            break;
                        default:
                            ManualSame(state, actual, phase!);
                            break;
                    }
                    state = actual;
                    completed++;
                    Console.WriteLine($"FINANCEMANUAL BROWSER SQL PASS {phase}");
                    await context.Response.WriteAsJsonAsync(new { phase }, context.RequestAborted);
                }
                catch (Exception error)
                {
                    failed = true;
                    Console.WriteLine("FINANCEMANUAL BROWSER SQL FAILED " + error.Message);
                    context.Response.StatusCode = 500;
                }
                finally { checkLock.Release(); }
                return;
            }

            // Forward the browser's actual request to the application pipeline.
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
                request.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            foreach (var header in response.Headers.Concat(response.Content.Headers))
            {
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
            Console.WriteLine($"FINANCEMANUAL BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-manual-browser");
        Console.WriteLine($"FINANCEMANUAL BROWSER READY run={run} api={port} origin={origin.Port} stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(8);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != phases.Length || !File.Exists(stop)) throw new InvalidOperationException($"Incomplete live manual browser fixture. Accepted {completed}/{phases.Length}.");
        ManualSame(state, await ManualStateAsync(factory), "final browser state after all responses");
        Console.WriteLine($"FINANCEMANUAL BROWSER FINAL {completed} verified UI/SQL phases.");
    }
}
