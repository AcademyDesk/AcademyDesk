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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPortalProvisioningAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreignAcademy, adminId;
        var links = new Dictionary<string, Guid>(); var foreignLinks = new Dictionary<string, Guid>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreignAcademy = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            links["Student"] = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); foreignLinks["Student"] = await db.Students.Where(x => x.AcademyId == foreignAcademy).Select(x => x.Id).SingleAsync();
            links["Teacher"] = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var teacher = new Teacher { AcademyId = foreignAcademy, FirstName = "Synthetic", LastName = "Foreign" }; db.Teachers.Add(teacher); foreignLinks["Teacher"] = teacher.Id;
            foreach (var (key, tenant) in new[] { ("Guardian", academy), ("ForeignGuardian", foreignAcademy) }) { var guardian = new Guardian { AcademyId = tenant, FirstName = "Synthetic", LastName = key }; db.Guardians.Add(guardian); if (tenant == academy) links["Guardian"] = guardian.Id; else foreignLinks["Guardian"] = guardian.Id; }
            await db.SaveChangesAsync();
            adminId = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var bypassId = await CreateAccessActorAsync(factory, "qa-portal-bypass@example.invalid", "AcademyAdmin", academy);
        var ownerId = await CreateAccessActorAsync(factory, "qa-portal-owner@example.invalid", "Owner", academy);
        await CreateAccessActorAsync(factory, "qa-portal-custom@example.invalid", "QA-PortalWorkforce", academy, permissions: "[\"workforce.manage\"]");
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var bypass = (await users.FindByIdAsync(bypassId.ToString()))!; bypass.IsPlatformOwner = true; if (!(await users.UpdateAsync(bypass)).Succeeded) throw new InvalidOperationException("Portal bypass fixture failed"); }
        var tokens = new Dictionary<string, string>();
        foreach (var (actor, email) in new[] { ("admin", "qa-admin-a@example.invalid"), ("teacher", "qa-teacher-a@example.invalid"), ("bypass", "qa-portal-bypass@example.invalid"), ("owner", "qa-portal-owner@example.invalid"), ("custom", "qa-portal-custom@example.invalid") }) tokens[actor] = await LoginAsync(client, email, "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var fault = new ProvisioningSqlFault(); // Reuse the accepted real-store fault helpers, unchanged.
        using var faultFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: fault);
        using var injected = faultFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            if (!faultFactory.PreflightPassed) throw new InvalidOperationException("Refused portal injection before owned SQL preflight");
            services.ConfigureDbContext<AcademyDeskDbContext>(options => options.AddInterceptors(new PortalAuditSqlFault(fault)));
            services.AddScoped<IUserValidator<ApplicationUser>>(sp => new ProvisioningUserValidator(fault, sp.GetRequiredService<IdentityDbContext>()));
            services.AddSingleton<IRoleValidator<ApplicationRole>>(new PortalRoleValidator(fault));
        }));
        using var faultClient = injected.CreateClient(new() { AllowAutoRedirect = false });
        int count = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), finance = await AccessFinancialSnapshotAsync(factory) });
        }
        Dictionary<string, object?> Body(string role, string email) => new() { ["role"] = role, ["email"] = email, ["password"] = "Synthetic!39Ab", ["studentId"] = role == "Student" ? links[role] : null, ["guardianId"] = role == "Guardian" ? links[role] : null, ["teacherId"] = role == "Teacher" ? links[role] : null };
        async Task Request(string label, Dictionary<string, object?> body, HttpStatusCode expected, string? actor = "admin", string mode = "none", Guid? routeAcademy = null)
        {
            await Task.Delay(250); var before = await Snapshot();
            using (var owned = await LinkedOwnedSqlAsync(manifest)) { }
            fault.Arm(mode, body.GetValueOrDefault("email")?.ToString()?.Trim() ?? "");
            var tenant = routeAcademy ?? academy; var path = $"/api/academies/{tenant}/portal-accounts";
            using var request = new HttpRequestMessage(HttpMethod.Post, path); if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            string text; HttpStatusCode status;
            try { using var response = await faultClient.SendAsync(request); status = response.StatusCode; text = await response.Content.ReadAsStringAsync(); }
            finally { fault.Disarm(); }
            var after = await Snapshot(); Check(status == expected, label + ": unexpected status " + (int)status);
            Check(fault.Faults == (mode == "none" ? 0 : 1), label + ": fault did not reach intended stage exactly once");
            if (mode == "assignment-result") Check(fault.PersistedUserObserved && fault.PendingMembershipObserved, label + ": assignment failure stage not proved");
            if (status == HttpStatusCode.InternalServerError) Check(JsonDocument.Parse(text).RootElement.GetProperty("message").GetString() == "An unexpected server error occurred.", label + ": store details leaked");
            if (status == HttpStatusCode.BadRequest && (mode.EndsWith("-result") || new[] { "Student-password", "Guardian-password", "Teacher-password", "missing-password-result" }.Contains(label))) Check(text.Contains("No changes were saved."), label + ": rollback response missing");
            var succeeds = status == HttpStatusCode.OK; Guid? newUser = null, addedAudit = null; int roleAdditions = 0;
            if (!succeeds) Check(before == after, label + ": rejected provisioning changed captured state");
            else
            {
                using var oldDoc = JsonDocument.Parse(before); using var savedDoc = JsonDocument.Parse(after); var old = oldDoc.RootElement; var saved = savedDoc.RootElement;
                roleAdditions = saved.GetProperty("roles").GetArrayLength() - old.GetProperty("roles").GetArrayLength(); Check(roleAdditions is 0 or 1, label + ": role delta mismatch");
                foreach (var field in old.EnumerateObject())
                {
                    if (!new[] { "users", "roles", "memberships", "audits" }.Contains(field.Name)) Check(field.Value.GetRawText() == saved.GetProperty(field.Name).GetRawText(), label + ": unrelated captured data changed: " + field.Name);
                    else
                    {
                        string Key(JsonElement row) => field.Name == "memberships" ? row.GetProperty("UserId").GetString() + ":" + row.GetProperty("RoleId").GetString() : row.GetProperty("Id").GetString()!;
                        var original = field.Value.EnumerateArray().ToDictionary(Key); var now = saved.GetProperty(field.Name).EnumerateArray().ToDictionary(Key);
                        var expectedDelta = field.Name == "roles" ? roleAdditions : field.Name == "audits" && actor == "bypass" ? 0 : 1;
                        Check(now.Count == original.Count + expectedDelta, label + ": addition count mismatch: " + field.Name);
                        foreach (var row in original) Check(now.TryGetValue(row.Key, out var preserved) && row.Value.GetRawText() == preserved.GetRawText(), label + ": old row modified: " + field.Name);
                    }
                }
                using var outputDoc = JsonDocument.Parse(text); var output = outputDoc.RootElement; var id = output.GetProperty("id").GetGuid(); newUser = id;
                var user = saved.GetProperty("users").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id); var email = body["email"]!.ToString()!.Trim(); var roleName = body["role"]!.ToString()!;
                Check(user.GetProperty("AcademyId").GetGuid() == academy && user.GetProperty("Email").GetString() == email && user.GetProperty("UserName").GetString() == email && output.GetProperty("email").GetString() == email && output.GetProperty("role").GetString() == roleName, label + ": identity/response mismatch");
                Check(user.GetProperty("DisplayName").GetString() == (body.GetValueOrDefault("displayName")?.ToString()?.Trim() ?? email) && user.GetProperty("IsActive").GetBoolean() && !user.GetProperty("IsPlatformOwner").GetBoolean() && !user.GetProperty("EmailConfirmed").GetBoolean(), label + ": optional/flag behavior changed");
                foreach (var (role, link) in new[] { ("Student", "StudentId"), ("Guardian", "GuardianId"), ("Teacher", "TeacherId") }) Check(role == roleName ? user.GetProperty(link).GetGuid() == links[role] : user.GetProperty(link).ValueKind == JsonValueKind.Null, label + ": role-specific person link mismatch");
                var roleId = saved.GetProperty("roles").EnumerateArray().Single(x => x.GetProperty("Name").GetString() == roleName).GetProperty("Id").GetGuid();
                Check(saved.GetProperty("memberships").EnumerateArray().Count(x => x.GetProperty("UserId").GetGuid() == id && x.GetProperty("RoleId").GetGuid() == roleId) == 1, label + ": role membership missing/duplicate");
                if (actor != "bypass")
                {
                    var oldIds = old.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audit = saved.GetProperty("audits").EnumerateArray().Single(x => !oldIds.Contains(x.GetProperty("Id").GetGuid())); addedAudit = audit.GetProperty("Id").GetGuid();
                    Check(audit.GetProperty("AcademyId").GetGuid() == academy && audit.GetProperty("ActorUserId").GetGuid() == (actor == "owner" ? ownerId : adminId) && audit.GetProperty("Action").GetString() == "POST PortalAccounts" && audit.GetProperty("EntityType").GetString() == "PortalAccounts", label + ": audit scope/actor mismatch");
                    using var metadata = JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!); Check(metadata.RootElement.GetProperty("Route").GetString() == path, label + ": audit route mismatch");
                }
                using var scope = factory.Services.CreateScope(); var assigned = await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().GetRolesAsync((await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(id.ToString()))!); Check(assigned.SequenceEqual(new[] { roleName }), label + ": effective roles mismatch");
            }
            Console.WriteLine("PORTALPROVISION EVIDENCE " + JsonSerializer.Serialize(new { label, actor, role = body.GetValueOrDefault("role"), mode, status = (int)status, response = text, succeeds, newUser, addedAudit, roleAdditions, fault.Faults, fault.PersistedUserObserved, fault.PendingMembershipObserved, beforeDigest = Hash(before), afterDigest = Hash(after), bodyDigest = Hash(text), exactSnapshotVerified = true }));
            count++; Console.WriteLine("PORTALPROVISION CASE " + label + " PASS.");
        }
        var first = "qa-portal-first@example.invalid";
        foreach (var mode in new[] { "role-result", "role-sql", "user-sql", "password-result" }) { var body = Body("Guardian", first); if (mode == "password-result") body["password"] = "bad"; await Request("missing-" + mode, body, mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode: mode == "password-result" ? "none" : mode); }
        await Request("missing-role-retry", Body("Guardian", first), HttpStatusCode.OK);
        foreach (var role in new[] { "Student", "Teacher" }) { var body = Body(role, "qa-portal-first-" + role.ToLowerInvariant() + "@example.invalid"); body["displayName"] = null; body["guardianId"] = links["Guardian"]; await Request("initial-" + role, body, HttpStatusCode.OK); }
        foreach (var role in new[] { "Student", "Guardian", "Teacher" })
        {
            var password = Body(role, "qa-portal-password-" + role + "@example.invalid"); password["password"] = "bad"; await Request(role + "-password", password, HttpStatusCode.BadRequest);
            foreach (var mode in new[] { "assignment-result", "assignment-sql", "audit-sql" }) { var body = Body(role, "qa-portal-" + role + "-" + mode + "@example.invalid"); await Request(role + "-" + mode, body, mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode: mode); await Request(role + "-" + mode + "-retry", body, HttpStatusCode.OK); }
            foreach (var invalid in new[] { "foreign", "missing" }) { var body = Body(role, "qa-portal-invalid@example.invalid"); body[char.ToLowerInvariant(role[0]) + role[1..] + "Id"] = invalid == "foreign" ? foreignLinks[role] : null; await Request(role + "-" + invalid + "-link", body, HttpStatusCode.BadRequest); }
        }
        await Request("duplicate", Body("Guardian", first), HttpStatusCode.BadRequest); await Request("normalized-duplicate", Body("Guardian", " " + first.ToUpperInvariant() + " "), HttpStatusCode.BadRequest);
        foreach (var (actor, tenant, expected) in new[] { ((string?)null, academy, HttpStatusCode.Unauthorized), ("teacher", academy, HttpStatusCode.Forbidden), ("custom", academy, HttpStatusCode.Forbidden), ("admin", foreignAcademy, HttpStatusCode.Forbidden), ("bypass", foreignAcademy, HttpStatusCode.Forbidden) }) await Request("denied-" + (actor ?? "anonymous"), Body("Guardian", "qa-portal-denied@example.invalid"), expected, actor, routeAcademy: tenant);
        await Request("owner-success", Body("Guardian", "qa-portal-owner-created@example.invalid"), HttpStatusCode.OK, "owner");
        foreach (var mode in new[] { "assignment-result", "assignment-sql" }) { var body = Body("Teacher", "qa-portal-bypass-" + mode + "@example.invalid"); await Request("bypass-" + mode, body, mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, "bypass", mode); await Request("bypass-" + mode + "-retry", body, HttpStatusCode.OK, "bypass"); }
        foreach (var field in new[] { "role", "email", "password" }) { var body = Body("Guardian", "qa-portal-blank@example.invalid"); body[field] = field == "password" ? "" : " "; await Request("blank-" + field, body, HttpStatusCode.BadRequest); }
        foreach (var (role, email) in new[] { ("Guardian", first), ("Student", "qa-portal-first-student@example.invalid"), ("Teacher", "qa-portal-first-teacher@example.invalid") })
        {
            var before = await Snapshot(); var access = await LoginAsync(faultClient, email, "Synthetic!39Ab"); faultClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access); using var response = await faultClient.GetAsync("/api/academies"); using var output = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check(response.StatusCode == HttpStatusCode.OK && output.RootElement.GetArrayLength() == 1 && output.RootElement[0].GetProperty("id").GetGuid() == academy && before == await Snapshot(), role + ": new login/exact own academy read failed/changed state");
            Console.WriteLine("PORTALPROVISION ACCESS " + role + " PASS: real login200, GET200 exact academyId, unchanged captured snapshot.");
        }
        Console.WriteLine($"PORTALPROVISION REGRESSION PASS: {count} real Identity/HTTP-SQL provisioning cases; checked role/user/membership/audit rollback, retries, typed links, optional defaults, actor/route audits, platform-bypass atomicity and unchanged authorization; six login/read controls separate. Source baseline only; self-service creation/browser/concurrency/full-critical OPEN.");
    }
    private sealed class PortalRoleValidator(ProvisioningSqlFault fault) : IRoleValidator<ApplicationRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role) => Task.FromResult(role.Name == "Guardian" && fault.Trip("role-result") ? IdentityResult.Failed(new IdentityError { Code = "SyntheticPortalRoleFailure", Description = "Synthetic role validation failure" }) : IdentityResult.Success);
    }
    private sealed class PortalAuditSqlFault(ProvisioningSqlFault fault) : DbCommandInterceptor
    {
        private void Inject(DbCommand command) { if (fault.Mode == "audit-sql" && command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.Ordinal) && fault.Trip("audit-sql")) command.CommandText = "THROW 51003, 'Synthetic portal success-audit fault', 1;\n" + command.CommandText; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
    }
}
