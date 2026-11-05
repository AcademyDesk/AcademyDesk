using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Additive opt-in mounted approval/execution proof. Real host/Identity/SQL,
    // synthetic prepared fixtures, no interpreter, no limiter or retained-test changes.
    private static async Task VerifyPentaOnboardingOperationBrowserAsync(QaRunManifest manifest)
    {
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true"
        });
        using var client = factory.CreateClient();
        Guid academy;
        using (var scope = factory.Services.CreateScope()) academy = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()
            .Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        const string email = "qa-onboarding-operation-browser@example.invalid";
        var actor = await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        var token = await LoginAsync(client, email, "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var cases = new List<object>(); var drafts = new List<Guid>();
        foreach (var name in new[] { "light-mobile", "dark-desktop", "lost-response", "tampered-approval" }) {
            var session = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1), ProtectedState = "Synthetic operation browser fixture; no inference" };
            using (var scope = factory.Services.CreateScope()) {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.PentaMiniSessions.Add(session); await db.SaveChangesAsync();
            }
            var draft = Guid.NewGuid(); drafts.Add(draft);
            var route = $"/api/academies/{academy}/penta/onboarding/conversations/{session.Id}";
            using var saved = await client.PutAsJsonAsync(route + $"/drafts/{draft}", new { expectedRevision = 0, contextVersion = 0, fields = new { } });
            if ((int)saved.StatusCode != 201) throw new InvalidOperationException("Operation browser empty draft fixture failed.");
            using var context = await client.GetAsync(route + $"/drafts/{draft}/core-context");
            if ((int)context.StatusCode != 200) throw new InvalidOperationException("Operation browser context fixture failed.");
            var snapshot = await context.Content.ReadFromJsonAsync<JsonElement>(); var requestId = Guid.NewGuid();
            var fields = new Dictionary<string, object> {
                ["StudentFirstName"] = new { kind = "text", value = "Synthetic" },
                ["StudentLastName"] = new { kind = "text", value = "Operation " + name + " " + manifest.RunId.ToString("N") },
                ["DateOfBirth"] = new { kind = "date", value = "2000-01-01" }
            };
            using var prepared = await client.PostAsJsonAsync(route + $"/drafts/{draft}/core/requests/{requestId}/prepare", new {
                request_ref = requestId.ToString("D"), scope = snapshot.GetProperty("scope"), base_revision = 1, catalog_revision = 1,
                kind = "host_prepare", candidates = new[] { new { tool_key = PentaOnboardingDraftService.Tool,
                    edit = new { mode = "replace", set = fields, remove = Array.Empty<string>() } } }, questions = Array.Empty<string>(), invalidates_review = true
            });
            if ((int)prepared.StatusCode != 201) throw new InvalidOperationException("Operation browser preparation failed: " + (int)prepared.StatusCode);
            using var read = await client.GetAsync(route + $"/drafts/{draft}");
            if ((int)read.StatusCode != 200) throw new InvalidOperationException("Operation browser draft read failed.");
            var view = await read.Content.ReadFromJsonAsync<JsonElement>();
            using var select = await client.PostAsJsonAsync(route + "/selection", new { requestId = Guid.NewGuid(), expectedVersion = 0,
                draftId = draft, draftRevision = view.GetProperty("revision").GetInt64(), payloadHash = view.GetProperty("payloadHash").GetString() });
            if ((int)select.StatusCode != 200) throw new InvalidOperationException("Operation browser selection failed.");
            cases.Add(new { name, conversation = session.Id, draft });
        }
        Guid[] oldStudents, beforeAudits;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            oldStudents = await db.Students.Select(x => x.Id).ToArrayAsync(); beforeAudits = await db.AuditLogs.Select(x => x.Id).ToArrayAsync();
        }
        async Task<string> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().Where(x => oldStudents.Contains(x.Id)).OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = await db.PentaOnboardingDrafts.AsNoTracking().Where(x => !drafts.Contains(x.Id)).OrderBy(x => x.Id).ToArrayAsync(),
                sessions = await db.PentaMiniSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                turns = await db.PentaMiniTurns.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync(),
                academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            }); // Private values in memory only, never evidence/console.
        }
        var before = await Snapshot(); client.DefaultRequestHeaders.Authorization = null;
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build();
        bridge.Run(async context => {
            var path = context.Request.Path.Value ?? "";
            var scoped = path.StartsWith($"/api/academies/{academy}/penta/onboarding/conversations/", StringComparison.Ordinal);
            var read = context.Request.Method == "GET" && (path == "/api/auth/session" || scoped &&
                (path.EndsWith("/current", StringComparison.Ordinal) || System.Text.RegularExpressions.Regex.IsMatch(path, @"/drafts/[0-9a-f-]{36}$")));
            var action = context.Request.Method == "POST" && scoped && System.Text.RegularExpressions.Regex.IsMatch(path,
                @"/drafts/[0-9a-f-]{36}/core/(challenge|confirm|execute|operation|status)$");
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                context.Request.Host.Host != "127.0.0.1" || !(read || action) || context.Request.ContentLength > 1024) {
                context.Response.StatusCode = 405; return;
            }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.Headers.TryGetValue("Authorization", out var authorization)) request.Headers.TryAddWithoutValidation("Authorization", authorization.ToArray());
            if (action) { using var buffer = new MemoryStream(); await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
                if (buffer.Length > 1024) { context.Response.StatusCode = 413; return; }
                request.Content = new ByteArrayContent(buffer.ToArray()); request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json"); }
            using var response = await client.SendAsync(request, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode; context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-operation-live.cjs"));
        start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Operation browser did not start.");
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { cases, token })); process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Console.Write(await stdout); var error = await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException("Operation SQL browser failed: " + error);
        await bridge.StopAsync();
        if (before != await Snapshot()) throw new InvalidOperationException("Operation browser changed unrelated domain/Identity/session/turn state.");
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var added = await db.Students.AsNoTracking().Where(x => !oldStudents.Contains(x.Id)).ToArrayAsync();
            var rows = await db.PentaOnboardingDrafts.AsNoTracking().Where(x => drafts.Contains(x.Id)).ToArrayAsync();
            var audits = await db.AuditLogs.AsNoTracking().Where(x => !beforeAudits.Contains(x.Id)).ToArrayAsync();
            var expectedAudits = new Dictionary<string, int> {
                ["PentaOnboardingConversationRead"] = 28, ["PentaOnboardingDraftRead"] = 7,
                ["PentaOnboardingDraftCoreApprovalIssued"] = 4, ["PentaOnboardingDraftCoreApproved"] = 3,
                ["PentaOnboardingDraftCoreOperationRead"] = 14, ["PentaOnboardingDraftCoreStatusRead"] = 14,
                ["PentaOnboardingExecuted"] = 3
            };
            if (added.Length != 3 || added.Any(x => x.AcademyId != academy || x.FirstName != "Synthetic" || x.DateOfBirth != new DateOnly(2000, 1, 1)) ||
                rows.Length != 4 || rows.Count(x => x.Status == "Succeeded") != 3 || rows.Single(x => x.Id == drafts[3]).Status != "Prepared" ||
                rows.Any(x => x.Revision != 2 || x.AcademyId != academy || x.ActorUserId != actor) ||
                audits.Length != 73 || expectedAudits.Any(x => audits.Count(a => a.Action == x.Key) != x.Value) ||
                audits.Any(x => x.AcademyId != academy || x.ActorUserId != actor) ||
                audits.Where(x => x.Action == "PentaOnboardingExecuted").Select(x => x.EntityId).Distinct().Count() != 3)
                throw new InvalidOperationException("Operation browser exact student/draft/execution-audit postconditions differ.");
            var evidence = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", "penta-onboarding-operation-live-" + manifest.RunId.ToString("N")));
            Directory.CreateDirectory(evidence);
            await File.WriteAllTextAsync(Path.Combine(evidence, "sql-postconditions.json"), JsonSerializer.Serialize(new {
                run = manifest.RunId.ToString("N"), passed = true, scenarios = 4, studentsCreated = 3, executionAudits = 3,
                succeededDrafts = 3, terminalFullDocumentReentries = 3, terminalReentryRequests = 24,
                noRepeatedApprovalOrExecution = true, manipulatedApprovalDraftRemainsPrepared = true, exactActorAndTenant = true,
                unrelatedDomainAndIdentityUnchanged = true, sessionsAndTurnsUnchanged = true,
                audits = audits.GroupBy(x => x.Action).OrderBy(x => x.Key).Select(x => new { action = x.Key, count = x.Count() }).ToArray(),
                interpreterConfigured = false, physicalDevices = "NOT RUN"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Console.WriteLine("PASS: PENTAONBOARDINGOPERATION 4 mounted SQL/browser scenarios plus3 terminal reload/reentries;3 students/3 execution audits/73exact audits; no repeated approval/execution; lost-response read recovery; manipulated approval denied; unrelated state unchanged; no inference/429.");
    }
}
