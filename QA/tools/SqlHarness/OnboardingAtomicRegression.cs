using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyOnboardingAtomicAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        await CreateAccessActorAsync(factory, "qa-onboarding-owner@example.invalid", "Owner", academy);
        var bypassId = await CreateAccessActorAsync(factory, "qa-onboarding-bypass@example.invalid", "AcademyAdmin", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var actor = (await users.FindByIdAsync(bypassId.ToString()))!; actor.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(actor)).Succeeded) throw new InvalidOperationException("Synthetic platform actor setup failed.");
        }
        var owner = await LoginAsync(client, "qa-onboarding-owner@example.invalid", "Synthetic!39Ab");
        var bypass = await LoginAsync(client, "qa-onboarding-bypass@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            // Fresh SQL contexts; credentials/hashes are never captured or printed.
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.UserName, x.AcademyId, x.StudentId, x.GuardianId }).ToArrayAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name }).ToArrayAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync()
            });
        }
        async Task Check(string mode, HttpStatusCode expected, string? token, Guid route, bool accounts = true)
        {
            await Task.Delay(650);
            var suffix = Guid.NewGuid().ToString("N");
            var body = new Dictionary<string, object?> {
                ["studentFirstName"] = "Atomic", ["studentLastName"] = suffix, ["dateOfBirth"] = "2015-01-01",
                ["studentEmail"] = "qa-student-" + suffix + "@example.invalid",
                ["parentFirstName"] = "Synthetic", ["parentLastName"] = "Parent", ["parentEmail"] = "qa-parent-" + suffix + "@example.invalid",
                ["studentUserName"] = accounts ? "qa-student-" + suffix : null, ["studentTemporaryPassword"] = accounts ? "Synthetic!39Ab" : null,
                ["parentUserName"] = accounts ? "qa-parent-" + suffix : null, ["parentTemporaryPassword"] = accounts ? "Synthetic!39Ab" : null
            };
            if (mode == "invalid-parent-password") body["parentTemporaryPassword"] = "x";
            if (mode == "duplicate-parent") body["parentUserName"] = "qa-admin-a@example.invalid";
            if (mode == "role-create-result")
            {
                using var scope = factory.Services.CreateScope();
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var role = await roles.FindByNameAsync("Guardian");
                if (role is not null && !(await roles.DeleteAsync(role)).Succeeded) throw new InvalidOperationException("Synthetic Guardian role setup failed.");
            }
            var fault = new OnboardingSqlFault(mode);
            using var faultFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: fault, isolatedDomainInterceptor: fault);
            using var configured = faultFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
                services.AddScoped<IUserValidator<ApplicationUser>>(sp => new OnboardingAssignmentValidator(fault, sp.GetRequiredService<IdentityDbContext>()));
                services.AddSingleton<IRoleValidator<ApplicationRole>>(new OnboardingRoleValidator(fault));
            }));
            using var faultClient = configured.CreateClient();
            var before = await Snapshot();
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{route}/student-onboarding") { Content = JsonContent.Create(body) };
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await faultClient.SendAsync(request);
            RequireFinanceStatus(response, expected, "atomic onboarding " + mode);
            if ((mode.EndsWith("-sql", StringComparison.Ordinal) || mode.EndsWith("-result", StringComparison.Ordinal)) && fault.Faults != 1) throw new InvalidOperationException("Requested failure stage not reached exactly once: " + mode);
            if (expected != HttpStatusCode.OK)
            {
                if (before != await Snapshot()) throw new InvalidOperationException("Failed onboarding left domain/Identity/audit state: " + mode);
                if (mode == "parent-role-result" && !fault.PersistedUserObserved) throw new InvalidOperationException("Membership-result fault did not observe the saved parent account.");
            }
            else
            {
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                using var beforeJson = JsonDocument.Parse(before); using var afterJson = JsonDocument.Parse(await Snapshot());
                foreach (var (field, delta) in new[] { ("students", 1), ("guardians", 1), ("links", 1), ("audits", 1), ("users", accounts ? 2 : 0), ("memberships", accounts ? 2 : 0) })
                    if (afterJson.RootElement.GetProperty(field).GetArrayLength() - beforeJson.RootElement.GetProperty(field).GetArrayLength() != delta) throw new InvalidOperationException("Unexpected successful onboarding row delta: " + field);
                var id = result.RootElement.GetProperty("id").GetGuid();
                var parentId = result.RootElement.GetProperty("parentId").GetGuid();
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                if (!await db.Students.AnyAsync(x => x.Id == id && x.AcademyId == academy) || !await db.StudentGuardians.AnyAsync(x => x.StudentId == id && x.GuardianId == parentId && x.CanAccessPortal)) throw new InvalidOperationException("Successful domain linkage missing.");
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                foreach (var role in new[] { "Student", "Guardian" })
                {
                    var name = "qa-" + (role == "Student" ? "student" : "parent") + "-" + suffix;
                    var user = await users.FindByNameAsync(name);
                    if (accounts ? user is null || user.AcademyId != academy || (role == "Student" ? user.StudentId != id : user.GuardianId != parentId) || !await users.IsInRoleAsync(user, role) : user is not null) throw new InvalidOperationException("Incorrect portal account/role linkage.");
                }
                if (result.RootElement.GetProperty("studentAccountCreated").GetBoolean() != accounts || result.RootElement.GetProperty("parentAccountCreated").GetBoolean() != accounts) throw new InvalidOperationException("Incorrect created-account result.");
                if (!await db.AuditLogs.AnyAsync(x => x.AcademyId == academy && x.Action == "POST StudentOnboarding" && x.MetadataJson!.Contains(request.RequestUri!.AbsolutePath))) throw new InvalidOperationException("Missing success audit.");
            }
            count++; Console.WriteLine("PASS: ONBOARDINGATOMIC " + mode + " HTTP=" + (int)expected + "; fresh domain/Identity/audit verified");
        }
        await Check("invalid-parent-password", HttpStatusCode.BadRequest, admin, academy);
        await Check("duplicate-parent", HttpStatusCode.BadRequest, admin, academy);
        foreach (var mode in new[] { "link-sql", "student-user-sql", "student-role-sql", "parent-user-sql", "parent-role-sql", "audit-sql" })
            await Check(mode, mode == "audit-sql" ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, admin, academy);
        await Check("parent-role-result", HttpStatusCode.BadRequest, admin, academy);
        await Check("role-create-result", HttpStatusCode.BadRequest, admin, academy);
        await Check("success", HttpStatusCode.OK, admin, academy);
        await Check("optional-accounts-omitted", HttpStatusCode.OK, admin, academy, accounts: false);
        await Check("anonymous", HttpStatusCode.Unauthorized, null, academy);
        await Check("teacher", HttpStatusCode.Forbidden, teacher, academy);
        await Check("foreign-route", HttpStatusCode.Forbidden, admin, foreign);
        foreach (var actor in new[] { owner, bypass })
        {
            await Check("invalid-parent-password", HttpStatusCode.BadRequest, actor, academy);
            await Check("success", HttpStatusCode.OK, actor, academy);
        }
        await Check("audit-sql", HttpStatusCode.InternalServerError, bypass, academy);
        await Check("foreign-route", HttpStatusCode.Forbidden, bypass, foreign);
        if (count != 21) throw new InvalidOperationException("Incomplete onboarding atomic matrix.");
        Console.WriteLine("PASS: ONBOARDINGATOMIC all 21 native SQL/Identity/HTTP cases; fault-stage rollback, successful account roles, optional accounts, Owner/platform opt-in and denied no-write controls.");
    }

    private sealed class OnboardingSqlFault(string mode) : DbCommandInterceptor
    {
        public string Mode => mode;
        public int Faults { get; private set; }
        public bool PersistedUserObserved { get; set; }
        public void Trip() => Faults++;
        private int users, memberships;
        private void Inject(DbCommand command)
        {
            var text = command.CommandText;
            var hit = text.Contains("INSERT INTO [AspNetUsers]", StringComparison.Ordinal) ? ++users : 0;
            var member = text.Contains("INSERT INTO [AspNetUserRoles]", StringComparison.Ordinal) ? ++memberships : 0;
            if (Faults != 0) return;
            var trip = mode switch {
                "link-sql" => text.Contains("INSERT INTO [StudentGuardians]", StringComparison.Ordinal),
                "student-user-sql" => hit == 1, "parent-user-sql" => hit == 2,
                "student-role-sql" => member == 1, "parent-role-sql" => member == 2,
                "audit-sql" => text.Contains("INSERT INTO [AuditLogs]", StringComparison.Ordinal), _ => false
            };
            if (!trip) return;
            Faults++; command.CommandText = "THROW 51009, 'Synthetic onboarding stage fault', 1;\n" + text;
        }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { Inject(command); return ValueTask.FromResult(result); }
    }

    private sealed class OnboardingRoleValidator(OnboardingSqlFault fault) : IRoleValidator<ApplicationRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role)
        {
            if (fault.Mode != "role-create-result" || role.Name != "Guardian") return Task.FromResult(IdentityResult.Success);
            fault.Trip(); return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "SyntheticRoleFailure", Description = "Synthetic role creation failure" }));
        }
    }

    private sealed class OnboardingAssignmentValidator(OnboardingSqlFault fault, IdentityDbContext db) : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (fault.Mode != "parent-role-result" || user.GuardianId is null || !db.ChangeTracker.Entries<IdentityUserRole<Guid>>().Any(x => x.State == EntityState.Added && x.Entity.UserId == user.Id)) return IdentityResult.Success;
            fault.PersistedUserObserved = await db.Users.AsNoTracking().AnyAsync(x => x.Id == user.Id);
            fault.Trip(); return IdentityResult.Failed(new IdentityError { Code = "SyntheticMembershipFailure", Description = "Synthetic role assignment failure" });
        }
    }
}
