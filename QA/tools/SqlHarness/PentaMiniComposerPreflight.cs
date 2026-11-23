using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record MiniComposerState(long Version, Guid? Pending, string ProtectedState,
        string TurnStatus, string? ProtectedReceipt, int Turns, int RequestAudits);

    private static async Task VerifyMiniComposerPreflightAsync(QaRunManifest manifest, Guid academyA,
        string tokenA, string tokenC, string tokenB)
    {
        const string code = "QA-C54-RECEIPT";
        var planner = new MiniFaultProvider
        {
            Next = new("0.1", "TOOL_REQUEST", "Request prepared.",
                new("SearchLearners", new() { ["name"] = JsonSerializer.SerializeToElement(code) }), "READ", MiniState.Empty),
            Entered = new(TaskCreationOptions.RunContinuationsAsynchronously),
            Release = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var auditFault = new MiniAuditFault();
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = "true"
        }, isolatedMiniProvider: planner, isolatedDomainInterceptor: auditFault);
        using var owner = factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        PentaRequire(factory.PreflightPassed, "Composer preflight missed disposable SQL validation.");

        var learner = new Student { AcademyId = academyA, FirstName = "Synthetic", LastName = "Composer", StudentNumber = code };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(row => row.Id == academyA)).EnabledModulesJson = "[\"Core\",\"Finance\"]";
            var invoice = new Invoice { AcademyId = academyA, StudentId = learner.Id, InvoiceNumber = code, TotalAmount = 123.45m, Currency = "INR" };
            db.AddRange(learner, invoice, new Payment { AcademyId = academyA, InvoiceId = invoice.Id, Amount = 123.44m, Currency = "INR" });
            await db.SaveChangesAsync();
        }

        var before = await FinanceSnapshotAsync();
        var root = $"/api/academies/{academyA:D}/penta/chat/conversations";
        using var created = await owner.PostAsync(root, null);
        RequireFinanceStatus(created, HttpStatusCode.Created, "composer preflight start");
        var session = (await created.Content.ReadFromJsonAsync<MiniSessionView>())!;
        var turnRoot = $"{root}/{session.ConversationId:D}/turns";
        var input = new MiniTurnInput(Guid.NewGuid(), session.Version, "Check the synthetic receipt.", "executor");

        var receiptEndpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.AsType() == typeof(PentaChatController) &&
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET") == true &&
                new[] { "academyId", "conversationId", "requestId" }.All(name =>
                    endpoint.RoutePattern.Parameters.Any(parameter => parameter.Name == name)))
            .ToArray();
        PentaRequire(receiptEndpoints.Length <= 1, "More than one Mini receipt route matched.");
        var receiptUrl = $"{turnRoot}/{input.RequestId:D}";
        if (receiptEndpoints.SingleOrDefault()?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } receiptAction)
        {
            receiptUrl = factory.Services.GetRequiredService<LinkGenerator>().GetPathByAction(
                receiptAction.ActionName, receiptAction.ControllerName,
                new { academyId = academyA, conversationId = session.ConversationId, requestId = input.RequestId, expectedVersion = input.ExpectedVersion })
                ?? throw new InvalidOperationException("Mini receipt route could not be generated.");
        }

        using var post = new HttpRequestMessage(HttpMethod.Post, turnRoot) { Content = JsonContent.Create(input) };
        var sending = owner.SendAsync(post, HttpCompletionOption.ResponseHeadersRead);
        try
        {
            await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var pending = await ReadTurnAsync();
            PentaRequire(pending.Version == session.Version && pending.Pending == input.RequestId && pending.Turns == 1 &&
                pending.TurnStatus == "Pending" && pending.ProtectedReceipt is null && planner.Calls == 1,
                "Composer pending turn did not retain exactly one durable claim.");
            using var pendingRead = await owner.GetAsync(receiptUrl);
            PentaRequire(!pendingRead.IsSuccessStatusCode && await ReadTurnAsync() == pending && planner.Calls == 1,
                "GET exposed a pending result or changed its durable claim.");
            if (receiptEndpoints.Length == 1)
                RequireFinanceStatus(pendingRead, HttpStatusCode.Conflict, "pending receipt recovery");
            Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT pending receipt withheld without dispatch PASS");
        }
        finally
        {
            planner.Release.TrySetResult(true);
        }

        // Treat the response body as lost. The caller never consumes it or repeats the POST.
        using (var discarded = await sending)
            RequireFinanceStatus(discarded, HttpStatusCode.Created, "composer preflight committed send");
        var committed = await ReadTurnAsync();
        PentaRequire(committed.Version == session.Version + 1 && committed.Pending is null && committed.Turns == 1 &&
            committed.TurnStatus == "RESULT" && committed.ProtectedReceipt is not null &&
            committed.RequestAudits == 2 && planner.Calls == 1, "Discarded response did not leave one completed, audited turn.");
        Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT discarded reply left one audited SQL receipt PASS");

        using var recovered = await owner.GetAsync(receiptUrl);
        var after = await ReadTurnAsync();
        if (receiptEndpoints.Length == 0)
        {
            PentaRequire(recovered.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed &&
                committed == after, "Baseline GET unexpectedly exposed or changed a receipt.");
            Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT GET recovery unavailable; C54 acceptance BLOCKED");
        }
        else
        {
            RequireFinanceStatus(recovered, HttpStatusCode.OK, "composer read-only recovery");
            var receipt = (await recovered.Content.ReadFromJsonAsync<MiniReceipt>())!;
            PentaRequire(recovered.Headers.CacheControl?.NoStore == true && receipt.ConversationId == session.ConversationId &&
                receipt.RequestId == input.RequestId && receipt.Version == committed.Version && receipt.Kind == "RESULT" &&
                receipt.Result is { Count: 1, Rows.Length: 1 } && receipt.Result.Rows[0].SourceId == learner.Id &&
                receipt.Result.Rows[0].Balances.Single() == new AcademyDesk.Api.Infrastructure.LearnerBalance("INR", 0.01m) &&
                after == committed with { RequestAudits = committed.RequestAudits + 1 },
                "GET returned incorrect data or changed the turn outside its audit.");
            using var visitor = factory.CreateClient();
            using (var unauthenticated = await visitor.GetAsync(receiptUrl))
                RequireFinanceStatus(unauthenticated, HttpStatusCode.Unauthorized, "anonymous receipt recovery");
            foreach (var other in new[] { tokenC, tokenB })
            {
                visitor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", other);
                using var denied = await visitor.GetAsync(receiptUrl);
                PentaRequire(denied.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound,
                    "GET exposed a receipt to a different actor or academy.");
            }
            Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT exact GET receipt and actor scope PASS; browser acceptance still required");

            var recoverable = await ReadTurnAsync();
            using (var unknown = await owner.GetAsync($"{turnRoot}/{Guid.NewGuid():D}"))
                RequireFinanceStatus(unknown, HttpStatusCode.NotFound, "unknown receipt recovery");
            await DeniedMutationAsync("expired", (s, _, _, _) => s.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1), HttpStatusCode.Gone);
            await DeniedMutationAsync("newer version", (s, _, _, _) => s.Version++, HttpStatusCode.Conflict);
            await DeniedMutationAsync("pending newer request", (s, _, _, _) => s.PendingRequestId = Guid.NewGuid(), HttpStatusCode.Conflict);
            await DeniedMutationAsync("onboarding purpose", (s, _, _, _) => s.ProtectedOnboardingState = "synthetic-purpose", HttpStatusCode.Conflict);
            await DeniedMutationAsync("tampered receipt", (_, t, _, _) => t.ProtectedReceipt = "synthetic-corruption", HttpStatusCode.Conflict);
            await DeniedMutationAsync("inactive learner", (_, _, s, _) => s.IsActive = false, HttpStatusCode.Conflict);
            await DeniedMutationAsync("revoked Finance module", (_, _, _, a) => a.EnabledModulesJson = "[\"Core\"]", HttpStatusCode.Forbidden);
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var actor = await db.PentaMiniSessions.Where(row => row.Id == session.ConversationId).Select(row => row.ActorUserId).SingleAsync();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = (await users.FindByIdAsync(actor.ToString("D")))!;
                var roles = (await users.GetRolesAsync(user)).Where(role => role is "Owner" or "AcademyAdmin").ToArray();
                PentaRequire(roles.Length > 0 && user.IsActive, "Receipt actor lacked its expected active role.");
                try
                {
                    PentaRequire((await users.RemoveFromRolesAsync(user, roles)).Succeeded, "QA role revocation failed.");
                    using var denied = await owner.GetAsync(receiptUrl);
                    RequireFinanceStatus(denied, HttpStatusCode.Forbidden, "current role revocation recovery");
                }
                finally { PentaRequire((await users.AddToRolesAsync(user, roles)).Succeeded, "QA role restoration failed."); }
                try
                {
                    user.IsActive = false;
                    PentaRequire((await users.UpdateAsync(user)).Succeeded, "QA account deactivation failed.");
                    using var denied = await owner.GetAsync(receiptUrl);
                    PentaRequire(denied.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden, "Inactive actor recovered a receipt.");
                }
                finally
                {
                    user.IsActive = true;
                    PentaRequire((await users.UpdateAsync(user)).Succeeded, "QA account restoration failed.");
                }
            }
            PentaRequire(await ReadTurnAsync() == recoverable && planner.Calls == 1,
                "Rejected recovery changed private state, audit or planner count.");

            auditFault.Enabled = true;
            try
            {
                using var failed = await owner.GetAsync(receiptUrl);
                RequireFinanceStatus(failed, HttpStatusCode.ServiceUnavailable, "receipt replay audit failure");
                var body = await failed.Content.ReadAsStringAsync();
                PentaRequire(!body.Contains(code, StringComparison.Ordinal) && !body.Contains(learner.Id.ToString("D"), StringComparison.OrdinalIgnoreCase),
                    "Audit failure leaked a receipt.");
            }
            finally { auditFault.Enabled = false; }
            PentaRequire(await ReadTurnAsync() == recoverable && planner.Calls == 1,
                "Failed recovery audit committed state or repeated the planner.");
            using (var health = await owner.GetAsync($"/api/academies/{academyA:D}/penta/chat/health"))
            {
                RequireFinanceStatus(health, HttpStatusCode.OK, "unavailable provider readiness");
                using var data = JsonDocument.Parse(await health.Content.ReadAsStringAsync());
                PentaRequire(data.RootElement.GetProperty("status").GetString() == "Unavailable", "QA provider was unexpectedly available.");
            }
            using (var finalRecovery = await owner.GetAsync(receiptUrl))
                RequireFinanceStatus(finalRecovery, HttpStatusCode.OK, "recovery after audit repair during provider downtime");
            PentaRequire(await ReadTurnAsync() == recoverable with { RequestAudits = recoverable.RequestAudits + 1 } && planner.Calls == 1,
                "Repaired recovery changed more than its replay audit.");
            Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT SQL denial boundaries, audit rollback and provider-down recovery PASS");

            async Task DeniedMutationAsync(string name, Action<PentaMiniSession, PentaMiniTurn, Student, Academy> mutate, HttpStatusCode expected)
            {
                await using var scope = factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var storedSession = await db.PentaMiniSessions.SingleAsync(row => row.Id == session.ConversationId);
                var storedTurn = await db.PentaMiniTurns.SingleAsync(row => row.SessionId == session.ConversationId && row.RequestId == input.RequestId);
                var storedLearner = await db.Students.SingleAsync(row => row.Id == learner.Id);
                var storedAcademy = await db.Academies.SingleAsync(row => row.Id == academyA);
                var entries = new object[] { storedSession, storedTurn, storedLearner, storedAcademy }.Select(db.Entry).ToArray();
                var original = entries.Select(entry => entry.CurrentValues.Clone()).ToArray();
                try
                {
                    mutate(storedSession, storedTurn, storedLearner, storedAcademy);
                    await db.SaveChangesAsync();
                    using var denied = await owner.GetAsync(receiptUrl);
                    RequireFinanceStatus(denied, expected, name + " receipt recovery");
                    var body = await denied.Content.ReadAsStringAsync();
                    PentaRequire(!body.Contains(code, StringComparison.Ordinal) && !body.Contains(learner.Id.ToString("D"), StringComparison.OrdinalIgnoreCase),
                        "Denied recovery leaked a receipt.");
                }
                finally
                {
                    for (var index = 0; index < entries.Length; index++) entries[index].CurrentValues.SetValues(original[index]);
                    await db.SaveChangesAsync();
                }
                PentaRequire(await ReadTurnAsync() == recoverable && planner.Calls == 1, name + " recovery changed state or dispatched planning.");
            }
        }

        PentaRequire(planner.Calls == 1 && before == await FinanceSnapshotAsync(),
            "Receipt preflight repeated planning or changed financial data.");
        Console.WriteLine("PENTA MINI COMPOSER PREFLIGHT protected baseline PASS");

        async Task<(int Invoices, decimal Gross, int Payments, decimal Collected)> FinanceSnapshotAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return (await db.Invoices.CountAsync(), await db.Invoices.SumAsync(row => row.TotalAmount),
                await db.Payments.CountAsync(), await db.Payments.SumAsync(row => row.Amount));
        }

        async Task<MiniComposerState> ReadTurnAsync()
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var current = await db.PentaMiniSessions.AsNoTracking().SingleAsync(row => row.Id == session.ConversationId);
            var rows = db.PentaMiniTurns.AsNoTracking().Where(row => row.AcademyId == academyA &&
                row.ActorUserId == current.ActorUserId && row.SessionId == session.ConversationId);
            var turn = await rows.SingleAsync(row => row.RequestId == input.RequestId);
            var request = input.RequestId.ToString("D");
            var audits = await db.AuditLogs.CountAsync(row => row.AcademyId == academyA && row.ActorUserId == current.ActorUserId &&
                row.EntityId == session.ConversationId && row.MetadataJson != null && row.MetadataJson.Contains(request));
            return new(current.Version, current.PendingRequestId, current.ProtectedState, turn.Status, turn.ProtectedReceipt,
                await rows.CountAsync(), audits);
        }
    }
}
