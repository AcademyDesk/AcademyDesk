using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.ReadModels;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaFinanceReadsAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy, Guid student)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string ownerEmail = "qa-penta-finance-owner@example.invalid";
        const string financeEmail = "qa-penta-finance-user@example.invalid";
        const string platformEmail = "qa-penta-finance-platform@example.invalid";
        const string studentEmail = "qa-consumer-student@example.invalid";
        const string overviewAudit = "PentaFinanceOverviewRead";
        const string invoiceAudit = "PentaFinanceInvoiceRead";
        const string fullModules = """["Core","Finance","FinanceControls"]""";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:FinanceReadEnabled"] = "true" };
        var root = $"/api/academies/{academy}";
        var overview = root + "/penta/finance/overview";
        string InvoiceRoute(Guid id) => root + $"/penta/finance/invoices/{id}";
        var count = 0;

        var foreign = await WithDb(db => db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync());
        var foreignStudent = await WithDb(db => db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync());
        var empty = new Academy { Name = "Synthetic empty finance read", EnabledModulesJson = fullModules };
        var inr = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-PENTA-INR", TotalAmount = 1000m, AdjustedAmount = 100m };
        var usd = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-PENTA-USD", TotalAmount = 120.40m,
            AdjustedAmount = 5.10m, Currency = "USD" };
        var cancelled = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-PENTA-CANCELLED", TotalAmount = 100m,
            Currency = "USD", Status = "Cancelled" };
        var foreignInvoice = new Invoice { AcademyId = foreign, StudentId = foreignStudent, InvoiceNumber = "QA-PENTA-FOREIGN", TotalAmount = 9999m,
            Currency = "CAD" };
        await WithDb(db =>
        {
            db.AddRange(empty, inr, usd, cancelled, foreignInvoice,
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 150m, Status = "Completed" },
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 250m, Status = "Reconciled" },
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 100m, Status = "Voided" },
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 200m, Status = "Pending" },
                new Payment { AcademyId = academy, InvoiceId = inr.Id, Amount = 9m, Currency = "EUR", Status = "Completed" },
                new Payment { AcademyId = academy, InvoiceId = usd.Id, Amount = 35.20m, Currency = "usd", Status = "Reconciled" },
                new Payment { AcademyId = foreign, InvoiceId = inr.Id, Amount = 25m, Status = "Completed" });
            return db.SaveChangesAsync();
        });
        await Modules(fullModules);
        var ownerId = await CreateAccessActorAsync(factory, ownerEmail, "Owner", empty.Id);
        await CreateAccessActorAsync(factory, financeEmail, "FinanceUser", academy);
        var platformId = await CreateAccessActorAsync(factory, platformEmail, "PlatformOwner", foreign);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var platformUser = (await users.FindByIdAsync(platformId.ToString("D")))!;
            platformUser.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(platformUser)).Succeeded) throw new InvalidOperationException("Platform owner fixture failed.");
        }

        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, adminEmail);
        using var owner = await SignIn(host, ownerEmail);
        using var disabled = await SignIn(factory, adminEmail);
        using var foreignActor = await SignIn(host, "qa-admin-b@example.invalid");
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var learner = await SignIn(host, studentEmail);
        using var finance = await SignIn(host, financeEmail);
        using var platform = await SignIn(host, platformEmail);
        using var anonymous = host.CreateClient();
        Guid actor;
        using (var scope = factory.Services.CreateScope())
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(adminEmail))!.Id;

        static async Task<HttpClient> SignIn(QaApiFactory owner, string email)
        {
            var client = owner.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }
        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> operation)
        {
            using var scope = factory.Services.CreateScope();
            return await operation(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }
        Task<int> Modules(string json) => WithDb(db => db.Academies.Where(x => x.Id == academy)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, json)));

        Task<string> State(bool audits = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            adjustments = await db.FinanceAdjustments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audit = audits ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : null
        }));
        void Pass(string label) { count++; Console.WriteLine($"FINANCE PENTA READ CASE {label} PASS"); }
        async Task Refuse(HttpClient client, string path, HttpStatusCode status, string label)
        {
            var before = await State();
            using var response = await client.GetAsync(path);
            RequireFinanceStatus(response, status, label);
            if (await State() != before) throw new InvalidOperationException(label + " changed the captured SQL state.");
            var payload = await response.Content.ReadAsStringAsync();
            if (payload.Contains(inr.InvoiceNumber, StringComparison.Ordinal) || payload.Contains("\"byCurrency\"", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(label + " exposed finance data.");
            Pass(label);
        }
        static async Task<T> Read<T>(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            RequireFinanceStatus(response, HttpStatusCode.OK, "protected finance read");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Finance result can be cached.");
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        await Refuse(disabled, overview, HttpStatusCode.NotFound, "default disabled");
        await Refuse(anonymous, overview, HttpStatusCode.Unauthorized, "anonymous");
        await Refuse(teacher, overview, HttpStatusCode.Forbidden, "teacher");
        await Refuse(learner, InvoiceRoute(inr.Id), HttpStatusCode.Forbidden, "student");
        using (var manualFinance = await finance.GetAsync(root + "/invoices"))
            RequireFinanceStatus(manualFinance, HttpStatusCode.OK, "delegated Manual finance");
        await Refuse(finance, overview, HttpStatusCode.Forbidden, "delegated finance remains Manual only");
        await Refuse(platform, overview, HttpStatusCode.Forbidden, "platform owner");
        await Refuse(foreignActor, overview, HttpStatusCode.Forbidden, "foreign academy route");
        await Refuse(admin, InvoiceRoute(foreignInvoice.Id), HttpStatusCode.NotFound, "foreign invoice id");
        await Refuse(admin, InvoiceRoute(Guid.NewGuid()), HttpStatusCode.NotFound, "missing invoice");

        var state = await State(audits: false);
        using var manualOverview = await admin.GetAsync(root + "/finance-governance/summary");
        RequireFinanceStatus(manualOverview, HttpStatusCode.OK, "Manual finance overview");
        using var manualJson = JsonDocument.Parse(await manualOverview.Content.ReadAsStringAsync());
        var manualTotals = manualJson.RootElement.GetProperty("byCurrency").Deserialize<FinanceCurrencyTotals[]>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        using (var response = await admin.GetAsync(overview))
        {
            RequireFinanceStatus(response, HttpStatusCode.OK, "PENTA finance overview");
            if (response.Headers.CacheControl?.NoStore != true) throw new InvalidOperationException("Finance overview can be cached.");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var summary = json.RootElement.Deserialize<PentaFinanceOverview>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            if (!summary.HasMixedCurrencies || json.RootElement.TryGetProperty("outstanding", out _) ||
                !summary.ByCurrency.SequenceEqual(manualTotals) || summary.ByCurrency.Any(x => x.Currency == "CAD") ||
                summary.ByCurrency.Single(x => x.Currency == "USD") != new FinanceCurrencyTotals("USD", 220.40m, 5.10m, 35.20m, 80.10m, 35.20m) ||
                summary.ByCurrency.Single(x => x.Currency == "EUR") != new FinanceCurrencyTotals("EUR", 0m, 0m, 9m, 0m, 0m))
                throw new InvalidOperationException("Protected currency totals diverge from Manual or mix currencies.");
        }
        Pass("grouped overview matches Manual without a mixed scalar");
        var emptyOverview = await Read<PentaFinanceOverview>(owner, $"/api/academies/{empty.Id}/penta/finance/overview");
        if (emptyOverview.ByCurrency.Length != 0 || emptyOverview.HasMixedCurrencies)
            throw new InvalidOperationException("Empty owner overview invented a currency or activity.");
        Pass("owner with no invoices receives empty currency totals");

        using var manualInvoices = await admin.GetAsync(root + "/invoices");
        RequireFinanceStatus(manualInvoices, HttpStatusCode.OK, "Manual invoice list");
        var invoices = (await manualInvoices.Content.ReadFromJsonAsync<InvoiceSummary[]>())!;
        foreach (var (id, paid, balance) in new[] { (inr.Id, 400m, 500m), (usd.Id, 35.20m, 80.10m) })
        {
            var result = await Read<PentaFinanceInvoice>(admin, InvoiceRoute(id));
            if (result.Invoice != invoices.Single(x => x.Id == id) || result.Invoice.PaidAmount != paid || result.Invoice.Balance != balance)
                throw new InvalidOperationException("Exact invoice balance differs from Manual or used foreign/void/pending money.");
            Pass("invoice matches Manual and exact currency for " + result.Invoice.Currency);
        }
        if (state != await State(audits: false)) throw new InvalidOperationException("A finance read changed financial records.");
        await WithDb(async db =>
        {
            var audit = await db.AuditLogs.Where(x => x.Action == overviewAudit || x.Action == invoiceAudit).ToListAsync();
            if (audit.Count != 4 || audit.Any(x => x.MetadataJson is not null) ||
                !audit.Any(x => x.AcademyId == academy && x.ActorUserId == actor && x.EntityId == academy && x.Action == overviewAudit) ||
                !audit.Any(x => x.AcademyId == empty.Id && x.ActorUserId == ownerId && x.EntityId == empty.Id && x.Action == overviewAudit) ||
                new[] { inr.Id, usd.Id }.Any(id => !audit.Any(x => x.AcademyId == academy && x.ActorUserId == actor &&
                    x.EntityId == id && x.Action == invoiceAudit)))
                throw new InvalidOperationException("Protected finance audit is missing, wrongly attributed or includes business payload.");
            return true;
        });
        Pass("authorized reads audit once with no ledger or notice writes");

        var fault = new EnrollmentSharedAuditFault(invoiceAudit);
        using (var faultyHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault))
        using (var faulty = await SignIn(faultyHost, adminEmail))
            await Refuse(faulty, InvoiceRoute(inr.Id), HttpStatusCode.ServiceUnavailable, "audit failure withholds invoice data");
        if (fault.Hits != 1) throw new InvalidOperationException("Audit refusal was not exercised.");

        await Modules("""["Core","Finance"]""");
        await Refuse(admin, overview, HttpStatusCode.Forbidden, "overview also requires FinanceControls");
        var onlyInvoice = await Read<PentaFinanceInvoice>(admin, InvoiceRoute(inr.Id));
        if (onlyInvoice.Invoice.Balance != 500m) throw new InvalidOperationException("Invoice-only module scope changed.");
        Pass("invoice remains available with Finance alone");
        await Modules("""["Core","FinanceControls"]""");
        await Refuse(admin, InvoiceRoute(inr.Id), HttpStatusCode.Forbidden, "invoice requires Finance");
        await Refuse(admin, overview, HttpStatusCode.Forbidden, "overview requires Finance");
        await Modules(fullModules);

        using (var collected = await admin.PostAsJsonAsync(root + "/payments", new { invoiceId = inr.Id, amount = 100m }))
            RequireFinanceStatus(collected, HttpStatusCode.Created, "Manual in-currency collection");
        var updated = await Read<PentaFinanceInvoice>(admin, InvoiceRoute(inr.Id));
        if (updated.Invoice.PaidAmount != 500m || updated.Invoice.Balance != 400m)
            throw new InvalidOperationException("Read did not reflect the committed Manual collection.");
        Pass("fresh read reflects a committed Manual collection");

        var revoke = new FinanceReadBeforeLock(async () =>
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = (await users.FindByIdAsync(actor.ToString("D")))!;
            if (!(await users.UpdateSecurityStampAsync(account)).Succeeded) throw new InvalidOperationException("Could not revoke synthetic bearer.");
        });
        using var revokedHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: revoke);
        using var revokedClient = await SignIn(revokedHost, adminEmail);
        await Refuse(revokedClient, InvoiceRoute(inr.Id), HttpStatusCode.Forbidden, "stamp change between initial policy and locked read");
        if (revoke.Hits != 1) throw new InvalidOperationException("Current authority check was not exercised after entry.");

        if (count != 21) throw new InvalidOperationException("Unexpected protected finance read count: " + count);
        Console.WriteLine($"FINANCE PENTA READ SUMMARY {count} native HTTP/SQL checks PASS.");
    }

    private sealed class FinanceReadBeforeLock(Func<Task> revoke) : DbCommandInterceptor
    {
        internal int Hits { get; private set; }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Hits == 0 && command.CommandText.StartsWith("SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK)", StringComparison.Ordinal))
            {
                Hits++;
                await revoke();
            }
            return result;
        }
    }
}
