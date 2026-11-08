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
    private static async Task ServePayrollAcceptanceAsync(QaRunManifest manifest)
    {
        const string api = "http://127.0.0.1:54999";
        const string origin = "http://127.0.0.1:54998";
        const string period = "November 2026";
        const string reference = "Synthetic positive-net acceptance";
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned payroll browser preflight failed.");

        Guid academy, admin;
        PayrollProfile monthly, alternate;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            var teacher = await db.Teachers.SingleAsync(x => x.AcademyId == academy);
            admin = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            var workerName = teacher.FirstName + " " + teacher.LastName;
            monthly = new PayrollProfile { AcademyId = academy, WorkerType = "Teacher", TeacherId = teacher.Id,
                WorkerName = workerName, MonthlyAmount = 1000.25m, EffectiveFrom = new DateOnly(2026, 10, 1) };
            alternate = new PayrollProfile { AcademyId = academy, WorkerType = "Teacher", TeacherId = teacher.Id,
                WorkerName = workerName, MonthlyAmount = 1200.25m, EffectiveFrom = new DateOnly(2026, 10, 2) };
            db.AddRange(monthly, alternate);
            await db.SaveChangesAsync();
        }

        var payoutsPath = $"/api/academies/{academy}/payroll/payouts";
        var before = await FinanceRecoveryStateAsync(factory);
        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "payroll-acceptance-fixture.json"), JsonSerializer.Serialize(new
        {
            run, origin, api, academy, profile = monthly.Id, alternate = alternate.Id, period, reference
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

            if (context.Request.Path == "/__qa/payroll-acceptance")
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
                    if (input?["run"]?.GetValue<string>() != run || failed || completed >= 4 || input["phase"]?.GetValue<int>() != completed + 1)
                        throw new InvalidOperationException("Unexpected payroll browser phase.");
                    var current = await FinanceRecoveryStateAsync(factory);
                    if (completed == 1)
                    {
                        var normalized = current.DeepClone().AsObject();
                        var row = ManualAdded(before, normalized, "payouts", 1).Single();
                        if (!Guid.TryParse(input["payoutId"]?.GetValue<string>(), out var payoutId))
                            throw new InvalidOperationException("Saved payout ID missing.");
                        FinanceRecoveryFields(row, new { Id = payoutId, AcademyId = academy, PayrollProfileId = monthly.Id,
                            GrossAmount = 1000.25m, Deductions = 1000.24m, NetAmount = .01m, Status = "Paid", Currency = "INR",
                            WorkerNameSnapshot = monthly.WorkerName, WorkerTypeSnapshot = monthly.WorkerType,
                            TeacherIdSnapshot = monthly.TeacherId, Reference = reference, PeriodLabel = period });
                        if (row["IdentityRecordedAtUtc"] is null || string.IsNullOrWhiteSpace(row["PayslipNumber"]?.GetValue<string>()))
                            throw new InvalidOperationException("Saved payslip identity is missing.");
                        var audit = ManualAdded(before, normalized, "audits", 1).Single();
                        FinanceRecoveryFields(audit, new { ActorUserId = admin, AcademyId = academy, Action = "POST Payroll" });
                        if (!audit["MetadataJson"]!.GetValue<string>().Contains(payoutsPath, StringComparison.Ordinal))
                            throw new InvalidOperationException("Payroll audit route differs.");
                        ManualSame(before, normalized, "payroll settlement unrelated state");
                    }
                    else
                    {
                        ManualSame(before, current, "refused payroll or read-only payslip");
                    }
                    before = current;
                    completed++;
                    Console.WriteLine($"PAYROLLBROWSER SQL PASS phase={completed}");
                    await context.Response.WriteAsJsonAsync(new { phase = completed }, context.RequestAborted);
                }
                catch (Exception error)
                {
                    failed = true;
                    Console.WriteLine("PAYROLLBROWSER SQL FAILED " + error.Message);
                    context.Response.StatusCode = 500;
                }
                finally { checkLock.Release(); }
                return;
            }

            var method = context.Request.Method;
            var route = context.Request.Path;
            if (!route.StartsWithSegments("/api") || !(method is "GET" or "OPTIONS" ||
                method == "POST" && (route == payoutsPath || route == "/api/auth/login" || route == "/api/auth/refresh")))
            {
                context.Response.StatusCode = 405;
                return;
            }
            await ForwardFinanceBrowserRequestAsync(context, client);
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-payroll-acceptance");
        Console.WriteLine($"PAYROLLBROWSER READY run={run} api=54999 origin=54998 stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != 4 || !File.Exists(stop))
            throw new InvalidOperationException($"Incomplete payroll browser verification: {completed}/4.");
        ManualSame(before, await FinanceRecoveryStateAsync(factory), "final payroll browser SQL state");
        Console.WriteLine("PAYROLLBROWSER FINAL 4 verified UI/SQL phases.");
    }
}
