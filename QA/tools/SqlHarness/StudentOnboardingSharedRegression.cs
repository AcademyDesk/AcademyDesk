using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyStudentOnboardingSharedExtrasAsync(QaApiFactory factory, HttpClient client,
        QaRunManifest manifest, Guid academy, Guid foreign)
    {
        var bearer = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine("PASS: ONBOARDINGSHARED " + label); }
        var basic = JsonSerializer.Deserialize<StudentOnboardingRequest>("""{"StudentFirstName":"Synthetic","StudentLastName":"Shared extras","DateOfBirth":"2000-01-01"}""")!;
        async Task<HttpResponseMessage> Post(StudentOnboardingRequest request)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/academies/{academy}/student-onboarding") { Content = JsonContent.Create(request) };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await client.SendAsync(message);
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new {
                students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.IsActive, x.StudentId, x.GuardianId }).ToArrayAsync(),
                roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name }).ToArrayAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync()
            });
        }
        async Task Reject(string label, StudentOnboardingRequest request)
        {
            var before = await Snapshot(); using var response = await Post(request);
            RequireFinanceStatus(response, HttpStatusCode.BadRequest, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Rejected onboarding changed domain/Identity/audit: " + label);
            Pass(label);
        }
        await Reject("minor requires guardian", basic with { DateOfBirth = new DateOnly(2015, 1, 1) });
        await Reject("optional guardian details cannot be partial", basic with { ParentCity = "Synthetic city" });
        await Reject("invalid optional email rejected", basic with { StudentEmail = "not-an-email" });
        await Reject("oversized optional field rejected", basic with { StudentPhone = new string('1', 31) });
        foreach (var email in new string?[] { null, "", " " })
            await Reject("parent account with missing/blank email returns400 without partial records",
                basic with { ParentFirstName = "Synthetic", ParentLastName = "Guardian", ParentEmail = email,
                    ParentUserName = "synthetic-parent-" + Guid.NewGuid().ToString("N"), ParentTemporaryPassword = "Synthetic!39Ab" });
        Guid foreignBranch, inactiveBranch;
        using (var setup = factory.Services.CreateScope()) {
            var db = setup.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var a = new Branch { AcademyId = foreign, Name = "Synthetic foreign branch" };
            var b = new Branch { AcademyId = academy, Name = "Synthetic inactive branch", IsActive = false };
            db.Branches.AddRange(a, b); await db.SaveChangesAsync(); foreignBranch = a.Id; inactiveBranch = b.Id;
        }
        await Reject("foreign branch rejected", basic with { BranchId = foreignBranch });
        await Reject("inactive branch rejected", basic with { BranchId = inactiveBranch });

        var optionalNumber = "student-shared-" + Guid.NewGuid().ToString("N");
        using (var response = await Post(basic with { StudentNumber = optionalNumber, StudentUserName = "synthetic-no-account", ParentCity = " ", StudentPhone = " " })) {
            RequireFinanceStatus(response, HttpStatusCode.OK, "adult optional onboarding");
            var result = (await response.Content.ReadFromJsonAsync<StudentOnboardingResult>())!;
            if (result.StudentAccountCreated || result.ParentAccountCreated || result.ParentId is not null || result.IsMinor)
                throw new InvalidOperationException("Optional incomplete account input falsely reported created account.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (!await db.Students.AnyAsync(x => x.Id == result.Id && x.AcademyId == academy && x.StudentNumber == optionalNumber))
                throw new InvalidOperationException("Optional onboarding did not persist its student number.");
        }
        Pass("adult optional blanks allowed; account-created flags reflect actual writes");
        await Reject("duplicate student number rejected without account/audit changes", basic with { StudentNumber = optionalNumber });
        foreach (var allow in new[] { false, true }) {
            using var response = await Post(basic with { ParentFirstName = "Synthetic", ParentLastName = "Guardian", AllowParentPortalAccess = allow,
                AllowAcademicProgress = false, AllowDocuments = false, AllowFinance = true, AllowLeave = true });
            RequireFinanceStatus(response, HttpStatusCode.OK, "adult guardian flags");
            var result = (await response.Content.ReadFromJsonAsync<StudentOnboardingResult>())!;
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var link = await db.StudentGuardians.SingleAsync(x => x.StudentId == result.Id);
            if (link.CanAccessPortal != allow || link.CanViewAcademicProgress || link.CanViewDocuments || link.CanViewFinance != allow ||
                link.CanManageLeave != allow || (link.AccessGrantedAtUtc is not null) != allow || result.StudentAccountCreated || result.ParentAccountCreated)
                throw new InvalidOperationException("Adult guardian portal/field permissions changed.");
            Pass("adult guardian access=" + allow + " respects individual permissions without creating accounts");
        }

        Guid actor, teacher;
        using (var scope = factory.Services.CreateScope()) {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            actor = await identity.Users.Where(x => x.UserName == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            teacher = await identity.Users.Where(x => x.UserName == "qa-teacher-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        ClaimsPrincipal Principal(Guid id, bool authenticated = true) => new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, "Owner")], authenticated ? "SyntheticHost" : null));
        async Task DirectDenied(string label, ClaimsPrincipal principal, bool staleRole = false)
        {
            using var direct = factory.Services.CreateScope();
            var db = direct.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = direct.ServiceProvider.GetRequiredService<IdentityDbContext>();
            if (staleRole) {
                // Prime the caller's Identity tracker, then independently revoke
                // membership. An old principal/tracked role must not preserve it.
                _ = await identity.Users.SingleAsync(x => x.Id == actor);
                _ = await identity.UserRoles.Where(x => x.UserId == actor).ToArrayAsync();
                using var changed = factory.Services.CreateScope(); var current = changed.ServiceProvider.GetRequiredService<IdentityDbContext>();
                current.UserRoles.RemoveRange(await current.UserRoles.Where(x => x.UserId == actor).ToListAsync()); await current.SaveChangesAsync();
            }
            var before = await Snapshot();
            var original = AcademyIdentityTransaction.Validate(db, identity); await using var tx = await db.Database.BeginTransactionAsync();
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, original, default);
            var outcome = await direct.ServiceProvider.GetRequiredService<StudentOnboardingService>().CreateAsync(principal, academy, basic, default);
            if (outcome.HttpStatus != 403 || outcome.Result is not null) throw new InvalidOperationException("Direct current-authority denial failed: " + label);
            await db.SaveChangesAsync(); await identity.SaveChangesAsync(); await tx.CommitAsync();
            if (before != await Snapshot()) throw new InvalidOperationException("Denied shared onboarding wrote records: " + label);
            Pass(label);
        }
        await DirectDenied("unauthenticated known admin cannot create", Principal(actor, false));
        await DirectDenied("Teacher with forged Owner claim cannot create", Principal(teacher));

        var payload = JsonSerializer.Serialize(new { tool_key = PentaOnboardingFields.Tool, tool_revision = 1, fields = new {
            StudentFirstName = new { kind = "text", value = "Synthetic" }, StudentLastName = new { kind = "text", value = "Typed shared student" },
            DateOfBirth = new { kind = "date", value = "2000-01-01" } } });
        if (!PentaOnboardingFields.TryRead(payload, out var candidate)) throw new InvalidOperationException("Strict onboarding fixture invalid.");
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var original = AcademyIdentityTransaction.Validate(db, identity); await using var tx = await db.Database.BeginTransactionAsync();
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, original, default);
            var outcome = await scope.ServiceProvider.GetRequiredService<StudentOnboardingService>().CreateAsync(Principal(actor), academy, candidate!, default);
            if (outcome.HttpStatus != 200 || outcome.Result is null || outcome.Result.StudentAccountCreated || outcome.Result.ParentAccountCreated)
                throw new InvalidOperationException("Strict typed candidate failed shared onboarding.");
            db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = outcome.Result.Id,
                EntityType = "Student", Action = "SyntheticSharedStudentOnboarding" });
            await db.SaveChangesAsync(); await tx.CommitAsync();
        }
        Pass("strict inactive typed candidate shares host transaction/domain/audit; no AI dispatch");

        // Real concurrent Manual and direct host callers compete for the same
        // academy/student number. This is actual SQL contention, not a mock.
        for (var attempt = 1; attempt <= 5; attempt++) {
            await Task.Delay(650);
            var number = "race-" + Guid.NewGuid().ToString("N"); var request = basic with { StudentNumber = number };
            async Task<int> Manual() { using var response = await Post(request); return (int)response.StatusCode; }
            async Task<int> Host() {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); var original = AcademyIdentityTransaction.Validate(db, identity);
                await using var tx = await db.Database.BeginTransactionAsync();
                await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, tx, original, default);
                var outcome = await scope.ServiceProvider.GetRequiredService<StudentOnboardingService>().CreateAsync(Principal(actor), academy, request, default);
                if (outcome.HttpStatus == 200) {
                    db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = actor, EntityId = outcome.Result!.Id,
                        EntityType = "Student", Action = "SyntheticSharedStudentRace" });
                    await db.SaveChangesAsync(); await tx.CommitAsync();
                }
                return outcome.HttpStatus;
            }
            var before = await Snapshot(); var statuses = await Task.WhenAll(Task.Run(Manual), Task.Run(Host));
            if (!statuses.Order().SequenceEqual(new[] { 200, 400 })) throw new InvalidOperationException("Mixed onboarding race did not yield one200/one400.");
            using var old = JsonDocument.Parse(before); using var after = JsonDocument.Parse(await Snapshot());
            foreach (var (field, delta) in new[] { ("students", 1), ("audits", 1), ("guardians", 0), ("links", 0), ("users", 0), ("roles", 0), ("memberships", 0) })
                if (after.RootElement.GetProperty(field).GetArrayLength() - old.RootElement.GetProperty(field).GetArrayLength() != delta)
                    throw new InvalidOperationException("Mixed onboarding race incorrect committed delta: " + field);
            using (var scope = factory.Services.CreateScope())
                if (await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Students.CountAsync(x => x.AcademyId == academy && x.StudentNumber == number) != 1)
                    throw new InvalidOperationException("Mixed race stored duplicate student number.");
            Pass("mixed Manual/shared-service duplicate race " + attempt + ":200/400,one student/one audit,no accounts,no429");
        }
        await DirectDenied("current revoked membership beats stale tracker and Owner claim", Principal(actor), staleRole: true);
        var evidence = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", "student-onboarding-shared-" + manifest.RunId.ToString("N")));
        Directory.CreateDirectory(evidence);
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new {
            run = manifest.RunId.ToString("N"), passed = true, retainedAtomicCases = 21, retainedServiceCases = 7,
            additionalCases = count, mixedRaces = 5, aiToolRegistered = false, modelInference = false, syntheticOnly = true,
            browserDevices = "NOT RUN", protectedConversation = "NOT INTEGRATED"
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: ONBOARDINGSHARED {count} additive SQL/HTTP/service cases,5 mixed races; retained21+7 atomic cases unchanged.");
    }
}
