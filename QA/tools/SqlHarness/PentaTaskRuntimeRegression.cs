using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Explicit synthetic QA provider; never registered by application composition.
    private sealed class TaskQaPlanner : IPentaTaskPlanner
    {
        internal int Calls;
        internal string Kind = "create";
        internal TaskCompletionSource<bool>? Entered, Release;
        public async Task<PentaTaskPlannerOutcome> PlanAsync(JsonElement request, CancellationToken token)
        {
            Calls++; Entered?.TrySetResult(true);
            if (Release is { } release) await release.Task.WaitAsync(token);
            if (Kind == "unavailable") return new(null, "Unavailable");
            object Field(string kind, object value) => new { kind, value };
            object Step(string id, string tool, object fields, object bindings) => new {
                id, tool_key = tool, tool_revision = 1, fields, bindings, depends_on = Array.Empty<string>() };
            var empty = new Dictionary<string, object>();
            object? plan = Kind == "create" ? new { contract = PentaTaskPlanning.Contract, base_revision = 0,
                steps = new[] {
                    Step("student", PentaOnboardingFields.Tool, new {
                        StudentFirstName = Field("text", "Synthetic"), StudentLastName = Field("text", "TaskLearner"),
                        DateOfBirth = Field("date", "2000-01-01") }, empty),
                    Step("batch", PentaTaskCatalog.BatchCreate, new {
                        Name = Field("text", "Synthetic Task Batch"), Capacity = Field("integer", 8),
                        DeliveryMode = Field("text", "Online") }, empty),
                    Step("enroll", PentaEnrollmentFields.CreateTool, empty, new {
                        StudentId = new { step_id = "student", output = "StudentId" },
                        BatchId = new { step_id = "batch", output = "BatchId" } }) } } : null;
            object[] edits = Kind == "edit" ? [new { step_id = "student",
                set = new { StudentLastName = Field("text", "CorrectedTaskLearner") }, remove = Array.Empty<string>() }] : [];
            return new(JsonSerializer.SerializeToElement(new {
                contract = PentaTaskDialogue.Contract, request_ref = request.GetProperty("request_ref").GetString(),
                kind = Kind, plan, edits }));
        }
    }
    private sealed class TaskAuditFailure : DbCommandInterceptor
    {
        internal bool Enabled;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken token = default)
        {
            if (Enabled && command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Synthetic task audit failure.");
            return base.ReaderExecutingAsync(command, eventData, result, token);
        }
    }

    private static async Task VerifyPentaTaskRuntimeAsync(QaRunManifest manifest, Guid academy,
        string actorToken, string otherActorToken, string foreignToken, string teacherToken)
    {
        var planner = new TaskQaPlanner(); var fault = new TaskAuditFailure();
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Tasks:Enabled"] = "true", ["Penta:Mini:Enabled"] = "true" },
            isolatedTaskPlanner: planner, isolatedDomainInterceptor: fault);
        using var actor = factory.CreateClient();
        PentaRequire(factory.PreflightPassed, "Task runtime missed owned SQL guard.");
        // Keep this scenario's genuine per-actor conversation budget independent
        // of the existing host-security packet. Never raise/reset product quotas.
        var address = $"task-owner-{Guid.NewGuid():N}@example.invalid";
        var password = "Qa!Task7" + Guid.NewGuid().ToString("N");
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var owner = new ApplicationUser { UserName = address, Email = address, EmailConfirmed = true,
                DisplayName = "Synthetic task owner", AcademyId = academy, IsActive = true };
            PentaRequire((await users.CreateAsync(owner, password)).Succeeded, "Task owner creation failed.");
            PentaRequire((await users.AddToRoleAsync(owner, "AcademyAdmin")).Succeeded, "Task owner role failed.");
        }
        actorToken = await LoginAsync(actor, address, password);
        actor.DefaultRequestHeaders.Authorization = new("Bearer", actorToken);
        var root = $"/api/academies/{academy:D}/penta/chat/tasks";
        string modules;
        await using (var scope = factory.Services.CreateAsyncScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.Academies.SingleAsync(x => x.Id == academy);
            modules = row.EnabledModulesJson; row.EnabledModulesJson = "[\"Core\",\"Finance\"]"; await db.SaveChangesAsync();
        }
        var checks = 0;
        async Task Check(bool passed, string message) { PentaRequire(passed, message); checks++; await Task.CompletedTask; }
        async Task<MiniSessionView> Create() {
            using var response = await actor.PostAsync(root + "/conversations", null);
            RequireFinanceStatus(response, HttpStatusCode.Created, "task conversation creation");
            return (await response.Content.ReadFromJsonAsync<MiniSessionView>())!;
        }
        string Turns(MiniSessionView session) => root + $"/conversations/{session.ConversationId:D}/turns";
        async Task<string> Snapshot() {
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.CountAsync(), batches = await db.Batches.CountAsync(),
                enrollments = await db.Enrollments.CountAsync(), invoices = await db.Invoices.CountAsync(), payments = await db.Payments.CountAsync(),
                totals = await db.Payments.GroupBy(x => x.Currency).OrderBy(x => x.Key).Select(x => new { Currency = x.Key, Amount = x.Sum(p => p.Amount) }).ToArrayAsync() });
        }
        async Task<(long Version, Guid? Pending, string State, string? Receipt, int Turns)> Stored(MiniSessionView session, Guid request) {
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var s = await db.PentaMiniSessions.AsNoTracking().SingleAsync(x => x.Id == session.ConversationId);
            var t = await db.PentaMiniTurns.AsNoTracking().SingleOrDefaultAsync(x => x.SessionId == s.Id && x.RequestId == request);
            return (s.Version, s.PendingRequestId, s.ProtectedState, t?.ProtectedReceipt,
                await db.PentaMiniTurns.CountAsync(x => x.SessionId == s.Id));
        }
        try
        {
            var before = await Snapshot();
            using (var catalog = await actor.GetAsync(root + "/catalog")) {
                RequireFinanceStatus(catalog, HttpStatusCode.OK, "task catalog");
                var data = await catalog.Content.ReadFromJsonAsync<JsonElement>();
                await Check(data.GetProperty("tools").GetArrayLength() == 9 && !data.GetProperty("executionEnabled").GetBoolean(), "Catalog mismatch or execution exposed.");
            }
            var session = await Create(); var input = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Synthetic compound task request.");
            using (var response = await actor.PostAsJsonAsync(Turns(session), input)) {
                RequireFinanceStatus(response, HttpStatusCode.Created, "task draft turn");
                var receipt = (await response.Content.ReadFromJsonAsync<PentaTaskTurnReceipt>())!;
                await Check(receipt.Kind == "DRAFT" && receipt.Tasks.GetArrayLength() == 3 && !receipt.ExecutionEnabled, "Compound task draft not retained.");
            }
            var stored = await Stored(session, input.RequestId);
            await Check(stored is { Version: 1, Pending: null, Turns: 1 } && stored.Receipt is not null &&
                !stored.State.Contains("TaskLearner", StringComparison.Ordinal) && !stored.Receipt.Contains("TaskLearner", StringComparison.Ordinal), "SQL state/receipt not atomically encrypted.");
            using (var recovered = await actor.GetAsync(Turns(session) + $"/{input.RequestId:D}")) {
                RequireFinanceStatus(recovered, HttpStatusCode.OK, "task GET recovery");
                await Check(recovered.Headers.CacheControl?.NoStore == true && planner.Calls == 1, "Recovery redispatched or lost no-store.");
            }
            fault.Enabled = true;
            try { using var deniedAudit = await actor.GetAsync(Turns(session) + $"/{input.RequestId:D}");
                RequireFinanceStatus(deniedAudit, HttpStatusCode.ServiceUnavailable, "mandatory task read audit"); }
            finally { fault.Enabled = false; }
            await Check(planner.Calls == 1 && await Stored(session, input.RequestId) == stored, "Failed recovery audit altered state or redispatched.");
            using (var legacy = await actor.PostAsJsonAsync($"/api/academies/{academy:D}/penta/chat/conversations/{session.ConversationId:D}/turns",
                new MiniTurnInput(Guid.NewGuid(), 1, "Synthetic legacy protocol attempt.", "executor")))
                RequireFinanceStatus(legacy, HttpStatusCode.Conflict, "task session legacy-purpose boundary");
            using (var duplicate = await actor.PostAsJsonAsync(Turns(session), input))
                RequireFinanceStatus(duplicate, HttpStatusCode.OK, "task identical replay");
            using (var conflict = await actor.PostAsJsonAsync(Turns(session), input with { Text = "different" }))
                RequireFinanceStatus(conflict, HttpStatusCode.Conflict, "task conflicting replay");
            await Check(planner.Calls == 1 && (await Stored(session, input.RequestId)).Turns == 1, "Replay duplicated claim/provider call.");
            planner.Kind = "edit";
            using (var edited = await actor.PostAsJsonAsync(Turns(session), new PentaTaskTurnInput(Guid.NewGuid(), 1, "Correct the last name."))) {
                RequireFinanceStatus(edited, HttpStatusCode.Created, "task retained edit");
                var receipt = (await edited.Content.ReadFromJsonAsync<PentaTaskTurnReceipt>())!;
                await Check(receipt.Version == 2 && receipt.Tasks.GetArrayLength() == 3 &&
                    receipt.Tasks[0].GetProperty("fields").GetProperty("StudentFirstName").GetProperty("value").GetString() == "Synthetic" &&
                    receipt.Tasks[0].GetProperty("fields").GetProperty("StudentLastName").GetProperty("value").GetString() == "CorrectedTaskLearner", "Stored draft could not roundtrip/refine safely.");
            }
            var calls = planner.Calls;
            using (var history = await actor.GetAsync(root + $"/conversations/{session.ConversationId:D}")) {
                RequireFinanceStatus(history, HttpStatusCode.OK, "protected task history");
                var data = (await history.Content.ReadFromJsonAsync<PentaTaskHistory>())!;
                await Check(data.Version == 2 && data.Turns.Select(x => x.Version).SequenceEqual(new long[] { 1, 2 }) && planner.Calls == calls,
                    "History lost chronology, scope or invoked planning.");
            }
            using (var page = await actor.GetAsync(root + $"/conversations/{session.ConversationId:D}?beforeVersion=2")) {
                RequireFinanceStatus(page, HttpStatusCode.OK, "task history pagination");
                var data = (await page.Content.ReadFromJsonAsync<PentaTaskHistory>())!;
                await Check(data.Turns.Count == 1 && data.Turns[0].Version == 1 && !data.HasMore, "History pagination returned the wrong turn.");
            }
            using var visitor = factory.CreateClient();
            foreach (var bearer in new[] { otherActorToken, foreignToken, teacherToken }) {
                visitor.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using var denied = await visitor.GetAsync(Turns(session) + $"/{input.RequestId:D}");
                await Check(denied.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden, "Another actor/role/tenant accessed task state.");
            }
            await Check(planner.Calls == calls, "Denied receipt access dispatched inference.");

            // Hold a claimed turn to test pending recovery and concurrent duplicate.
            planner.Kind = "create"; planner.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously); planner.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var concurrent = await Create(); var pendingInput = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Synthetic concurrent tasks.");
            var posting = actor.PostAsJsonAsync(Turns(concurrent), pendingInput);
            try {
                await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); var atClaim = planner.Calls;
                using var pending = await actor.GetAsync(Turns(concurrent) + $"/{pendingInput.RequestId:D}");
                using var repeated = await actor.PostAsJsonAsync(Turns(concurrent), pendingInput);
                await Check(pending.StatusCode == HttpStatusCode.Conflict && repeated.StatusCode == HttpStatusCode.Conflict && planner.Calls == atClaim,
                    "Pending request exposed a result or dispatched twice.");
            } finally { planner.Release.TrySetResult(true); }
            using (var completed = await posting) RequireFinanceStatus(completed, HttpStatusCode.Created, "claimed task completion");
            planner.Entered = planner.Release = null;

            // Mandatory completion audit failure must roll back state and receipt.
            var rollback = await Create(); var failedInput = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Synthetic rollback tasks.");
            planner.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously); planner.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var failing = actor.PostAsJsonAsync(Turns(rollback), failedInput);
            try { await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10)); fault.Enabled = true; }
            finally { planner.Release.TrySetResult(true); }
            using (var failed = await failing) RequireFinanceStatus(failed, HttpStatusCode.ServiceUnavailable, "mandatory task completion audit");
            fault.Enabled = false; planner.Entered = planner.Release = null;
            var rolled = await Stored(rollback, failedInput.RequestId);
            await Check(rolled.Version == 0 && rolled.Pending == failedInput.RequestId && rolled.Receipt is null && rolled.Turns == 1,
                "Audit failure committed an unaudited task state or receipt.");
            using (var unknown = await actor.GetAsync(Turns(rollback) + $"/{failedInput.RequestId:D}"))
                RequireFinanceStatus(unknown, HttpStatusCode.Conflict, "unknown task outcome recovery");

            foreach (var revoked in new[] { "actor", "module" })
            {
                planner.Kind = "create"; var revocation = await Create(); var revocationInput = new PentaTaskTurnInput(Guid.NewGuid(), 0, "Synthetic authority change.");
                planner.Entered = new(TaskCreationOptions.RunContinuationsAsynchronously); planner.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
                var postingRevoked = actor.PostAsJsonAsync(Turns(revocation), revocationInput);
                try {
                    await planner.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    await using var change = factory.Services.CreateAsyncScope();
                    var domain = change.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    if (revoked == "module") { (await domain.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\"]"; await domain.SaveChangesAsync(); }
                    else {
                        var actorId = await domain.PentaMiniSessions.Where(x => x.Id == revocation.ConversationId).Select(x => x.ActorUserId).SingleAsync();
                        var identities = change.ServiceProvider.GetRequiredService<IdentityDbContext>();
                        (await identities.Users.SingleAsync(x => x.Id == actorId)).IsActive = false; await identities.SaveChangesAsync();
                    }
                    planner.Release.TrySetResult(true);
                    using var denied = await postingRevoked;
                    RequireFinanceStatus(denied, HttpStatusCode.Forbidden, "current " + revoked + " revocation after planning");
                    var unchanged = await Stored(revocation, revocationInput.RequestId);
                    await Check(unchanged.Version == 0 && unchanged.Pending == revocationInput.RequestId && unchanged.Receipt is null,
                        "Revoked authority committed a draft or private receipt.");
                }
                finally {
                    planner.Release.TrySetResult(true); planner.Entered = planner.Release = null;
                    await using var restore = factory.Services.CreateAsyncScope();
                    var domain = restore.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                    (await domain.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\",\"Finance\"]";
                    await domain.SaveChangesAsync();
                    var actorId = await domain.PentaMiniSessions.Where(x => x.Id == revocation.ConversationId).Select(x => x.ActorUserId).SingleAsync();
                    var identities = restore.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    (await identities.Users.SingleAsync(x => x.Id == actorId)).IsActive = true; await identities.SaveChangesAsync();
                }
            }

            planner.Kind = "unavailable"; var unavailable = await Create();
            using (var response = await actor.PostAsJsonAsync(Turns(unavailable), new PentaTaskTurnInput(Guid.NewGuid(), 0, "Provider unavailable."))) {
                RequireFinanceStatus(response, HttpStatusCode.Created, "recoverable task provider failure");
                await Check((await response.Content.ReadFromJsonAsync<PentaTaskTurnReceipt>())?.Kind == "ERROR", "Provider outage became false success.");
            }
            await Check(await Snapshot() == before, "Planning changed student/batch/enrollment/financial records.");
            Console.WriteLine($"PENTA TASK NATIVE {checks} SQL/Identity/API checks PASS; synthetic planner, no model or domain mutation.");
        }
        finally {
            fault.Enabled = false; planner.Release?.TrySetResult(true);
            await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = modules; await db.SaveChangesAsync();
        }
    }
}
