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
    private static async Task ServeReportingBrowserAsync(QaRunManifest manifest)
    {
        const string api = "http://127.0.0.1:54999";
        const string origin = "http://127.0.0.1:54998";
        const string oldEvidence = "Synthetic original bank evidence";
        const string newEvidence = "Synthetic restored bank evidence";
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned reporting browser preflight failed.");

        Guid academy, foreign, admin;
        var inr = new Invoice { InvoiceNumber = "P0-TRANSITION", TotalAmount = 1000m, AdjustedAmount = 200m, Status = "Paid" };
        var usd = new Invoice { InvoiceNumber = "=C20-USD", TotalAmount = 100.55m, AdjustedAmount = .05m, Currency = "usd", Status = "PartiallyPaid" };
        var cancelled = new Invoice { InvoiceNumber = "C20-CANCELLED", TotalAmount = 20m, Currency = "EUR", Status = "cAnCeLlEd" };
        var target = new Payment { InvoiceId = inr.Id, Amount = 200m, Status = "Reconciled", ReconciliationReference = oldEvidence,
            ReconciledAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) };
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            admin = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            foreach (var invoice in new[] { inr, usd, cancelled })
            {
                invoice.AcademyId = academy;
                invoice.StudentId = student;
                invoice.DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));
            }
            target.AcademyId = academy;
            var payroll = new PayrollProfile { AcademyId = academy, WorkerType = "Teacher", WorkerName = "Synthetic reporting worker", MonthlyAmount = 90m };
            db.AddRange(inr, usd, cancelled, target,
                new Invoice { AcademyId = foreign, StudentId = foreignStudent, InvoiceNumber = "C20-FOREIGN", TotalAmount = 9999m },
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 600m, Status = "Completed" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 30.35m, Currency = "USD", Status = "Reconciled" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 70.15m, Currency = "INR", Status = "Completed" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 100m, Currency = "USD", Status = "Pending" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 100m, Currency = "USD", Status = "Failed" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 100m, Currency = "USD", Status = "Voided" },
                new Expense { AcademyId = academy, Description = "Synthetic INR expense", Amount = 20.05m },
                new Expense { AcademyId = academy, Description = "Synthetic USD expense", Amount = 5.25m, Currency = "USD" },
                payroll, new PayrollPayout { AcademyId = academy, PayrollProfileId = payroll.Id, PayslipNumber = "QA-REPORTS-PENDING",
                    PeriodLabel = "Synthetic period", GrossAmount = 90m, NetAmount = 90m, Status = "Approved", Currency = "EUR",
                    WorkerNameSnapshot = payroll.WorkerName, WorkerTypeSnapshot = payroll.WorkerType, IdentityRecordedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        var basePath = $"/api/academies/{academy}";
        var invoicesPath = basePath + "/invoices";
        var paymentsPath = basePath + "/payments";
        var statusPath = $"{paymentsPath}/{target.Id}/status";
        var reconcilePath = $"{paymentsPath}/{target.Id}/reconcile";
        async Task<JsonObject> Snapshot()
        {
            var snapshot = await FinanceRecoveryStateAsync(factory);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            snapshot["expenses"] = JsonSerializer.SerializeToNode(await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            return snapshot;
        }
        var deniedForeign = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var deniedTeacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var state = await Snapshot();
        foreach (var (label, bearer, route, status) in new[]
        {
            ("foreign", deniedForeign, invoicesPath, HttpStatusCode.Forbidden),
            ("teacher", deniedTeacher, invoicesPath, HttpStatusCode.Forbidden),
            ("module", deniedForeign, $"/api/academies/{foreign}/invoices", HttpStatusCode.Forbidden)
        })
        {
            using var response = await ManualSendAsync(client, bearer, "GET", route);
            RequireFinanceStatus(response, status, label);
            ManualSame(state, await Snapshot(), "invoice read denial " + label);
            Console.WriteLine("REPORTING AUTH PASS " + label);
        }

        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "reporting-fixture.json"), JsonSerializer.Serialize(new
        {
            run, origin, api, academy, invoice = inr.Id, usdInvoice = usd.Id, usdNumber = usd.InvoiceNumber, payment = target.Id,
            invoiceNumber = inr.InvoiceNumber, paymentReference = newEvidence
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

            if (context.Request.Path == "/__qa/reporting")
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
                    if (input?["run"]?.GetValue<string>() != run || failed || completed >= 5 || input["phase"]?.GetValue<int>() != completed + 1)
                        throw new InvalidOperationException("Unexpected reporting browser phase.");
                    var actual = await Snapshot();
                    if (completed == 1)
                    {
                        var normalized = actual.DeepClone().AsObject();
                        var invoice = ManualRow(normalized, "invoices", usd.Id);
                        FinanceRecoveryFields(invoice, new { Status = "Paid", AdjustedAmount = .05m });
                        invoice["Status"] = ManualRow(state, "invoices", usd.Id)["Status"]!.DeepClone();
                        var payment = ManualAdded(state, normalized, "payments", 1).Single();
                        if (!Guid.TryParse(input["paymentId"]?.GetValue<string>(), out var paymentId))
                            throw new InvalidOperationException("Confirmed browser payment ID missing.");
                        FinanceRecoveryFields(payment, new { Id = paymentId, AcademyId = academy, InvoiceId = usd.Id,
                            Amount = 70.15m, Currency = "usd", Status = "Completed", Method = "UPI" });
                        var audit = ManualAdded(state, normalized, "audits", 1).Single();
                        FinanceRecoveryFields(audit, new { ActorUserId = admin, AcademyId = academy, Action = "POST Payments" });
                        if (!audit["MetadataJson"]!.GetValue<string>().Contains(paymentsPath, StringComparison.Ordinal))
                            throw new InvalidOperationException("Collection audit route differs.");
                        ManualSame(state, normalized, "legacy receipt and unrelated collection state");
                    }
                    else if (completed is 2 or 4)
                    {
                        var voiding = completed == 2;
                        var normalized = actual.DeepClone().AsObject();
                        var invoice = ManualRow(normalized, "invoices", inr.Id);
                        var payment = ManualRow(normalized, "payments", target.Id);
                        FinanceRecoveryFields(invoice, new { Status = voiding ? "PartiallyPaid" : "Paid", AdjustedAmount = 200m });
                        FinanceRecoveryFields(payment, new { Amount = 200m, Status = voiding ? "Voided" : "Reconciled",
                            ReconciliationReference = voiding ? oldEvidence : newEvidence });
                        var oldPayment = ManualRow(state, "payments", target.Id);
                        var oldTimestamp = oldPayment["ReconciledAtUtc"]!.GetValue<DateTime>();
                        var timestamp = payment["ReconciledAtUtc"]!.GetValue<DateTime>();
                        if (voiding ? timestamp != oldTimestamp : timestamp <= oldTimestamp)
                            throw new InvalidOperationException("Reconciliation timestamp does not match the transition.");
                        invoice["Status"] = ManualRow(state, "invoices", inr.Id)["Status"]!.DeepClone();
                        foreach (var field in new[] { "Status", "ReconciliationReference", "ReconciledAtUtc" })
                            payment[field] = oldPayment[field]!.DeepClone();
                        var audit = ManualAdded(state, normalized, "audits", 1).Single();
                        FinanceRecoveryFields(audit, new { ActorUserId = admin, AcademyId = academy, Action = "PATCH Payments" });
                        if (!audit["MetadataJson"]!.GetValue<string>().Contains(voiding ? statusPath : reconcilePath, StringComparison.Ordinal))
                            throw new InvalidOperationException("Payment transition audit route differs.");
                        ManualSame(state, normalized, "payment transition unrelated state");
                    }
                    else
                    {
                        ManualSame(state, actual, "reporting reads or rejected payment action");
                    }
                    state = actual;
                    completed++;
                    Console.WriteLine($"REPORTING SQL PASS phase={completed}");
                    await context.Response.WriteAsJsonAsync(new { phase = completed }, context.RequestAborted);
                }
                catch (Exception error)
                {
                    failed = true;
                    Console.WriteLine("REPORTING SQL FAILED " + error.Message);
                    context.Response.StatusCode = 500;
                }
                finally { checkLock.Release(); }
                return;
            }

            var method = context.Request.Method;
            var route = context.Request.Path;
            var permittedWrite = method == "POST" && (route == paymentsPath || route == "/api/auth/login" || route == "/api/auth/refresh") ||
                method == "PATCH" && (route == statusPath || route == reconcilePath);
            if (!route.StartsWithSegments("/api") || !(method is "GET" or "OPTIONS" || permittedWrite))
            {
                context.Response.StatusCode = 405;
                return;
            }
            await ForwardFinanceBrowserRequestAsync(context, client);
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-reporting-browser");
        Console.WriteLine($"REPORTING READY run={run} api=54999 origin=54998 stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != 5 || !File.Exists(stop))
            throw new InvalidOperationException($"Incomplete reporting browser verification: {completed}/5.");
        ManualSame(state, await Snapshot(), "final reporting browser SQL state");
        Console.WriteLine("REPORTING FINAL 5 verified UI/SQL phases.");
    }
}
