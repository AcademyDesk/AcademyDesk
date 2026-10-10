using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

internal static partial class SqlHarnessEntryPoint
{
    private sealed class PentaQaClock : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now = now.Add(duration);
    }
    private sealed class PentaAuditFaultInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QA PENTA audit-store write fault.");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
    private sealed class CountingPentaProvider : IPentaSyntheticProvider
    {
        private readonly FixedPentaSyntheticProvider inner = new();
        private int calls;
        private int blockNext;
        private TaskCompletionSource<bool>? entered;
        private TaskCompletionSource<bool>? release;
        public int Calls => Volatile.Read(ref calls);
        public bool Malformed { get; set; }
        public bool Throw { get; set; }

        public Task BlockNextAsync()
        {
            entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Interlocked.Exchange(ref blockNext, 1);
            return entered.Task;
        }

        public void Release() => release?.TrySetResult(true);

        public async Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token)
        {
            Interlocked.Increment(ref calls);
            if (Interlocked.Exchange(ref blockNext, 0) == 1)
            {
                entered!.TrySetResult(true);
                await release!.Task.WaitAsync(token);
            }
            if (Throw) throw new InvalidOperationException("Synthetic provider fault.");
            return Malformed
                ? new PentaSyntheticResult(Guid.NewGuid(), capability, "unregistered.tool", "bad", true)
                : await inner.ExecuteAsync(capability, token);
        }
    }

    private sealed class FakeModelProbe : IPentaModelProvider
    {
        public string ProviderName => "qa.fake";
        public string? ModelName => "qa.fake-model";
        public int Calls { get; private set; }
        public PentaProviderProbeResult Next { get; set; } = new("Succeeded", 33, 8);
        public Task<PentaProviderProbeResult> ProbeAsync(CancellationToken token)
        {
            Calls++;
            return Task.FromResult(Next);
        }
    }

    private static async Task<HttpResponseMessage> PentaPostAsync(HttpClient client, string path,
        string key, string text = "synthetic request", string capability = "pulse")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { text, capability })
        };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task VerifyPentaFoundationAsync(QaRunManifest manifest)
    {
        var provider = new CountingPentaProvider();
        using var enabledFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }, isolatedPentaProvider: provider);
        using var enabled = enabledFactory.CreateClient(new() { AllowAutoRedirect = false });
        if (!enabledFactory.PreflightPassed) throw new InvalidOperationException("PENTA enabled host missed QA preflight.");

        var academyA = Guid.NewGuid();
        var academyB = Guid.NewGuid();
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Academies.AddRange(
                new Academy { Id = academyA, Name = "PENTA synthetic A" },
                new Academy { Id = academyB, Name = "PENTA synthetic B" });
            await db.SaveChangesAsync();

            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            foreach (var name in new[] { "AcademyAdmin", "Teacher" })
                PentaRequire((await roles.CreateAsync(new ApplicationRole { Name = name, IsSystemRole = true })).Succeeded,
                    $"Could not create PENTA synthetic {name} role.");
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var (name, academyId, role, isPlatform) in new[]
            {
                ("admin-a", (Guid?)academyA, "AcademyAdmin", false),
                ("admin-c", (Guid?)academyA, "AcademyAdmin", false),
                ("admin-b", (Guid?)academyB, "AcademyAdmin", false),
                ("teacher-a", (Guid?)academyA, "Teacher", false),
                ("platform", (Guid?)null, "", true)
            })
            {
                var address = $"penta-{name}@example.invalid";
                var user = new ApplicationUser
                {
                    UserName = address, Email = address, EmailConfirmed = true,
                    DisplayName = $"Synthetic {name}", AcademyId = academyId,
                    IsPlatformOwner = isPlatform, IsActive = true
                };
                PentaRequire((await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded,
                    $"Could not create PENTA synthetic {name} user.");
                if (role.Length > 0)
                    PentaRequire((await users.AddToRoleAsync(user, role)).Succeeded,
                        $"Could not assign PENTA synthetic {name} role.");
            }
        }

        var pathA = $"/api/academies/{academyA}/penta/turns";
        var pathB = $"/api/academies/{academyB}/penta/turns";
        var valid = new { text = "synthetic request", capability = "pulse" };

        using (var anonymous = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Anonymous PENTA call was not 401.");
        PentaRequire(provider.Calls == 0, "Anonymous call reached PENTA provider.");

        var tokenA = await LoginAsync(enabled, "penta-admin-a@example.invalid", "Synthetic!39Ab");
        var tokenC = await LoginAsync(enabled, "penta-admin-c@example.invalid", "Synthetic!39Ab");
        var tokenB = await LoginAsync(enabled, "penta-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(enabled, "penta-teacher-a@example.invalid", "Synthetic!39Ab");
        var platform = await LoginAsync(enabled, "penta-platform@example.invalid", "Synthetic!39Ab");

        var contextA = $"/api/academies/{academyA}/penta/academy-context";
        var contextB = $"/api/academies/{academyB}/penta/academy-context";
        using (var anonymous = await enabled.GetAsync(contextA))
            PentaRequire(anonymous.StatusCode == HttpStatusCode.Unauthorized,
                "Anonymous R0 academy read was not 401.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher);
        using (var denied = await enabled.GetAsync(contextA))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Teacher R0 academy read was not 403.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platform);
        using (var denied = await enabled.GetAsync(contextA))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Platform owner R0 academy read was not 403.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        using (var denied = await enabled.GetAsync(contextA))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Foreign academy R0 read was not 403.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var denied = await enabled.GetAsync(contextB))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Admin R0 read crossed academy scope.");
        using (var read = await enabled.GetAsync(contextA))
        {
            PentaRequire(read.StatusCode == HttpStatusCode.OK,
                "Authorized R0 academy read failed.");
            var state = await read.Content.ReadFromJsonAsync<PentaAcademyContext>();
            PentaRequire(state is { AcademyId: var id, Name: "PENTA synthetic A", Capability: "twin",
                Tool: PentaAcademyContextService.ToolName, Source: "Academy" } && id == academyA &&
                state.SourcePath == "/api/academies" && state.AsOfUtc != default,
                "R0 projection was not the fixed, sourced own-academy context.");
        }
        PentaRequire(provider.Calls == 0, "R0 read called the synthetic provider.");
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.AuditLogs.CountAsync(x => x.Action == "PentaAcademyContextRead" &&
                x.AcademyId == academyA && x.EntityId == academyA) == 1,
                "R0 read did not persist exactly one own-academy audit entry.");
            PentaRequire(await db.AuditLogs.CountAsync(x => x.Action == "PentaAcademyContextRead" &&
                x.AcademyId == academyB) == 0,
                "Denied R0 read wrote a foreign-academy audit entry.");
            PentaRequire(await db.PentaExecutions.CountAsync() == 0 &&
                await db.PentaAttempts.CountAsync() == 0 &&
                await db.AdminWorkItems.CountAsync() == 0,
                "R0 read wrote a domain action or execution.");
        }
        using (var faultFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" },
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var faultClient = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "R0 audit-fault host missed SQL preflight.");
            var auditFailed = false;
            try
            {
                await using var faultScope = faultFactory.Services.CreateAsyncScope();
                var users = faultScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
                await faultScope.ServiceProvider.GetRequiredService<PentaAcademyContextService>()
                    .ReadAsync(academyA, actorId, CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { auditFailed = true; }
            catch (InvalidOperationException ex) when (ex.Message.Contains("QA PENTA audit-store write fault"))
            { auditFailed = true; }
            PentaRequire(auditFailed, "R0 read did not fail closed on audit write failure.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.AuditLogs.CountAsync(x => x.Action == "PentaAcademyContextRead" &&
                x.AcademyId == academyA) == 1,
                "R0 audit fault changed the successful read's durable evidence.");
        }
        await VerifyPentaStudentSearchAsync(manifest, enabledFactory, enabled,
            academyA, academyB, tokenA, tokenB, teacher, platform, provider);
        await VerifyPentaBatchSearchAsync(manifest, enabledFactory, enabled,
            academyA, academyB, tokenA, tokenB, teacher, platform, provider);

        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var foreign = await enabled.PostAsJsonAsync(pathB, valid))
            PentaRequire(foreign.StatusCode == HttpStatusCode.Forbidden, "Foreign academy PENTA call was not 403.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", teacher);
        using (var denied = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden, "Teacher PENTA call was not 403.");
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", platform);
        using (var denied = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden, "Platform owner PENTA call was not 403.");
        PentaRequire(provider.Calls == 0, "Denied role or tenant reached PENTA provider.");

        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var tampered = await enabled.PostAsJsonAsync(pathA,
            new { text = "synthetic", capability = "executor", actorUserId = Guid.NewGuid() }))
            PentaRequire(tampered.StatusCode == HttpStatusCode.BadRequest, "Actor tampering was not rejected.");
        using (var tampered = await enabled.PostAsJsonAsync(pathA,
            new { text = "synthetic", capability = "executor", tool = "payments.create" }))
            PentaRequire(tampered.StatusCode == HttpStatusCode.BadRequest, "Tool tampering was not rejected.");
        using (var invalid = await enabled.PostAsJsonAsync(pathA, new { text = "synthetic", capability = "payments" }))
            PentaRequire(invalid.StatusCode == HttpStatusCode.BadRequest, "Unknown capability was not rejected.");
        using (var oversized = await enabled.PostAsJsonAsync(pathA,
            new { text = new string('X', 4001), capability = "pulse" }))
            PentaRequire(oversized.StatusCode == HttpStatusCode.BadRequest, "Oversized PENTA text was not rejected.");
        PentaRequire(provider.Calls == 0, "Invalid input reached PENTA provider.");

        using var disabledFactory = new QaApiFactory(manifest, isolatedPentaProvider: provider);
        using var disabled = disabledFactory.CreateClient(new() { AllowAutoRedirect = false });
        disabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var unavailable = await disabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(unavailable.StatusCode == HttpStatusCode.NotFound, "Default-off PENTA route was usable.");
        PentaRequire(provider.Calls == 0, "Disabled feature reached PENTA provider.");

        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.IsActive = false;
            await db.SaveChangesAsync();
        }
        using (var inactive = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(inactive.StatusCode == HttpStatusCode.Forbidden, "Inactive academy reached PENTA.");
        PentaRequire(provider.Calls == 0, "Inactive academy reached PENTA provider.");

        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Id == academyA);
            academy.IsActive = true;
            await db.SaveChangesAsync();
        }
        using (var success = await PentaPostAsync(enabled, pathA, "sql-penta-first"))
        {
            PentaRequire(success.StatusCode == HttpStatusCode.OK, "Enabled synthetic request did not succeed.");
            var result = await success.Content.ReadFromJsonAsync<PentaSyntheticResult>();
            PentaRequire(result is { Synthetic: true, Tool: PentaSyntheticDispatcher.ToolName, Capability: "pulse" } &&
                result.CorrelationId != Guid.Empty && !result.Message.Contains("synthetic request", StringComparison.Ordinal),
                "Synthetic result was malformed or echoed user text.");
        }
        PentaRequire(provider.Calls == 1, "Successful diagnostic did not make exactly one provider call.");
        using (var replay = await PentaPostAsync(enabled, pathA, "sql-penta-first"))
            PentaRequire(replay.StatusCode == HttpStatusCode.OK, "Successful request did not replay.");
        using (var conflict = await PentaPostAsync(enabled, pathA, "sql-penta-first", "changed"))
            PentaRequire(conflict.StatusCode == HttpStatusCode.Conflict, "Changed arguments did not conflict.");
        PentaRequire(provider.Calls == 1, "Retry or conflict dispatched the provider again.");

        provider.Malformed = true;
        using (var invalid = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(invalid.StatusCode == HttpStatusCode.BadGateway, "Malformed fake response was accepted.");
        PentaRequire(provider.Calls == 2, "Malformed response was not exercised.");

        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audits = await db.AuditLogs.AsNoTracking()
                .Where(x => x.AcademyId == academyA && x.EntityType == "PentaExecution").ToListAsync();
            PentaRequire(audits.Count == 2, "PENTA execution audit count was wrong.");
            PentaRequire(await db.PentaExecutions.CountAsync(x => x.AcademyId == academyA) == 2,
                "PENTA retry created a duplicate execution.");
        }

        provider.Malformed = false;
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        using (var own = await enabled.PostAsJsonAsync(pathB, new { text = "synthetic", capability = "twin" }))
            PentaRequire(own.StatusCode == HttpStatusCode.OK, "Second academy did not pass its own policy.");
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("penta-admin-b@example.invalid")
                ?? throw new InvalidOperationException("Synthetic admin B vanished.");
            user.IsActive = false;
            PentaRequire((await users.UpdateAsync(user)).Succeeded, "Could not inactivate synthetic admin B.");
        }
        using (var inactive = await enabled.PostAsJsonAsync(pathB, valid))
            PentaRequire(inactive.StatusCode == HttpStatusCode.Forbidden, "Inactive account reached PENTA.");
        PentaRequire(provider.Calls == 3, "Inactive account reached PENTA provider.");

        using var concurrentA = enabledFactory.CreateClient(new() { AllowAutoRedirect = false });
        using var concurrentB = enabledFactory.CreateClient(new() { AllowAutoRedirect = false });
        concurrentA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        concurrentB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var entered = provider.BlockNextAsync();
        var firstCall = PentaPostAsync(concurrentA, pathA, "sql-penta-race");
        await entered.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            using var pending = await PentaPostAsync(concurrentB, pathA, "sql-penta-race");
            PentaRequire(pending.StatusCode == HttpStatusCode.Accepted, "Concurrent retry was not 202 while first execution was pending.");
            PentaRequire(provider.Calls == 4, "Concurrent retry dispatched a duplicate provider call.");
        }
        finally { provider.Release(); }
        using var completed = await firstCall;
        PentaRequire(completed.StatusCode == HttpStatusCode.OK, "Claimed request did not finish successfully.");
        using (var replay = await PentaPostAsync(concurrentB, pathA, "sql-penta-race"))
            PentaRequire(replay.StatusCode == HttpStatusCode.OK, "Concurrent completed replay was not 200.");
        using (var restartedFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }, isolatedPentaProvider: provider))
        using (var restarted = restartedFactory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            PentaRequire(restartedFactory.PreflightPassed, "Restarted PENTA host missed QA preflight.");
            restarted.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            using var replay = await PentaPostAsync(restarted, pathA, "sql-penta-race");
            PentaRequire(replay.StatusCode == HttpStatusCode.OK, "Restarted host did not replay the durable outcome.");
            PentaRequire(provider.Calls == 4, "Restarted host dispatched a duplicate provider call.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.PentaExecutions.AsNoTracking().Where(x => x.AcademyId == academyA &&
                x.IdempotencyKey == "sql-penta-race").ToListAsync();
            PentaRequire(rows.Count == 1 && rows[0].Status == "Succeeded", "Concurrent key stored duplicate or incomplete execution.");
            PentaRequire(await db.PentaAttempts.CountAsync(x => x.AcademyId == academyA &&
                x.ExecutionId == rows[0].Id) == 1, "Concurrent key stored duplicate attempts.");
        }
        PentaRequire(provider.Calls == 4, "Concurrent key reached provider more than once.");

        // This factory changes only guarded pilot limits; it shares the same
        // run-owned SQL so the prior three academy-A reservations still count.
        using (var budgetFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?>
            {
                ["Penta:Enabled"] = "true", ["Penta:SyntheticDailyUserLimit"] = "20",
                ["Penta:SyntheticDailyAcademyLimit"] = "4"
            }, isolatedPentaProvider: provider))
        using (var budgetA = budgetFactory.CreateClient(new() { AllowAutoRedirect = false }))
        using (var budgetC = budgetFactory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            PentaRequire(budgetFactory.PreflightPassed, "Budget host missed QA preflight.");
            budgetA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            budgetC.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
            var calls = await Task.WhenAll(
                PentaPostAsync(budgetA, pathA, "tenant-budget-a"),
                PentaPostAsync(budgetC, pathA, "tenant-budget-c"));
            using var responseA = calls[0];
            using var responseC = calls[1];
            PentaRequire(calls.Count(x => x.StatusCode == HttpStatusCode.OK) == 1 &&
                calls.Count(x => x.StatusCode == HttpStatusCode.TooManyRequests) == 1,
                "Concurrent academy quota did not admit exactly one call.");
            var winner = responseA.StatusCode == HttpStatusCode.OK ? budgetA : budgetC;
            var winnerKey = responseA.StatusCode == HttpStatusCode.OK ? "tenant-budget-a" : "tenant-budget-c";
            using var replay = await PentaPostAsync(winner, pathA, winnerKey);
            PentaRequire(replay.StatusCode == HttpStatusCode.OK, "Quota-exhausted replay did not return saved result.");
            PentaRequire(provider.Calls == 5, "Concurrent budget boundary dispatched more than one call.");
        }
        using (var userBudgetFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?>
            {
                ["Penta:Enabled"] = "true", ["Penta:SyntheticDailyUserLimit"] = "3",
                ["Penta:SyntheticDailyAcademyLimit"] = "100"
            }, isolatedPentaProvider: provider))
        using (var userA = userBudgetFactory.CreateClient(new() { AllowAutoRedirect = false }))
        using (var userC = userBudgetFactory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            userA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
            userC.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
            using var denied = await PentaPostAsync(userA, pathA, "user-budget-denied");
            PentaRequire(denied.StatusCode == HttpStatusCode.TooManyRequests,
                "User quota did not reject actor A.");
            using var accepted = await PentaPostAsync(userC, pathA, "user-budget-allowed");
            PentaRequire(accepted.StatusCode == HttpStatusCode.OK,
                "User quota incorrectly rejected a second academy admin.");
            PentaRequire(provider.Calls == 6, "User quota dispatched an extra call.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.PentaUsageReservations.CountAsync(x => x.AcademyId == academyA) == 5,
                "Academy quota created an incorrect reservation count.");
            PentaRequire(await db.PentaUsageEntries.CountAsync(x => x.AcademyId == academyA) == 5,
                "Known synthetic outcomes did not reconcile usage exactly once.");
            var execution = await db.PentaExecutions.AsNoTracking()
                .FirstAsync(x => x.AcademyId == academyA);
            db.PentaApprovals.Add(new PentaApproval
            {
                AcademyId = academyB, ExecutionId = execution.Id, InitiatorUserId = Guid.NewGuid(),
                Status = "AwaitingApproval", DecisionDigest = new string('0', 64),
                PolicyVersion = "qa-only", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
            });
            var foreignLinkRejected = false;
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException) { foreignLinkRejected = true; }
            PentaRequire(foreignLinkRejected, "Cross-academy approval FK was accepted.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(!await db.PentaApprovals.AnyAsync(), "Synthetic R0 created an approval record.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("penta-admin-b@example.invalid")
                ?? throw new InvalidOperationException("Synthetic admin B vanished.");
            user.IsActive = true;
            PentaRequire((await users.UpdateAsync(user)).Succeeded, "Could not reactivate synthetic admin B.");
        }
        provider.Throw = true;
        enabled.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        using (var unknown = await PentaPostAsync(enabled, pathB, "sql-penta-unknown"))
            PentaRequire(unknown.StatusCode == HttpStatusCode.BadGateway,
                "Provider fault was not recorded as an unavailable outcome.");
        provider.Throw = false;
        using (var replay = await PentaPostAsync(enabled, pathB, "sql-penta-unknown"))
            PentaRequire(replay.StatusCode == HttpStatusCode.BadGateway,
                "Unknown outcome was blindly replayed.");
        PentaRequire(provider.Calls == 7, "Unknown outcome invoked provider more than once.");
        using (var restartedFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }, isolatedPentaProvider: provider))
        using (var restarted = restartedFactory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            restarted.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
            using var replay = await PentaPostAsync(restarted, pathB, "sql-penta-unknown");
            PentaRequire(replay.StatusCode == HttpStatusCode.BadGateway,
                "Restarted host re-executed an unknown outcome.");
        }
        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var unknown = await db.PentaExecutions.AsNoTracking()
                .SingleAsync(x => x.AcademyId == academyB && x.IdempotencyKey == "sql-penta-unknown");
            var reservation = await db.PentaUsageReservations.AsNoTracking()
                .SingleAsync(x => x.AcademyId == academyB && x.ExecutionId == unknown.Id);
            PentaRequire(unknown.Status == "OutcomeUnknown" && reservation.Status == "UsageUnknown" &&
                reservation.UnitsReserved == 1 && reservation.ActualCost is null,
                "Unknown outcome released or incorrectly reconciled its reservation.");
            PentaRequire(!await db.PentaUsageEntries.AnyAsync(x => x.AcademyId == academyB &&
                x.ReservationId == reservation.Id), "Unknown usage acquired a false actual entry.");
            PentaRequire(await db.PentaAttempts.CountAsync(x => x.AcademyId == academyB &&
                x.ExecutionId == unknown.Id) == 1, "Unknown outcome did not record exactly one attempt.");
        }
        PentaRequire(provider.Calls == 7, "Restarted unknown outcome invoked provider.");
        await VerifyPentaPricedBudgetAsync(manifest, enabledFactory, academyA);
        await VerifyPentaProbeCoordinationAsync(manifest, enabledFactory, academyB);
        await VerifyPentaCompletionFaultAsync(manifest, enabledFactory, academyA, tokenA);
        await VerifyPentaRecoveryCompletionRaceAsync(manifest, enabledFactory, academyA, tokenA);
        await VerifyPentaDraftApprovalAsync(manifest, enabledFactory, academyA, academyB,
            tokenA, tokenC, tokenB, teacher, platform);
        await VerifyPentaConversationsAsync(manifest, academyA, academyB,
            tokenA, tokenC, tokenB, teacher, platform);
        if (Environment.GetEnvironmentVariable("QA_PENTA_HOST_SECURITY") == "1")
            await VerifyPentaHostSecurityAsync(manifest, academyA, academyB, tokenA, tokenC, tokenB, teacher);
        if (Environment.GetEnvironmentVariable("QA_PENTA_MINI") == "1")
            await VerifyPentaMiniAsync(manifest, academyA, academyB, tokenA, tokenC, tokenB, teacher);
        Console.WriteLine("PENTA PASS: real Identity/SQL, single dispatch/restart, concurrent tenant/user quota, scoped approval FK, known usage and unknown-outcome retention.");
        if (Environment.GetEnvironmentVariable("QA_PENTA_BROWSER") == "1")
            await ServePentaBrowserAsync(manifest, academyA);
    }

    private static async Task VerifyPentaStudentSearchAsync(QaRunManifest manifest,
        QaApiFactory factory, HttpClient client, Guid academyA, Guid academyB,
        string tokenA, string tokenB, string teacher, string platform, CountingPentaProvider provider)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            for (var number = 0; number < 12; number++)
                db.Students.Add(new Student { AcademyId = academyA, FirstName = "Meera",
                    LastName = $"Learner{number:00}", StudentNumber = $"QA-A-{number:00}",
                    Email = "private@example.invalid", MedicalOrAccessibilityNotes = "private medical note" });
            db.Students.Add(new Student { AcademyId = academyA, FirstName = "Meera",
                LastName = "Inactive", IsActive = false });
            db.Students.Add(new Student { AcademyId = academyB, FirstName = "Meera",
                LastName = "Foreign" });
            await db.SaveChangesAsync();
        }
        var pathA = $"/api/academies/{academyA}/penta/student-search?q=Meera";
        var pathB = $"/api/academies/{academyB}/penta/student-search?q=Meera";
        Dictionary<Guid, string> ownStudents;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            ownStudents = await db.Students.AsNoTracking()
                .Where(x => x.AcademyId == academyA && x.IsActive)
                .ToDictionaryAsync(x => x.Id, x => x.FirstName + " " + x.LastName);
        }
        client.DefaultRequestHeaders.Authorization = null;
        using (var denied = await client.GetAsync(pathA))
            PentaRequire(denied.StatusCode == HttpStatusCode.Unauthorized,
                "Anonymous student search was not 401.");
        foreach (var (bearer, label) in new[] { (teacher, "Teacher"), (platform, "Platform owner"),
            (tokenB, "Foreign academy admin") })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var denied = await client.GetAsync(pathA);
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                $"{label} student search was not 403.");
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var denied = await client.GetAsync(pathB))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Student search crossed academy scope.");
        using (var invalid = await client.GetAsync($"/api/academies/{academyA}/penta/student-search?q=x"))
            PentaRequire(invalid.StatusCode == HttpStatusCode.BadRequest,
                "Single-character student search was accepted.");
        using (var read = await client.GetAsync(pathA))
        {
            PentaRequire(read.StatusCode == HttpStatusCode.OK, "Own-academy student search failed.");
            var body = await read.Content.ReadAsStringAsync();
            PentaRequire(!body.Contains("private@example.invalid", StringComparison.Ordinal) &&
                !body.Contains("private medical note", StringComparison.Ordinal) &&
                !body.Contains("Foreign", StringComparison.Ordinal) &&
                !body.Contains("Inactive", StringComparison.Ordinal),
                "Student search disclosed a private, foreign or inactive field.");
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var rows = root.GetProperty("rows");
            PentaRequire(root.GetProperty("tool").GetString() == PentaStudentSearchService.ToolName &&
                root.GetProperty("academyId").GetGuid() == academyA &&
                root.GetProperty("maxRows").GetInt32() == 10 &&
                root.GetProperty("hasMore").GetBoolean() && rows.GetArrayLength() == 10 &&
                rows.EnumerateArray().All(x => x.EnumerateObject().Count() == 4 &&
                    ownStudents.TryGetValue(x.GetProperty("sourceId").GetGuid(), out var name) &&
                    name == x.GetProperty("displayName").GetString() &&
                    x.GetProperty("sourcePath").GetString() ==
                    $"/student-management?studentId={x.GetProperty("sourceId").GetGuid():D}"),
                "Student search did not return ten minimal source-linked rows.");
        }
        PentaRequire(provider.Calls == 0, "Student search invoked the model or synthetic provider.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.AuditLogs.CountAsync(x => x.Action == "PentaStudentSearchRead" &&
                x.AcademyId == academyA) == 1 &&
                await db.AuditLogs.CountAsync(x => x.Action == "PentaStudentSearchRead" &&
                    x.AcademyId == academyB) == 0 &&
                await db.PentaExecutions.CountAsync() == 0,
                "Student search was not audited exactly once or created a PENTA execution.");
        }
        using (var faultFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" },
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var faultClient = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Student search audit-fault host missed SQL preflight.");
            var failed = false;
            try
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
                await scope.ServiceProvider.GetRequiredService<PentaStudentSearchService>()
                    .ReadAsync(academyA, actorId, "Meera", CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { failed = true; }
            catch (InvalidOperationException ex) when (ex.Message.Contains("QA PENTA audit-store write fault"))
            { failed = true; }
            PentaRequire(failed, "Student search returned data despite audit write failure.");
        }
        Console.WriteLine("PENTA STUDENT SEARCH PASS: real SQL bounded own-academy source rows, 401/403, no private fields/provider call, audit rollback.");
    }

    private static async Task VerifyPentaBatchSearchAsync(QaRunManifest manifest,
        QaApiFactory factory, HttpClient client, Guid academyA, Guid academyB,
        string tokenA, string tokenB, string teacher, string platform, CountingPentaProvider provider)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var courseA = new ProgramCourse { AcademyId = academyA, Name = "QA Piano" };
            var courseB = new ProgramCourse { AcademyId = academyB, Name = "QA Piano" };
            db.Courses.AddRange(courseA, courseB);
            for (var number = 0; number < 12; number++)
                db.Batches.Add(new Batch { AcademyId = academyA, CourseId = courseA.Id,
                    Name = $"QA Piano Batch {number:00}", BatchCode = $"QA-PI-{number:00}",
                    AdminNotes = "private batch note", MeetingLink = "https://private.invalid/meeting" });
            db.Batches.Add(new Batch { AcademyId = academyA, CourseId = courseA.Id,
                Name = "QA Piano Inactive", IsActive = false });
            db.Batches.Add(new Batch { AcademyId = academyB, CourseId = courseB.Id,
                Name = "QA Piano Foreign" });
            await db.SaveChangesAsync();
        }
        var pathA = $"/api/academies/{academyA}/penta/batch-search?q=QA%20Piano";
        var pathB = $"/api/academies/{academyB}/penta/batch-search?q=QA%20Piano";
        Dictionary<Guid, (string Name, string? Code)> ownBatches;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            ownBatches = await db.Batches.AsNoTracking()
                .Where(x => x.AcademyId == academyA && x.IsActive)
                .ToDictionaryAsync(x => x.Id, x => ValueTuple.Create(x.Name, x.BatchCode));
        }
        client.DefaultRequestHeaders.Authorization = null;
        using (var denied = await client.GetAsync(pathA))
            PentaRequire(denied.StatusCode == HttpStatusCode.Unauthorized,
                "Anonymous batch search was not 401.");
        foreach (var (bearer, label) in new[] { (teacher, "Teacher"), (platform, "Platform owner"),
            (tokenB, "Foreign academy admin") })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var denied = await client.GetAsync(pathA);
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                $"{label} batch search was not 403.");
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        using (var denied = await client.GetAsync(pathB))
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden,
                "Batch search crossed academy scope.");
        using (var invalid = await client.GetAsync($"/api/academies/{academyA}/penta/batch-search?q=x"))
            PentaRequire(invalid.StatusCode == HttpStatusCode.BadRequest,
                "Single-character batch search was accepted.");
        using (var read = await client.GetAsync(pathA))
        {
            PentaRequire(read.StatusCode == HttpStatusCode.OK, "Own-academy batch search failed.");
            var body = await read.Content.ReadAsStringAsync();
            PentaRequire(!body.Contains("private batch note", StringComparison.Ordinal) &&
                !body.Contains("private.invalid", StringComparison.Ordinal) &&
                !body.Contains("Inactive", StringComparison.Ordinal) &&
                !body.Contains("Foreign", StringComparison.Ordinal),
                "Batch search disclosed a private, foreign or inactive field.");
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            var rows = root.GetProperty("rows");
            PentaRequire(root.GetProperty("tool").GetString() == PentaBatchSearchService.ToolName &&
                root.GetProperty("academyId").GetGuid() == academyA &&
                root.GetProperty("maxRows").GetInt32() == 10 &&
                root.GetProperty("hasMore").GetBoolean() && rows.GetArrayLength() == 10 &&
                rows.EnumerateArray().All(x => x.EnumerateObject().Count() == 4 &&
                    ownBatches.TryGetValue(x.GetProperty("sourceId").GetGuid(), out var batch) &&
                    batch.Name == x.GetProperty("name").GetString() &&
                    batch.Code == x.GetProperty("batchCode").GetString() &&
                    x.GetProperty("sourcePath").GetString() == "/batch-setup"),
                "Batch search did not return ten minimal source rows.");
        }
        using (var code = await client.GetAsync($"/api/academies/{academyA}/penta/batch-search?q=QA-PI-11"))
        {
            PentaRequire(code.StatusCode == HttpStatusCode.OK, "Batch code search failed.");
            using var json = JsonDocument.Parse(await code.Content.ReadAsStringAsync());
            PentaRequire(json.RootElement.GetProperty("rows").GetArrayLength() == 1,
                "Batch code search did not identify one row.");
        }
        using (var empty = await client.GetAsync($"/api/academies/{academyA}/penta/batch-search?q=Absent"))
        {
            PentaRequire(empty.StatusCode == HttpStatusCode.OK, "Zero-result batch search failed.");
            using var json = JsonDocument.Parse(await empty.Content.ReadAsStringAsync());
            PentaRequire(json.RootElement.GetProperty("rows").GetArrayLength() == 0,
                "Zero-result batch search disclosed a row.");
        }
        PentaRequire(provider.Calls == 0, "Batch search invoked the model or synthetic provider.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.AuditLogs.CountAsync(x => x.Action == "PentaBatchSearchRead" &&
                x.AcademyId == academyA) == 3 &&
                await db.AuditLogs.CountAsync(x => x.Action == "PentaBatchSearchRead" &&
                    x.AcademyId == academyB) == 0 &&
                await db.PentaExecutions.CountAsync() == 0,
                "Batch search audit or execution boundary failed.");
        }
        using (var faultFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" },
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var faultClient = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Batch search audit-fault host missed SQL preflight.");
            var failed = false;
            try
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
                await scope.ServiceProvider.GetRequiredService<PentaBatchSearchService>()
                    .ReadAsync(academyA, actorId, "QA Piano", CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { failed = true; }
            catch (InvalidOperationException ex) when (ex.Message.Contains("QA PENTA audit-store write fault"))
            { failed = true; }
            PentaRequire(failed, "Batch search returned data despite audit write failure.");
        }
        Console.WriteLine("PENTA BATCH SEARCH PASS: real SQL bounded own-academy source rows, 401/403, no private fields/provider call, audit rollback.");
    }

    private static async Task VerifyPentaProbeCoordinationAsync(QaRunManifest manifest,
        QaApiFactory factory, Guid academyId)
    {
        var probe = new FakeModelProbe();
        var settings = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Penta:Enabled"] = "true", ["Penta:Provider:Enabled"] = "true",
            ["Penta:Provider:SyntheticProbeApproved"] = "true",
            ["Penta:Provider:OrchestrationEnabled"] = "true"
        }).Build();
        var disabled = new ConfigurationBuilder().Build();
        var policy = new PentaPricedPolicy("qa.fake", "qa.fake-model", "qa.probe.v1", 300m, 300m,
            2048, 64, .7m, .7m); // Test-only quote; no live provider rate is approved.
        Guid actorId;
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            actorId = (await users.FindByEmailAsync("penta-admin-b@example.invalid"))!.Id;
            foreach (var id in ids)
            {
                var task = new PentaTask { AcademyId = academyId, ActorUserId = actorId,
                    Capability = "pulse", Status = "Executing" };
                db.PentaTasks.Add(task);
                db.PentaExecutions.Add(new PentaExecution { Id = id, AcademyId = academyId,
                    ActorUserId = actorId, TaskId = task.Id, ToolName = PentaProbeCoordinator.ToolName,
                    IdempotencyKey = $"qa-probe-{id}", InputDigest = new string('B', 64),
                    Status = "Executing" });
            }
            await db.SaveChangesAsync();
        }
        async Task<PentaBudgetResult> Run(Guid id, IConfiguration config, PentaPricedPolicy quote)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var coordinator = new PentaProbeCoordinator(
                scope.ServiceProvider.GetRequiredService<PentaBudgetService>(), probe, config,
                scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
            return await coordinator.RunAsync(academyId, actorId, id, quote, CancellationToken.None);
        }
        PentaRequire(await Run(ids[0], disabled, policy) == PentaBudgetResult.InvalidPolicy && probe.Calls == 0,
            "Disabled orchestration reached the model probe.");
        PentaRequire(await Run(ids[0], settings, policy with { Model = "wrong" }) ==
            PentaBudgetResult.InvalidPolicy && probe.Calls == 0,
            "Provider/model mismatch reached the model probe.");
        PentaRequire(await Run(ids[0], settings, policy) == PentaBudgetResult.Reconciled && probe.Calls == 1,
            "Priced synthetic probe did not reconcile once.");
        PentaRequire(await Run(ids[0], settings, policy) == PentaBudgetResult.NotFound && probe.Calls == 1,
            "Completed probe was redispatched.");
        probe.Next = new("OutcomeUnknown");
        PentaRequire(await Run(ids[1], settings, policy) == PentaBudgetResult.UsageUnknown && probe.Calls == 2,
            "Ambiguous probe did not retain unknown usage.");
        PentaRequire(await Run(ids[2], settings, policy) == PentaBudgetResult.Exhausted && probe.Calls == 2,
            "Unknown probe usage released its reserved budget.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.PentaUsageReservations.AsNoTracking()
                .Where(x => x.AcademyId == academyId && ids.Contains(x.ExecutionId)).ToListAsync();
            PentaRequire(rows.Count == 2 && rows.Any(x => x.ExecutionId == ids[0] &&
                x.Status == "Reconciled" && x.InputTokensObserved == 33 && x.OutputTokensObserved == 8) &&
                rows.Any(x => x.ExecutionId == ids[1] && x.Status == "UsageUnknown" && x.ActualCost == null),
                "Probe budget ledger did not retain exact observed and unknown outcomes.");
            PentaRequire(await db.PentaAttempts.CountAsync(x => ids.Contains(x.ExecutionId)) == 2 &&
                await db.AuditLogs.CountAsync(x => x.Action == "PentaSyntheticProbeCompleted" &&
                    x.EntityId.HasValue && ids.Contains(x.EntityId.Value)) == 2 &&
                await db.PentaUsageEntries.CountAsync(x => x.ReservationId == rows[0].Id ||
                    x.ReservationId == rows[1].Id) == 1,
                "Probe outcome, usage and audit were not recorded exactly once.");
        }
        // Reserve normally, then fault only the completion audit write. Usage,
        // execution and attempt must roll back together, with no replay.
        await using (var scope = factory.Services.CreateAsyncScope())
            PentaRequire(await scope.ServiceProvider.GetRequiredService<PentaBudgetService>()
                .ReserveAsync(academyId, actorId, ids[3], policy with { DailyUserUsd = 2m,
                    DailyAcademyUsd = 2m }, CancellationToken.None, PentaProbeCoordinator.ToolName) ==
                PentaBudgetResult.Reserved, "Could not reserve completion-fault fixture.");
        using (var faultFactory = new QaApiFactory(manifest,
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var faultClient = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Probe completion fault host missed SQL preflight.");
            var faulted = false;
            try
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PentaBudgetService>()
                    .CompleteProbeAsync(academyId, actorId, ids[3], policy with { DailyUserUsd = 2m,
                        DailyAcademyUsd = 2m }, new("Succeeded", 33, 8), CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { faulted = true; }
            catch (InvalidOperationException ex) when (ex.Message.Contains("QA PENTA audit-store write fault"))
            { faulted = true; }
            PentaRequire(faulted, "Injected probe completion audit fault did not fail closed.");
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaUsageReservations.AsNoTracking().SingleAsync(x => x.ExecutionId == ids[3]);
            PentaRequire(row.Status == "Reserved" && row.ActualCost == null &&
                await db.PentaExecutions.AnyAsync(x => x.Id == ids[3] && x.Status == "Executing") &&
                !await db.PentaAttempts.AnyAsync(x => x.ExecutionId == ids[3]) &&
                !await db.PentaUsageEntries.AnyAsync(x => x.ReservationId == row.Id),
                "Probe completion audit fault partially committed outcome or usage.");
        }
        Console.WriteLine("PENTA PROBE PASS: disabled/mismatch denied, fake call budgeted, observed usage finalized, ambiguous usage held, exhausted denied, completion audit rollback.");
    }

    private static async Task VerifyPentaPricedBudgetAsync(QaRunManifest manifest, QaApiFactory factory, Guid academyId)
    {
        Guid actorId;
        var executionIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
            foreach (var executionId in executionIds)
            {
                var task = new PentaTask { AcademyId = academyId, ActorUserId = actorId,
                    Capability = "pulse", Status = "Executing", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) };
                db.PentaTasks.Add(task);
                db.PentaExecutions.Add(new PentaExecution { Id = executionId, AcademyId = academyId,
                    ActorUserId = actorId, TaskId = task.Id, ToolName = "qa.priced.contract",
                    IdempotencyKey = $"priced-{executionId}", InputDigest = new string('A', 64),
                    Status = "Executing", CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10) });
            }
            await db.SaveChangesAsync();
        }
        var policy = new PentaPricedPolicy("qa.fake", "qa.fake-model", "qa.v1", 300m, 300m,
            1000, 1000, 1m, 1m); // Test-only pricing, not an approved live model.
        async Task<PentaBudgetResult> Reserve(Guid id, PentaPricedPolicy quote)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PentaBudgetService>()
                .ReserveAsync(academyId, actorId, id, quote, CancellationToken.None);
        }
        PentaRequire(await Reserve(executionIds[0], policy with { PriceVersion = "" }) ==
            PentaBudgetResult.InvalidPolicy, "Missing price version did not fail closed.");
        var claims = await Task.WhenAll(Reserve(executionIds[0], policy), Reserve(executionIds[1], policy));
        PentaRequire(claims.Count(x => x == PentaBudgetResult.Reserved) == 1 &&
            claims.Count(x => x == PentaBudgetResult.Exhausted) == 1,
            "Concurrent priced budget admitted more than one 0.6 USD reservation under 1 USD cap.");
        var winner = claims[0] == PentaBudgetResult.Reserved ? executionIds[0] : executionIds[1];
        var loser = claims[0] == PentaBudgetResult.Reserved ? executionIds[1] : executionIds[0];
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var budget = scope.ServiceProvider.GetRequiredService<PentaBudgetService>();
            PentaRequire(!await budget.MarkStaleUnknownAsync(academyId, winner, DateTime.UtcNow.AddMinutes(-1),
                CancellationToken.None), "Recovery accepted a too-recent cutoff.");
            PentaRequire(await budget.MarkStaleUnknownAsync(academyId, winner, DateTime.UtcNow.AddMinutes(-5),
                CancellationToken.None), "Stale claim was not marked unknown.");
        }
        PentaRequire(await Reserve(loser, policy) == PentaBudgetResult.Exhausted,
            "Unknown usage released a priced reservation.");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var budget = scope.ServiceProvider.GetRequiredService<PentaBudgetService>();
            PentaRequire(await budget.ReconcileAsync(academyId, winner, "qa-observed", 500, 500,
                policy with { InputUsdPerMillion = 1m }, CancellationToken.None) == PentaBudgetResult.InvalidPolicy,
                "Repriced observation bypassed original price snapshot.");
            PentaRequire(await budget.ReconcileAsync(academyId, winner, "qa-observed", 1001, 0,
                policy, CancellationToken.None) == PentaBudgetResult.InvalidUsage,
                "Over-limit token observation was accepted.");
            PentaRequire(await budget.ReconcileAsync(academyId, winner, "qa-observed", 500, 500,
                policy, CancellationToken.None) == PentaBudgetResult.Reconciled,
                "Known usage did not reconcile.");
            PentaRequire(await budget.ReconcileAsync(academyId, winner, "qa-observed", 500, 500,
                policy, CancellationToken.None) == PentaBudgetResult.AlreadyReconciled,
                "Duplicate usage reconciliation was accepted.");
            var row = await db.PentaUsageReservations.AsNoTracking().SingleAsync(x => x.ExecutionId == winner);
            PentaRequire(row.EstimatedCost == .6m && row.ActualCost == .3m &&
                row.Status == "Reconciled" && row.InputTokensObserved == 500 &&
                await db.PentaUsageEntries.CountAsync(x => x.ReservationId == row.Id) == 1,
                "Priced usage snapshot or reconciliation ledger is incorrect.");
            PentaRequire(await db.PentaExecutions.AnyAsync(x => x.Id == winner && x.Status == "OutcomeUnknown") &&
                await db.PentaAttempts.CountAsync(x => x.ExecutionId == winner) == 1,
                "Stale execution was not durably blocked from blind retry.");
        }
        PentaRequire(await Reserve(loser, policy) == PentaBudgetResult.Reserved,
            "Reconciled lower actual cost did not release unused budget.");

        // Command interception is scoped to a second preflighted QA host and
        // requires no DDL or permission elevation on the run-owned SQL store.
        using (var faultFactory = new QaApiFactory(manifest,
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var faultClient = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Audit-fault host missed SQL preflight.");
            var failed = false;
            try
            {
                await using var faultScope = faultFactory.Services.CreateAsyncScope();
                await faultScope.ServiceProvider.GetRequiredService<PentaBudgetService>()
                    .ReserveAsync(academyId, actorId, executionIds[2],
                        policy with { DailyUserUsd = 2m, DailyAcademyUsd = 2m }, CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { failed = true; }
            catch (InvalidOperationException ex) when (ex.Message.Contains("QA PENTA audit-store write fault"))
            { failed = true; }
            PentaRequire(failed, "Injected audit fault did not fail the reservation transaction.");
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(!await db.PentaUsageReservations.AnyAsync(x => x.ExecutionId == executionIds[2]),
                "Audit fault left an unaudited priced reservation.");
        }
        Console.WriteLine("PENTA PRICED PASS: version fail-closed, concurrent cap, unknown retention, snapshot reconciliation, stale recovery and audit rollback.");
    }

    private static async Task VerifyPentaCompletionFaultAsync(QaRunManifest manifest,
        QaApiFactory factory, Guid academyId, string bearer)
    {
        var provider = new CountingPentaProvider();
        var key = $"qa-completion-fault-{Guid.NewGuid():N}";
        Guid actorId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
        }
        using (var faultFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" },
            isolatedPentaProvider: provider, isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var client = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Completion-fault host missed SQL preflight.");
            var failed = false;
            try
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PentaExecutionService>()
                    .RunAsync(academyId, actorId, "pulse", "qa completion fault", key, CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { failed = true; }
            PentaRequire(failed && provider.Calls == 1, "Completion audit fault did not occur after one fake dispatch.");
        }
        Guid executionId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var execution = await db.PentaExecutions.SingleAsync(x => x.AcademyId == academyId && x.IdempotencyKey == key);
            executionId = execution.Id;
            PentaRequire(execution.Status == "Executing" &&
                await db.PentaAttempts.CountAsync(x => x.ExecutionId == executionId) == 0 &&
                await db.AuditLogs.CountAsync(x => x.EntityType == "PentaExecution" && x.EntityId == executionId) == 0 &&
                await db.PentaUsageReservations.AnyAsync(x => x.ExecutionId == executionId && x.Status == "Reserved"),
                "Completion fault did not leave an unreconciled durable claim.");
            execution.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10);
            await db.SaveChangesAsync();
        }
        using (var client = factory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var pending = await PentaPostAsync(client, $"/api/academies/{academyId}/penta/turns", key,
                "qa completion fault");
            PentaRequire(pending.StatusCode == HttpStatusCode.Accepted && provider.Calls == 1,
                "Restart replay redispatched an ambiguous completion.");
        }
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var budget = scope.ServiceProvider.GetRequiredService<PentaBudgetService>();
            PentaRequire(await budget.MarkStaleUnknownAsync(academyId, executionId,
                DateTime.UtcNow.AddMinutes(-5), CancellationToken.None),
                "Ambiguous completion could not be conservatively recovered.");
        }
        using (var client = factory.CreateClient(new() { AllowAutoRedirect = false }))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var unknown = await PentaPostAsync(client, $"/api/academies/{academyId}/penta/turns", key,
                "qa completion fault");
            PentaRequire(unknown.StatusCode == HttpStatusCode.BadGateway && provider.Calls == 1,
                "Unknown completion was replayed as a successful new dispatch.");
        }
        Console.WriteLine("PENTA RECOVERY PASS: completion audit fault held claim/usage, restart did not redispatch, stale claim marked unknown.");
    }

    private static async Task VerifyPentaRecoveryCompletionRaceAsync(QaRunManifest manifest,
        QaApiFactory normalFactory, Guid academyId, string bearer)
    {
        var provider = new CountingPentaProvider();
        using var runningFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }, isolatedPentaProvider: provider);
        using var client = runningFactory.CreateClient(new() { AllowAutoRedirect = false });
        PentaRequire(runningFactory.PreflightPassed, "Recovery race host missed SQL preflight.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        var key = $"qa-recovery-race-{Guid.NewGuid():N}";
        var entered = provider.BlockNextAsync();
        var running = PentaPostAsync(client, $"/api/academies/{academyId}/penta/turns", key,
            "qa recovery race");
        await entered.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            Guid executionId;
            await using (var scope = normalFactory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var execution = await db.PentaExecutions.SingleAsync(x => x.AcademyId == academyId &&
                    x.IdempotencyKey == key);
                executionId = execution.Id;
                execution.CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10);
                await db.SaveChangesAsync();
            }
            await using (var scope = normalFactory.Services.CreateAsyncScope())
            {
                var budget = scope.ServiceProvider.GetRequiredService<PentaBudgetService>();
                PentaRequire(await budget.MarkStaleUnknownAsync(academyId, executionId,
                    DateTime.UtcNow.AddMinutes(-5), CancellationToken.None),
                    "Recovery could not mark held fake dispatch unknown.");
            }
            provider.Release();
            using var result = await running;
            PentaRequire(result.StatusCode == HttpStatusCode.BadGateway && provider.Calls == 1,
                "Late fake completion overwrote the recovered unknown outcome.");
            await using var verifyScope = normalFactory.Services.CreateAsyncScope();
            var verify = verifyScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var reservation = await verify.PentaUsageReservations.AsNoTracking()
                .SingleAsync(x => x.ExecutionId == executionId);
            PentaRequire(await verify.PentaExecutions.AnyAsync(x => x.Id == executionId &&
                x.Status == "OutcomeUnknown") &&
                reservation.Status == "UsageUnknown" && reservation.ActualCost == null &&
                !await verify.PentaUsageEntries.AnyAsync(x => x.AcademyId == academyId &&
                    x.ReservationId == reservation.Id),
                "Late completion changed the recovery ledger.");
        }
        finally { provider.Release(); }
        Console.WriteLine("PENTA RECOVERY RACE PASS: late fake-provider completion could not overwrite unknown state or release usage.");
    }

    private static async Task VerifyPentaDraftApprovalAsync(QaRunManifest manifest,
        QaApiFactory normalFactory, Guid academyA, Guid academyB, string tokenA,
        string tokenC, string tokenB, string teacher, string platform)
    {
        var clock = new PentaQaClock();
        using var factory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" }, isolatedClock: clock);
        using var adminA = factory.CreateClient(new() { AllowAutoRedirect = false });
        using var adminC = factory.CreateClient(new() { AllowAutoRedirect = false });
        using var deniedClient = factory.CreateClient(new() { AllowAutoRedirect = false });
        PentaRequire(factory.PreflightPassed, "Draft approval host missed SQL preflight.");
        adminA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        adminC.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenC);
        var path = $"/api/academies/{academyA}/penta/draft-previews";
        async Task<HttpResponseMessage> Preview(HttpClient client, string key, string title = "Review schedule")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            { Content = JsonContent.Create(new { title }) };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request);
        }
        using (var anonymous = await Preview(deniedClient, "qa-draft-denied"))
            PentaRequire(anonymous.StatusCode == HttpStatusCode.Unauthorized, "Anonymous preview was not 401.");
        foreach (var (token, label) in new[] { (teacher, "teacher"), (platform, "platform"), (tokenB, "foreign") })
        {
            deniedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var denied = await Preview(deniedClient, $"qa-draft-{label}");
            PentaRequire(denied.StatusCode == HttpStatusCode.Forbidden, $"{label} preview was not denied.");
        }
        var sameKey = "qa-draft-concurrent-preview";
        var pair = await Task.WhenAll(Preview(adminA, sameKey), Preview(adminA, sameKey));
        using var first = pair[0];
        using var second = pair[1];
        PentaRequire(new[] { first.StatusCode, second.StatusCode }.Count(x => x == HttpStatusCode.Created) == 1 &&
            new[] { first.StatusCode, second.StatusCode }.Count(x => x == HttpStatusCode.OK) == 1,
            "Concurrent preview did not create once and replay once.");
        var firstState = await first.Content.ReadFromJsonAsync<PentaDraftPreviewState>();
        var secondState = await second.Content.ReadFromJsonAsync<PentaDraftPreviewState>();
        PentaRequire(firstState is not null && secondState is not null &&
            firstState.ApprovalId == secondState.ApprovalId && firstState.Digest == secondState.Digest,
            "Concurrent preview did not return one bound approval.");
        var approvalId = firstState!.ApprovalId;
        using (var changed = await Preview(adminA, sameKey, "Review attendance"))
            PentaRequire(changed.StatusCode == HttpStatusCode.Conflict, "Changed preview arguments were accepted.");
        using (var foreignActor = await adminC.GetAsync($"{path}/{approvalId}"))
            PentaRequire(foreignActor.StatusCode == HttpStatusCode.NotFound, "Second admin read another actor's preview.");
        using (var foreignTenant = await deniedClient.GetAsync($"/api/academies/{academyB}/penta/draft-previews/{approvalId}"))
            PentaRequire(foreignTenant.StatusCode != HttpStatusCode.OK, "Foreign tenant read approval.");
        using (var wrong = await adminA.PostAsJsonAsync($"{path}/{approvalId}/confirm",
            new { digest = new string('0', 64) }))
            PentaRequire(wrong.StatusCode == HttpStatusCode.Conflict, "Wrong digest confirmed preview.");
        using (var other = await adminC.PostAsJsonAsync($"{path}/{approvalId}/confirm",
            new { digest = firstState.Digest }))
            PentaRequire(other.StatusCode == HttpStatusCode.NotFound,
                "Another academy admin confirmed the initiating actor's preview.");
        var confirmations = await Task.WhenAll(
            adminA.PostAsJsonAsync($"{path}/{approvalId}/confirm", new { digest = firstState.Digest }),
            adminA.PostAsJsonAsync($"{path}/{approvalId}/confirm", new { digest = firstState.Digest }));
        using (var retryA = confirmations[0]) using (var retryB = confirmations[1])
            PentaRequire(retryA.StatusCode == HttpStatusCode.OK && retryB.StatusCode == HttpStatusCode.OK,
                "Concurrent matching confirmations did not replay approved state.");
        await using (var scope = normalFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(await db.PentaApprovals.CountAsync(x => x.Id == approvalId && x.Status == "Approved") == 1 &&
                await db.AuditLogs.CountAsync(x => x.EntityType == "PentaApproval" && x.EntityId == approvalId) == 2 &&
                !await db.AdminWorkItems.AnyAsync(x => x.AcademyId == academyA),
                "Confirmation duplicated audit or created a domain work item.");
        }
        using var integrity = await Preview(adminA, "qa-draft-integrity", "Review attendance");
        PentaRequire(integrity.StatusCode == HttpStatusCode.Created, "Integrity fixture preview failed.");
        var integrityState = (await integrity.Content.ReadFromJsonAsync<PentaDraftPreviewState>())!;
        await using (var scope = normalFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var execution = await db.PentaExecutions.SingleAsync(x => x.Id == integrityState.ExecutionId);
            execution.ProposalPayloadJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Review schedule" });
            await db.SaveChangesAsync();
        }
        using (var tampered = await adminA.PostAsJsonAsync($"{path}/{integrityState.ApprovalId}/confirm",
            new { digest = integrityState.Digest }))
            PentaRequire(tampered.StatusCode == HttpStatusCode.Conflict,
                "Stored proposal changed without requiring a new preview.");
        using var expiring = await Preview(adminA, "qa-draft-expiring");
        PentaRequire(expiring.StatusCode == HttpStatusCode.Created, "Expiry fixture preview failed.");
        var expiringState = (await expiring.Content.ReadFromJsonAsync<PentaDraftPreviewState>())!;
        clock.Advance(TimeSpan.FromMinutes(11));
        using (var expired = await adminA.PostAsJsonAsync($"{path}/{expiringState.ApprovalId}/confirm",
            new { digest = expiringState.Digest }))
            PentaRequire(expired.StatusCode == HttpStatusCode.Gone, "Expired preview was confirmed.");

        // Force preparation's audit insert to fail; no task/execution/approval may survive.
        int beforeTasks, beforeExecutions, beforeApprovals;
        await using (var scope = normalFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            beforeTasks = await db.PentaTasks.CountAsync();
            beforeExecutions = await db.PentaExecutions.CountAsync();
            beforeApprovals = await db.PentaApprovals.CountAsync();
        }
        using (var faultFactory = new QaApiFactory(manifest,
            new Dictionary<string, string?> { ["Penta:Enabled"] = "true" },
            isolatedDomainInterceptor: new PentaAuditFaultInterceptor()))
        {
            using var client = faultFactory.CreateClient();
            PentaRequire(faultFactory.PreflightPassed, "Draft audit-fault host missed SQL preflight.");
            Guid actorId;
            await using (var scope = normalFactory.Services.CreateAsyncScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                actorId = (await users.FindByEmailAsync("penta-admin-a@example.invalid"))!.Id;
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actorId.ToString())], "QA"));
            var failed = false;
            try
            {
                await using var scope = faultFactory.Services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PentaDraftApprovalService>()
                    .PrepareAsync(principal, academyA, "Review attendance",
                        "qa-draft-audit-fault", CancellationToken.None);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("QA PENTA audit-store write fault") == true)
            { failed = true; }
            PentaRequire(failed, "Draft audit fault did not interrupt preparation.");
        }
        await using (var scope = normalFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            PentaRequire(!await db.PentaExecutions.AnyAsync(x => x.IdempotencyKey == "qa-draft-audit-fault") &&
                await db.PentaTasks.CountAsync() == beforeTasks &&
                await db.PentaExecutions.CountAsync() == beforeExecutions &&
                await db.PentaApprovals.CountAsync() == beforeApprovals,
                "Audit fault left an unaudited preview.");
        }
        Console.WriteLine("PENTA DRAFT APPROVAL PASS: actor/tenant denial, concurrent replay/confirm, digest/expiry, no domain effect, audit rollback.");
    }

    private static void PentaRequire(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
