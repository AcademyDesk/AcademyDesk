using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static JsonObject CoreOnboardingBinding(JsonElement operation, bool approval = false)
    {
        var result = JsonSerializer.SerializeToNode(new { operation_ref = operation.GetProperty("ref").GetString(),
            revision = operation.GetProperty("draft_revision").GetInt64(), payload_hash = operation.GetProperty("payload_hash").GetString() })!.AsObject();
        if (approval) result["approval_ref"] = operation.GetProperty("approval_ref").GetString();
        return result;
    }

    private static async Task<JsonElement> ConfirmPreparedOnboardingAsync(HttpClient client, string route, JsonElement prepared)
    {
        using var challenge = await client.PostAsJsonAsync(route+"/core/challenge",CoreOnboardingBinding(prepared));
        if ((int)challenge.StatusCode != 200) throw new InvalidOperationException("Core challenge fixture failed: " + (int)challenge.StatusCode);
        var operation = await challenge.Content.ReadFromJsonAsync<JsonElement>();
        using var confirm = await client.PostAsJsonAsync(route+"/core/confirm",CoreOnboardingBinding(operation,true));
        if ((int)confirm.StatusCode != 200) throw new InvalidOperationException("Core confirmation fixture failed: " + (int)confirm.StatusCode);
        var approved = await confirm.Content.ReadFromJsonAsync<JsonElement>();
        if (approved.GetProperty("status").GetString() != "approved" ||
            approved.GetProperty("approval_ref").GetString() != operation.GetProperty("approval_ref").GetString())
            throw new InvalidOperationException("Core confirmation lost exact challenge binding.");
        return approved;
    }

    private static async Task VerifyPentaOnboardingApprovalAsync(QaApiFactory baseline, QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> { ["Penta:OperationalDraftContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true", ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true" };
        var clock = new OnboardingDraftClock();
        using var factory = new QaApiFactory(manifest,flags,isolatedClock:clock);
        Guid academy, foreign, session;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var actor = await CreateAccessActorAsync(factory,"qa-core-approval@example.invalid","AcademyAdmin",academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = new PentaMiniSession { AcademyId = academy,ActorUserId = actor,Version = 0,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1),ProtectedState = "Synthetic approval fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        async Task<HttpClient> Client(string email, QaApiFactory? host = null) {
            var client = (host ?? factory).CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",await LoginAsync(client,email,"Synthetic!39Ab")); return client;
        }
        using var admin = await Client("qa-core-approval@example.invalid");
        using var other = await Client("qa-admin-a@example.invalid");
        using var teacher = await Client("qa-teacher-a@example.invalid");
        using var anonymous = factory.CreateClient();
        string Route(Guid id, Guid? tenant = null) => $"/api/academies/{tenant ?? academy}/penta/onboarding/conversations/{session}/drafts/{id}";
        Guid Id(JsonElement operation) => Guid.Parse(operation.GetProperty("draft_ref").GetString()!);
        async Task<JsonElement> Current(Guid id) {
            using var response = await admin.GetAsync(Route(id)+"/core-context");
            if ((int)response.StatusCode != 200) throw new InvalidOperationException("Approval current failed: " + (int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        async Task<JsonElement> Prepare(string name, bool minor = false) {
            var id = Guid.NewGuid();
            using (var saved = await admin.PutAsJsonAsync(Route(id),new { expectedRevision = 0,contextVersion = 0,fields = new { } }))
                if ((int)saved.StatusCode != 201) throw new InvalidOperationException("Approval save fixture failed.");
            var current = await Current(id); var request = Guid.NewGuid();
            var fields = new Dictionary<string,object> { ["StudentFirstName"] = new { kind = "text",value = "Synthetic" },
                ["StudentLastName"] = new { kind = "text",value = name },["DateOfBirth"] = new { kind = "date",value = minor ? "2015-01-01" : "2000-01-01" } };
            if (minor) {
                fields["ParentFirstName"] = new { kind = "text",value = "Synthetic" };
                fields["ParentLastName"] = new { kind = "text",value = "Core guardian" };
                fields["ParentEmail"] = new { kind = "text",value = "core-guardian-"+manifest.RunId.ToString("N")+"@example.invalid" };
                fields["AllowFinance"] = new { kind = "boolean",value = false };
            }
            using var response = await admin.PostAsJsonAsync(Route(id)+$"/core/requests/{request}/prepare",new {
                request_ref = request.ToString("D"),scope = current.GetProperty("scope"),base_revision = 1,catalog_revision = 1,kind = "host_prepare",
                candidates = new[] { new { tool_key = PentaOnboardingDraftService.Tool,edit = new { mode = "replace",set = fields,remove = Array.Empty<string>() } } },
                questions = Array.Empty<string>(),invalidates_review = true });
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Approval preparation fixture failed: " + (int)response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<JsonElement>())[0].Clone();
        }
        async Task<string> Snapshot(bool includeDrafts = true) {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = includeDrafts ? await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : [],
                audits = includeDrafts ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : [],
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id,x.AcademyId,x.UserName }).ToArrayAsync(),
                roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        var count = 0; var journeys = new List<object>();
        void Pass(string text) { count++; Console.WriteLine("PASS: PENTAONBOARDINGAPPROVAL " + text); }
        async Task Deny(string label,int status,Func<Task<HttpResponseMessage>> call) {
            var before = await Snapshot(); using var response = await call();
            if ((int)response.StatusCode != status || before != await Snapshot()) throw new InvalidOperationException("Approval denial failed: " + label + " expected=" + status + " actual=" + (int)response.StatusCode);
            Pass(label+" status="+status+" no state changes");
        }
        async Task<JsonElement> Read(string route, object binding, HttpClient? client = null, int expected = 200) {
            using var response = await (client ?? admin).PostAsJsonAsync(route,binding);
            if ((int)response.StatusCode != expected) throw new InvalidOperationException("Approval port failed expected="+expected+" actual="+(int)response.StatusCode);
            return await response.Content.ReadFromJsonAsync<JsonElement>();
        }
        var prepared = await Prepare("Opaque proof lifecycle"); var id = Id(prepared); var binding = CoreOnboardingBinding(prepared);
        await Deny("anonymous challenge",401,() => anonymous.PostAsJsonAsync(Route(id)+"/core/challenge",binding));
        await Deny("teacher challenge",403,() => teacher.PostAsJsonAsync(Route(id)+"/core/challenge",binding));
        await Deny("foreign challenge",403,() => admin.PostAsJsonAsync(Route(id,foreign)+"/core/challenge",binding));
        await Deny("other actor challenge",404,() => other.PostAsJsonAsync(Route(id)+"/core/challenge",binding));
        using (var disabled = await Client("qa-core-approval@example.invalid",baseline))
            await Deny("default disabled",404,() => disabled.PostAsJsonAsync(Route(id)+"/core/challenge",binding));
        foreach (var change in new[] { "model-approval","operation","revision","hash" }) {
            var bad = binding.DeepClone().AsObject();
            if (change == "model-approval") bad["approved"] = true;
            if (change == "operation") bad["operation_ref"] = "onboarding:foreign:2";
            if (change == "revision") bad["revision"] = 3;
            if (change == "hash") bad["payload_hash"] = new string('b',64);
            await Deny("altered "+change,change == "model-approval" ? 400 : 409,() => admin.PostAsJsonAsync(Route(id)+"/core/challenge",bad));
        }
        await Deny("legacy confirmation cannot approve Core preparation",409,() => admin.PostAsJsonAsync(Route(id)+"/authorize-creation",new { revision = 2,payloadHash = prepared.GetProperty("payload_hash").GetString(),intent = PentaOnboardingDraftService.CreationIntent }));
        var fabricated = binding.DeepClone().AsObject(); fabricated["approval_ref"] = Guid.NewGuid().ToString("D");
        await Deny("fabricated proof confirm",409,() => admin.PostAsJsonAsync(Route(id)+"/core/confirm",fabricated));
        await Deny("fabricated proof execute",409,() => admin.PostAsJsonAsync(Route(id)+"/core/execute",fabricated));
        var domain = await Snapshot(false);
        var responses = await Task.WhenAll(admin.PostAsJsonAsync(Route(id)+"/core/challenge",binding),admin.PostAsJsonAsync(Route(id)+"/core/challenge",binding));
        JsonElement challenge;
        try {
            if (responses.Any(x => (int)x.StatusCode != 200)) throw new InvalidOperationException("Parallel challenge statuses failed.");
            challenge = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(challenge,await responses[1].Content.ReadFromJsonAsync<JsonElement>()) || domain != await Snapshot(false) ||
                challenge.GetProperty("status").GetString() != "awaiting_approval" || challenge.GetProperty("approval_ref").ValueKind != JsonValueKind.String)
                throw new InvalidOperationException("Challenge race changed domain or gave approval.");
        } finally { foreach (var response in responses) response.Dispose(); }
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingDraftCoreApprovalIssued") != 1) throw new InvalidOperationException("Duplicate challenge audit.");
        }
        Pass("parallel challenge returns one opaque nonce/expiry/audit, never approval or domain writes");
        var approvedBinding = CoreOnboardingBinding(challenge,true);
        await Deny("challenge alone cannot execute",409,() => admin.PostAsJsonAsync(Route(id)+"/core/execute",approvedBinding));
        var priorState = await Snapshot();
        var challengeAgain = await Read(Route(id)+"/core/challenge",binding);
        if (!JsonElement.DeepEquals(challenge,challengeAgain) || priorState != await Snapshot()) throw new InvalidOperationException("Challenge replay extended TTL or wrote state.");
        Pass("challenge replay does not extend expiry or add audit");
        responses = await Task.WhenAll(admin.PostAsJsonAsync(Route(id)+"/core/confirm",approvedBinding),admin.PostAsJsonAsync(Route(id)+"/core/confirm",approvedBinding));
        JsonElement approved;
        try {
            if (responses.Any(x => (int)x.StatusCode != 200)) throw new InvalidOperationException("Parallel confirmation statuses failed.");
            approved = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(approved,await responses[1].Content.ReadFromJsonAsync<JsonElement>()) || domain != await Snapshot(false) ||
                approved.GetProperty("status").GetString() != "approved") throw new InvalidOperationException("Confirmation mismatch/domain dispatch.");
        } finally { foreach (var response in responses) response.Dispose(); }
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingDraftCoreApproved") != 1) throw new InvalidOperationException("Duplicate confirmation audit.");
        }
        Pass("parallel exact confirmation200/200 commits one approval audit and no domain writes");
        var before = await Current(id);
        if (!JsonElement.DeepEquals(approved,await Read(Route(id)+"/core/operation",binding))) throw new InvalidOperationException("Operation read mismatch.");
        var awaitingResult = await Read(Route(id)+"/core/status",binding);
        if (awaitingResult.GetProperty("status").GetString() != "approved" || awaitingResult.GetProperty("receipt_ref").ValueKind != JsonValueKind.Null ||
            awaitingResult.GetProperty("source_refs").GetArrayLength() != 0) throw new InvalidOperationException("Uncommitted status fabricated success.");
        Pass("operation/status expose bound approval but no invented receipt/source success");
        await Deny("legacy execute cannot bypass Core proof",409,() => admin.PostAsJsonAsync(Route(id)+"/execute",new { revision = 2,payloadHash = approved.GetProperty("payload_hash").GetString() }));
        responses = await Task.WhenAll(admin.PostAsJsonAsync(Route(id)+"/core/execute",approvedBinding),admin.PostAsJsonAsync(Route(id)+"/core/execute",approvedBinding));
        JsonElement receipt;
        try {
            if (!responses.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200,201 })) throw new InvalidOperationException("Core execution race expected201/200, never429.");
            receipt = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
            if (!JsonElement.DeepEquals(receipt,await responses[1].Content.ReadFromJsonAsync<JsonElement>()) || receipt.GetProperty("status").GetString() != "succeeded" ||
                receipt.GetProperty("source_refs").GetArrayLength() != 1) throw new InvalidOperationException("Committed Core receipt mismatch.");
        } finally { foreach (var response in responses) response.Dispose(); }
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Students.CountAsync(x => x.AcademyId == academy && x.LastName == "Opaque proof lifecycle") != 1 ||
                await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingExecuted") != 1) throw new InvalidOperationException("Core execution duplicated student/audit.");
        }
        Pass("parallel execution201/200, identical source-backed receipt, one student and execution audit");
        var after = await Current(id); var finishedOperation = await Read(Route(id)+"/core/operation",binding);
        if (finishedOperation.GetProperty("preview").GetString()!.Contains("No student has been created",StringComparison.Ordinal))
            throw new InvalidOperationException("Succeeded operation preview falsely claims no creation.");
        var result = await Read(Route(id)+"/core/status",binding);
        if (!JsonElement.DeepEquals(receipt,result)) throw new InvalidOperationException("Core status differs from committed receipt.");
        journeys.Add(new { before,approved,receipt,after,finishedOperation });
        Pass("committed status returns exact durable receipt and real audit/source references");
        var minor = await Prepare("Core minor and guardian",true); var minorId = Id(minor);
        var minorApproved = await ConfirmPreparedOnboardingAsync(admin,Route(minorId),minor);
        int usersBefore;
        using (var scope = factory.Services.CreateScope()) usersBefore = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync();
        var minorReceipt = await Read(Route(minorId)+"/core/execute",CoreOnboardingBinding(minorApproved,true),expected:201);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var sources = minorReceipt.GetProperty("source_refs");
            if (sources.GetArrayLength() != 2 || await identity.Users.CountAsync() != usersBefore ||
                !await db.Students.AnyAsync(x => x.Id == Guid.Parse(sources[0].GetString()!) && x.AcademyId == academy) ||
                !await db.Guardians.AnyAsync(x => x.Id == Guid.Parse(sources[1].GetString()!) && x.AcademyId == academy))
                throw new InvalidOperationException("Core minor guardian sources or no-account guarantee failed.");
        }
        Pass("Core minor creation retains guardian domain rules and two owned source references with no portal accounts");
        var savedClock = clock.Value; clock.Value = clock.Value.AddMinutes(11);
        using (var restarted = new QaApiFactory(manifest,flags,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",restarted)) {
            priorState = await Snapshot(); var replay = await Read(Route(id)+"/core/execute",approvedBinding,client);
            if (!JsonElement.DeepEquals(receipt,replay) || priorState != await Snapshot()) throw new InvalidOperationException("Post-expiry restart replay redispatched or lost receipt.");
            if (!JsonElement.DeepEquals(receipt,await Read(Route(id)+"/core/status",binding,client))) throw new InvalidOperationException("Post-expiry status missing committed result.");
        }
        Pass("fresh host committed replay/status survives approval and draft expiry without redispatch");
        clock.Value = savedClock;
        var expired = await Prepare("Expired opaque proof"); var expiredId = Id(expired);
        var expiredChallenge = await Read(Route(expiredId)+"/core/challenge",CoreOnboardingBinding(expired));
        clock.Value = clock.Value.AddMinutes(6);
        await Deny("expired confirmation",410,() => admin.PostAsJsonAsync(Route(expiredId)+"/core/confirm",CoreOnboardingBinding(expiredChallenge,true)));
        var renewed = await Read(Route(expiredId)+"/core/challenge",CoreOnboardingBinding(expired));
        if (renewed.GetProperty("approval_ref").GetString() == expiredChallenge.GetProperty("approval_ref").GetString()) throw new InvalidOperationException("Expired challenge silently reused.");
        await Deny("superseded proof",409,() => admin.PostAsJsonAsync(Route(expiredId)+"/core/confirm",CoreOnboardingBinding(expiredChallenge,true)));
        Pass("only explicit new challenge replaces expired nonce within original draft TTL");
        clock.Value = savedClock;
        var expiredApproved = await Prepare("Expired approved operation"); var expiredApprovedId = Id(expiredApproved);
        var expiredApprovedOperation = await ConfirmPreparedOnboardingAsync(admin,Route(expiredApprovedId),expiredApproved);
        clock.Value = clock.Value.AddMinutes(6);
        await Deny("expired approved execution",410,() => admin.PostAsJsonAsync(Route(expiredApprovedId)+"/core/execute",CoreOnboardingBinding(expiredApprovedOperation,true)));
        await Deny("expired approval cannot project usable operation",410,() => admin.PostAsJsonAsync(Route(expiredApprovedId)+"/core/operation",CoreOnboardingBinding(expiredApprovedOperation)));
        clock.Value = savedClock;
        var edit = await Prepare("Edited opaque proof"); var editId = Id(edit); var editApproved = await ConfirmPreparedOnboardingAsync(admin,Route(editId),edit);
        using (var response = await admin.PutAsJsonAsync(Route(editId),new { expectedRevision = 2,contextVersion = 0,fields = new { } }))
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Core draft edit fixture failed.");
        await Deny("edit invalidates old proof",409,() => admin.PostAsJsonAsync(Route(editId)+"/core/execute",CoreOnboardingBinding(editApproved,true)));
        await Deny("edit cannot erase Core origin for legacy authorization",409,() => admin.PostAsJsonAsync(Route(editId)+"/authorize-creation",new { revision = 3,payloadHash = PentaOperationalPayload.Digest(JsonSerializer.Serialize(new { tool_key = PentaOnboardingDraftService.Tool,tool_revision = 1,fields = new { } })),intent = PentaOnboardingDraftService.CreationIntent }));
        var rollback = await Prepare("Approval audit rollback"); var rollbackId = Id(rollback); var fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",faulty)) {
            await Deny("challenge audit failure full rollback",503,() => client.PostAsJsonAsync(Route(rollbackId)+"/core/challenge",CoreOnboardingBinding(rollback)));
            if (fault.Faults != 1) throw new InvalidOperationException("Challenge audit fault missing.");
        }
        var rollbackChallenge = await Read(Route(rollbackId)+"/core/challenge",CoreOnboardingBinding(rollback));
        fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",faulty)) {
            await Deny("confirmation audit failure full rollback",503,() => client.PostAsJsonAsync(Route(rollbackId)+"/core/confirm",CoreOnboardingBinding(rollbackChallenge,true)));
            if (fault.Faults != 1) throw new InvalidOperationException("Approval audit fault missing.");
        }
        var rollbackApproved = await Read(Route(rollbackId)+"/core/confirm",CoreOnboardingBinding(rollbackChallenge,true));
        fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",faulty)) {
            await Deny("execution audit failure rolls student and proof status back",503,() => client.PostAsJsonAsync(Route(rollbackId)+"/core/execute",CoreOnboardingBinding(rollbackApproved,true)));
            if (fault.Faults != 1) throw new InvalidOperationException("Core execution audit fault missing.");
        }
        fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:fault,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",faulty)) {
            await Deny("required status read audit withholds result",503,() => client.PostAsJsonAsync(Route(id)+"/core/status",binding));
            if (fault.Faults != 1) throw new InvalidOperationException("Core status audit fault missing.");
        }
        var deadline = await Prepare("Approval expires during creation"); var deadlineId = Id(deadline); var deadlineApproved = await ConfirmPreparedOnboardingAsync(admin,Route(deadlineId),deadline);
        var expiryFault = new OnboardingApprovalExpiryFault(clock);
        using (var faulty = new QaApiFactory(manifest,flags,isolatedDomainInterceptor:expiryFault,isolatedClock:clock))
        using (var client = await Client("qa-core-approval@example.invalid",faulty)) {
            await Deny("approval expires during domain create full rollback",410,() => client.PostAsJsonAsync(Route(deadlineId)+"/core/execute",CoreOnboardingBinding(deadlineApproved,true)));
            if (expiryFault.Faults != 1) throw new InvalidOperationException("Approval deadline fault missing.");
        }
        clock.Value = savedClock;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var auditId = Guid.Parse(receipt.GetProperty("audit_ref").GetString()!);
            var audit = await db.AuditLogs.SingleAsync(x => x.Id == auditId); db.AuditLogs.Remove(audit); await db.SaveChangesAsync();
        }
        await Deny("missing committed audit cannot report success",409,() => admin.PostAsJsonAsync(Route(id)+"/core/status",binding));
        await Deny("missing committed audit cannot replay execution",409,() => admin.PostAsJsonAsync(Route(id)+"/core/execute",approvedBinding));
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            (await identity.Users.SingleAsync(x => x.Id == actor)).ConcurrencyStamp = Guid.NewGuid().ToString("N"); await identity.SaveChangesAsync();
        }
        await Deny("changed current authority cannot confirm",409,() => admin.PostAsJsonAsync(Route(rollbackId)+"/core/confirm",CoreOnboardingBinding(rollbackApproved,true)));
        await Deny("changed current authority cannot execute",409,() => admin.PostAsJsonAsync(Route(rollbackId)+"/core/execute",CoreOnboardingBinding(rollbackApproved,true)));
        await Deny("changed current authority cannot read status",409,() => admin.PostAsJsonAsync(Route(rollbackId)+"/core/status",CoreOnboardingBinding(rollbackApproved)));
        var directory = Path.Combine(Directory.GetCurrentDirectory(),"QA","EVIDENCE","penta-onboarding-approval-"+manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory); await File.WriteAllTextAsync(Path.Combine(directory,"core-approval.json"),JsonSerializer.Serialize(new { journeys }));
        if (count != 39) throw new InvalidOperationException("Incomplete Core approval matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGAPPROVAL all "+count+" HTTP/real SQL cases; host-only challenge/confirmation/execution/status, no human-browser or model qualification claim.");
    }

    private sealed class OnboardingApprovalExpiryFault(OnboardingDraftClock clock) : DbCommandInterceptor
    {
        public int Faults { get; private set; }
        private void Advance(DbCommand command) {
            if (Faults == 0 && command.CommandText.Contains("INSERT INTO [Students]",StringComparison.Ordinal)) { Faults++; clock.Value = clock.Value.AddMinutes(6); }
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<DbDataReader> result,CancellationToken token = default) { Advance(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,CommandEventData eventData,InterceptionResult<int> result,CancellationToken token = default) { Advance(command); return ValueTask.FromResult(result); }
    }
}
