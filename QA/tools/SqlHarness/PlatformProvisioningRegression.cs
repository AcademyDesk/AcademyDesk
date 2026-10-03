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
    private static async Task VerifyPlatformProvisioningAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, ownerId;
        using (var scope = factory.Services.CreateScope()) academy = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        ownerId = await CreateAccessActorAsync(factory, "qa-provision-owner@example.invalid", "PlatformOwner", academy);
        await CreateAccessActorAsync(factory, "qa-provision-role-only@example.invalid", "PlatformOwner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var owner = (await users.FindByIdAsync(ownerId.ToString()))!;
            owner.IsPlatformOwner = true; owner.AcademyId = null; owner.DisplayName = "Synthetic provisioning owner";
            if (!(await users.UpdateAsync(owner)).Succeeded) throw new InvalidOperationException("Provisioning owner fixture failed");
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.PlatformSettings.Add(new PlatformSettings { DefaultTrialDays = 45 }); await db.SaveChangesAsync();
        }
        var tokens = new Dictionary<string, string>();
        foreach (var (name, email) in new[] { ("platform", "qa-provision-owner@example.invalid"), ("admin", "qa-admin-a@example.invalid"), ("teacher", "qa-teacher-a@example.invalid"), ("role-only", "qa-provision-role-only@example.invalid") }) tokens[name] = await LoginAsync(client, email, "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var fault = new ProvisioningSqlFault();
        using var faultFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: fault);
        using var injected = faultFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            if (!faultFactory.PreflightPassed) throw new InvalidOperationException("Refused fault injection before owned SQL preflight");
            services.ConfigureDbContext<AcademyDeskDbContext>(options => options.AddInterceptors(fault));
            services.AddScoped<IUserValidator<ApplicationUser>>(sp => new ProvisioningUserValidator(fault, sp.GetRequiredService<IdentityDbContext>()));
            services.AddSingleton<IRoleValidator<ApplicationRole>>(new ProvisioningRoleValidator(fault));
        }));
        using var faultClient = injected.CreateClient(new() { AllowAutoRedirect = false });
        int count = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), settings = await db.PlatformSettings.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), academyAudits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(), claims = await identity.UserClaims.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), logins = await identity.UserLogins.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.LoginProvider).ToListAsync(), roleClaims = await identity.RoleClaims.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), finance = await AccessFinancialSnapshotAsync(factory) });
        }
        async Task Request(string label, string body, HttpStatusCode expected, string? actor = "platform", string mode = "none", bool succeeds = false)
        {
            await Task.Delay(650); var before = await Snapshot(); var started = DateTime.UtcNow;
            using (var owned = await LinkedOwnedSqlAsync(manifest)) { } // Every arm rechecks the exact owned SQL marker.
            fault.Arm(mode, "qa-provision-" + label + "@example.invalid");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/academies");
            if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            string text; HttpStatusCode status;
            try { using var response = await faultClient.SendAsync(request); status = response.StatusCode; text = await response.Content.ReadAsStringAsync(); }
            finally { fault.Disarm(); }
            var ended = DateTime.UtcNow; var after = await Snapshot();
            Console.WriteLine("PLATFORMPROVISION HTTP " + JsonSerializer.Serialize(new { label, actor, body, mode, status = (int)status, response = text, beforeDigest = Hash(before), afterDigest = Hash(after) }));
            Check(status == expected, label + ": unexpected HTTP status " + (int)status);
            if (mode != "none") Check(fault.Faults == 1, label + ": injected stage not reached exactly once");
            if (mode == "assignment-result") Check(fault.PersistedUserObserved && fault.PendingMembershipObserved, label + ": returned failure was not at real assignment update after persisted user and staged membership");
            if (expected == HttpStatusCode.InternalServerError) Check(JsonDocument.Parse(text).RootElement.GetProperty("message").GetString() == "An unexpected server error occurred.", label + ": exception detail leaked");
            if (expected == HttpStatusCode.BadRequest && mode.EndsWith("-result")) Check(text.Contains("No changes were saved."), label + ": rollback response missing");
            Guid? newAcademy = null, addedAudit = null;
            int roleAdditions = 0;
            if (!succeeds) Check(before == after, label + ": rejected provisioning left captured academy/Identity/audit/finance changes");
            else
            {
                using var oldDoc = JsonDocument.Parse(before); using var savedDoc = JsonDocument.Parse(after); var old = oldDoc.RootElement; var saved = savedDoc.RootElement;
                roleAdditions = saved.GetProperty("roles").GetArrayLength() - old.GetProperty("roles").GetArrayLength(); Check(roleAdditions is 0 or 1, label + ": role count mismatch");
                foreach (var field in old.EnumerateObject())
                {
                    if (!new[] { "academies", "users", "memberships", "audits", "roles" }.Contains(field.Name)) Check(field.Value.GetRawText() == saved.GetProperty(field.Name).GetRawText(), label + ": unrelated captured collection changed: " + field.Name);
                    else
                    {
                        string Key(JsonElement row) => field.Name == "memberships" ? row.GetProperty("UserId").GetString() + ":" + row.GetProperty("RoleId").GetString() : row.GetProperty("Id").GetString()!;
                        var original = field.Value.EnumerateArray().ToDictionary(Key); var now = saved.GetProperty(field.Name).EnumerateArray().ToDictionary(Key);
                        Check(now.Count == original.Count + (field.Name == "roles" ? roleAdditions : 1), label + ": addition count mismatch: " + field.Name);
                        foreach (var row in original) Check(now.TryGetValue(row.Key, out var preserved) && row.Value.GetRawText() == preserved.GetRawText(), label + ": old row changed: " + field.Name);
                    }
                }
                using var responseDoc = JsonDocument.Parse(text); var output = responseDoc.RootElement; var id = output.GetProperty("id").GetGuid(); newAcademy = id;
                using var inputDoc = JsonDocument.Parse(body); var input = inputDoc.RootElement;
                var login = input.GetProperty("adminUserName").GetString()!.Trim(); var email = login.Contains('@') ? login : login + "@academydesk.local";
                var rowAcademy = saved.GetProperty("academies").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id);
                var expiry = rowAcademy.GetProperty("SubscriptionEndsAtUtc").GetDateTime(); Check(expiry >= started.AddDays(45) && expiry <= ended.AddDays(45), label + ": prior trial-duration fix regressed");
                Check(rowAcademy.GetProperty("Name").GetString() == input.GetProperty("academyName").GetString()!.Trim() && rowAcademy.GetProperty("SubscriptionPlan").GetString() == "Trial" && rowAcademy.GetProperty("SubscriptionStatus").GetString() == "Trial", label + ": academy mismatch");
                var admin = saved.GetProperty("users").EnumerateArray().Single(x => x.GetProperty("Email").GetString() == email);
                Check(admin.GetProperty("AcademyId").GetGuid() == id && admin.GetProperty("EmailConfirmed").GetBoolean() && admin.GetProperty("IsActive").GetBoolean() && !admin.GetProperty("IsPlatformOwner").GetBoolean(), label + ": admin scope/flags mismatch");
                Check(output.GetProperty("userName").GetString() == email && output.GetProperty("displayName").GetString() == admin.GetProperty("DisplayName").GetString(), label + ": success response mismatch");
                var role = saved.GetProperty("roles").EnumerateArray().Single(x => x.GetProperty("Name").GetString() == "AcademyAdmin");
                var userId = admin.GetProperty("Id").GetGuid(); var roleId = role.GetProperty("Id").GetGuid();
                Check(saved.GetProperty("memberships").EnumerateArray().Count(x => x.GetProperty("UserId").GetGuid() == userId && x.GetProperty("RoleId").GetGuid() == roleId) == 1, label + ": missing/duplicate membership");
                var originalAuditIds = old.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audit = saved.GetProperty("audits").EnumerateArray().Single(x => !originalAuditIds.Contains(x.GetProperty("Id").GetGuid())); addedAudit = audit.GetProperty("Id").GetGuid();
                Check(audit.GetProperty("ActorUserId").GetGuid() == ownerId && audit.GetProperty("ActorName").GetString() == "Synthetic provisioning owner" && audit.GetProperty("Action").GetString() == "Academy onboarded" && audit.GetProperty("EntityType").GetString() == "Academy" && audit.GetProperty("EntityId").GetGuid() == id, label + ": audit mismatch");
            }
            Console.WriteLine("PLATFORMPROVISION EVIDENCE " + JsonSerializer.Serialize(new { label, actor, mode, status = (int)status, response = text, succeeds, newAcademy, addedAudit, roleAdditions, fault.Faults, fault.PersistedUserObserved, fault.PendingMembershipObserved, beforeDigest = Hash(before), afterDigest = Hash(after), bodyDigest = Hash(text), exactSnapshotVerified = true }));
            count++; Console.WriteLine("PLATFORMPROVISION CASE " + label + " PASS.");
        }
        string Intake(string email, string password = "Synthetic!39Ab") => JsonSerializer.Serialize(new { academyName = "Synthetic Provisioning " + email, adminUserName = email, password });
        // Remove this fixture-only role and its fixture memberships before the missing-role
        // branch. No production or pre-existing developer Identity store is touched.
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var role = await identity.Roles.SingleAsync(x => x.Name == "AcademyAdmin");
            identity.UserRoles.RemoveRange(await identity.UserRoles.Where(x => x.RoleId == role.Id).ToListAsync()); identity.Roles.Remove(role); await identity.SaveChangesAsync();
        }
        var retryEmail = "qa-provision-retry@example.invalid";
        foreach (var mode in new[] { "role-result", "role-sql", "password-result", "user-sql", "assignment-result", "assignment-sql", "audit-sql" })
        {
            var label = "missing-role-" + mode;
            // Matching login is shared across all failures, so the final retry proves no
            // orphan identity/uniqueness obstruction was left by any stage.
            fault.TargetOverride = retryEmail;
            await Request(label, Intake(retryEmail, mode == "password-result" ? "bad" : "Synthetic!39Ab"), mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode: mode == "password-result" ? "none" : mode);
        }
        fault.TargetOverride = null;
        await Request("missing-role-retry", Intake(retryEmail), HttpStatusCode.Created, succeeds: true);
        foreach (var mode in new[] { "password-result", "user-sql", "assignment-result", "assignment-sql", "audit-sql" })
        {
            var label = "existing-role-" + mode; var email = "qa-provision-" + label + "@example.invalid";
            await Request(label, Intake(email, mode == "password-result" ? "bad" : "Synthetic!39Ab"), mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode: mode == "password-result" ? "none" : mode);
            await Request(label + "-retry", Intake(email), HttpStatusCode.Created, succeeds: true);
        }
        await Request("duplicate-admin", Intake(retryEmail), HttpStatusCode.Conflict);
        await Request("normalized-duplicate-admin", Intake("  " + retryEmail.ToUpperInvariant() + "  "), HttpStatusCode.Conflict);
        foreach (var field in new[] { "academyName", "adminUserName", "password" })
        {
            var body = new Dictionary<string, object?> { ["academyName"] = "Synthetic rejected", ["adminUserName"] = "qa-provision-blank@example.invalid", ["password"] = "Synthetic!39Ab" }; body[field] = " ";
            await Request("blank-" + field, JsonSerializer.Serialize(body), HttpStatusCode.BadRequest);
        }
        await Request("null-required-fields", "{\"academyName\":null,\"adminUserName\":null,\"password\":null}", HttpStatusCode.BadRequest);
        await Request("malformed-json", "{", HttpStatusCode.BadRequest);
        foreach (var actor in new string?[] { null, "admin", "teacher", "role-only" }) await Request("denied-" + (actor ?? "anonymous"), Intake("qa-provision-denied@example.invalid"), actor is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, actor);
        // Standard optional omission and explicit null both remain valid; a bare login
        // keeps the existing local-email normalization contract.
        await Request("optional-null-bare-login", JsonSerializer.Serialize(new { academyName = "  Synthetic optional  ", adminUserName = "qa-provision-bare", password = "Synthetic!39Ab", legalName = (string?)null, countryCode = (string?)null, timeZone = (string?)null, adminDisplayName = (string?)null }), HttpStatusCode.Created, succeeds: true);
        // Fresh real login plus scoped read is a separate access control, not a fake role check.
        var loginToken = await LoginAsync(faultClient, retryEmail, "Synthetic!39Ab");
        faultClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginToken);
        var beforeRead = await Snapshot(); using var readResponse = await faultClient.GetAsync("/api/academies");
        Check(readResponse.StatusCode == HttpStatusCode.OK && beforeRead == await Snapshot(), "Provisioned admin scoped read failed/changed data");
        using var readDoc = JsonDocument.Parse(await readResponse.Content.ReadAsStringAsync()); Check(readDoc.RootElement.GetArrayLength() == 1, "Provisioned admin saw multiple academies");
        Console.WriteLine("PLATFORMPROVISION ACCESS PASS: real provisioned-admin login200 and own-academy GET200; fresh captured read snapshot unchanged; two controls separate from provisioning case count.");
        Console.WriteLine($"PLATFORMPROVISION REGRESSION PASS: {count} real Identity/HTTP-SQL cases; returned role-creation/assignment failures, actual SQL THROW at role/user/membership/audit writes, exact atomic rollback and retries, optional/uniqueness/auth controls. Accepted source baseline only; other provisioning endpoints/browser/concurrency/full-critical gates OPEN.");
    }

    private sealed class ProvisioningSqlFault : DbCommandInterceptor
    {
        public string? TargetOverride { get; set; }
        public string Mode { get; private set; } = "none";
        public string Target { get; private set; } = "";
        public int Faults { get; private set; }
        public bool PersistedUserObserved { get; set; }
        public bool PendingMembershipObserved { get; set; }
        public void Arm(string mode, string target) { if (Mode != "none") throw new InvalidOperationException("Fault already armed"); Mode = mode; Target = TargetOverride ?? target; Faults = 0; PersistedUserObserved = PendingMembershipObserved = false; }
        public void Disarm() => Mode = "none";
        public bool Trip(string mode) { if (Mode != mode) return false; Faults++; return true; }
        private void Inject(DbCommand command)
        {
            var table = Mode switch { "role-sql" => "AspNetRoles", "user-sql" => "AspNetUsers", "assignment-sql" => "AspNetUserRoles", "audit-sql" => "PlatformAuditEntries", _ => null };
            if (table is null || !command.CommandText.Contains("INSERT INTO [" + table + "]", StringComparison.Ordinal)) return;
            if (Mode == "user-sql" && !command.Parameters.Cast<DbParameter>().Any(x => Equals(x.Value, Target))) return;
            Faults++; command.CommandText = "THROW 51002, 'Synthetic platform provisioning store fault', 1;\n" + command.CommandText;
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
    }
    private sealed class ProvisioningRoleValidator(ProvisioningSqlFault fault) : IRoleValidator<ApplicationRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role) => Task.FromResult(role.Name == "AcademyAdmin" && fault.Trip("role-result") ? IdentityResult.Failed(new IdentityError { Code = "SyntheticRoleFailure", Description = "Synthetic role validation failure" }) : IdentityResult.Success);
    }
    private sealed class ProvisioningUserValidator(ProvisioningSqlFault fault, IdentityDbContext db) : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (fault.Mode != "assignment-result" || user.Email != fault.Target) return IdentityResult.Success;
            var pending = db.ChangeTracker.Entries<IdentityUserRole<Guid>>().Any(x => x.State == EntityState.Added && x.Entity.UserId == user.Id);
            if (!pending) return IdentityResult.Success; // Normal CreateAsync validation still executes against the real store.
            fault.PendingMembershipObserved = true; fault.PersistedUserObserved = await db.Users.AsNoTracking().AnyAsync(x => x.Id == user.Id);
            fault.Trip("assignment-result"); return IdentityResult.Failed(new IdentityError { Code = "SyntheticAssignmentFailure", Description = "Synthetic role assignment validation failure" });
        }
    }
}
