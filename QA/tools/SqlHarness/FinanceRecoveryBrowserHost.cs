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
    private const string FinanceEvidence = "C9 bank evidence";
    private const string FinancePeriod = "November 2026";
    private const string FinanceInvoiceNumber = "C9-RECONCILE";

    private static async Task<JsonObject> FinanceRecoveryStateAsync(QaApiFactory factory)
    {
        var state = await ManualStateAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        state["profiles"] = JsonSerializer.SerializeToNode(await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        state["payouts"] = JsonSerializer.SerializeToNode(await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        return state;
    }

    private static void FinanceRecoveryFields(JsonObject row, object expected)
    {
        foreach (var (name, value) in JsonSerializer.SerializeToNode(expected)!.AsObject())
        {
            var matches = value is JsonValue number && number.TryGetValue<decimal>(out var amount)
                ? row[name] is JsonValue actual && actual.TryGetValue<decimal>(out var actualAmount) && actualAmount == amount
                : JsonNode.DeepEquals(row[name], value);
            if (!row.ContainsKey(name) || !matches)
                throw new InvalidOperationException($"Unexpected saved finance field: {name}.");
        }
    }

    private static async Task ServeFinanceRecoveryBrowserAsync(QaRunManifest manifest)
    {
        const string api = "http://127.0.0.1:54999";
        const string origin = "http://127.0.0.1:54998";
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned finance browser backend preflight failed.");

        Guid academy, admin, student, teacher;
        var invoice = new Invoice { InvoiceNumber = FinanceInvoiceNumber, TotalAmount = 1000m, Status = "PartiallyPaid" };
        var payment = new Payment { InvoiceId = invoice.Id, Amount = 600m, Status = "Completed" };
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            admin = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            invoice.AcademyId = academy;
            invoice.StudentId = student;
            invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            payment.AcademyId = academy;
            db.AddRange(invoice, payment);
            await db.SaveChangesAsync();
        }
        var basePath = $"/api/academies/{academy}";
        var mutations = new (string Method, string Path, string Controller)[]
        {
            ("POST", basePath + "/invoices", "Invoices"),
            ("POST", basePath + "/payroll/profiles", "Payroll"),
            ("POST", basePath + "/payroll/payouts", "Payroll"),
            ("PATCH", $"{basePath}/payments/{payment.Id}/reconcile", "Payments")
        };
        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "finance-recovery-fixture.json"), JsonSerializer.Serialize(new
        {
            run, origin, api, academy, student, teacher, payment = payment.Id,
            invoiceNumber = FinanceInvoiceNumber, period = FinancePeriod, evidence = FinanceEvidence
        }));

        var state = await FinanceRecoveryStateAsync(factory);
        var completed = 0;
        var failed = false;
        Guid profileId = default;
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
            var method = context.Request.Method;
            var route = context.Request.Path;
            if (route != "/__qa/finance-recovery")
            {
                if (!route.StartsWithSegments("/api") ||
                    !(method is "GET" or "OPTIONS" || method == "POST" && (route == "/api/auth/login" || route == "/api/auth/refresh") ||
                        mutations.Any(entry => method == entry.Method && route == entry.Path)))
                {
                    context.Response.StatusCode = 405;
                    return;
                }
                await ForwardFinanceBrowserRequestAsync(context, client);
                return;
            }
            if (method != "POST" || context.Request.ContentLength is not (> 0 and <= 2048))
            {
                context.Response.StatusCode = 400;
                return;
            }
            await checkLock.WaitAsync(context.RequestAborted);
            try
            {
                var input = await context.Request.ReadFromJsonAsync<JsonObject>(context.RequestAborted);
                if (input?["run"]?.GetValue<string>() != run || failed || completed >= 8 || input["phase"]?.GetValue<int>() != completed + 1)
                    throw new InvalidOperationException("Unexpected finance recovery phase.");
                var actual = await FinanceRecoveryStateAsync(factory);
                var normalized = actual.DeepClone().AsObject();
                if (completed % 2 == 0)
                {
                    if (!Guid.TryParse(input["id"]?.GetValue<string>(), out var id)) throw new InvalidOperationException("Real saved row ID required.");
                    switch (completed)
                    {
                        case 0:
                            FinanceRecoveryFields(ManualAdded(state, normalized, "invoices", 1).Single(),
                                new { Id = id, AcademyId = academy, StudentId = student, TotalAmount = 123.45m,
                                    Currency = "INR", DueDate = new DateOnly(2026, 12, 15), Status = "Issued" });
                            break;
                        case 2:
                            if (!DateOnly.TryParseExact(input["effectiveFrom"]?.GetValue<string>(), "yyyy-MM-dd", out var effective))
                                throw new InvalidOperationException("Real selected effective date required.");
                            FinanceRecoveryFields(ManualAdded(state, normalized, "profiles", 1).Single(),
                                new { Id = id, AcademyId = academy, TeacherId = teacher, WorkerType = "Teacher", WorkerName = "Synthetic Teacher",
                                    PaymentModel = "Monthly", MonthlyAmount = 1000.25m, EffectiveFrom = effective, IsActive = true });
                            profileId = id;
                            break;
                        case 4:
                            var payout = ManualAdded(state, normalized, "payouts", 1).Single();
                            FinanceRecoveryFields(payout,
                                new { Id = id, AcademyId = academy, PayrollProfileId = profileId, TeacherIdSnapshot = teacher,
                                    WorkerNameSnapshot = "Synthetic Teacher", WorkerTypeSnapshot = "Teacher", PeriodLabel = FinancePeriod,
                                    GrossAmount = 1000.25m, Deductions = 100.10m, NetAmount = 900.15m, Status = "Paid", Currency = "INR" });
                            if (payout["IdentityRecordedAtUtc"] is null || string.IsNullOrWhiteSpace(payout["PayslipNumber"]?.GetValue<string>()))
                                throw new InvalidOperationException("Saved payslip identity is missing.");
                            break;
                        case 6:
                            var reconciled = ManualRow(normalized, "payments", payment.Id);
                            FinanceRecoveryFields(reconciled, new { Id = id, Amount = 600m, Status = "Reconciled", ReconciliationReference = FinanceEvidence });
                            if (reconciled["ReconciledAtUtc"] is null) throw new InvalidOperationException("Reconciliation time is missing.");
                            reconciled["Status"] = "Completed";
                            reconciled["ReconciliationReference"] = null;
                            reconciled["ReconciledAtUtc"] = null;
                            break;
                    }
                    var mutation = mutations[completed / 2];
                    var audit = ManualAdded(state, normalized, "audits", 1).Single();
                    FinanceRecoveryFields(audit, new { ActorUserId = admin, AcademyId = academy, Action = $"{mutation.Method} {mutation.Controller}" });
                    using var metadata = JsonDocument.Parse(audit["MetadataJson"]!.GetValue<string>());
                    if (metadata.RootElement.GetProperty("Route").GetString() != mutation.Path)
                        throw new InvalidOperationException("Unexpected finance audit route.");
                }
                ManualSame(state, normalized, "unrelated records and no finance replay");
                state = actual;
                completed++;
                Console.WriteLine($"C9FINANCE SQL PASS phase={completed}");
                await context.Response.WriteAsJsonAsync(new { phase = completed }, context.RequestAborted);
            }
            catch (Exception error)
            {
                failed = true;
                Console.WriteLine("C9FINANCE SQL FAILED " + error.Message);
                context.Response.StatusCode = 500;
            }
            finally { checkLock.Release(); }
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-finance-recovery");
        Console.WriteLine($"C9FINANCE READY run={run} api=54999 origin=54998 stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != 8 || !File.Exists(stop))
            throw new InvalidOperationException($"Incomplete finance recovery verification: {completed}/8.");
        ManualSame(state, await FinanceRecoveryStateAsync(factory), "final finance recovery SQL state");
        Console.WriteLine("C9FINANCE FINAL 8 verified UI/SQL phases.");
    }
}
