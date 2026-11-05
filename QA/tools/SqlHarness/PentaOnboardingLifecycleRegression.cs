using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaOnboardingLifecycleAsync(QaApiFactory baseline, QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true", ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true" };
        var clock = new OnboardingDraftClock();
        using var factory = new QaApiFactory(manifest, flags, isolatedClock: clock);
        Guid academy, foreign, session;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var actor = await CreateAccessActorAsync(factory, "qa-core-lifecycle@example.invalid", "AcademyAdmin", academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic lifecycle fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        async Task<HttpClient> Client(string email, QaApiFactory? host = null) {
            var client = (host ?? factory).CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab")); return client;
        }
        using var admin = await Client("qa-core-lifecycle@example.invalid");
        using var other = await Client("qa-admin-a@example.invalid");
        using var teacher = await Client("qa-teacher-a@example.invalid");
        using var anonymous = factory.CreateClient();
        string Route(Guid id, Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/penta/onboarding/conversations/{session}/drafts/{id}";
        string TransitionRoute(Guid id, Guid request, Guid? tenant = null) => Route(id, tenant) + $"/core/requests/{request}/transition";
        object Fields(string name) => new Dictionary<string,object> {
            ["StudentFirstName"] = new { kind = "text", value = "Synthetic" }, ["StudentLastName"] = new { kind = "text", value = name },
            ["DateOfBirth"] = new { kind = "date", value = "2000-01-01" } };
        async Task<JsonElement> Saved(string name) {
            var id = Guid.NewGuid(); using var response = await admin.PutAsJsonAsync(Route(id), new { expectedRevision = 0, contextVersion = 0, fields = Fields(name) });
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Lifecycle fixture save failed: " + (int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task<JsonElement> Current(Guid id) {
            using var response = await admin.GetAsync(Route(id) + "/core-context");
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Lifecycle current failed: " + (int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        JsonObject Decision(JsonElement snapshot, Guid request, string kind) => JsonSerializer.SerializeToNode(new {
            request_ref = request.ToString("D"), scope = snapshot.GetProperty("scope"), base_revision = snapshot.GetProperty("draft").GetProperty("revision").GetInt64(),
            catalog_revision = 1, kind, candidates = Array.Empty<object>(), questions = Array.Empty<string>(), invalidates_review = true })!.AsObject();
        async Task<string> Snapshot(bool includeDrafts = true) {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = includeDrafts ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : [],
                audits = includeDrafts ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : [],
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.UserName }).ToArrayAsync(),
                roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        var count = 0; var journeys = new List<object>();
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGLIFECYCLE " + label); }
        async Task Deny(string label, int status, Func<Task<HttpResponseMessage>> call) {
            var before = await Snapshot(); using var response = await call();
            if ((int)response.StatusCode != status || before != await Snapshot()) throw new InvalidOperationException("Lifecycle denial failed: " + label + " expected=" + status + " actual=" + (int)response.StatusCode);
            Pass(label + " status=" + status + " no state changes");
        }
        async Task<JsonElement> Transition(JsonElement before, string kind, bool record = true) {
            var id = Guid.Parse(before.GetProperty("draft").GetProperty("ref").GetString()!); var request = Guid.NewGuid(); var decision = Decision(before,request,kind);
            var domain = await Snapshot(false);
            using var response = await admin.PostAsJsonAsync(TransitionRoute(id,request),decision);
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Transition failed: " + kind + " status=" + (int)response.StatusCode);
            var after = await response.Content.ReadFromJsonAsync<JsonElement>(); var draft = after.GetProperty("draft");
            var target = kind == "request_pause" ? "paused" : kind == "request_cancel" ? "cancelled" : "collecting";
            if (draft.GetProperty("revision").GetInt64() != before.GetProperty("draft").GetProperty("revision").GetInt64()+1 ||
                draft.GetProperty("status").GetString() != target || draft.GetProperty("operation_ref").ValueKind != JsonValueKind.Null ||
                !JsonElement.DeepEquals(draft.GetProperty("fields"), before.GetProperty("draft").GetProperty("fields")) || domain != await Snapshot(false))
                throw new InvalidOperationException("Lifecycle mutated domain/fields, retained authorization or returned wrong CAS state.");
            if (record) journeys.Add(new { before, decision, after });
            return after;
        }
        var saved = await Saved("Lifecycle retained fields"); var id = saved.GetProperty("id").GetGuid(); var hash = saved.GetProperty("payloadHash").GetString()!;
        var current = await Current(id); var requestId = Guid.NewGuid(); var pause = Decision(current,requestId,"request_pause");
        await Deny("anonymous",401,() => anonymous.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        await Deny("teacher",403,() => teacher.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        await Deny("other actor",404,() => other.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        await Deny("foreign tenant",403,() => admin.PostAsJsonAsync(TransitionRoute(id,requestId,foreign),pause));
        using (var disabled = await Client("qa-core-lifecycle@example.invalid",baseline))
            await Deny("default disabled",404,() => disabled.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        foreach (var change in new[] { "approval", "scope", "base", "catalog", "execute", "fields" }) {
            var bad = pause.DeepClone().AsObject();
            if (change == "approval") bad["approved"] = true;
            if (change == "scope") bad["scope"]!["actor_id"] = Guid.NewGuid().ToString("D");
            if (change == "base") bad["base_revision"] = 2;
            if (change == "catalog") bad["catalog_revision"] = 2;
            if (change == "execute") bad["kind"] = "execute";
            if (change == "fields") bad["candidates"] = new JsonArray(new JsonObject());
            await Deny("untrusted " + change,change is "scope" or "base" ? 409 : 400,() => admin.PostAsJsonAsync(TransitionRoute(id,requestId),bad));
        }
        var resumeId = Guid.NewGuid(); await Deny("resume requires paused draft",409,() => admin.PostAsJsonAsync(TransitionRoute(id,resumeId),Decision(current,resumeId,"request_resume")));
        var domainBefore = await Snapshot(false);
        var responses = await Task.WhenAll(admin.PostAsJsonAsync(TransitionRoute(id,requestId),pause), admin.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        JsonElement paused;
        try {
            if (responses.Any(x => (int)x.StatusCode != 200)) throw new InvalidOperationException("Same-request pause race failed.");
            paused = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(paused,await responses[1].Content.ReadFromJsonAsync<JsonElement>()) || domainBefore != await Snapshot(false))
                throw new InvalidOperationException("Pause replay differs or wrote domain records.");
        } finally { foreach (var response in responses) response.Dispose(); }
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.PentaOnboardingDrafts.SingleAsync(x => x.Id == id);
            if (row.Revision != 2 || row.ExpiresAtUtc != saved.GetProperty("expiresAtUtc").GetDateTime() ||
                await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingDraftCorePaused") != 1)
                throw new InvalidOperationException("Pause race duplicated revision/audit or extended TTL.");
        }
        journeys.Add(new { before = current, decision = pause, after = paused });
        Pass("parallel identical pause returns exact same snapshot, one revision/audit, unchanged TTL and no domain writes");
        var stable = await Snapshot(); clock.Value = clock.Value.AddSeconds(1);
        using (var restarted = new QaApiFactory(manifest, flags, isolatedClock: clock))
        using (var client = await Client("qa-core-lifecycle@example.invalid",restarted))
        using (var replay = await client.PostAsJsonAsync(TransitionRoute(id,requestId),pause))
            if ((int)replay.StatusCode != 200 || !JsonElement.DeepEquals(paused,await replay.Content.ReadFromJsonAsync<JsonElement>()) || stable != await Snapshot())
                throw new InvalidOperationException("Restarted lifecycle replay failed.");
        Pass("fresh host/provider lost-response replay retains exact snapshot without write or TTL extension");
        await Deny("paused save blocked",409,() => admin.PutAsJsonAsync(Route(id),new { expectedRevision = 2, contextVersion = 0, fields = Fields("Bypass") }));
        await Deny("paused review blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/review",new { revision = 2,payloadHash = hash }));
        await Deny("paused authorize blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/authorize-creation",new { revision = 2,payloadHash = hash,intent = PentaOnboardingDraftService.CreationIntent }));
        await Deny("paused execute blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/execute",new { revision = 2,payloadHash = hash }));
        await Deny("paused validation blocked",409,() => admin.GetAsync(Route(id)+$"/core/requests/{Guid.NewGuid()}/validation"));
        var prepareId = Guid.NewGuid(); var prepare = Decision(paused,prepareId,"host_prepare");
        prepare["candidates"] = JsonSerializer.SerializeToNode(new[] { new { tool_key = PentaOnboardingDraftService.Tool,edit = new { mode = "replace",set = Fields("Paused bypass"),remove = Array.Empty<string>() } } });
        await Deny("paused preparation blocked",409,() => admin.PostAsJsonAsync(Route(id)+$"/core/requests/{prepareId}/prepare",prepare));
        var sameIdChanged = Decision(current,requestId,"request_cancel");
        await Deny("same nonce changed kind blocked",409,() => admin.PostAsJsonAsync(TransitionRoute(id,requestId),sameIdChanged));
        var resumed = await Transition(paused,"request_resume"); Pass("resume preserves fields and resets to collecting without approval");
        await Deny("old pause replay after resume",409,() => admin.PostAsJsonAsync(TransitionRoute(id,requestId),pause));
        var reused = Decision(resumed,requestId,"request_pause");
        await Deny("nonce cannot be reused at newer revision",409,() => admin.PostAsJsonAsync(TransitionRoute(id,requestId),reused));
        await Deny("old revision execution after resume",409,() => admin.PostAsJsonAsync(Route(id)+"/execute",new { revision = 2,payloadHash = hash }));
        // Real preparation -> host authorization -> pause -> resume must discard
        // both preparation identity and creation authority, not just a UI flag.
        var prepareCurrentId = Guid.NewGuid(); var preparedInput = Decision(resumed,prepareCurrentId,"host_prepare");
        preparedInput["candidates"] = JsonSerializer.SerializeToNode(new[] { new { tool_key = PentaOnboardingDraftService.Tool,edit = new { mode = "replace",set = Fields("Prepared lifecycle"),remove = Array.Empty<string>() } } });
        JsonElement operation;
        using (var response = await admin.PostAsJsonAsync(Route(id)+$"/core/requests/{prepareCurrentId}/prepare",preparedInput)) {
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Lifecycle preparation fixture failed.");
            operation = (await response.Content.ReadFromJsonAsync<JsonElement>())[0].Clone();
        }
        hash = operation.GetProperty("payload_hash").GetString()!;
        await ConfirmPreparedOnboardingAsync(admin,Route(id),operation);
        var authorized = await Current(id); paused = await Transition(authorized,"request_pause");
        Pass("pause invalidates a real prepared and creation-authorized draft without domain mutation");
        resumed = await Transition(paused,"request_resume");
        await Deny("resumed latest revision is not authorized",409,() => admin.PostAsJsonAsync(Route(id)+"/execute",new { revision = 6,payloadHash = hash }));
        await Deny("old preparation replay after transitions",409,() => admin.PostAsJsonAsync(Route(id)+$"/core/requests/{prepareCurrentId}/prepare",preparedInput));
        await Deny("preparation nonce cannot become a lifecycle request",409,() => admin.PostAsJsonAsync(TransitionRoute(id,prepareCurrentId),Decision(resumed,prepareCurrentId,"request_pause")));
        var cancelled = await Transition(resumed,"request_cancel"); Pass("cancel retains draft fields/history, no student deletion or writes");
        await Deny("cancelled save blocked",409,() => admin.PutAsJsonAsync(Route(id),new { expectedRevision = 7, contextVersion = 0,fields = Fields("Resurrect") }));
        foreach (var kind in new[] { "request_resume", "request_pause", "request_cancel" }) {
            var nonce = Guid.NewGuid(); await Deny("cancelled terminal " + kind,409,() => admin.PostAsJsonAsync(TransitionRoute(id,nonce),Decision(cancelled,nonce,kind)));
        }
        await Deny("cancelled authorize blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/authorize-creation",new { revision = 7,payloadHash = hash,intent = PentaOnboardingDraftService.CreationIntent }));
        await Deny("cancelled execute blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/execute",new { revision = 7,payloadHash = hash }));
        await Deny("cancelled review blocked",409,() => admin.PostAsJsonAsync(Route(id)+"/review",new { revision = 7,payloadHash = hash }));
        await Deny("cancelled validation blocked",409,() => admin.GetAsync(Route(id)+$"/core/requests/{Guid.NewGuid()}/validation"));
        var cancelledPrepareNonce = Guid.NewGuid(); var cancelledPrepare = Decision(cancelled,cancelledPrepareNonce,"host_prepare");
        cancelledPrepare["candidates"] = preparedInput["candidates"]!.DeepClone();
        await Deny("cancelled preparation blocked",409,() => admin.PostAsJsonAsync(Route(id)+$"/core/requests/{cancelledPrepareNonce}/prepare",cancelledPrepare));
        var raceSaved = await Saved("Distinct requests"); var raceId = raceSaved.GetProperty("id").GetGuid(); var raceCurrent = await Current(raceId);
        var nonceA = Guid.NewGuid(); var nonceB = Guid.NewGuid(); var decisionA = Decision(raceCurrent,nonceA,"request_pause"); var decisionB = Decision(raceCurrent,nonceB,"request_cancel");
        responses = await Task.WhenAll(admin.PostAsJsonAsync(TransitionRoute(raceId,nonceA),decisionA),admin.PostAsJsonAsync(TransitionRoute(raceId,nonceB),decisionB));
        try { if (!responses.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200,409 })) throw new InvalidOperationException("Distinct lifecycle CAS race failed."); }
        finally { foreach (var response in responses) response.Dispose(); }
        Pass("competing pause/cancel requests exactly one200 and one409, never429");
        var rollbackSaved = await Saved("Lifecycle audit rollback"); var rollbackId = rollbackSaved.GetProperty("id").GetGuid(); var rollbackCurrent = await Current(rollbackId);
        var rollbackNonce = Guid.NewGuid(); var rollbackDecision = Decision(rollbackCurrent,rollbackNonce,"request_cancel"); var fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client = await Client("qa-core-lifecycle@example.invalid",faulty)) {
            await Deny("required audit full SQL rollback",503,() => client.PostAsJsonAsync(TransitionRoute(rollbackId,rollbackNonce),rollbackDecision));
            if (fault.Faults != 1) throw new InvalidOperationException("Lifecycle audit fault not reached.");
        }
        using (var retry = await admin.PostAsJsonAsync(TransitionRoute(rollbackId,rollbackNonce),rollbackDecision))
            if ((int)retry.StatusCode != 200) throw new InvalidOperationException("Independently verified rollback retry failed.");
        Pass("explicit retry only after verified full rollback succeeds once");
        var done = await Saved("Completed lifecycle"); var doneId = done.GetProperty("id").GetGuid(); var doneHash = done.GetProperty("payloadHash").GetString();
        using (var response = await admin.PostAsJsonAsync(Route(doneId)+"/authorize-creation",new { revision = 1,payloadHash = doneHash,intent = PentaOnboardingDraftService.CreationIntent }))
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Completed lifecycle authorize failed.");
        using (var response = await admin.PostAsJsonAsync(Route(doneId)+"/execute",new { revision = 1,payloadHash = doneHash }))
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Completed lifecycle execution failed.");
        var doneCurrent = await Current(doneId); var doneNonce = Guid.NewGuid();
        await Deny("committed student cannot be cancelled",409,() => admin.PostAsJsonAsync(TransitionRoute(doneId,doneNonce),Decision(doneCurrent,doneNonce,"request_cancel")));
        var executionRace = await Saved("Execution versus cancel"); var executionRaceId = executionRace.GetProperty("id").GetGuid(); var executionRaceHash = executionRace.GetProperty("payloadHash").GetString();
        using (var response = await admin.PostAsJsonAsync(Route(executionRaceId)+"/authorize-creation",new { revision = 1,payloadHash = executionRaceHash,intent = PentaOnboardingDraftService.CreationIntent }))
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Execution/cancel authorization fixture failed.");
        var executionCurrent = await Current(executionRaceId); var executionNonce = Guid.NewGuid();
        responses = await Task.WhenAll(admin.PostAsJsonAsync(Route(executionRaceId)+"/execute",new { revision = 1,payloadHash = executionRaceHash }),
            admin.PostAsJsonAsync(TransitionRoute(executionRaceId,executionNonce),Decision(executionCurrent,executionNonce,"request_cancel")));
        try {
            var statuses = responses.Select(x => (int)x.StatusCode).ToArray();
            if (!(statuses.SequenceEqual(new[] { 201,409 }) || statuses.SequenceEqual(new[] { 409,200 }))) throw new InvalidOperationException("Execution/cancel race statuses failed.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaOnboardingDrafts.SingleAsync(x => x.Id == executionRaceId);
            var won = statuses[0] == 201;
            if (row.Status != (won ? "Succeeded" : "Cancelled") || row.Revision != (won ? 1 : 2) ||
                await db.Students.CountAsync(x => x.AcademyId == academy && x.LastName == "Execution versus cancel") != (won ? 1 : 0) ||
                await db.AuditLogs.CountAsync(x => x.EntityId == executionRaceId && x.Action == "PentaOnboardingExecuted") != (won ? 1 : 0))
                throw new InvalidOperationException("Execution/cancel domain, audit or state integrity failed.");
        } finally { foreach (var response in responses) response.Dispose(); }
        Pass("execution/cancel race serializes to one committed outcome; cannot cancel a committed student, never429");
        var privateSaved = Guid.NewGuid();
        using (var response = await admin.PutAsJsonAsync(Route(privateSaved),new { expectedRevision = 0,contextVersion = 0,
            fields = new Dictionary<string,object> { ["MedicalOrAccessibilityNotes"] = new { kind = "text",value = "Synthetic private note" } } }))
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Secure lifecycle fixture failed: " + (int)response.StatusCode);
        var secureDecision = Decision(rollbackCurrent,Guid.NewGuid(),"request_cancel"); secureDecision["scope"]!["connection_id"] = "academydesk-onboarding:" + privateSaved.ToString("N");
        var secureNonce = Guid.Parse(secureDecision["request_ref"]!.GetValue<string>());
        await Deny("secure context failure rolls lifecycle back without medical disclosure",409,() => admin.PostAsJsonAsync(TransitionRoute(privateSaved,secureNonce),secureDecision));
        var expired = await Saved("Expired lifecycle"); var expiredId = expired.GetProperty("id").GetGuid(); var expiredCurrent = await Current(expiredId); var expiredNonce = Guid.NewGuid();
        var beforeExpiry = clock.Value; clock.Value = clock.Value.AddMinutes(11);
        await Deny("expired lifecycle never extends draft TTL",410,() => admin.PostAsJsonAsync(TransitionRoute(expiredId,expiredNonce),Decision(expiredCurrent,expiredNonce,"request_pause")));
        clock.Value = beforeExpiry;
        var stale = await Saved("Stale authority"); var staleId = stale.GetProperty("id").GetGuid(); var staleCurrent = await Current(staleId); var staleNonce = Guid.NewGuid(); var staleDecision = Decision(staleCurrent,staleNonce,"request_pause");
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaMiniSessions.SingleAsync(x => x.Id == session)).Version = 1; await db.SaveChangesAsync();
        }
        await Deny("stale conversation context",409,() => admin.PostAsJsonAsync(TransitionRoute(staleId,staleNonce),staleDecision));
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaMiniSessions.SingleAsync(x => x.Id == session)).Version = 0; await db.SaveChangesAsync();
        }
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await identity.Users.SingleAsync(x => x.Id == actor)).ConcurrencyStamp = Guid.NewGuid().ToString("N"); await identity.SaveChangesAsync();
        }
        await Deny("changed current authority",409,() => admin.PostAsJsonAsync(TransitionRoute(staleId,staleNonce),staleDecision));
        var directory = Path.Combine(Directory.GetCurrentDirectory(),"QA","EVIDENCE","penta-onboarding-lifecycle-"+manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory,"core-lifecycle.json"),JsonSerializer.Serialize(new { journeys }));
        if (count != 48) throw new InvalidOperationException("Incomplete lifecycle SQL matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGLIFECYCLE all " + count + " HTTP/real SQL cases; draft-only transitions, no model activation or human-browser approval claim.");
    }
}
