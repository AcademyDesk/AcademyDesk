using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
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
    private static async Task VerifyPentaCollectionReadsAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid student)
    {
        const string email = "qa-admin-a@example.invalid";
        const string retiringEmail = "qa-penta-collection-retiring@example.invalid";
        const string collectionsAudit = "PentaFinanceCollectionsRead";
        const string reconciliationAudit = "PentaFinanceReconciliationRead";
        const string fullModules = """["Core","Finance","FinanceControls"]""";
        const string privateReference = "QA-private-bank-reference";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:FinanceReadEnabled"] = "true" };
        var root = $"/api/academies/{academy}";
        var collections = root + "/penta/finance/collections";
        string Reconciliation(Guid id) => root + $"/penta/finance/invoices/{id}/reconciliation";
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var count = 0;

        var foreign = await WithDb(db => db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync());
        var foreignStudent = await WithDb(db => db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync());
        var cent = Invoice("CENT", 120.40m, "USD", -400, adjustment: 20.10m);
        var unpaid = Invoice("INR", 80m, "INR", -399);
        var paid = Invoice("PAID", 30m, "USD", -500, "Paid");
        var cancelled = Invoice("CANCELLED", 45m, "USD", -500, "Cancelled");
        var dueToday = Invoice("TODAY", 65m, "USD", 0);
        var adjusted = Invoice("ADJUSTED", 75m, "USD", -500, adjustment: 75m);
        var settled = Invoice("SETTLED", 90m, "USD", -500);
        var foreignInvoice = new Invoice { AcademyId = foreign, StudentId = foreignStudent, InvoiceNumber = "QA-PENTA-COLL-FOREIGN",
            Currency = "CAD", DueDate = today.AddDays(-600), TotalAmount = 900m };
        var matches = Enumerable.Range(0, 22).Select(i => Invoice($"EXTRA-{i:D2}", 10m, "USD", -398 + i / 2)).ToArray();

        var completed = Payment(cent, 30.10m, "USD", "Completed", now.AddDays(-10));
        var reconciled = Payment(cent, 70.19m, "usd", "Reconciled", now.AddMinutes(-1));
        var otherCurrency = Payment(cent, 9m, "EUR", "Completed", now.AddMinutes(-2));
        var voided = Payment(cent, 20m, "USD", "Voided", now.AddMinutes(-3));
        var pending = Payment(cent, 10m, "USD", "Pending", now.AddMinutes(-4));
        var foreignReceipt = new Payment { AcademyId = foreign, InvoiceId = cent.Id, Currency = "USD", Amount = 3m, PaidAtUtc = now };
        var archived = Enumerable.Range(0, 21).Select(_ => Payment(cent, 1m, "USD", "Voided", now.AddDays(-1))).ToArray();
        await WithDb(db =>
        {
            db.AddRange(cent, unpaid, paid, cancelled, dueToday, adjusted, settled, foreignInvoice,
                completed, reconciled, otherCurrency, voided, pending, foreignReceipt, Payment(settled, 90m, "USD", "Completed", now));
            db.AddRange(matches);
            db.AddRange(archived);
            return db.SaveChangesAsync();
        });
        var retiring = await CreateAccessActorAsync(factory, retiringEmail, "AcademyAdmin", academy);
        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, email);
        using var disabled = await SignIn(factory, email);
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var foreignActor = await SignIn(host, "qa-admin-b@example.invalid");
        using var anonymous = host.CreateClient();
        Guid actor;
        using (var scope = factory.Services.CreateScope())
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id;

        Invoice Invoice(string code, decimal total, string currency, int dueDays, string status = "Issued", decimal adjustment = 0m) =>
            new() { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-PENTA-COLL-" + code, Currency = currency,
                DueDate = today.AddDays(dueDays), TotalAmount = total, AdjustedAmount = adjustment, Status = status };
        Payment Payment(Invoice invoice, decimal amount, string currency, string status, DateTime time) =>
            new() { AcademyId = academy, InvoiceId = invoice.Id, Amount = amount, Currency = currency, Status = status, PaidAtUtc = time,
                ReconciledAtUtc = status == "Reconciled" ? time.AddSeconds(1) : null, Method = "QA-private-method",
                Reference = privateReference, ReconciliationReference = status == "Reconciled" ? privateReference : null };
        static async Task<HttpClient> SignIn(QaApiFactory owner, string identity)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, identity, "Synthetic!39Ab"));
            return client;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<int> Modules(string json) => WithDb(db => db.Academies.Where(x => x.Id == academy)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, json)));
        Task<string> State(bool audit = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            tasks = await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audit = audit ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : null
        }));
        void Pass(string label) { count++; Console.WriteLine($"FINANCE PENTA COLLECTION CASE {label} PASS"); }
        async Task Refuse(HttpClient client, string path, HttpStatusCode status, string label)
        {
            var before = await State();
            using var response = await client.GetAsync(path);
            RequireFinanceStatus(response, status, label);
            var payload = await response.Content.ReadAsStringAsync();
            if (before != await State() || payload.Contains(cent.InvoiceNumber, StringComparison.Ordinal) ||
                payload.Contains(privateReference, StringComparison.Ordinal) || payload.Contains("\"payments\"", StringComparison.Ordinal))
                throw new InvalidOperationException(label + " wrote data or exposed a finance result.");
            Pass(label);
        }
        static async Task<T> Read<T>(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            RequireFinanceStatus(response, HttpStatusCode.OK, "protected collections or reconciliation read");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Finance result must be no-store.");
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        await Refuse(disabled, collections, HttpStatusCode.NotFound, "default-disabled collections");
        await Refuse(anonymous, Reconciliation(cent.Id), HttpStatusCode.Unauthorized, "anonymous reconciliation");
        await Refuse(teacher, collections, HttpStatusCode.Forbidden, "teacher denied");
        await Refuse(foreignActor, collections, HttpStatusCode.Forbidden, "foreign tenant denied");
        await Refuse(admin, Reconciliation(foreignInvoice.Id), HttpStatusCode.NotFound, "foreign invoice denied");
        await Refuse(admin, Reconciliation(Guid.NewGuid()), HttpStatusCode.NotFound, "missing invoice denied");

        var before = await State(audit: false);
        using var manualCollections = await admin.GetAsync(root + "/finance-governance/collections");
        RequireFinanceStatus(manualCollections, HttpStatusCode.OK, "Manual overdue queue");
        var queue = (await manualCollections.Content.ReadFromJsonAsync<CollectionInvoiceSummary[]>())!;
        var protectedQueue = await Read<PentaFinanceCollections>(admin, collections);
        if (protectedQueue.Invoices.Length != 20 || !protectedQueue.HasMore || !protectedQueue.Invoices.SequenceEqual(queue.Take(20)) ||
            protectedQueue.Invoices[0].Id != cent.Id || protectedQueue.Invoices[0].Balance != .01m ||
            protectedQueue.Invoices[0].DaysOverdue != 400 || protectedQueue.Invoices[1].Id != unpaid.Id ||
            queue.Any(x => new[] { paid.Id, cancelled.Id, adjusted.Id, settled.Id, dueToday.Id, foreignInvoice.Id }.Contains(x.Id)))
            throw new InvalidOperationException("Overdue page differs from the Manual queue or lost the one-cent invoice.");
        Pass("bounded overdue queue matches Manual, keeps mixed currencies separate and preserves the cent");

        using var manualPayment = await admin.GetAsync(root + $"/payments?invoiceId={cent.Id:D}");
        RequireFinanceStatus(manualPayment, HttpStatusCode.OK, "Manual payment list");
        var paymentRows = (await manualPayment.Content.ReadFromJsonAsync<PaymentSummary[]>())!;
        using var manualInvoice = await admin.GetAsync(root + "/invoices");
        RequireFinanceStatus(manualInvoice, HttpStatusCode.OK, "Manual invoice list");
        var expectedInvoice = (await manualInvoice.Content.ReadFromJsonAsync<InvoiceSummary[]>())!.Single(x => x.Id == cent.Id);
        using (var response = await admin.GetAsync(Reconciliation(cent.Id)))
        {
            RequireFinanceStatus(response, HttpStatusCode.OK, "protected reconciliation");
            var raw = await response.Content.ReadAsStringAsync();
            if (response.Headers.CacheControl?.NoStore != true || raw.Contains(privateReference, StringComparison.Ordinal) ||
                raw.Contains("QA-private-method", StringComparison.Ordinal) || raw.Contains("\"reconciliationReference\"", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Protected response exposed free-form payment metadata.");
            var result = JsonSerializer.Deserialize<PentaFinanceReconciliation>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            var expectedPayments = paymentRows.Take(20).Select(p => new PentaReconciliationPayment(p.Id, p.Amount, p.Currency,
                p.Status, p.PaidAtUtc, p.ReconciledAtUtc, (p.Status is "Completed" or "Reconciled") &&
                p.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase)));
            if (result.Invoice != expectedInvoice || result.Invoice.Balance != .01m || result.Invoice.PaidAmount != 100.29m ||
                !result.HasMore || result.Payments.Length != 20 || !result.Payments.SequenceEqual(expectedPayments) ||
                result.Payments.Any(x => x.Id == completed.Id || x.Id == foreignReceipt.Id) ||
                result.Payments.Single(x => x.Id == reconciled.Id).CountsTowardInvoiceBalance != true ||
                new[] { otherCurrency.Id, voided.Id, pending.Id }.Any(id => result.Payments.Single(x => x.Id == id).CountsTowardInvoiceBalance))
                throw new InvalidOperationException("Payment preview, exact invoice or eligible payment classification differs.");
        }
        Pass("bounded reconciliation matches Manual, omits private references and retains full invoice totals");

        var empty = await Read<PentaFinanceReconciliation>(admin, Reconciliation(unpaid.Id));
        if (empty.Invoice.Balance != 80m || empty.Payments.Length != 0 || empty.HasMore)
            throw new InvalidOperationException("Invoice with no payments returned phantom records.");
        Pass("unpaid invoice returns an empty reconciliation preview");
        if (before != await State(audit: false)) throw new InvalidOperationException("Reads changed finance or follow-up records.");
        await WithDb(async db =>
        {
            var audit = await db.AuditLogs.Where(x => x.Action == collectionsAudit || x.Action == reconciliationAudit).ToListAsync();
            if (audit.Count != 3 || audit.Any(x => x.AcademyId != academy || x.ActorUserId != actor || x.MetadataJson != null) ||
                !audit.Any(x => x.Action == collectionsAudit && x.EntityType == "Academy" && x.EntityId == academy) ||
                new[] { cent.Id, unpaid.Id }.Any(id => !audit.Any(x => x.Action == reconciliationAudit && x.EntityType == "Invoice" && x.EntityId == id)))
                throw new InvalidOperationException("Read audit attribution differs.");
            return true;
        });
        Pass("reads append only scoped actor audits");

        foreach (var (path, action) in new[] { (collections, collectionsAudit), (Reconciliation(cent.Id), reconciliationAudit) })
        {
            var fault = new EnrollmentSharedAuditFault(action);
            using var failing = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault);
            using var client = await SignIn(failing, email);
            await Refuse(client, path, HttpStatusCode.ServiceUnavailable, action + " audit failure withholds data");
            if (fault.Hits != 1) throw new InvalidOperationException("Expected read audit fault was not reached.");
        }

        await Modules("""["Core","Finance"]""");
        await Refuse(admin, collections, HttpStatusCode.Forbidden, "collections requires FinanceControls");
        var financeOnly = await Read<PentaFinanceReconciliation>(admin, Reconciliation(cent.Id));
        if (financeOnly.Invoice.Balance != .01m) throw new InvalidOperationException("Finance-only reconciliation was unavailable.");
        Pass("reconciliation available with Finance alone");
        await Modules("""["Core","FinanceControls"]""");
        await Refuse(admin, Reconciliation(cent.Id), HttpStatusCode.Forbidden, "reconciliation requires Finance");
        await Modules(fullModules);

        Guid paymentId;
        using (var pay = await admin.PostAsJsonAsync(root + "/payments", new { invoiceId = cent.Id, amount = .01m }))
        {
            RequireFinanceStatus(pay, HttpStatusCode.Created, "Manual cent collection");
            paymentId = (await pay.Content.ReadFromJsonAsync<PaymentSummary>())!.Id;
        }
        using (var reconcile = await admin.PatchAsJsonAsync(root + $"/payments/{paymentId}/reconcile", new { reference = privateReference }))
            RequireFinanceStatus(reconcile, HttpStatusCode.OK, "Manual reconciliation");
        var fresh = await Read<PentaFinanceReconciliation>(admin, Reconciliation(cent.Id));
        var freshQueue = await Read<PentaFinanceCollections>(admin, collections);
        if (fresh.Invoice.Balance != 0m || freshQueue.Invoices.Any(x => x.Id == cent.Id) ||
            fresh.Payments.Single(x => x.Id == paymentId) is not { Status: "Reconciled", CountsTowardInvoiceBalance: true, ReconciledAtUtc: not null })
            throw new InvalidOperationException("Protected reads failed to reflect Manual payment and reconciliation.");
        Pass("fresh results reflect Manual settlement and reconciliation");

        var removeRole = new FinanceReadBeforeLock(async () =>
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(retiring.ToString("D")))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded) throw new InvalidOperationException("Role fixture change failed.");
        });
        using var roleHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: removeRole);
        using var roleClient = await SignIn(roleHost, retiringEmail);
        await Refuse(roleClient, collections, HttpStatusCode.Forbidden, "role revoked after initial authorization");
        if (removeRole.Hits != 1) throw new InvalidOperationException("Post-entry role check was not reached.");

        if (count != 17) throw new InvalidOperationException("Unexpected protected collection check count: " + count);
        Console.WriteLine($"FINANCE PENTA COLLECTION SUMMARY {count} native HTTP/SQL checks PASS.");
    }
}
