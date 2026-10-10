using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyGuardianRevocationAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_GUARDIAN_REVOCATION_BASELINE") == "1";
        using var errors = new GuardianQueryErrorProbe();
        factory.Services.GetRequiredService<ILoggerFactory>().AddProvider(errors);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid academy, foreign, adminActor; Guardian guardian; Student unrelated, foreignChild;
        var children = new List<(string Label, Student Child, StudentGuardian Link)>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            guardian = new() { AcademyId = academy, FirstName = "Revocation", LastName = "Guardian" };
            unrelated = new() { AcademyId = academy, FirstName = "Unlinked", LastName = "Sentinel" };
            foreignChild = new() { AcademyId = foreign, FirstName = "Foreign", LastName = "Sentinel" };
            db.AddRange(guardian, unrelated, foreignChild);
            foreach (var (label, dob) in new[] { ("minor", (DateOnly?)today.AddYears(-10)), ("adult", (DateOnly?)today.AddYears(-25)), ("birthday18", (DateOnly?)today.AddYears(-18)), ("unknown", (DateOnly?)null) })
            {
                var child = new Student { AcademyId = academy, FirstName = "Revoke", LastName = label, DateOfBirth = dob };
                var link = new StudentGuardian { AcademyId = academy, StudentId = child.Id, GuardianId = guardian.Id, Relationship = "Parent", IsPrimary = true, CanAccessPortal = true, AccessGrantedAtUtc = DateTime.UtcNow.AddDays(-1) };
                db.AddRange(child, link); children.Add((label, child, link));
            }
            // A second guardian for the minor must remain unaffected by this guardian's revocation.
            var second = new Guardian { AcademyId = academy, FirstName = "Other", LastName = "Guardian" };
            db.AddRange(second, new StudentGuardian { AcademyId = academy, StudentId = children[0].Child.Id, GuardianId = second.Id, CanAccessPortal = true, AccessGrantedAtUtc = DateTime.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync();
            adminActor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var actor = await CreateAccessActorAsync(factory, "qa-revocation-parent@example.invalid", "Guardian", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(actor.ToString()))!;
            user.GuardianId = guardian.Id;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Guardian revocation Identity fixture failed");
        }
        var parentToken = await LoginAsync(client, "qa-revocation-parent@example.invalid", "Synthetic!39Ab");
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var results = new List<object>();
        string Route(Guid child) => $"/api/academies/{academy}/students/{child}/guardians/{guardian.Id}/portal-access";
        async Task<JsonElement> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.SerializeToElement(new
            {
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                resources = await db.LearningResources.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.GuardianId, x.IsActive }).ToListAsync(),
                files = Directory.GetFiles(manifest.WebRoot, "*", SearchOption.AllDirectories).Concat(Directory.GetFiles(manifest.StorageRoot, "*", SearchOption.AllDirectories))
                    .OrderBy(x => x, StringComparer.Ordinal).Select(x => new { path = Path.GetRelativePath(manifest.Root, x), hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(x))) }).ToArray()
            });
        }
        async Task<(JsonElement Body, JsonElement Before, JsonElement After)> Send(string label, HttpRequestMessage request, string? auth, HttpStatusCode expected, bool mutation = false)
        {
            using (request)
            {
                if (today != DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("UTC birthday fixture crossed date boundary; rerun fresh");
                await Task.Delay(650); // Preserve the real limiter, including rejection of 429 as a false pass.
                var before = await Snapshot();
                if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
                using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, label);
                if (expected == HttpStatusCode.InternalServerError &&
                    (errors.LastException is not InvalidOperationException || !errors.LastException.Message.Contains("could not be translated", StringComparison.Ordinal)))
                    throw new InvalidOperationException("Guardian child-list diagnostic was not the expected EF query translation failure");
                var after = await Snapshot();
                if (!mutation && !JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Guardian denied/read request changed SQL, authority, audit or files: " + label);
                var text = await response.Content.ReadAsStringAsync();
                var body = string.IsNullOrWhiteSpace(text) ? JsonSerializer.SerializeToElement<object?>(null) : JsonDocument.Parse(text).RootElement.Clone();
                results.Add(new { label, status = (int)response.StatusCode });
                Console.WriteLine($"GUARDIANREVOKE CASE {label} status={(int)response.StatusCode} {(baseline ? "OBSERVED" : "PASS")}.");
                return (body, before, after);
            }
        }
        async Task Read(string label, Student child, bool allowed)
        {
            var detail = await Send(label + "-details", new(HttpMethod.Get, $"/api/portal/students/{child.Id}"), parentToken, allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden);
            if (allowed && detail.Body.GetProperty("name").GetString() != child.FirstName + " " + child.LastName) throw new InvalidOperationException("Guardian returned wrong child");
            foreach (var path in new[] { $"/api/portal/guardians/{guardian.Id}/children", "/api/portal/me" })
            {
                var list = await Send(label + (path.EndsWith("/me") ? "-me" : "-children"), new(HttpMethod.Get, path), parentToken,
                    baseline && !path.EndsWith("/me") ? HttpStatusCode.InternalServerError : HttpStatusCode.OK);
                if (baseline && !path.EndsWith("/me")) continue; // Classified independent query failure, not a security denial/pass.
                var items = path.EndsWith("/me") ? list.Body.GetProperty("children") : list.Body;
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var expected = await db.StudentGuardians.AsNoTracking().Where(x => x.AcademyId == academy && x.GuardianId == guardian.Id && x.CanAccessPortal && x.AccessRevokedAtUtc == null).Select(x => x.StudentId).ToListAsync();
                var actual = items.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToList();
                if (!actual.OrderBy(x => x).SequenceEqual(expected.OrderBy(x => x)) || actual.Contains(child.Id) != allowed || actual.Contains(unrelated.Id) || actual.Contains(foreignChild.Id)) throw new InvalidOperationException("Guardian child visibility does not match current grants");
            }
        }
        async Task Change(string label, Student child, StudentGuardian link, object payload, bool access, bool flags)
        {
            var start = DateTime.UtcNow;
            var result = await Send(label, new(HttpMethod.Patch, Route(child.Id)) { Content = JsonContent.Create(payload) }, adminToken, HttpStatusCode.OK, true);
            var end = DateTime.UtcNow;
            foreach (var field in new[] { "students", "guardians", "notifications", "resources", "users", "files" })
                if (!JsonElement.DeepEquals(result.Before.GetProperty(field), result.After.GetProperty(field))) throw new InvalidOperationException("Guardian command changed " + field);
            var oldLinks = result.Before.GetProperty("links").EnumerateArray().ToArray(); var newLinks = result.After.GetProperty("links").EnumerateArray().ToArray();
            if (oldLinks.Length != newLinks.Length || oldLinks.Where(x => x.GetProperty("Id").GetGuid() != link.Id).Any(x => !newLinks.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Other guardian grants changed");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var persisted = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.Id == link.Id);
            if (persisted.CanAccessPortal != access || persisted.CanViewAcademicProgress != flags || persisted.CanViewFinance != flags || persisted.CanViewDocuments != flags || persisted.CanManageLeave != flags || persisted.Relationship != "Parent" || !persisted.IsPrimary || persisted.StudentId != child.Id || persisted.GuardianId != guardian.Id || persisted.AcademyId != academy) throw new InvalidOperationException("Explicit guardian authority not persisted as expected");
            var timestamp = access ? persisted.AccessGrantedAtUtc : persisted.AccessRevokedAtUtc;
            if (!timestamp.HasValue || timestamp < start || timestamp > end || (access ? persisted.AccessRevokedAtUtc is not null : persisted.AccessGrantedAtUtc is not null)) throw new InvalidOperationException("Guardian grant/revoke timestamps mismatch");
            var body = result.Body;
            if (body.GetProperty("guardianId").GetGuid() != guardian.Id || body.GetProperty("canAccessPortal").GetBoolean() != access || body.GetProperty("canViewAcademicProgress").GetBoolean() != flags || body.GetProperty("canViewFinance").GetBoolean() != flags || body.GetProperty("canViewDocuments").GetBoolean() != flags || body.GetProperty("canManageLeave").GetBoolean() != flags || body.GetProperty(access ? "accessGrantedAtUtc" : "accessRevokedAtUtc").GetDateTime() != timestamp || body.GetProperty(access ? "accessRevokedAtUtc" : "accessGrantedAtUtc").ValueKind != JsonValueKind.Null) throw new InvalidOperationException("Guardian response differs from persisted grant");
            var oldAudits = result.Before.GetProperty("audits").EnumerateArray().ToArray(); var audits = result.After.GetProperty("audits").EnumerateArray().ToArray();
            if (audits.Length != oldAudits.Length + 1 || oldAudits.Any(x => !audits.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Guardian audit count/prior rows mismatch");
            var added = audits.Single(x => !oldAudits.Any(y => JsonElement.DeepEquals(x, y)));
            if (added.GetProperty("ActorUserId").GetGuid() != adminActor || added.GetProperty("AcademyId").GetGuid() != academy || added.GetProperty("Action").GetString() != "PATCH StudentGuardians" || JsonDocument.Parse(added.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString() != Route(child.Id)) throw new InvalidOperationException("Guardian audit attribution mismatch");
        }
        object Explicit(bool access, bool permissions) => new { allowPortalAccess = access, allowAcademicProgress = permissions, allowFinance = permissions, allowDocuments = permissions, allowLeave = permissions };
        foreach (var item in baseline ? children.Take(1) : children)
        {
            await Read(item.Label + "-active", item.Child, true);
            await Change(item.Label + "-explicit-revoke", item.Child, item.Link, Explicit(false, false), baseline, false);
            await Read(item.Label + "-explicit-revoke", item.Child, baseline);
            if (baseline)
            {
                await Change(item.Label + "-omitted-revoke", item.Child, item.Link, new { allowPortalAccess = false }, true, true);
                await Read(item.Label + "-omitted-revoke", item.Child, true);
                break;
            }
            await Change(item.Label + "-restricted-regrant", item.Child, item.Link, Explicit(true, false), true, false);
            await Read(item.Label + "-restricted-regrant", item.Child, true);
            await Change(item.Label + "-omitted-revoke", item.Child, item.Link, new { allowPortalAccess = false }, false, false);
            await Read(item.Label + "-omitted-revoke", item.Child, false);
            await Change(item.Label + "-default-regrant", item.Child, item.Link, new { allowPortalAccess = true }, true, true);
            await Read(item.Label + "-default-regrant", item.Child, true);
        }
        if (!baseline)
        {
            var item = children[0];
            foreach (var (label, auth, status, target) in new[] {
                ("anonymous", (string?)null, HttpStatusCode.Unauthorized, item.Child.Id),
                ("foreign-admin", (string?)foreignToken, HttpStatusCode.Forbidden, item.Child.Id),
                ("parent-self-escalation", (string?)parentToken, HttpStatusCode.Forbidden, item.Child.Id),
                ("teacher", (string?)teacherToken, HttpStatusCode.Forbidden, item.Child.Id),
                ("unlinked", (string?)adminToken, HttpStatusCode.NotFound, unrelated.Id),
                ("foreign-child", (string?)adminToken, HttpStatusCode.NotFound, foreignChild.Id),
                ("missing-child", (string?)adminToken, HttpStatusCode.NotFound, Guid.NewGuid()) })
                await Send(label + "-command-denied", new(HttpMethod.Patch, Route(target)) { Content = JsonContent.Create(Explicit(true, true)) }, auth, status);
            await Send("unlinked-child-denied", new(HttpMethod.Get, $"/api/portal/students/{unrelated.Id}"), parentToken, HttpStatusCode.Forbidden);
            await Send("foreign-child-denied", new(HttpMethod.Get, $"/api/portal/students/{foreignChild.Id}"), parentToken, HttpStatusCode.Forbidden);
            var unlink = await Send("unlink-minor", new(HttpMethod.Delete, Route(item.Child.Id).Replace("/portal-access", "", StringComparison.Ordinal)), adminToken, HttpStatusCode.NoContent, true);
            foreach (var field in new[] { "students", "guardians", "notifications", "resources", "users", "files" })
                if (!JsonElement.DeepEquals(unlink.Before.GetProperty(field), unlink.After.GetProperty(field))) throw new InvalidOperationException("Unlink changed " + field);
            var prior = unlink.Before.GetProperty("links").EnumerateArray().ToArray(); var remaining = unlink.After.GetProperty("links").EnumerateArray().ToArray();
            if (remaining.Length != prior.Length - 1 || remaining.Any(x => !prior.Any(y => JsonElement.DeepEquals(x, y))) || remaining.Any(x => x.GetProperty("Id").GetGuid() == item.Link.Id)) throw new InvalidOperationException("Unlink changed unrelated grants");
            if (unlink.After.GetProperty("audits").GetArrayLength() != unlink.Before.GetProperty("audits").GetArrayLength() + 1) throw new InvalidOperationException("Unlink missing audit");
            await Read("unlinked-minor", item.Child, false);
            await Send("regrant-unlinked-denied", new(HttpMethod.Patch, Route(item.Child.Id)) { Content = JsonContent.Create(Explicit(true, true)) }, adminToken, HttpStatusCode.NotFound);
        }
        var evidence = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "guardian-revocation", manifest.RunId.ToString("N"));
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "results.json"), JsonSerializer.Serialize(new { run = manifest.RunId, baseline, utcDate = today, cases = results.Count, results }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"GUARDIANREVOKE {(baseline ? "BASELINE CLASSIFIED: minor explicit/omitted revocations silently retain access; NOT security acceptance" : "REGRESSION PASS")} cases={results.Count}; real Identity/SQL/HTTP and current grants.");
    }

    private sealed class GuardianQueryErrorProbe : ILoggerProvider, ILogger
    {
        public Exception? LastException { get; private set; }
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { if (logLevel >= LogLevel.Error && exception is not null) LastException = exception; }
        public void Dispose() { }
    }
}
