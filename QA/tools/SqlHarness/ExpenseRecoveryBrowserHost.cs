using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task ServeExpenseRecoveryBrowserAsync(QaRunManifest manifest)
    {
        const string api = "http://127.0.0.1:54999";
        const string origin = "http://127.0.0.1:54998";
        const string lostDescription = "Synthetic expense after lost response";
        const string savedDescription = "Synthetic expense after failed refresh";
        const string unsentDescription = "Synthetic expense never sent";
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned expense browser preflight failed.");
        Guid academy, actor;
        Branch branch;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            branch = new Branch { AcademyId = academy, Name = "Synthetic Branch", AddressLine1 = "Synthetic private address" };
            db.AddRange(branch, new Expense { AcademyId = academy, Description = "Synthetic USD history", Amount = 5.25m,
                Currency = "USD", BranchId = branch.Id });
            await db.SaveChangesAsync();
        }

        async Task<JsonObject> Snapshot()
        {
            var state = await FinanceRecoveryStateAsync(factory);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            state["expenses"] = JsonSerializer.SerializeToNode(await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            state["branches"] = JsonSerializer.SerializeToNode(await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            return state;
        }
        var state = await Snapshot();
        var expensePath = $"/api/academies/{academy}/expenses";
        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "expense-browser-fixture.json"), JsonSerializer.Serialize(new
        {
            run, origin, api, academy, branchId = branch.Id, lostDescription, savedDescription, unsentDescription
        }));

        var completed = 0;
        var failed = false;
        using var checkLock = new SemaphoreSlim(1);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(api);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
        await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != 54999)
            {
                context.Response.StatusCode = 400;
                return;
            }
            if (context.Request.Path == "/__qa/expense-recovery")
            {
                if (context.Request.Method != "POST" || context.Request.ContentLength is not (> 0 and <= 2048))
                {
                    context.Response.StatusCode = 400;
                    return;
                }
                await checkLock.WaitAsync(context.RequestAborted);
                try
                {
                    var input = await context.Request.ReadFromJsonAsync<JsonObject>(context.RequestAborted);
                    if (input?["run"]?.GetValue<string>() != run || failed || completed >= 6 || input["phase"]?.GetValue<int>() != completed + 1)
                        throw new InvalidOperationException("Unexpected expense recovery phase.");
                    var actual = await Snapshot();
                    if (completed is 0 or 2)
                    {
                        var first = completed == 0;
                        if (!Guid.TryParse(input["expenseId"]?.GetValue<string>(), out var expenseId))
                            throw new InvalidOperationException("The saved expense ID is missing.");
                        var normalized = actual.DeepClone().AsObject();
                        var row = ManualAdded(state, normalized, "expenses", 1).Single();
                        FinanceRecoveryFields(row, new { Id = expenseId, AcademyId = academy, Currency = "INR",
                            Description = first ? lostDescription : savedDescription, Amount = first ? 25.55m : 27.05m,
                            Category = "General", BranchId = first ? (Guid?)null : branch.Id, Status = "Recorded" });
                        var audit = ManualAdded(state, normalized, "audits", 1).Single();
                        FinanceRecoveryFields(audit, new { ActorUserId = actor, AcademyId = academy, Action = "POST Expenses" });
                        if (!audit["MetadataJson"]!.GetValue<string>().Contains(expensePath, StringComparison.Ordinal))
                            throw new InvalidOperationException("Expense actor audit route differs.");
                        ManualSame(state, normalized, "expense write unrelated state");
                    }
                    else
                    {
                        ManualSame(state, actual, "expense recovery reads cannot replay writes");
                    }
                    state = actual;
                    completed++;
                    Console.WriteLine($"EXPENSEBROWSER SQL PASS phase={completed}");
                    await context.Response.WriteAsJsonAsync(new { phase = completed }, context.RequestAborted);
                }
                catch (Exception error)
                {
                    failed = true;
                    Console.WriteLine("EXPENSEBROWSER SQL FAILED " + error.Message);
                    context.Response.StatusCode = 500;
                }
                finally { checkLock.Release(); }
                return;
            }
            var method = context.Request.Method;
            var route = context.Request.Path;
            if (!route.StartsWithSegments("/api") || !(method is "GET" or "OPTIONS" ||
                method == "POST" && (route == expensePath || route == "/api/auth/login" || route == "/api/auth/refresh")))
            {
                context.Response.StatusCode = 405;
                return;
            }
            await ForwardFinanceBrowserRequestAsync(context, client);
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-expense-recovery");
        Console.WriteLine($"EXPENSEBROWSER READY run={run} api=54999 origin=54998 stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != 6 || !File.Exists(stop))
            throw new InvalidOperationException($"Incomplete expense recovery verification: {completed}/6.");
        ManualSame(state, await Snapshot(), "final expense recovery SQL state");
        Console.WriteLine("EXPENSEBROWSER FINAL 6 verified UI/SQL phases.");
    }
}
