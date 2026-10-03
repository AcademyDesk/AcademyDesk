using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
    private static async Task VerifyRoleReplacementAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        client.DefaultRequestHeaders.Authorization = null;
        await VerifyRoleReplacementCoreAsync(factory, client);
        Guid academy, foreign, teacherLink, studentLink, guardianLink, adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacherLink = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            studentLink = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var guardian = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Role Guardian" };
            db.Guardians.Add(guardian); await db.SaveChangesAsync(); guardianLink = guardian.Id;
            adminId = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var ownerId = await CreateAccessActorAsync(factory, "qa-role-owner@example.invalid", "Owner", academy);
        var bypassId = await CreateAccessActorAsync(factory, "qa-role-bypass@example.invalid", "AcademyAdmin", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bypass = (await users.FindByIdAsync(bypassId.ToString()))!; bypass.IsPlatformOwner = true;
            Check((await users.UpdateAsync(bypass)).Succeeded, "Bypass fixture creation");
        }
        var actors = new Dictionary<string, (Guid id, string token)> {
            ["admin"] = (adminId, await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab")),
            ["owner"] = (ownerId, await LoginAsync(client, "qa-role-owner@example.invalid", "Synthetic!39Ab")),
            ["bypass"] = (bypassId, await LoginAsync(client, "qa-role-bypass@example.invalid", "Synthetic!39Ab"))
        };
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var fault = new RoleReplacementFault();
        using var faultFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: fault);
        using var injected = faultFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            if (!faultFactory.PreflightPassed) throw new InvalidOperationException("Refused role injection before owned SQL preflight.");
            services.ConfigureDbContext<AcademyDeskDbContext>(options => options.AddInterceptors(fault));
            services.AddScoped<IUserValidator<ApplicationUser>>(sp => new RoleReplacementUserValidator(fault, sp.GetRequiredService<IdentityDbContext>()));
        }));
        using var faultClient = injected.CreateClient(new() { AllowAutoRedirect = false });
        int cases = 0, failures = 0, successes = 0, unchanged = 0;
        static void Check(bool accepted, string label) { if (!accepted) throw new InvalidOperationException("Role replacement: " + label); }
        static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(),
                grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync()
            });
        }
        async Task<(ApplicationUser user, string[] roles)> State(Guid id)
        {
            using var scope = factory.Services.CreateScope(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user = await identity.Users.AsNoTracking().SingleAsync(x => x.Id == id);
            var roles = await (from member in identity.UserRoles.AsNoTracking() join role in identity.Roles.AsNoTracking() on member.RoleId equals role.Id where member.UserId == id orderby role.Name select role.Name!).ToArrayAsync();
            return (user, roles);
        }
        async Task<Guid> Target(string label, string[] protectedRoles, bool platform = false, Guid? tenant = null)
        {
            using var scope = factory.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            foreach (var role in protectedRoles) if (!await roleManager.RoleExistsAsync(role))
                Check((await roleManager.CreateAsync(new ApplicationRole { Name = role })).Succeeded, "Protected fixture role");
            var email = "qa-replacement-" + label + "@example.invalid";
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, AcademyId = tenant ?? academy, DisplayName = label, IsActive = true, IsPlatformOwner = platform,
                StudentId = protectedRoles.Contains("Student") ? studentLink : null, TeacherId = protectedRoles.Contains("Teacher") ? teacherLink : null, GuardianId = protectedRoles.Contains("Guardian") ? guardianLink : null };
            Check((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded, "Target creation");
            Check((await users.AddToRolesAsync(user, protectedRoles.Concat(["Sales", "QA-Finance", "QA-Messages"]))).Succeeded, "Target memberships");
            return user.Id;
        }
        async Task Case(string label, Guid id, string actor, int expected, string mode = "none", string role = "Operations", Guid? routeTenant = null)
        {
            var before = await Snapshot(); var state = await State(id);
            using (var scope = factory.Services.CreateScope()) successes = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.CountAsync();
            if (mode != "none") fault.Arm(mode, id);
            using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/academies/{routeTenant ?? academy}/staff/{id}/role");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actor == "foreign" ? foreignToken : actors[actor].token);
            request.Content = JsonContent.Create(new { role });
            using var response = await (mode == "none" ? client : faultClient).SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Check((int)response.StatusCode == expected, label + " status " + (int)response.StatusCode);
            if (mode != "none") { Check(fault.Hits == 1, label + " fault actually executed exactly once"); failures++; }
            var after = await Snapshot(); var result = await State(id);
            if (expected != 200) { Check(before == after, label + " full fresh SQL rollback/no-write"); unchanged++; }
            else
            {
                var protectedRoles = state.roles.Where(x => x is "Owner" or "AcademyAdmin" or "Student" or "Guardian" or "Teacher").Concat(["Operations"]).OrderBy(x => x).ToArray();
                using var payload = JsonDocument.Parse(body);
                var reported = payload.RootElement.GetProperty("roles").EnumerateArray().Select(x => x.GetString()!).OrderBy(x => x).ToArray();
                Check(reported.SequenceEqual(result.roles) && result.roles.SequenceEqual(protectedRoles), label + " reported/SQL exact protected membership");
                Check(state.user.SecurityStamp != result.user.SecurityStamp, label + " stamp rotation");
                Check(state.user.IsPlatformOwner == result.user.IsPlatformOwner && state.user.AcademyId == result.user.AcademyId &&
                    state.user.StudentId == result.user.StudentId && state.user.TeacherId == result.user.TeacherId && state.user.GuardianId == result.user.GuardianId &&
                    state.user.Email == result.user.Email && state.user.DisplayName == result.user.DisplayName && state.user.IsActive == result.user.IsActive &&
                    state.user.PasswordHash == result.user.PasswordHash && state.user.LockoutEnd == result.user.LockoutEnd, label + " independent identity fields preserved");
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.OccurredAtUtc).ToListAsync();
                Check(audits.Count == successes + 1, label + " exactly one audit");
                var audit = audits.Last();
                Check(audit.AcademyId == academy && audit.ActorUserId == actors[actor].id && audit.Action == "PATCH Staff" &&
                    audit.EntityType == "Staff" && audit.MetadataJson!.Contains(request.RequestUri!.AbsolutePath, StringComparison.Ordinal), label + " attributed success audit");
                Check(JsonDocument.Parse(before).RootElement.GetProperty("grants").ToString() == JsonDocument.Parse(after).RootElement.GetProperty("grants").ToString(), label + " grants unchanged");
            }
            cases++;
            Console.WriteLine("ROLEREPLACE EVIDENCE " + JsonSerializer.Serialize(new { label, actor, mode, status = (int)response.StatusCode, faultHits = mode == "none" ? 0 : fault.Hits, roles = result.roles, exactSnapshotVerified = expected != 200, beforeDigest = Digest(before), afterDigest = Digest(after), bodyDigest = Digest(body) }));
            fault.Disarm();
        }
        foreach (var actor in new[] { "admin", "owner", "bypass" })
            foreach (var mode in new[] { "remove-result", "add-result", "stamp-result", "remove-sql", "add-sql", "stamp-sql", "audit-sql" })
            {
                var id = await Target(actor + "-" + mode, []);
                await Case(actor + "-" + mode + "-rollback", id, actor, 500, mode);
                await Case(actor + "-" + mode + "-retry", id, actor, 200);
            }
        foreach (var protectedRole in new[] { "Owner", "AcademyAdmin", "Student", "Guardian", "Teacher" })
        {
            var id = await Target("protected-" + protectedRole, [protectedRole]);
            await Case("preserve-" + protectedRole, id, "admin", 200, role: "operations");
        }
        var multiple = await Target("multiple-protected", ["Owner", "AcademyAdmin", "Student", "Guardian", "Teacher"], platform: true);
        await Case("all-protected-links-platform-flag-preserved", multiple, "admin", 200);
        var grantedId = await Target("explicit-grant", []);
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            identity.AccessGrants.Add(new AccessGrant { AcademyId = academy, UserId = grantedId, PermissionsJson = "[\"finance.manage\"]", IsPermanent = true, GrantedByUserId = adminId });
            await identity.SaveChangesAsync();
        }
        await Case("independent-explicit-grant-preserved", grantedId, "admin", 200);
        var grantToken = await LoginAsync(client, "qa-replacement-explicit-grant@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        using (var request = new HttpRequestMessage(HttpMethod.Get, $"/api/academies/{academy}/invoices"))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", grantToken);
            using var response = await client.SendAsync(request); Check(response.IsSuccessStatusCode, "Preserved grant effective access");
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Check(body.RootElement.GetArrayLength() == 1 && body.RootElement[0].GetProperty("invoiceNumber").GetString() == "QA-ROLE-PRIVATE", "Preserved grant positive payload");
        }
        Console.WriteLine("ROLEREPLACE CONTROL preserved-explicit-grant-native-login-finance200");
        var ownId = await Target("validation", []);
        await Case("invalid-role", ownId, "admin", 400, role: "Unknown");
        await Case("null-role", ownId, "admin", 400, role: null!);
        await Case("foreign-actor", ownId, "foreign", 403);
        await Case("foreign-route", ownId, "admin", 403, routeTenant: foreign);
        var foreignId = await Target("foreign-target", [], tenant: foreign);
        await Case("foreign-target", foreignId, "admin", 404);
        // Local fallback must retain the controller's tenant restriction.
        await Case("bypass-foreign-target", foreignId, "bypass", 404);
        await Case("bypass-foreign-route", ownId, "bypass", 403, routeTenant: foreign);
        Console.WriteLine("ROLEREPLACE EXTENDED SUMMARY " + JsonSerializer.Serialize(new { cases, injectedFailures = failures, unchangedSnapshots = unchanged, protectedRoles = 5, preservedExplicitGrant = true, actors = 3, nativeIdentitySql = true, accepted = true }));
    }

    private sealed class RoleReplacementFault : DbCommandInterceptor
    {
        public string Mode { get; private set; } = "none";
        public Guid Target { get; private set; }
        public int Hits { get; private set; }
        public bool StampPending { get; set; }
        public void Arm(string mode, Guid target) { if (Mode != "none") throw new InvalidOperationException("Role fault already armed"); Mode = mode; Target = target; Hits = 0; StampPending = false; }
        public void Disarm() => Mode = "none";
        public bool Trip(string mode) { if (Mode != mode) return false; Hits++; return true; }
        private void Inject(DbCommand command)
        {
            var match = Mode switch {
                "remove-sql" => command.CommandText.Contains("DELETE FROM [AspNetUserRoles]", StringComparison.Ordinal),
                "add-sql" => command.CommandText.Contains("INSERT INTO [AspNetUserRoles]", StringComparison.Ordinal),
                "stamp-sql" => StampPending && command.CommandText.Contains("UPDATE [AspNetUsers]", StringComparison.Ordinal),
                "audit-sql" => command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.Ordinal),
                _ => false
            };
            if (!match) return;
            Hits++; command.CommandText = "THROW 51004, 'Synthetic role replacement SQL fault', 1;\n" + command.CommandText;
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
    }
    private sealed class RoleReplacementUserValidator(RoleReplacementFault fault, IdentityDbContext db) : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (fault.Mode == "none" || user.Id != fault.Target) return IdentityResult.Success;
            var memberships = db.ChangeTracker.Entries<IdentityUserRole<Guid>>().Where(x => x.Entity.UserId == user.Id).ToArray();
            var removing = memberships.Any(x => x.State == EntityState.Deleted);
            var adding = memberships.Any(x => x.State == EntityState.Added);
            if (!removing && !adding)
                fault.StampPending = await db.Users.AsNoTracking().Where(x => x.Id == user.Id).Select(x => x.SecurityStamp).SingleAsync() != user.SecurityStamp;
            var mode = removing ? "remove-result" : adding ? "add-result" : fault.StampPending ? "stamp-result" : "none";
            return mode != "none" && fault.Trip(mode) ? IdentityResult.Failed(new IdentityError { Code = "SyntheticRoleReplacementFailure", Description = "Synthetic role validation failure" }) : IdentityResult.Success;
        }
    }
}
