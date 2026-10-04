using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed class CountingPentaProvider : IPentaSyntheticProvider
    {
        private readonly FixedPentaSyntheticProvider inner = new();
        public int Calls { get; private set; }
        public bool Malformed { get; set; }

        public Task<PentaSyntheticResult> ExecuteAsync(string capability, CancellationToken token)
        {
            Calls++;
            return Malformed
                ? Task.FromResult(new PentaSyntheticResult(Guid.NewGuid(), capability, "unregistered.tool", "bad", true))
                : inner.ExecuteAsync(capability, token);
        }
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
        var tokenB = await LoginAsync(enabled, "penta-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(enabled, "penta-teacher-a@example.invalid", "Synthetic!39Ab");
        var platform = await LoginAsync(enabled, "penta-platform@example.invalid", "Synthetic!39Ab");

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
        using (var success = await enabled.PostAsJsonAsync(pathA, valid))
        {
            PentaRequire(success.StatusCode == HttpStatusCode.OK, "Enabled synthetic request did not succeed.");
            var result = await success.Content.ReadFromJsonAsync<PentaSyntheticResult>();
            PentaRequire(result is { Synthetic: true, Tool: PentaSyntheticDispatcher.ToolName, Capability: "pulse" } &&
                result.CorrelationId != Guid.Empty && !result.Message.Contains("synthetic request", StringComparison.Ordinal),
                "Synthetic result was malformed or echoed user text.");
        }
        PentaRequire(provider.Calls == 1, "Successful diagnostic did not make exactly one provider call.");

        provider.Malformed = true;
        using (var invalid = await enabled.PostAsJsonAsync(pathA, valid))
            PentaRequire(invalid.StatusCode == HttpStatusCode.BadGateway, "Malformed fake response was accepted.");
        PentaRequire(provider.Calls == 2, "Malformed response was not exercised.");

        await using (var scope = enabledFactory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var audits = await db.AuditLogs.AsNoTracking()
                .Where(x => x.AcademyId == academyA && x.EntityType == "Penta").ToListAsync();
            PentaRequire(audits.Count == 1, "PENTA synthetic success/denial generic audit count was wrong.");
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
        Console.WriteLine("PENTA PASS: real Identity/SQL, two academy admins, default-off, role/tenant/platform/inactive denial, input limits, synthetic result, malformed provider, audit scope.");
    }

    private static void PentaRequire(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
