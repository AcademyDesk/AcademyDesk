using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private static async Task VerifyProvisioningConcurrencyAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        const string password = "Synthetic!39Ab";
        Guid academy, adminId; var links = new Dictionary<string, Guid>(); var ownerIds = new List<Guid>(); var ownerTokens = new List<string>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            links["Student"] = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            links["Teacher"] = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var guardian = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Concurrency" }; db.Guardians.Add(guardian); links["Guardian"] = guardian.Id; await db.SaveChangesAsync();
            adminId = (await users.FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
            for (int i = 0; i < 2; i++)
            {
                var email = $"qa-concurrency-owner-{i}@example.invalid"; var user = new ApplicationUser { Email = email, UserName = email, DisplayName = "Synthetic unassigned " + i };
                Check((await users.CreateAsync(user, password)).Succeeded, "Unassigned creator fixture failed"); ownerIds.Add(user.Id);
            }
        }
        foreach (var i in Enumerable.Range(0, 2)) ownerTokens.Add(await LoginAsync(client, $"qa-concurrency-owner-{i}@example.invalid", password));
        var platformId = await CreateAccessActorAsync(factory, "qa-concurrency-platform@example.invalid", "QA-ConcurrencyPlatform", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var owner = (await users.FindByIdAsync(platformId.ToString()))!; owner.IsPlatformOwner = true; owner.AcademyId = null;
            Check((await users.UpdateAsync(owner)).Succeeded, "Platform owner fixture failed");
        }
        var platformToken = await LoginAsync(client, "qa-concurrency-platform@example.invalid", password); var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", password); client.DefaultRequestHeaders.Authorization = null;
        var barrier = new ProvisioningConcurrencyBarrier();
        using var concurrentFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: barrier);
        using var http = concurrentFactory.CreateClient(new() { AllowAutoRedirect = false }); int mutations = 0, pairs = 0, reads = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(), claims = await identity.UserClaims.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), logins = await identity.UserLogins.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.LoginProvider).ToListAsync(), roleClaims = await identity.RoleClaims.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), finance = await AccessFinancialSnapshotAsync(factory) });
        }
        Dictionary<string, object?> Body(string role, string email, string label) => role switch
        {
            "Owner" => new() { ["name"] = "Synthetic concurrency " + label },
            "AcademyAdmin" => new() { ["academyName"] = "Synthetic concurrency " + label, ["adminUserName"] = email, ["password"] = password },
            _ => new() { ["role"] = role, ["email"] = email, ["password"] = password, [char.ToLowerInvariant(role[0]) + role[1..] + "Id"] = links[role] }
        };
        string Route(string role) => role switch { "Owner" => "/api/academies", "AcademyAdmin" => "/api/platform/academies", _ => $"/api/academies/{academy}/portal-accounts" };
        async Task<(int Status, string Text)> Send(string role, string token, Dictionary<string, object?> body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Route(role)); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token); request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request); return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        (Guid User, Guid Academy) Success(string label, string role, string before, string after, Dictionary<string, object?> body, string response, Guid? creator, bool newRole)
        {
            using var oldDoc = JsonDocument.Parse(before); using var savedDoc = JsonDocument.Parse(after); using var outputDoc = JsonDocument.Parse(response); var old = oldDoc.RootElement; var saved = savedDoc.RootElement; var output = outputDoc.RootElement;
            var expected = new Dictionary<string, int> { ["academies"] = role is "Owner" or "AcademyAdmin" ? 1 : 0, ["users"] = role == "Owner" ? 0 : 1, ["memberships"] = 1, ["roles"] = newRole ? 1 : 0, ["platformAudits"] = role == "AcademyAdmin" ? 1 : 0, ["audits"] = role is "Student" or "Guardian" or "Teacher" ? 1 : 0 };
            foreach (var field in old.EnumerateObject())
            {
                if (!expected.TryGetValue(field.Name, out var delta)) { Check(field.Value.GetRawText() == saved.GetProperty(field.Name).GetRawText(), label + ": unrelated captured state changed: " + field.Name); continue; }
                string Key(JsonElement row) => field.Name == "memberships" ? row.GetProperty("UserId").GetString() + ":" + row.GetProperty("RoleId").GetString() : row.GetProperty("Id").GetString()!;
                var original = field.Value.EnumerateArray().ToDictionary(Key); var now = saved.GetProperty(field.Name).EnumerateArray().ToDictionary(Key);
                Check(now.Count == original.Count + delta, label + ": incorrect aggregate row delta: " + field.Name);
                foreach (var row in original)
                {
                    Check(now.ContainsKey(row.Key), label + ": old row removed: " + field.Name);
                    if (field.Name == "users" && role == "Owner" && row.Key == creator.ToString())
                    {
                        foreach (var column in row.Value.EnumerateObject()) if (!new[] { "AcademyId", "DisplayName", "ConcurrencyStamp" }.Contains(column.Name)) Check(column.Value.GetRawText() == now[row.Key].GetProperty(column.Name).GetRawText(), label + ": original creator field changed: " + column.Name);
                    }
                    else Check(row.Value.GetRawText() == now[row.Key].GetRawText(), label + ": old row changed: " + field.Name);
                }
            }
            var academyId = role is "Owner" or "AcademyAdmin" ? output.GetProperty("id").GetGuid() : academy;
            var email = (body.GetValueOrDefault("adminUserName") ?? body.GetValueOrDefault("email"))?.ToString()?.Trim();
            var user = role == "Owner" ? saved.GetProperty("users").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == creator) : saved.GetProperty("users").EnumerateArray().Single(x => string.Equals(x.GetProperty("Email").GetString(), email, StringComparison.OrdinalIgnoreCase));
            var userId = user.GetProperty("Id").GetGuid(); Check(user.GetProperty("AcademyId").GetGuid() == academyId, label + ": user association mismatch");
            var roleId = saved.GetProperty("roles").EnumerateArray().Single(x => x.GetProperty("Name").GetString() == role).GetProperty("Id").GetGuid();
            Check(saved.GetProperty("memberships").EnumerateArray().Count(x => x.GetProperty("UserId").GetGuid() == userId && x.GetProperty("RoleId").GetGuid() == roleId) == 1, label + ": missing/duplicate membership");
            if (role is "Owner" or "AcademyAdmin")
            {
                var row = saved.GetProperty("academies").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == academyId); Check(row.GetProperty("Name").GetString() == body[role == "Owner" ? "name" : "academyName"]!.ToString(), label + ": orphan/wrong academy");
            }
            else
            {
                Check(output.GetProperty("id").GetGuid() == userId && output.GetProperty("role").GetString() == role, label + ": response account mismatch");
                foreach (var field in new[] { "Student", "Guardian", "Teacher" }) Check(user.GetProperty(field + "Id").ValueKind == (field == role ? JsonValueKind.String : JsonValueKind.Null) && (field != role || user.GetProperty(field + "Id").GetGuid() == links[role]), label + ": typed link mismatch");
            }
            var auditField = role == "AcademyAdmin" ? "platformAudits" : "audits";
            if (role != "Owner")
            {
                var oldIds = old.GetProperty(auditField).EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audit = saved.GetProperty(auditField).EnumerateArray().Single(x => !oldIds.Contains(x.GetProperty("Id").GetGuid()));
                Check(audit.GetProperty("ActorUserId").GetGuid() == (role == "AcademyAdmin" ? platformId : adminId), label + ": audit actor mismatch");
                if (role == "AcademyAdmin") Check(audit.GetProperty("EntityId").GetGuid() == academyId && audit.GetProperty("Action").GetString() == "Academy onboarded", label + ": platform audit mismatch");
                else Check(audit.GetProperty("AcademyId").GetGuid() == academy && audit.GetProperty("Action").GetString() == "POST PortalAccounts" && JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!).RootElement.GetProperty("Route").GetString() == Route(role), label + ": portal audit mismatch");
            }
            return (userId, academyId);
        }
        async Task ReadControl(string label, string role, string email, string originalToken, Guid expectedAcademy)
        {
            var before = await Snapshot(); var access = role == "Owner" ? originalToken : await LoginAsync(http, email.Trim(), password);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access); using var response = await http.GetAsync("/api/academies"); using var output = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); http.DefaultRequestHeaders.Authorization = null;
            Check(response.StatusCode == HttpStatusCode.OK && output.RootElement.GetArrayLength() == 1 && output.RootElement[0].GetProperty("id").GetGuid() == expectedAcademy && before == await Snapshot(), label + ": exact academy access/read state failed");
            reads++; Console.WriteLine("PROVISIONRACE ACCESS " + JsonSerializer.Serialize(new { label, role, realLogin = role != "Owner", originalToken = role == "Owner", getStatus = 200, expectedAcademy, unchangedSnapshot = true }));
        }
        async Task Pair(string role, bool missingRole, bool duplicate)
        {
            var label = missingRole ? "initialize-" + role : "duplicate-" + role;
            using (var owned = await LinkedOwnedSqlAsync(manifest)) { }
            if (missingRole)
            {
                // Fixture-only removal before the observed boundary; never a developer/production store.
                using var scope = factory.Services.CreateScope(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var existing = await identity.Roles.SingleOrDefaultAsync(x => x.Name == role);
                if (existing is not null) { identity.UserRoles.RemoveRange(await identity.UserRoles.Where(x => x.RoleId == existing.Id).ToListAsync()); identity.Roles.Remove(existing); await identity.SaveChangesAsync(); }
            }
            var emails = new[] { "qa-concurrency-" + label.ToLowerInvariant() + "-a@example.invalid", "qa-concurrency-" + label.ToLowerInvariant() + "-b@example.invalid" };
            if (duplicate) emails[1] = "  " + emails[0].ToUpperInvariant() + "  ";
            var bodies = Enumerable.Range(0, 2).Select(i => Body(role, emails[i], label + "-" + i)).ToArray();
            var tokens = role == "Owner" ? ownerTokens.ToArray() : new[] { role == "AcademyAdmin" ? platformToken : adminToken, role == "AcademyAdmin" ? platformToken : adminToken };
            var before = await Snapshot(); barrier.Arm(missingRole ? "AspNetRoles" : "AspNetUsers", missingRole ? role : emails[0]);
            (int Status, string Text)[] result;
            try { result = await Task.WhenAll(Send(role, tokens[0], bodies[0]), Send(role, tokens[1], bodies[1])).WaitAsync(TimeSpan.FromSeconds(40)); } finally { barrier.Disarm(); }
            var after = await Snapshot(); var successStatus = role is "Owner" or "AcademyAdmin" ? 201 : 200;
            Check(barrier.Arrivals == 2 && result.Count(x => x.Status == successStatus) == 1 && result.Count(x => x.Status == 500) == 1, label + ": controlled native uniqueness race did not produce one commit/one sanitized rejection");
            var winner = Array.FindIndex(result, x => x.Status == successStatus); var loser = 1 - winner;
            Check(JsonDocument.Parse(result[loser].Text).RootElement.GetProperty("message").GetString() == "An unexpected server error occurred.", label + ": SQL details leaked");
            var committed = Success(label, role, before, after, bodies[winner], result[winner].Text, role == "Owner" ? ownerIds[winner] : null, missingRole);
            Console.WriteLine("PROVISIONRACE PAIR " + JsonSerializer.Serialize(new { label, role, missingRole, duplicate, statuses = result.Select(x => x.Status).ToArray(), responses = result.Select(x => x.Text).ToArray(), barrier.Arrivals, bothNativeInsertsObserved = true, noPartialLoserVerified = true, committedUser = committed.User, committedAcademy = committed.Academy, beforeDigest = Hash(before), afterDigest = Hash(after), roleAdditions = missingRole ? 1 : 0, userAdditions = role == "Owner" ? 0 : 1, academyAdditions = role is "Owner" or "AcademyAdmin" ? 1 : 0, membershipAdditions = 1, auditAdditions = role == "Owner" ? 0 : 1, exactSnapshotVerified = true }));
            pairs++; mutations += 2;
            before = after; var retry = await Send(role, tokens[loser], bodies[loser]); after = await Snapshot(); mutations++;
            (Guid User, Guid Academy)? retried = null;
            if (duplicate) Check(retry.Status == (role == "AcademyAdmin" ? 409 : 400) && before == after, label + ": duplicate replay created/changed captured state");
            else { Check(retry.Status == successStatus, label + ": clean first-role loser retry failed"); retried = Success(label + "-retry", role, before, after, bodies[loser], retry.Text, role == "Owner" ? ownerIds[loser] : null, false); }
            Console.WriteLine("PROVISIONRACE RETRY " + JsonSerializer.Serialize(new { label, role, duplicate, status = retry.Status, response = retry.Text, beforeDigest = Hash(before), afterDigest = Hash(after), exactSnapshotVerified = true, creates = !duplicate }));
            await ReadControl(label + "-winner", role, emails[winner], tokens[winner], committed.Academy);
            if (retried is not null) await ReadControl(label + "-retry", role, emails[loser], tokens[loser], retried.Value.Academy);
            if (role == "AcademyAdmin" && missingRole)
            {
                using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); Check((await users.AddToRoleAsync((await users.FindByIdAsync(adminId.ToString()))!, "AcademyAdmin")).Succeeded, "Fixture admin membership restoration failed");
            }
        }
        foreach (var role in new[] { "Owner", "AcademyAdmin", "Student", "Guardian", "Teacher" }) await Pair(role, missingRole: true, duplicate: false);
        foreach (var role in new[] { "AcademyAdmin", "Guardian" }) await Pair(role, missingRole: false, duplicate: true);
        Check(pairs == 7 && mutations == 21 && reads == 12, "Concurrency coverage count mismatch");
        Console.WriteLine($"PROVISIONRACE REGRESSION PASS: {pairs} controlled real SQL concurrency pairs; {mutations} mutation requests including seven retries/replays; {reads} separate exact-academy GET controls. Atomic integrity PASS; seven native uniqueness losers still return generic500, conflict UX gap OPEN. Product unchanged; no repeated source audit or baseline/failure tests; browser/commit/cancellation/full-critical OPEN.");
    }

    private sealed class ProvisioningConcurrencyBarrier : DbCommandInterceptor
    {
        private string? table; private string target = ""; private int arrivals; private TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Arrivals => arrivals;
        public void Arm(string selectedTable, string selectedTarget) { if (table is not null) throw new InvalidOperationException("Concurrency barrier already armed"); table = selectedTable; target = selectedTarget.Trim(); arrivals = 0; gate = new(TaskCreationOptions.RunContinuationsAsynchronously); }
        public void Disarm() => table = null;
        private async Task ObserveAsync(DbCommand command, CancellationToken token)
        {
            if (table is null || !command.CommandText.Contains("INSERT INTO [" + table + "]", StringComparison.Ordinal) || !command.Parameters.Cast<DbParameter>().Any(x => x.Value is string value && string.Equals(value.Trim(), target, StringComparison.OrdinalIgnoreCase))) return;
            if (Interlocked.Increment(ref arrivals) == 2) gate.TrySetResult();
            await gate.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
            // Do not rewrite SQL or manufacture exceptions/results: SQL's real unique
            // indexes decide the loser after both native validators reached INSERT.
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { await ObserveAsync(command, cancellationToken); return result; }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { await ObserveAsync(command, cancellationToken); return result; }
    }
}
