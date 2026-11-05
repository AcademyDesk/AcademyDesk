using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    // Opt-in browser extension, not a replacement for any retained SQL assertions.
    private static async Task VerifyPentaOnboardingReviewBrowserAsync(QaRunManifest manifest)
    {
        var flags = new Dictionary<string, string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true"
        };
        using var factory = new QaApiFactory(manifest, flags);
        using var client = factory.CreateClient();
        Guid academy;
        using (var scope = factory.Services.CreateScope()) academy = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()
            .Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        const string email = "qa-onboarding-review-browser@example.invalid";
        var actor = await CreateAccessActorAsync(factory, email, "AcademyAdmin", academy);
        var tokens = new Dictionary<string, string>();
        foreach (var pair in new[] { ("admin", email), ("other", "qa-admin-a@example.invalid"),
            ("foreign", "qa-admin-b@example.invalid"), ("teacher", "qa-teacher-a@example.invalid") })
            tokens[pair.Item1] = await LoginAsync(client, pair.Item2, "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens["admin"]);
        async Task<object> Fixture(bool pending)
        {
            var session = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1), ProtectedState = "Synthetic passive browser fixture; no inference" };
            using (var scope = factory.Services.CreateScope()) {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.PentaMiniSessions.Add(session); await db.SaveChangesAsync();
            }
            var draft = Guid.NewGuid(); var route = $"/api/academies/{academy}/penta/onboarding/conversations/{session.Id}";
            using var save = await client.PutAsJsonAsync(route + $"/drafts/{draft}", new { expectedRevision = 0, contextVersion = 0,
                fields = new Dictionary<string, object> { ["StudentFirstName"] = new { kind = "text", value = "SQL-REVIEW-ONLY" } } });
            if ((int)save.StatusCode != 201) throw new InvalidOperationException("Review browser draft fixture failed: HTTP " + (int)save.StatusCode);
            var saved = await save.Content.ReadFromJsonAsync<JsonElement>(); var hash = saved.GetProperty("payloadHash").GetString();
            using (var read = await client.GetAsync(route + $"/drafts/{draft}")) {
                if ((int)read.StatusCode != 200) throw new InvalidOperationException("Review fixture expiry read failed.");
                var value = await read.Content.ReadFromJsonAsync<JsonElement>();
                var expiry = value.GetProperty("expiresAtUtc").GetString();
                using var scope = factory.Services.CreateScope();
                var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PentaOnboardingDrafts
                    .AsNoTracking().Where(x => x.Id == draft).Select(x => x.ExpiresAtUtc).SingleAsync();
                if (stored.Kind != DateTimeKind.Unspecified || expiry is null || !expiry.EndsWith('Z') ||
                    !DateTimeOffset.TryParse(expiry, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var timestamp) || timestamp.UtcTicks != stored.Ticks)
                    throw new InvalidOperationException("Fresh SQL draft expiry lost explicit UTC or shifted persisted ticks.");
            }
            using var select = await client.PostAsJsonAsync(route + "/selection", new { requestId = Guid.NewGuid(), expectedVersion = 0,
                draftId = draft, draftRevision = 1, payloadHash = hash });
            if ((int)select.StatusCode != 200) throw new InvalidOperationException("Review browser selection fixture failed.");
            if (pending) {
                using var claim = await client.PostAsJsonAsync(route + "/claims", new { requestId = Guid.NewGuid(), expectedVersion = 1,
                    draftRevision = 1, payloadHash = hash, text = "Synthetic unresolved claim; never interpreted" });
                if ((int)claim.StatusCode != 202) throw new InvalidOperationException("Review browser pending fixture failed.");
            }
            return new { conversation = session.Id, draft };
        }
        var selected = await Fixture(false); var pending = await Fixture(true);
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                sessions = await db.PentaMiniSessions.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                turns = await db.PentaMiniTurns.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                userRoles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync(),
                academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            }); // In memory only; no private payloads/Identity values logged or saved.
        }
        var initial = await Snapshot(); Guid[] beforeAudits;
        using (var scope = factory.Services.CreateScope()) beforeAudits = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>()
            .AuditLogs.Select(x => x.Id).ToArrayAsync();
        // New factory has an independent unmodified production limiter. No pacing/limit override.
        client.DefaultRequestHeaders.Authorization = null;
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build();
        bridge.Run(async context => {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            // Browser cannot send login, draft, selection, claim, approval or domain writes through this relay.
            var path = context.Request.Path.Value ?? "";
            if (context.Request.Method != "GET" || !(path == "/api/auth/session" ||
                path.StartsWith("/api/academies/", StringComparison.Ordinal) && path.Contains("/penta/onboarding/conversations/", StringComparison.Ordinal) &&
                (path.EndsWith("/current", StringComparison.Ordinal) || path.Contains("/drafts/", StringComparison.Ordinal) ||
                 System.Text.RegularExpressions.Regex.IsMatch(path, @"/claims/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/result$")))) { context.Response.StatusCode = 405; return; }
            using var request = new HttpRequestMessage(HttpMethod.Get, context.Request.Path + context.Request.QueryString);
            if (context.Request.Headers.TryGetValue("Authorization", out var authorization)) request.Headers.TryAddWithoutValidation("Authorization", authorization.ToArray());
            using var response = await client.SendAsync(request, context.RequestAborted);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-review-live.cjs"));
        start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(manifest.RunId.ToString("N"));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Review browser did not start.");
        // Synthetic real bearer tokens go through stdin only, never command line/evidence/console.
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new { selected, pending, tokens })); process.StandardInput.Close();
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        Console.Write(await stdout); var error = await stderr;
        if (process.ExitCode != 0) throw new InvalidOperationException("SQL review browser failed: " + error);
        if (initial != await Snapshot()) throw new InvalidOperationException("Passive review browser mutated domain, Identity or operational state.");
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audits = await db.AuditLogs.AsNoTracking().Where(x => !beforeAudits.Contains(x.Id)).ToArrayAsync();
            if (audits.Length != 32 || audits.Count(x => x.Action == "PentaOnboardingConversationRead") != 22 ||
                audits.Count(x => x.Action == "PentaOnboardingDraftRead") != 9 ||
                audits.Count(x => x.Action == "PentaOnboardingConversationResultRead") != 1 ||
                audits.Any(x => x.AcademyId != academy || x.ActorUserId != actor ||
                x.Action is not ("PentaOnboardingConversationRead" or "PentaOnboardingDraftRead" or "PentaOnboardingConversationResultRead")))
                throw new InvalidOperationException("Passive review browser exact authoritative read audits differ.");
        }
        var evidence = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", "penta-onboarding-review-live-" + manifest.RunId.ToString("N")));
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "sql-postconditions.json"), JsonSerializer.Serialize(new {
            run = manifest.RunId.ToString("N"), passed = true, browserScenarios = 15, exactReadAudits = 32,
            conversationReadAudits = 22, draftReadAudits = 9, pendingResultReadAudits = 1, actorAndTenantMatched = true,
            domainAndIdentityUnchanged = true, draftsSessionsAndTurnsUnchanged = true,
            interpreterConfigured = false, browserWrites = 0, physicalDevices = "NOT RUN"
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: PENTAONBOARDINGREVIEW 15 real authenticated SQL/browser scenarios;32 exact actor/tenant read audits including explicit pending recovery; unchanged domain/Identity/drafts/sessions/turns; no interpreter or browser writes/429.");
        if (Environment.GetEnvironmentVariable("QA_ONBOARDING_SESSION_BROWSER") == "1") {
            // Additive epoch/replacement evidence, AFTER the unchanged32-audit
            // predecessor assertion. The read-only relay still forbids all writes.
            Guid[] prior;
            using (var scope = factory.Services.CreateScope()) prior = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.Select(x => x.Id).ToArrayAsync();
            var extraStart = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            extraStart.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-onboarding-session-live.cjs"));
            extraStart.ArgumentList.Add(bridge.Urls.Single()); extraStart.ArgumentList.Add(manifest.RunId.ToString("N"));
            using var extra = Process.Start(extraStart) ?? throw new InvalidOperationException("Session ownership browser did not start.");
            await extra.StandardInput.WriteAsync(JsonSerializer.Serialize(new { selected, pending, tokens })); extra.StandardInput.Close();
            var extraOutput = extra.StandardOutput.ReadToEndAsync(); var extraError = extra.StandardError.ReadToEndAsync();
            try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2)); await extra.WaitForExitAsync(timeout.Token); }
            catch { if (!extra.HasExited) extra.Kill(entireProcessTree: true); throw; }
            Console.Write(await extraOutput);
            if (extra.ExitCode != 0) throw new InvalidOperationException("Session ownership browser failed: " + await extraError);
            if (initial != await Snapshot()) throw new InvalidOperationException("Session ownership browser changed domain/Identity/operational state.");
            using var verification = factory.Services.CreateScope();
            var audits = await verification.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.AsNoTracking().Where(x => !prior.Contains(x.Id)).ToArrayAsync();
            if (audits.Length != 11 || audits.Count(x => x.Action == "PentaOnboardingConversationRead") != 8 ||
                audits.Count(x => x.Action == "PentaOnboardingDraftRead") != 3 ||
                audits.Any(x => x.AcademyId != academy || x.ActorUserId != actor || x.Action is not ("PentaOnboardingConversationRead" or "PentaOnboardingDraftRead")))
                throw new InvalidOperationException("Session ownership exact authoritative read audits differ.");
            var extraEvidence = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", "penta-onboarding-session-live-" + manifest.RunId.ToString("N")));
            await File.WriteAllTextAsync(Path.Combine(extraEvidence, "sql-postconditions.json"), JsonSerializer.Serialize(new {
                run = manifest.RunId.ToString("N"), passed = true, browserScenarios = 6, nativeGets = 17, exactReadAudits = 11,
                conversationReadAudits = 8, draftReadAudits = 3, actorAndTenantMatched = true,
                domainIdentityAndOperationalStateUnchanged = true, modelInference = false, browserWrites = 0
            }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("PASS: PENTAONBOARDINGSESSION 6 native SQL/exported-browser replacement cases;17 GETs/11exact scoped read audits;no state mutation/model/replay/429.");
        }
        await bridge.StopAsync();
    }
}
