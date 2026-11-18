using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
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
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = "true"
        }, isolatedMiniProvider: planner);
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
