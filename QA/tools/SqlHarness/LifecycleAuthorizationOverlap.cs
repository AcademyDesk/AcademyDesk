using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // QA scheduling only. Pause BEFORE the real SQL authorization query runs;
    // do not alter query text/results, auth, application responses or isolation.
    private sealed class LifecycleAuthorizationGate : DbCommandInterceptor
    {
        private readonly object sync = new();
        private TaskCompletionSource entered = NewSignal(), release = NewSignal();
        private Func<string, bool>? match;
        private int claimed;
        private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm(bool teacher)
        {
            lock (sync)
            {
                if (match is not null) throw new InvalidOperationException("Lifecycle gate already armed.");
                entered = NewSignal(); release = NewSignal(); claimed = 0;
                match = teacher
                    ? sql => sql.Contains("SELECT CASE", StringComparison.Ordinal) && sql.Contains("[Teachers]", StringComparison.Ordinal) && sql.Contains("[Academies]", StringComparison.Ordinal)
                    : sql => sql.Contains("[StudentGuardians]", StringComparison.Ordinal) && sql.Contains("[CanAccessPortal] = CAST(1 AS bit)", StringComparison.Ordinal) && sql.Contains("[AccessRevokedAtUtc] IS NULL", StringComparison.Ordinal);
            }
        }
        public Task WaitAsync() => entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
        public void Release() { lock (sync) { match = null; release.TrySetResult(); } }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Task? wait = null;
            lock (sync)
                if (match?.Invoke(command.CommandText) == true && Interlocked.CompareExchange(ref claimed, 1, 0) == 0)
                { entered.TrySetResult(); wait = release.Task; }
            if (wait is not null) await wait.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return result;
        }
    }

    private static async Task<JsonElement> LifecycleOverlapState(QaApiFactory factory, QaRunManifest manifest)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var files = Directory.GetFiles(manifest.WebRoot, "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(manifest.StorageRoot, "*", SearchOption.AllDirectories)).OrderBy(x => x, StringComparer.Ordinal)
            .Select(x => new { path = Path.GetRelativePath(manifest.Root, x), hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x))) }).ToArray();
        return JsonSerializer.SerializeToElement(new
        {
            academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            resources = await db.LearningResources.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.TeacherId, x.GuardianId, x.IsActive }).ToArrayAsync(),
            roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync(), files
        });
    }
    private static void LifecycleSame(JsonElement before, JsonElement after, params string[] except)
    {
        foreach (var field in before.EnumerateObject())
            if (!except.Contains(field.Name) && !JsonElement.DeepEquals(field.Value, after.GetProperty(field.Name)))
                throw new InvalidOperationException("Lifecycle overlap changed protected snapshot field: " + field.Name);
    }
    private static async Task<HttpResponseMessage> LifecycleHttp(HttpClient client, HttpRequestMessage request, string token)
    {
        await Task.Delay(650);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.TooManyRequests) { response.Dispose(); throw new InvalidOperationException("Lifecycle overlap cannot pass with429."); }
        return response;
    }

    private static async Task VerifyTeacherAuthorizationOverlapAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, LifecycleAuthorizationGate gate)
    {
        Console.WriteLine("REVOCATION Teacher waiting unchanged limiter window; retained62 checks precede this gate.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        Guid academy, teacher; Batch batch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            batch = await db.Batches.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Name == "Synthetic Batch");
            teacher = batch.TeacherId!.Value;
        }
        var token = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        var initial = await LifecycleOverlapState(factory, manifest); var results = new List<object>(); var checks = 0;
        async Task Active(string mode, bool active)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (mode == "academy") (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = active;
            else (await db.Teachers.SingleAsync(x => x.Id == teacher)).IsActive = active;
            await db.SaveChangesAsync();
        }
        HttpRequestMessage Request(string action, string label)
        {
            if (action == "resources") return new(HttpMethod.Get, $"/api/teacher/resources?batchId={batch.Id}");
            if (action == "note") return new(HttpMethod.Post, "/api/teacher/resources/note") { Content = JsonContent.Create(new TeacherCreateResourceNoteRequest(batch.Id, null, null, label, "Synthetic queued authorization", null)) };
            var form = new MultipartFormDataContent(); form.Add(new StringContent(batch.Id.ToString()), "BatchId"); form.Add(new StringContent(label), "Title"); form.Add(new ByteArrayContent(new byte[] {1,2,3,4,5}), "File", "overlap-fixture.pdf");
            return new(HttpMethod.Post, "/api/teacher/resources/upload") { Content = form };
        }
        foreach (var mode in new[] { "academy", "teacher" }) foreach (var action in new[] { "resources", "note", "upload" }) for (var n = 1; n <= 5; n++)
        {
            var label = $"queued-{mode}-{action}-{n}"; var before = await LifecycleOverlapState(factory, manifest);
            gate.Arm(teacher: true); using var request = Request(action, label); var pending = LifecycleHttp(client, request, token);
            JsonElement revoked = default;
            try
            {
                await gate.WaitAsync(); await Active(mode, false); revoked = await LifecycleOverlapState(factory, manifest);
                LifecycleSame(before, revoked, mode == "academy" ? "academies" : "teachers");
                var field = mode == "academy" ? "academies" : "teachers"; var id = mode == "academy" ? academy : teacher;
                var oldRows = before.GetProperty(field).EnumerateArray().ToArray(); var newRows = revoked.GetProperty(field).EnumerateArray().ToArray();
                if (oldRows.Length != newRows.Length || newRows.Single(x => x.GetProperty("Id").GetGuid() == id).GetProperty("IsActive").GetBoolean()) throw new InvalidOperationException("Authority revocation did not commit.");
                foreach (var old in oldRows)
                {
                    var current = newRows.Single(x => x.GetProperty("Id").GetGuid() == old.GetProperty("Id").GetGuid());
                    foreach (var property in old.EnumerateObject())
                        if (!(old.GetProperty("Id").GetGuid() == id && property.Name == "IsActive") && !JsonElement.DeepEquals(property.Value, current.GetProperty(property.Name))) throw new InvalidOperationException("Authority fixture changed another field/tenant.");
                }
            }
            finally { gate.Release(); }
            using var denied = await pending; RequireFinanceStatus(denied, HttpStatusCode.Forbidden, label);
            LifecycleSame(revoked, await LifecycleOverlapState(factory, manifest)); checks++;
            if ((await denied.Content.ReadAsStringAsync()).Contains("Lifecycle own sentinel", StringComparison.Ordinal)) throw new InvalidOperationException("Revoked Teacher data disclosure.");
            await Active(mode, true); LifecycleSame(before, await LifecycleOverlapState(factory, manifest));
            using var restoredRequest = Request("resources", label); using var restored = await LifecycleHttp(client, restoredRequest, token); RequireFinanceStatus(restored, HttpStatusCode.OK, label+"-restored");
            var restoredBody = await restored.Content.ReadAsStringAsync();
            if (!restoredBody.Contains("Lifecycle own sentinel", StringComparison.Ordinal) || restoredBody.Contains("Lifecycle foreign sentinel", StringComparison.Ordinal) || restoredBody.Contains("Lifecycle unassigned sentinel", StringComparison.Ordinal)) throw new InvalidOperationException("Restored Teacher scope mismatch.");
            LifecycleSame(before, await LifecycleOverlapState(factory, manifest)); checks++;
            results.Add(new { label, barrier = "before real domain authorization SQL", committedRevocation = true, denied = 403, restored = 200 });
            Console.WriteLine($"REVOCATION Teacher PASS {label}: request queued, revocation committed,403/full15-field no-write/files; same-token restored200.");
        }
        LifecycleSame(initial, await LifecycleOverlapState(factory, manifest));
        if (checks != 60 || results.Count != 30) throw new InvalidOperationException("Teacher overlap matrix incomplete.");
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA/EVIDENCE/lifecycle-overlap", manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "teacher.json"), JsonSerializer.Serialize(new { run = manifest.RunId, pairs = 30, checks, results, boundary = "Queued before authorization SQL; not post-admission cancellation, natural lock contention or browser/device acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("REVOCATION Teacher PASS:30 coordinated queued-authority cases/60 denial-restoration checks, exact SQL/Identity/finance/audit/files preserved, no429. Post-admission/browser/devices NOT RUN.");
    }

    private static async Task VerifyGuardianAuthorizationOverlapAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, LifecycleAuthorizationGate gate)
    {
        Console.WriteLine("REVOCATION Guardian waiting unchanged limiter window; retained90 checks precede this gate."); await Task.Delay(TimeSpan.FromSeconds(60));
        Guid academy, adminActor; Guardian guardian; Student child; StudentGuardian link; Invoice invoice;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            guardian = new() { AcademyId = academy, FirstName = "Queued", LastName = "Guardian" };
            child = new() { AcademyId = academy, FirstName = "Queued", LastName = "Minor", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-10) };
            link = new() { AcademyId = academy, StudentId = child.Id, GuardianId = guardian.Id, CanAccessPortal = true, AccessGrantedAtUtc = DateTime.UtcNow, Relationship = "Parent" };
            invoice = new() { AcademyId = academy, StudentId = child.Id, InvoiceNumber = "QA-Q-"+child.Id.ToString("N"), TotalAmount = 1000m };
            db.AddRange(guardian, child, link, invoice); await db.SaveChangesAsync();
            adminActor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        var parent = await CreateAccessActorAsync(factory, "qa-queued-parent@example.invalid", "Guardian", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(parent.ToString()))!; user.GuardianId = guardian.Id;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Queued guardian fixture failed.");
        }
        var token = await LoginAsync(client, "qa-queued-parent@example.invalid", "Synthetic!39Ab"); var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        var route = $"/api/academies/{academy}/students/{child.Id}/guardians/{guardian.Id}/portal-access";
        async Task Change(ParentPortalAccessRequest value)
        {
            var before = await LifecycleOverlapState(factory, manifest); using var command = new HttpRequestMessage(HttpMethod.Patch, route) { Content = JsonContent.Create(value) };
            using var response = await LifecycleHttp(client, command, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "queued guardian permission mutation");
            var after = await LifecycleOverlapState(factory, manifest); LifecycleSame(before, after, "links", "audits");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var wire = json.RootElement;
            var stored = after.GetProperty("links").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == link.Id);
            foreach (var (field, jsonField, expected) in new[] {
                ("CanAccessPortal", "canAccessPortal", value.AllowPortalAccess),
                ("CanViewAcademicProgress", "canViewAcademicProgress", value.AllowPortalAccess && value.AllowAcademicProgress),
                ("CanViewFinance", "canViewFinance", value.AllowPortalAccess && value.AllowFinance),
                ("CanViewDocuments", "canViewDocuments", value.AllowPortalAccess && value.AllowDocuments),
                ("CanManageLeave", "canManageLeave", value.AllowPortalAccess && value.AllowLeave) })
                if (stored.GetProperty(field).GetBoolean() != expected || wire.GetProperty(jsonField).GetBoolean() != expected) throw new InvalidOperationException("Queued guardian SQL/wire flags differ.");
            var previous = before.GetProperty("links").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == link.Id);
            var mutable = new[] { "CanAccessPortal", "CanViewAcademicProgress", "CanViewFinance", "CanViewDocuments", "CanManageLeave", "AccessGrantedAtUtc", "AccessRevokedAtUtc" };
            foreach (var property in previous.EnumerateObject()) if (!mutable.Contains(property.Name) && !JsonElement.DeepEquals(property.Value, stored.GetProperty(property.Name))) throw new InvalidOperationException("Queued permission write changed link identity/relationship.");
            if (before.GetProperty("links").GetArrayLength() != after.GetProperty("links").GetArrayLength()) throw new InvalidOperationException("Queued guardian link count changed.");
            foreach (var (field, jsonField, populated) in new[] { ("AccessGrantedAtUtc", "accessGrantedAtUtc", value.AllowPortalAccess), ("AccessRevokedAtUtc", "accessRevokedAtUtc", !value.AllowPortalAccess) })
                if (populated ? stored.GetProperty(field).ValueKind == JsonValueKind.Null || wire.GetProperty(jsonField).ValueKind == JsonValueKind.Null || stored.GetProperty(field).GetDateTime().Ticks != wire.GetProperty(jsonField).GetDateTime().Ticks : stored.GetProperty(field).ValueKind != JsonValueKind.Null || wire.GetProperty(jsonField).ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Queued guardian authority timestamps differ.");
            foreach (var old in before.GetProperty("links").EnumerateArray()) if (old.GetProperty("Id").GetGuid() != link.Id && !after.GetProperty("links").EnumerateArray().Any(x => JsonElement.DeepEquals(old, x))) throw new InvalidOperationException("Queued guardian mutation changed another grant.");
            var oldAudits = before.GetProperty("audits").EnumerateArray().ToArray(); var audits = after.GetProperty("audits").EnumerateArray().ToArray();
            if (audits.Length != oldAudits.Length + 1 || oldAudits.Any(x => !audits.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Queued guardian mutation audit count differs.");
            var audit = audits.Single(x => !oldAudits.Any(y => JsonElement.DeepEquals(x,y)));
            if (audit.GetProperty("AcademyId").GetGuid() != academy || audit.GetProperty("ActorUserId").GetGuid() != adminActor || audit.GetProperty("Action").GetString() != "PATCH StudentGuardians" || JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString() != route) throw new InvalidOperationException("Queued guardian audit actor/tenant/route differs.");
        }
        var results = new List<object>();
        foreach (var mode in new[] { "portal", "finance" }) for (var n = 1; n <= 5; n++)
        {
            var path = $"/api/portal/students/{child.Id}" + (mode == "finance" ? $"/invoices/{invoice.Id}/download" : "");
            var before = await LifecycleOverlapState(factory, manifest); gate.Arm(teacher: false); using var request = new HttpRequestMessage(HttpMethod.Get, path); var pending = LifecycleHttp(client, request, token);
            JsonElement revoked = default;
            try { await gate.WaitAsync(); await Change(mode == "portal" ? new(false) : new(true, AllowFinance: false)); revoked = await LifecycleOverlapState(factory, manifest); }
            finally { gate.Release(); }
            using var denied = await pending; RequireFinanceStatus(denied, HttpStatusCode.Forbidden, $"queued guardian-{mode}-{n}"); LifecycleSame(revoked, await LifecycleOverlapState(factory, manifest));
            var deniedText = await denied.Content.ReadAsStringAsync(); if (deniedText.Contains(invoice.InvoiceNumber, StringComparison.Ordinal) || deniedText.Contains("Queued Minor", StringComparison.Ordinal)) throw new InvalidOperationException("Revoked guardian protected content disclosed.");
            await Change(new(true)); var restoredState = await LifecycleOverlapState(factory, manifest); using var restoredRequest = new HttpRequestMessage(HttpMethod.Get, path); using var restored = await LifecycleHttp(client, restoredRequest, token); RequireFinanceStatus(restored, HttpStatusCode.OK, "queued guardian restored");
            var body = await restored.Content.ReadAsStringAsync(); if (mode == "portal" ? !body.Contains("Queued Minor", StringComparison.Ordinal) : !body.Contains(invoice.InvoiceNumber, StringComparison.Ordinal) || restored.Content.Headers.ContentType?.MediaType != "text/html") throw new InvalidOperationException("Restored guardian own read/document differs.");
            LifecycleSame(restoredState, await LifecycleOverlapState(factory, manifest));
            results.Add(new { mode, attempt = n, barrier = "before real guardian grant SQL", revoked = 403, restored = 200 });
            Console.WriteLine($"REVOCATION Guardian PASS {mode}-{n}: queued request, real PATCH200/committed SQL grant+exact actor audit,403/no-write, real regrant200/same-token own read200.");
        }
        if (results.Count != 10) throw new InvalidOperationException("Guardian overlap matrix incomplete.");
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA/EVIDENCE/lifecycle-overlap", manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "guardian.json"), JsonSerializer.Serialize(new { run = manifest.RunId, pairs = 10, permissionWrites = 20, checks = 20, results, boundary = "Queued before grant SQL; not post-admission cancellation, natural lock contention or browser/device acceptance" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("REVOCATION Guardian PASS:10 coordinated queued-grant cases/20 denial-restoration reads/20 real revoke-regrant PATCH writes/exact audits/no429. Post-admission/browser/devices NOT RUN.");
    }
}
