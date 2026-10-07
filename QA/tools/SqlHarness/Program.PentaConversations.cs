using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaConversationsAsync(QaRunManifest manifest, Guid academyA, Guid academyB,
        string tokenA, string tokenC, string tokenB, string teacher, string platform)
    {
        var settings = new Dictionary<string, string?>
        { ["Penta:Enabled"] = "true", ["Penta:ConversationContractsEnabled"] = "true" };
        using var factory = new QaApiFactory(manifest, settings);
        using var adminA = factory.CreateClient();
        using var adminC = factory.CreateClient();
        using var other = factory.CreateClient();
        PentaRequire(factory.PreflightPassed, "Conversation SQL host missed owned-target preflight.");
        adminA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        adminC.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
        var path = $"/api/academies/{academyA}/penta/conversations";
        async Task<PentaConversationSummary> Create()
        {
            using var response = await adminA.PostAsJsonAsync(path, new { requestId = Guid.NewGuid() });
            PentaRequire(response.StatusCode == HttpStatusCode.Created, "Conversation creation was not 201.");
            return (await response.Content.ReadFromJsonAsync<PentaConversationSummary>())!;
        }
        PentaConversationTurnRequest Turn() => new(Guid.NewGuid(), 0, PentaConversationContract.FirstInput, "executor");
        var privateConversation = await Create();
        var privatePath = $"{path}/{privateConversation.ConversationId}";
        using (var denied = await adminC.GetAsync(privatePath))
            PentaRequire(denied.StatusCode == HttpStatusCode.NotFound, "A second admin saw private conversation history.");
        foreach (var credential in new[] { tokenB, teacher, platform })
        {
            other.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credential);
            using var denied = await other.GetAsync(privatePath);
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden, "Role/tenant conversation read was not denied.");
        }
        other.DefaultRequestHeaders.Authorization = null;
        using (var denied = await other.GetAsync(privatePath))
            PentaRequire(denied.StatusCode == HttpStatusCode.Unauthorized, "Anonymous history read was not denied.");
        using (var disabledFactory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }))
        using (var disabled = disabledFactory.CreateClient())
        {
            disabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            using var denied = await disabled.GetAsync(privatePath);
            PentaRequire(denied.StatusCode == HttpStatusCode.NotFound, "Conversation flag defaulted on.");
        }

        // Two distinct requests against the same version: exactly one accepted.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var conversation = await Create();
            var turnPath = $"{path}/{conversation.ConversationId}/turns";
            var requests = new[] { Turn(), Turn() };
            var responses = await Task.WhenAll(requests.Select(request => adminA.PostAsJsonAsync(turnPath, request)));
            try
            {
                var statuses = responses.Select(x => (int)x.StatusCode).OrderBy(x => x).ToArray();
                PentaRequire(statuses.SequenceEqual(new[] { 201, 409 }), "Stale-tab pair was not exactly 201/409 (or was throttled).");
                await using var scope = factory.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                PentaRequire(await db.PentaConversationTurns.CountAsync(x => x.ConversationId == conversation.ConversationId) == 1 &&
                    await db.PentaConversationMessages.CountAsync(x => x.ConversationId == conversation.ConversationId) == 2 &&
                    (await db.PentaConversations.SingleAsync(x => x.Id == conversation.ConversationId)).ContextVersion == 1,
                    "Concurrent stale turns stored duplicate messages or lost context.");
                Console.WriteLine($"PENTA CONVERSATION RACE {attempt}/5 PASS: statuses=201,409 version=1 turns=1 messages=2 no429.");
            }
            finally { foreach (var response in responses) response.Dispose(); }
        }
        var replayConversation = await Create();
        var replayPath = $"{path}/{replayConversation.ConversationId}/turns";
        var same = Turn();
        var twins = await Task.WhenAll(adminA.PostAsJsonAsync(replayPath, same), adminA.PostAsJsonAsync(replayPath, same));
        Guid turnId;
        try
        {
            PentaRequire(twins.Select(x => (int)x.StatusCode).OrderBy(x => x).SequenceEqual(new[] { 200, 201 }),
                "Concurrent identical requests did not return 201/200.");
            var receipts = await Task.WhenAll(twins.Select(x => x.Content.ReadFromJsonAsync<PentaConversationTurnReceipt>()));
            turnId = receipts[0]!.TurnId;
            PentaRequire(receipts[1]!.TurnId == turnId, "Replay changed the stored turn identity.");
        }
        finally { foreach (var response in twins) response.Dispose(); }
        using (var restartedFactory = new QaApiFactory(manifest, settings))
        using (var restarted = restartedFactory.CreateClient())
        {
            restarted.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            using var replay = await restarted.PostAsJsonAsync(replayPath, same);
            PentaRequire(replay.StatusCode == HttpStatusCode.OK &&
                (await replay.Content.ReadFromJsonAsync<PentaConversationTurnReceipt>())!.TurnId == turnId,
                "Restart lost conversation request replay.");
        }
        using (var changed = await adminA.PostAsJsonAsync(replayPath, same with { Capability = "twin" }))
            PentaRequire(changed.StatusCode == HttpStatusCode.Conflict, "Changed arguments reused a turn ID.");
        using (var realPrompt = await adminA.PostAsJsonAsync(replayPath, Turn() with { Text = "Show real student details" }))
            PentaRequire(realPrompt.StatusCode == HttpStatusCode.BadRequest, "C1a stored arbitrary customer prompt content.");

        Guid actorA, actorC;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            actorA = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
            actorC = (await users.FindByEmailAsync("penta-admin-c@example.invalid"))!.Id;
        }
        // The composite FK also prevents a DB writer from attaching a different
        // actor or academy to an existing conversation (beyond HTTP filtering).
        foreach (var (academy, actor) in new[] { (academyA, actorC), (academyB, actorA) })
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.PentaConversationTurns.Add(new PentaConversationTurn
            {
                AcademyId = academy, ActorUserId = actor, ConversationId = privateConversation.ConversationId,
                RequestId = Guid.NewGuid(), Capability = "executor", InputDigest = new string('0', 64),
                ExpectedContextVersion = 0, CompletedContextVersion = 1
            });
            var denied = false;
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 547 })
            { denied = true; }
            PentaRequire(denied, "Conversation/actor/academy FK allowed a foreign turn.");
        }
        // Required audit failure must roll back both new history and version.
        using (var faultFactory = new QaApiFactory(manifest, settings,
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var init = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Conversation audit-fault host missed preflight.");
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorA.ToString())], "QA"));
            foreach (var creating in new[] { true, false })
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<PentaConversationService>();
                var failed = false;
                try
                {
                    if (creating) await service.CreateAsync(principal, academyA,
                        new PentaConversationCreateRequest(Guid.NewGuid()), CancellationToken.None);
                    else await service.AppendAsync(principal, academyA, privateConversation.ConversationId, Turn(), CancellationToken.None);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
                { failed = true; }
                PentaRequire(failed, "Conversation audit fault did not fail the operation.");
            }
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.PentaConversations.CountAsync(x => x.AcademyId == academyA) == 7 &&
                await db.PentaConversationTurns.CountAsync(x => x.AcademyId == academyA) == 6 &&
                await db.PentaConversationMessages.CountAsync(x => x.AcademyId == academyA) == 12 &&
                (await db.PentaConversations.SingleAsync(x => x.Id == privateConversation.ConversationId)).ContextVersion == 0,
                "Audit rollback or replay left duplicate/partial conversation rows.");
        }
        // Current role revocation denies history even with the old valid token.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var actor = (await users.FindByIdAsync(actorA.ToString()))!;
            PentaRequire((await users.RemoveFromRoleAsync(actor, "AcademyAdmin")).Succeeded, "Could not revoke conversation fixture role.");
        }
        using (var revoked = await adminA.GetAsync(privatePath))
            PentaRequire(revoked.StatusCode == HttpStatusCode.Forbidden, "Revoked actor retained private conversation access.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            PentaRequire((await users.AddToRoleAsync((await users.FindByIdAsync(actorA.ToString()))!, "AcademyAdmin")).Succeeded,
                "Could not restore synthetic fixture role.");
        }
        Console.WriteLine("PENTA CONVERSATION PASS: private actor/tenant/role, flags, 5/5 stale-tab races, identical replay/restart, scoped FKs and create/turn audit rollback. Synthetic-only; no live model/domain operation.");
    }
}
