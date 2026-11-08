using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyLegacyCurrencyWritesAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        Guid academy, student, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }

        var root = $"/api/academies/{academy}";
        var payments = root + "/payments";
        var invoices = root + "/invoices";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var total = 0;
        var failed = 0;
        static void Assert(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        async Task<Invoice> Seed(string label, decimal billed, decimal adjusted, string status,
            decimal matching, decimal legacy)
        {
            var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "LCW-" + label,
                TotalAmount = billed, AdjustedAmount = adjusted, Currency = "usd", Status = status, DueDate = today.AddDays(-1) };
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(invoice);
            db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = legacy, Currency = "INR", Status = "Completed" });
            if (matching > 0)
                db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = matching, Currency = "USD", Status = "Reconciled" });
            await db.SaveChangesAsync();
            return invoice;
        }
        async Task SaveFixture(object entity)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.Add(entity);
            await db.SaveChangesAsync();
        }
        static void Normalize(JsonObject before, JsonObject after, string table, Guid id, params string[] fields)
        {
            var old = ManualRow(before, table, id);
            var changed = ManualRow(after, table, id);
            foreach (var field in fields)
                changed[field] = old[field]?.DeepClone();
        }
        static JsonObject Body(string key, string value) => new() { [key] = value };
        static JsonObject Collect(Guid invoiceId, decimal amount) => new() { ["invoiceId"] = invoiceId, ["amount"] = amount, ["method"] = "UPI" };

        async Task Case(string label, string method, string path, JsonObject body, HttpStatusCode expected,
            Invoice invoice, string? nextStatus = null, decimal? nextBalance = null, string? audit = null,
            Func<JsonObject, JsonObject, HttpResponseMessage, Task>? assertDelta = null, HttpClient? targetClient = null)
        {
            total++;
            var before = await CurrencySupportStateAsync(factory);
            using var response = await ManualSendAsync(targetClient ?? client, token, method, path, body);
            var actual = (int)response.StatusCode;
            try
            {
                Assert(response.StatusCode == expected, "HTTP result differs.");
                var after = await CurrencySupportStateAsync(factory);
                if (nextStatus is not null)
                {
                    FinanceRecoveryFields(ManualRow(after, "invoices", invoice.Id), new { Status = nextStatus });
                    Normalize(before, after, "invoices", invoice.Id, "Status");
                    var rawBeforeRead = await CurrencySupportStateAsync(factory);
                    using var read = await ManualSendAsync(client, token, "GET", invoices);
                    RequireFinanceStatus(read, HttpStatusCode.OK, label + " fresh invoices");
                    var all = await read.Content.ReadFromJsonAsync<JsonElement>();
                    var row = all.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == invoice.Id);
                    Assert(row.GetProperty("status").GetString() == nextStatus && row.GetProperty("balance").GetDecimal() == nextBalance,
                        "Fresh invoice balance or status differs.");
                    ManualSame(rawBeforeRead, await CurrencySupportStateAsync(factory), label + " read-side state");
                }
                if (assertDelta is not null) await assertDelta(before, after, response);
                if (audit is not null)
                {
                    var entry = ManualAdded(before, after, "audits", 1).Single();
                    FinanceRecoveryFields(entry, new { AcademyId = academy, ActorUserId = actor, Action = audit });
                    Assert(entry["MetadataJson"]!.GetValue<string>().Contains(path, StringComparison.Ordinal), "Audit route differs.");
                }
                ManualSame(before, after, label);
                Console.WriteLine($"LEGACYCURRENCY CASE {label} PASS status={actual}");
            }
            catch (InvalidOperationException error)
            {
                failed++;
                Console.WriteLine($"LEGACYCURRENCY CASE {label} FAIL status={actual} reason={error.Message}");
            }
        }

        var collection = await Seed("COLLECT", 100.55m, .05m, "PartiallyPaid", 30.35m, 70.15m);
        await Case("collection-over-by-cent", "POST", payments, Collect(collection.Id, 70.16m), HttpStatusCode.BadRequest, collection);
        await Case("collection-exact-read-balance", "POST", payments, Collect(collection.Id, 70.15m), HttpStatusCode.Created,
            collection, "Paid", 0m, "POST Payments", async (before, after, response) =>
            {
                var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
                FinanceRecoveryFields(ManualAdded(before, after, "payments", 1).Single(),
                    new { Id = saved.GetProperty("id").GetGuid(), AcademyId = academy, InvoiceId = collection.Id,
                        Amount = 70.15m, Currency = "usd", Status = "Completed", Method = "UPI" });
            });

        var invoiceStatus = await Seed("STATUS", 100m, 0m, "Issued", 0m, 100m);
        await Case("invoice-status-refuses-false-paid", "PATCH", $"{invoices}/{invoiceStatus.Id}/status", Body("status", "Paid"),
            HttpStatusCode.BadRequest, invoiceStatus);
        await Case("invoice-status-accepts-overdue", "PATCH", $"{invoices}/{invoiceStatus.Id}/status", Body("status", "Overdue"),
            HttpStatusCode.OK, invoiceStatus, "Overdue", 100m, "PATCH Invoices");

        async Task<Payment> Target(Invoice invoice, string status, string currency = "USD")
        {
            var payment = new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 200m, Currency = currency, Status = status,
                ReconciliationReference = "Original synthetic evidence", ReconciledAtUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc) };
            await SaveFixture(payment);
            return payment;
        }
        Func<JsonObject, JsonObject, HttpResponseMessage, Task> PaymentDelta(Payment payment, string status, string evidence, bool changedEvidence) =>
            (before, after, _) =>
            {
                var current = ManualRow(after, "payments", payment.Id);
                FinanceRecoveryFields(current, new { Status = status, ReconciliationReference = evidence });
                if (changedEvidence)
                    Assert(current["ReconciledAtUtc"]!.GetValue<DateTime>() > payment.ReconciledAtUtc, "New bank timestamp missing.");
                Normalize(before, after, "payments", payment.Id,
                    changedEvidence ? new[] { "Status", "ReconciliationReference", "ReconciledAtUtc" } : new[] { "Status" });
                return Task.CompletedTask;
            };

        var voidInvoice = await Seed("VOID", 1000m, 200m, "Paid", 600m, 200m);
        var toVoid = await Target(voidInvoice, "Reconciled");
        await Case("void-reopens-same-currency-balance", "PATCH", $"{payments}/{toVoid.Id}/status", Body("status", "Voided"),
            HttpStatusCode.OK, voidInvoice, "PartiallyPaid", 200m, "PATCH Payments", PaymentDelta(toVoid, "Voided", "Original synthetic evidence", false));

        var restore = await Seed("RESTORE", 1000m, 200m, "PartiallyPaid", 600m, 200m);
        var toRestore = await Target(restore, "Voided");
        await Case("restore-completed-at-adjusted-balance", "PATCH", $"{payments}/{toRestore.Id}/status", Body("status", "Completed"),
            HttpStatusCode.OK, restore, "Paid", 0m, "PATCH Payments", PaymentDelta(toRestore, "Completed", "Original synthetic evidence", false));
        var restoreReconcile = await Seed("RECONCILE", 1000m, 200m, "PartiallyPaid", 600m, 200m);
        var toReconcile = await Target(restoreReconcile, "Voided");
        await Case("restore-with-new-bank-evidence", "PATCH", $"{payments}/{toReconcile.Id}/reconcile", Body("reference", "New synthetic evidence"),
            HttpStatusCode.OK, restoreReconcile, "Paid", 0m, "PATCH Payments", PaymentDelta(toReconcile, "Reconciled", "New synthetic evidence", true));

        var wrongRestore = await Seed("WRONG-TARGET", 1000m, 200m, "PartiallyPaid", 600m, 100m);
        var wrong = await Target(wrongRestore, "Voided", "INR");
        await Case("restore-refuses-wrong-target-currency", "PATCH", $"{payments}/{wrong.Id}/status", Body("status", "Completed"),
            HttpStatusCode.BadRequest, wrongRestore);

        var adjustInvoice = await Seed("ADJUST", 100.55m, .05m, "PartiallyPaid", 40.10m, 80m);
        FinanceAdjustment Proposal(decimal amount) => new() { AcademyId = academy, InvoiceId = adjustInvoice.Id,
            Type = "Discount", Amount = amount, Currency = "USD", Reason = "Synthetic legacy currency adjustment" };
        var excess = Proposal(60.41m);
        var exact = Proposal(60.40m);
        await SaveFixture(excess);
        await SaveFixture(exact);
        var approval = new JsonObject { ["approve"] = true, ["notes"] = "Synthetic currency review" };
        await Case("approval-over-by-cent", "PATCH", $"{root}/finance-adjustments/{excess.Id}/approval", approval,
            HttpStatusCode.BadRequest, adjustInvoice);
        await Case("approval-exact-read-balance", "PATCH", $"{root}/finance-adjustments/{exact.Id}/approval", approval,
            HttpStatusCode.OK, adjustInvoice, "Paid", 0m, "PATCH FinanceAdjustments", (before, after, _) =>
            {
                FinanceRecoveryFields(ManualRow(after, "invoices", adjustInvoice.Id), new { AdjustedAmount = 60.45m });
                Normalize(before, after, "invoices", adjustInvoice.Id, "AdjustedAmount");
                var saved = ManualRow(after, "adjustments", exact.Id);
                FinanceRecoveryFields(saved, new { Status = "Approved", ApprovalNotes = "Synthetic currency review" });
                Assert(saved["AppliedAtUtc"] is not null && saved["ApprovedAtUtc"] is not null, "Approval timestamps missing.");
                Normalize(before, after, "adjustments", exact.Id, "Status", "ApprovalNotes", "AppliedAtUtc", "ApprovedAtUtc");
                return Task.CompletedTask;
            });

        var auditInvoice = await Seed("AUDIT", 100m, 0m, "Issued", 0m, 100m);
        var fault = new LegacyCurrencyAuditFault();
        using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
        using var failingClient = failing.CreateClient(new() { AllowAutoRedirect = false });
        await Case("collection-required-audit-failure-rolls-back", "POST", payments, Collect(auditInvoice.Id, 100m),
            HttpStatusCode.InternalServerError, auditInvoice, assertDelta: (_, _, _) =>
            {
                Assert(fault.Hits == 1, "Required collection audit was not reached once.");
                return Task.CompletedTask;
            }, targetClient: failingClient);

        Console.WriteLine($"LEGACYCURRENCY SUMMARY {total - failed}/{total} native HTTP/SQL checks passed");
        if (failed != 0) throw new InvalidOperationException("Legacy currency write acceptance failed.");
    }

    private sealed class LegacyCurrencyAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(entry =>
                entry.State == EntityState.Added && entry.Entity.Action == "POST Payments"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required legacy currency audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
