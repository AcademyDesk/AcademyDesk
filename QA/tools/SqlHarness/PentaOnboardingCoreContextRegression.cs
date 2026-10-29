using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaOnboardingCoreContextAsync(QaApiFactory baseline, QaRunManifest manifest)
    {
        var flags = new Dictionary<string,string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true", ["Penta:OperationalOnboardingExecutionContractsEnabled"] = "true",
            ["Penta:OperationalOnboardingHttpContractsEnabled"] = "true" };
        var clock = new OnboardingDraftClock();
        using var factory = new QaApiFactory(manifest, flags, isolatedClock: clock);
        Guid academy, foreign, session, branch, hiddenBranch;
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var actor = await CreateAccessActorAsync(factory, "qa-core-context@example.invalid", "AcademyAdmin", academy);
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var owned = new Branch { AcademyId = academy, Name = "Synthetic context branch" };
            var hidden = new Branch { AcademyId = foreign, Name = "Synthetic foreign context branch" };
            db.Branches.AddRange(owned, hidden, new Branch { AcademyId = academy, Name = "Synthetic inactive context branch", IsActive = false });
            branch = owned.Id; hiddenBranch = hidden.Id;
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 0,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic Core fixture; no inference" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        async Task<HttpClient> Client(string email, QaApiFactory? host = null) {
            var client = (host ?? factory).CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }
        using var admin = await Client("qa-core-context@example.invalid");
        using var other = await Client("qa-admin-a@example.invalid");
        using var teacher = await Client("qa-teacher-a@example.invalid");
        using var anonymous = factory.CreateClient();
        string Route(Guid draft, Guid? tenant = null, Guid? conversation = null) => $"/api/academies/{tenant ?? academy}/penta/onboarding/conversations/{conversation ?? session}/drafts/{draft}";
        async Task Save(Guid id, string fields, long revision = 0) {
            using var response = await admin.PutAsJsonAsync(Route(id), new { expectedRevision = revision, contextVersion = 0, fields = JsonDocument.Parse(fields).RootElement });
            if ((int)response.StatusCode != 201) throw new InvalidOperationException("Core context fixture save failed: " + (int)response.StatusCode);
        }
        async Task<string> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.IsActive, x.ConcurrencyStamp }).ToArrayAsync(),
                roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine("PASS: PENTAONBOARDINGCONTEXT " + label); }
        async Task Deny(string label, int expected, Func<Task<HttpResponseMessage>> call) {
            var before = await Snapshot(); using var response = await call();
            if ((int)response.StatusCode != expected || before != await Snapshot())
                throw new InvalidOperationException("Core context denial failed: " + label + " expected=" + expected + " actual=" + (int)response.StatusCode);
            Pass(label + " status=" + expected + " no state changes");
        }
        var id = Guid.NewGuid(); await Save(id, "{}");
        var suffix = "/core-context";
        await Deny("anonymous", 401, () => anonymous.GetAsync(Route(id) + suffix));
        await Deny("teacher", 403, () => teacher.GetAsync(Route(id) + suffix));
        await Deny("foreign tenant", 403, () => admin.GetAsync(Route(id, tenant: foreign) + suffix));
        await Deny("other admin draft privacy", 404, () => other.GetAsync(Route(id) + suffix));
        await Deny("unknown draft", 404, () => admin.GetAsync(Route(Guid.NewGuid()) + suffix));
        await Deny("foreign conversation", 404, () => admin.GetAsync(Route(id, conversation: Guid.NewGuid()) + suffix));
        using (var disabled = await Client("qa-core-context@example.invalid", baseline))
            await Deny("default disabled", 404, () => disabled.GetAsync(Route(id) + suffix));
        var projections = new List<JsonElement>();
        async Task<JsonElement> Current(Guid draft) {
            using var response = await admin.GetAsync(Route(draft) + suffix);
            if ((int)response.StatusCode != 200 || response.Headers.CacheControl?.NoStore != true)
                throw new InvalidOperationException("Core context read failed: " + (int)response.StatusCode);
            var value = await response.Content.ReadFromJsonAsync<JsonElement>(); projections.Add(value); return value;
        }
        var empty = await Current(id);
        var fields = empty.GetProperty("tools")[0].GetProperty("fields");
        if (empty.GetProperty("version").GetString() != "operational-draft-0.3.0" || fields.GetArrayLength() != 29 ||
            empty.GetProperty("draft").GetProperty("status").GetString() != "collecting" ||
            empty.GetProperty("draft").GetProperty("operation_ref").ValueKind != JsonValueKind.Null ||
            empty.GetProperty("scope").GetProperty("context_version").GetInt64() != 1 ||
            empty.GetProperty("scope").GetProperty("actor_id").GetString() != actor.ToString("D") ||
            empty.GetProperty("scope").GetProperty("tenant_id").GetString() != academy.ToString("D") ||
            empty.GetRawText().Contains("ProtectedState", StringComparison.Ordinal) || empty.GetRawText().Contains("SecurityStamp", StringComparison.Ordinal))
            throw new InvalidOperationException("Core scope/catalog projection mismatch.");
        Pass("owned empty draft full29-field catalog, host actor/tenant/positive context and no operation/secret projection");
        var choices = fields.EnumerateArray().Single(x => x.GetProperty("key").GetString() == "BranchId").GetProperty("choices").EnumerateArray().ToArray();
        if (!choices.Any(x => x.GetProperty("id").GetString() == branch.ToString("D")) || choices.Any(x =>
            x.GetProperty("id").GetString() == hiddenBranch.ToString("D") || x.GetProperty("label").GetString() == "Synthetic inactive context branch"))
            throw new InvalidOperationException("Core branch source leakage.");
        Pass("only active owned stable branch choices; foreign/inactive excluded");
        var partial = JsonSerializer.Serialize(new Dictionary<string, object> {
            ["StudentFirstName"] = new { kind = "text", value = "Synthetic" },
            ["DateOfBirth"] = new { kind = "date", value = "2015-01-01" },
            ["BranchId"] = new { kind = "reference", value = branch.ToString("D") } });
        await Save(id, partial, 1); var minor = await Current(id);
        if (minor.GetProperty("tools")[0].GetProperty("fields").EnumerateArray().Count(x => x.GetProperty("required").GetBoolean()) != 6 ||
            minor.GetProperty("draft").GetProperty("revision").GetInt64() != 2)
            throw new InvalidOperationException("Core minor conditional requirements missing.");
        Pass("retained partial fields/revision, real owned branch identity and shared conditional minor requirements");
        var adultId = Guid.NewGuid();
        var adult = "{\"StudentFirstName\":{\"kind\":\"text\",\"value\":\"Synthetic\"},\"StudentLastName\":{\"kind\":\"text\",\"value\":\"Core context student\"},\"DateOfBirth\":{\"kind\":\"date\",\"value\":\"2000-01-01\"}}";
        await Save(adultId, adult);
        using (var review = await admin.GetAsync(Route(adultId))) {
            var saved = (await review.Content.ReadFromJsonAsync<AcademyDesk.Api.Intelligence.Penta.PentaOnboardingDraftView>())!;
            using var ordinary = await admin.PostAsJsonAsync(Route(adultId) + "/review", new { revision = 1, payloadHash = saved.PayloadHash });
            if ((int)ordinary.StatusCode != 200) throw new InvalidOperationException("Core review fixture failed.");
            var reviewed = await Current(adultId);
            if (reviewed.GetProperty("draft").GetProperty("status").GetString() != "collecting") throw new InvalidOperationException("Core ordinary review became approval.");
            Pass("ordinary review is not Core approval/preparation");
            using var authorization = await admin.PostAsJsonAsync(Route(adultId) + "/authorize-creation", new { revision = 1, payloadHash = saved.PayloadHash,
                intent = AcademyDesk.Api.Intelligence.Penta.PentaOnboardingDraftService.CreationIntent });
            if ((int)authorization.StatusCode != 200) throw new InvalidOperationException("Core authorization fixture failed.");
            var approved = await Current(adultId);
            if (approved.GetProperty("draft").GetProperty("status").GetString() != "approved" || approved.GetProperty("draft").GetProperty("operation_ref").GetString() is null)
                throw new InvalidOperationException("Core actual host authorization projection failed.");
            Pass("approved only from actual scoped host authorization with stable operation ref");
            using var execution = await admin.PostAsJsonAsync(Route(adultId) + "/execute", new { revision = 1, payloadHash = saved.PayloadHash });
            if ((int)execution.StatusCode != 201) throw new InvalidOperationException("Core execution fixture failed.");
            var succeeded = await Current(adultId);
            if (succeeded.GetProperty("draft").GetProperty("status").GetString() != "succeeded") throw new InvalidOperationException("Core committed status missing.");
            Pass("succeeded source/audit independently verified; no model execution");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audit = await db.AuditLogs.SingleAsync(x => x.EntityId == adultId && x.Action == "PentaOnboardingExecuted");
            var originalAction = audit.Action; audit.Action = "Synthetic changed execution evidence"; await db.SaveChangesAsync();
            await Deny("missing authoritative execution audit", 409, () => admin.GetAsync(Route(adultId) + suffix));
            audit.Action = originalAction; await db.SaveChangesAsync();
        }
        var privateId = Guid.NewGuid(); await Save(privateId, "{\"MedicalOrAccessibilityNotes\":{\"kind\":\"text\",\"value\":\"Synthetic private note\"}}");
        await Deny("medical context stays secure, never silently omitted", 409, () => admin.GetAsync(Route(privateId) + suffix));
        var wrongBranchId = Guid.NewGuid(); await Save(wrongBranchId, JsonSerializer.Serialize(new { BranchId = new { kind = "reference", value = hiddenBranch.ToString("D") } }));
        await Deny("foreign branch draft not exported", 409, () => admin.GetAsync(Route(wrongBranchId) + suffix));
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaMiniSessions.SingleAsync(x => x.Id == session)).Version = 1; await db.SaveChangesAsync();
        }
        await Deny("stale conversation", 409, () => admin.GetAsync(Route(id) + suffix));
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.PentaMiniSessions.SingleAsync(x => x.Id == session)).Version = 0; await db.SaveChangesAsync();
        }
        var fault = new OnboardingExecutionFault("audit");
        using (var faulty = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault, isolatedClock: clock))
        using (var client = await Client("qa-core-context@example.invalid", faulty)) {
            await Deny("required context audit rollback", 503, () => client.GetAsync(Route(id) + suffix));
            if (fault.Faults != 1) throw new InvalidOperationException("Core context audit fault not reached.");
        }
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user = await identity.Users.SingleAsync(x => x.Id == actor); user.ConcurrencyStamp = Guid.NewGuid().ToString("N"); await identity.SaveChangesAsync();
        }
        await Deny("changed current authority closes draft epoch", 409, () => admin.GetAsync(Route(id) + suffix));
        var fresh = Guid.NewGuid(); await Save(fresh, "{}"); var newScope = await Current(fresh);
        if (newScope.GetProperty("scope").GetProperty("connection_id").GetString() == empty.GetProperty("scope").GetProperty("connection_id").GetString())
            throw new InvalidOperationException("Authority epoch reused old scope.");
        Pass("fresh authorized draft gets distinct immutable authority scope");
        clock.Value = clock.Value.AddMinutes(11);
        await Deny("expired scope", 410, () => admin.GetAsync(Route(fresh) + suffix));
        // Source-only synthetic context receipt, never credentials or DP state.
        // The owned SQL host/preflight above bounds every exported source row.
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "penta-onboarding-core-context-" + manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(projections);
        await File.WriteAllTextAsync(Path.Combine(directory, "snapshots.json"), json);
        Console.WriteLine("PASS: PENTAONBOARDINGCONTEXT synthetic snapshot receipt SHA256=" + Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))));
        if (count != 21) throw new InvalidOperationException("Incomplete Core context matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGCONTEXT all " + count + " HTTP/real SQL cases; host current context/catalog only, no inference or activation.");
    }
}
