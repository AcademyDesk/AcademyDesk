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
    private static async Task VerifyPentaOnboardingPreparationAsync(QaApiFactory baseline, QaRunManifest manifest)
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
        var actor = await CreateAccessActorAsync(factory, "qa-core-prepare@example.invalid", "AcademyAdmin", academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic preparation fixture; no inference" };
            db.PentaMiniSessions.Add(row);
            db.Students.AddRange(new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Preparation duplicate", DateOfBirth = new DateOnly(2000,1,1) },
                new Student { AcademyId = foreign, FirstName = "Synthetic", LastName = "Foreign match only", DateOfBirth = new DateOnly(2000,1,1) });
            await db.SaveChangesAsync(); session = row.Id;
        }
        async Task<HttpClient> Client(string email, QaApiFactory? host = null) {
            var client = (host ?? factory).CreateClient(); var bearer = await LoginAsync(client, email, "Synthetic!39Ab");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer); return client;
        }
        using var admin = await Client("qa-core-prepare@example.invalid");
        using var other = await Client("qa-admin-a@example.invalid");
        using var teacher = await Client("qa-teacher-a@example.invalid");
        using var anonymous = factory.CreateClient();
        string Route(Guid id, Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/penta/onboarding/conversations/{session}/drafts/{id}";
        string PrepareRoute(Guid id, Guid request, Guid? tenant = null) => Route(id, tenant) + $"/core/requests/{request}/prepare";
        object Fields(string suffix, bool minor = false) => new Dictionary<string,object> {
            ["StudentFirstName"] = new { kind = "text", value = " Synthetic " }, ["StudentLastName"] = new { kind = "text", value = suffix },
            ["DateOfBirth"] = new { kind = "date", value = minor ? "2015-01-01" : "2000-01-01" },
            ["AllowFinance"] = new { kind = "boolean", value = false } };
        async Task<Guid> Save(object? fields = null) {
            var id = Guid.NewGuid(); using var response = await admin.PutAsJsonAsync(Route(id), new { expectedRevision = 0, contextVersion = 0, fields = fields ?? new { } });
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Preparation fixture save failed: " + (int)response.StatusCode); return id;
        }
        async Task<JsonElement> Current(Guid id) {
            using var response = await admin.GetAsync(Route(id) + "/core-context");
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Preparation current fixture failed.");
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        JsonObject Decision(JsonElement snapshot, Guid request, object fields) => JsonSerializer.SerializeToNode(new {
            request_ref = request.ToString("D"), scope = snapshot.GetProperty("scope"), base_revision = snapshot.GetProperty("draft").GetProperty("revision").GetInt64(),
            catalog_revision = 1, kind = "host_prepare", candidates = new[] { new { tool_key = PentaOnboardingDraftService.Tool,
                edit = new { mode = "replace", set = fields, remove = Array.Empty<string>() } } }, questions = Array.Empty<string>(), invalidates_review = true })!.AsObject();
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
        var count = 0; var validations = new List<JsonElement>(); var journeys = new List<object>();
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGPREPARATION " + label); }
        async Task Deny(string label, int status, Func<Task<HttpResponseMessage>> call) {
            var before = await Snapshot(); using var response = await call();
            if ((int)response.StatusCode != status || before != await Snapshot()) throw new InvalidOperationException("Preparation denial failed: " + label + " expected=" + status + " actual=" + (int)response.StatusCode);
            Pass(label + " status=" + status + " no state changes");
        }
        async Task<JsonElement> Validation(Guid id, string status) {
            using var response = await admin.GetAsync(Route(id) + $"/core/requests/{Guid.NewGuid()}/validation");
            var report = await response.Content.ReadFromJsonAsync<JsonElement>();
            if ((int)response.StatusCode != 200 || report.GetProperty("status").GetString() != status) throw new InvalidOperationException("Validation report failed: " + status);
            validations.Add(report); return report;
        }
        var id = await Save(); var current = await Current(id); var request = Guid.NewGuid();
        var decision = Decision(current, request, Fields("Prepared Core student"));
        await Deny("anonymous prepare", 401, () => anonymous.PostAsJsonAsync(PrepareRoute(id,request), decision));
        await Deny("teacher prepare", 403, () => teacher.PostAsJsonAsync(PrepareRoute(id,request), decision));
        await Deny("other admin draft", 404, () => other.PostAsJsonAsync(PrepareRoute(id,request), decision));
        await Deny("foreign tenant", 403, () => admin.PostAsJsonAsync(PrepareRoute(id,request,foreign), decision));
        using (var disabled = await Client("qa-core-prepare@example.invalid", baseline))
            await Deny("default disabled", 404, () => disabled.PostAsJsonAsync(PrepareRoute(id,request), decision));
        foreach (var change in new[] { "approval", "scope", "tool", "base", "kind" }) {
            var bad = decision.DeepClone().AsObject();
            if (change == "approval") bad["approved"] = true;
            if (change == "scope") bad["scope"]!["actor_id"] = Guid.NewGuid().ToString("D");
            if (change == "tool") bad["candidates"]![0]!["tool_key"] = "ExecuteSQL";
            if (change == "base") bad["base_revision"] = 2;
            if (change == "kind") bad["kind"] = "execute";
            await Deny("untrusted " + change, change is "scope" or "base" ? 409 : 400, () => admin.PostAsJsonAsync(PrepareRoute(id,request), bad));
        }
        var domainBefore = await Snapshot(false);
        var racing = await Task.WhenAll(admin.PostAsJsonAsync(PrepareRoute(id,request), decision), admin.PostAsJsonAsync(PrepareRoute(id,request), decision));
        JsonElement operations;
        try {
            if (!racing.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200,201 })) throw new InvalidOperationException("Preparation one-request race failed.");
            operations = await racing[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(operations, await racing[1].Content.ReadFromJsonAsync<JsonElement>()) || domainBefore != await Snapshot(false))
                throw new InvalidOperationException("Preparation replay differs or created domain records.");
        } finally { foreach (var response in racing) response.Dispose(); }
        var operation = operations[0]; var hash = operation.GetProperty("payload_hash").GetString()!;
        if (operation.GetProperty("status").GetString() != "awaiting_approval" || operation.GetProperty("approval_ref").ValueKind != JsonValueKind.Null ||
            operation.GetProperty("draft_revision").GetInt64() != 2 || operation.GetProperty("canonical_fields").TryGetProperty("StudentEmail", out _) ||
            operation.GetProperty("canonical_fields").GetProperty("StudentFirstName").GetProperty("value").GetString() != "Synthetic" ||
            operation.GetProperty("canonical_fields").GetProperty("AllowFinance").GetProperty("value").GetBoolean()) throw new InvalidOperationException("Canonical/default/unapproved preparation failed.");
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingDraftCorePrepared") != 1) throw new InvalidOperationException("Preparation duplicated mandatory audit.");
        }
        Pass("parallel same request201/200, identical canonical preparation/revision/audit, no student/guardian/account writes");
        var after = await Current(id); journeys.Add(new { before = current, decision, operations, after });
        if (after.GetProperty("draft").GetProperty("status").GetString() != "awaiting_approval") throw new InvalidOperationException("Prepared draft context not linked.");
        Pass("current host draft matches prepared operation; never approved by Core");
        var unchanged = await Snapshot();
        using (var replay = await admin.PostAsJsonAsync(PrepareRoute(id,request), decision))
            if ((int)replay.StatusCode != 200 || unchanged != await Snapshot()) throw new InvalidOperationException("Preparation replay changed state.");
        Pass("fresh HTTP replay has no additional write/audit or expiry extension");
        var mutated = decision.DeepClone().AsObject(); mutated["candidates"]![0]!["edit"]!["set"]!["StudentLastName"]!["value"] = "Different content";
        await Deny("same request changed payload", 409, () => admin.PostAsJsonAsync(PrepareRoute(id,request), mutated));
        await Deny("preparation is not execution authorization", 409, () => admin.PostAsJsonAsync(Route(id) + "/execute", new { revision = 2, payloadHash = hash }));
        await Validation(id, "valid"); Pass("actual current prepared draft validates with exact scope/revision and zero issues");
        var approved = await ConfirmPreparedOnboardingAsync(admin,Route(id),operation);
        using (var execution = await admin.PostAsJsonAsync(Route(id) + "/core/execute", CoreOnboardingBinding(approved,true)))
            if ((int)execution.StatusCode != 201) throw new InvalidOperationException("Prepared explicit execution failed.");
        if ((await Current(id)).GetProperty("draft").GetProperty("status").GetString() != "succeeded") throw new InvalidOperationException("Prepared history lost across completion.");
        Pass("separate authenticated authorization executes existing domain once; protected request history survives receipt/status");

        var edited = await Save(); var oldContext = await Current(edited); var oldRequest = Guid.NewGuid(); var first = Decision(oldContext, oldRequest, Fields("Edited preparation"));
        using (var response = await admin.PostAsJsonAsync(PrepareRoute(edited,oldRequest), first))
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Edit fixture preparation failed.");
        using (var edit = await admin.PutAsJsonAsync(Route(edited), new { expectedRevision = 2, contextVersion = 0, fields = Fields("Edited replacement") }))
            if ((int)edit.StatusCode != 201) throw new InvalidOperationException("Edit fixture replacement failed.");
        await Deny("old request replay after edit", 409, () => admin.PostAsJsonAsync(PrepareRoute(edited,oldRequest), first));
        var editedContext = await Current(edited); var reused = Decision(editedContext, oldRequest, Fields("Reused request"));
        await Deny("request ID cannot be reused at a newer revision", 409, () => admin.PostAsJsonAsync(PrepareRoute(edited,oldRequest), reused));
        var newRequest = Guid.NewGuid(); var next = Decision(editedContext, newRequest, Fields("New explicit preparation"));
        using (var response = await admin.PostAsJsonAsync(PrepareRoute(edited,newRequest), next))
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Fresh request after correction failed.");
        Pass("explicit correction invalidates preparation; new request succeeds without losing old request identity");
        var partialId = await Save(); var report = await Validation(partialId,"invalid");
        if (report.GetProperty("issues").GetArrayLength() != 3) throw new InvalidOperationException("Required-field report missing.");
        Pass("partial draft asks exact missing name/DOB fields");
        var minorId = await Save(Fields("Minor missing guardian", true)); report = await Validation(minorId,"invalid");
        if (!report.GetProperty("issues").EnumerateArray().Any(x => x.GetProperty("field").GetString() == "ParentEmail")) throw new InvalidOperationException("Minor guardian requirement missing.");
        Pass("minor guardian requirements come from domain validation");
        var duplicateId = await Save(Fields("Preparation duplicate")); report = await Validation(duplicateId,"ambiguous");
        if (report.GetProperty("source_refs").GetArrayLength() != 1 || report.GetProperty("issues")[0].GetProperty("choices").GetArrayLength() != 1)
            throw new InvalidOperationException("Scoped duplicate choices missing.");
        Pass("possible same-person duplicate provides owned stable choices, no auto-select/merge");
        var foreignOnly = await Save(Fields("Foreign match only")); report = await Validation(foreignOnly,"valid");
        if (report.GetProperty("source_refs").GetArrayLength() != 0) throw new InvalidOperationException("Foreign duplicate leaked.");
        Pass("foreign matching person is absent from validation sources");
        var privateId = await Save(new Dictionary<string,object> { ["MedicalOrAccessibilityNotes"] = new { kind = "text", value = "Synthetic private note" } });
        report = await Validation(privateId,"secure_step_required");
        if (report.GetRawText().Contains("Synthetic private note", StringComparison.Ordinal)) throw new InvalidOperationException("Secure-step report leaked private text.");
        Pass("secure host step report contains no medical note value");
        var invalidId = await Save(Fields("Invalid preparation")); var invalidCurrent = await Current(invalidId); var invalidRequest = Guid.NewGuid();
        await Deny("invalid candidate never reserves preparation", 400, () => admin.PostAsJsonAsync(PrepareRoute(invalidId,invalidRequest), Decision(invalidCurrent,invalidRequest,new { })));
        var rollbackId = await Save(); var rollbackCurrent = await Current(rollbackId); var rollbackRequest = Guid.NewGuid(); var rollbackDecision = Decision(rollbackCurrent,rollbackRequest,Fields("Rollback prepare"));
        var fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault, isolatedClock: clock))
        using (var client = await Client("qa-core-prepare@example.invalid",faulty)) {
            await Deny("mandatory preparation audit full rollback", 503, () => client.PostAsJsonAsync(PrepareRoute(rollbackId,rollbackRequest),rollbackDecision));
            if (fault.Faults != 1) throw new InvalidOperationException("Preparation audit fault not observed.");
        }
        using (var retry = await admin.PostAsJsonAsync(PrepareRoute(rollbackId,rollbackRequest),rollbackDecision))
            if ((int)retry.StatusCode != 201) throw new InvalidOperationException("Known rollback retry failed.");
        Pass("explicit retry after independently verified rollback prepares once");
        var faultValidation = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: faultValidation, isolatedClock: clock))
        using (var client = await Client("qa-core-prepare@example.invalid",faulty)) {
            await Deny("mandatory validation audit failure no response facts/state", 503, () => client.GetAsync(Route(partialId) + $"/core/requests/{Guid.NewGuid()}/validation"));
            if (faultValidation.Faults != 1) throw new InvalidOperationException("Validation audit fault not reached.");
        }
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await identity.Users.SingleAsync(x => x.Id == actor)).ConcurrencyStamp = Guid.NewGuid().ToString("N"); await identity.SaveChangesAsync();
        }
        await Deny("changed authority denies prepared replay", 409, () => admin.PostAsJsonAsync(PrepareRoute(rollbackId,rollbackRequest),rollbackDecision));
        await Deny("changed authority denies validation", 409, () => admin.GetAsync(Route(partialId) + $"/core/requests/{Guid.NewGuid()}/validation"));
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "penta-onboarding-preparation-" + manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory,"core-preparation.json"), JsonSerializer.Serialize(new { journeys, validations }));
        if (count != 31) throw new InvalidOperationException("Incomplete preparation SQL matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGPREPARATION all " + count + " HTTP/real SQL cases; preparation/validation only, separate explicit domain execution tested, no model activation.");
    }
}
