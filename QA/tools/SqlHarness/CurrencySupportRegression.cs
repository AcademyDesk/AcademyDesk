using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCurrencySupportAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string ownerEmail = "qa-currency-platform@example.invalid";
        const string roleOnlyEmail = "qa-currency-role-only@example.invalid";
        const string password = "Synthetic!39Ab";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid academy, foreign, student, foreignStudent, admin;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            admin = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(adminEmail))!.Id;
        }

        var owner = await CreateAccessActorAsync(factory, ownerEmail, "PlatformOwner", foreign);
        await CreateAccessActorAsync(factory, roleOnlyEmail, "PlatformOwner", academy);
        async Task SetOwnerFlag(bool enabled)
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(owner.ToString()))!;
            user.IsPlatformOwner = enabled;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Platform fixture update failed.");
        }
        await SetOwnerFlag(true);
        var adminToken = await LoginAsync(client, adminEmail, password);
        var ownerToken = await LoginAsync(client, ownerEmail, password);
        var roleOnly = await LoginAsync(client, roleOnlyEmail, password);
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", password);
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", password);
        client.DefaultRequestHeaders.Authorization = null;

        var root = $"/api/academies/{academy}";
        var finance = root + "/finance-governance";
        var intake = root + "/platform-services/support-cases";
        const string overview = "/api/platform/overview";
        const string triage = "/api/platform/support-cases";
        var count = 0;
        void Pass(string label)
        {
            count++;
            Console.WriteLine("CURRENCYSUPPORT CASE " + label + " PASS");
        }
        static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }
        async Task<JsonElement> Read(string label, string? token, string route, HttpStatusCode status = HttpStatusCode.OK)
        {
            var before = await CurrencySupportStateAsync(factory);
            using var response = await ManualSendAsync(client, token, "GET", route);
            RequireFinanceStatus(response, status, label);
            ManualSame(before, await CurrencySupportStateAsync(factory), label);
            return status == HttpStatusCode.OK ? await response.Content.ReadFromJsonAsync<JsonElement>() : default;
        }
        async Task Reject(string label, string? token, string method, string route, JsonObject body, HttpStatusCode status)
        {
            var before = await CurrencySupportStateAsync(factory);
            using var response = await ManualSendAsync(client, token, method, route, body);
            RequireFinanceStatus(response, status, label);
            ManualSame(before, await CurrencySupportStateAsync(factory), label);
            Pass(label);
        }
        static void Money(JsonElement row, params (string Field, decimal Value)[] expected)
        {
            foreach (var (field, value) in expected)
                Check(row.GetProperty(field).GetDecimal() == value, "Monetary field differs: " + field);
        }
        static Dictionary<string, JsonElement> Groups(JsonElement value, params string[] currencies)
        {
            var groups = value.GetProperty("byCurrency").EnumerateArray().ToDictionary(x => x.GetProperty("currency").GetString()!);
            Check(groups.Keys.SequenceEqual(currencies), "Currency groups differ.");
            Check(value.GetProperty("hasMixedCurrencies").GetBoolean() == (currencies.Length > 1), "Mixed flag differs.");
            Check(value.GetProperty("currency").GetString() == (currencies.Length == 1 ? currencies[0] : null), "Currency scalar differs.");
            return groups;
        }
        static void NullMoney(JsonElement row, params string[] fields)
        {
            foreach (var field in fields) Check(row.GetProperty(field).ValueKind == JsonValueKind.Null, "Mixed scalar must be null: " + field);
        }

        var academyEmpty = await Read("academy-empty", adminToken, finance + "/summary");
        Groups(academyEmpty);
        Money(academyEmpty, ("grossBilled", 0), ("outstanding", 0), ("overdueInvoices", 0));
        Pass("academy-empty");
        var platformEmpty = await Read("platform-empty", ownerToken, overview);
        Groups(platformEmpty);
        Money(platformEmpty, ("totalBilled", 0), ("outstandingBilling", 0), ("overdueInvoices", 0));
        Pass("platform-empty");

        Invoice Invoice(string code, string currency, decimal amount, decimal adjusted = 0, string status = "Issued") =>
            new() { AcademyId = academy, StudentId = student, InvoiceNumber = code, TotalAmount = amount, AdjustedAmount = adjusted,
                Currency = currency, Status = status, DueDate = today.AddDays(-3) };
        Payment Receipt(Invoice invoice, decimal amount, string status, string? currency = null) =>
            new() { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = amount, Status = status, Currency = currency ?? invoice.Currency };
        PlatformBillingInvoice Bill(Guid tenant, string number, string currency, decimal amount, string status) =>
            new() { AcademyId = tenant, InvoiceNumber = number, Amount = amount, Currency = currency, Status = status,
                PeriodStart = today.AddMonths(-1), PeriodEnd = today.AddDays(-10), DueDate = today.AddDays(-3) };
        async Task Seed(params object[] rows)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.AddRange(rows);
            await db.SaveChangesAsync();
        }

        var dollars = Invoice("C17-USD", "usd", 100.55m, .05m);
        await Seed(dollars, Receipt(dollars, 10.25m, "Completed", "USD"), Receipt(dollars, 20.10m, "Reconciled"),
            Bill(academy, "C18-GBP-PAID", "GBP", 25.34m, " pAiD "), Bill(academy, "C18-EUR-DRAFT", "EUR", 900, " Draft "),
            Bill(foreign, "C18-USD-VOID", "USD", 800, "vOiD"));
        var academySingle = await Read("academy-single-usd", adminToken, finance + "/summary");
        Groups(academySingle, "USD");
        Money(academySingle, ("grossBilled", 100.55m), ("approvedAdjustments", .05m), ("collected", 30.35m),
            ("outstanding", 70.15m), ("reconciled", 20.10m), ("overdueInvoices", 1));
        Pass("academy-single-usd");
        var platformSingle = await Read("platform-single-gbp", ownerToken, overview);
        Groups(platformSingle, "GBP");
        Money(platformSingle, ("totalBilled", 25.34m), ("collectedBilling", 25.34m), ("outstandingBilling", 0), ("overdueInvoices", 0));
        Pass("platform-single-gbp-and-draft-void-exclusions");

        var overpaid = Invoice("C17-INR-OVERPAID", "INR", 100);
        var unpaid = Invoice("C17-INR-DUE", "inr", 80);
        var cancelled = Invoice("C17-INR-CANCELLED", "INR", 30, status: "cAnCeLlEd");
        var foreignInvoice = new Invoice { AcademyId = foreign, StudentId = foreignStudent, InvoiceNumber = "C17-FOREIGN",
            TotalAmount = 500, Currency = "EUR", DueDate = today.AddDays(-4) };
        await Seed(overpaid, unpaid, cancelled, foreignInvoice, Receipt(overpaid, 150, "Completed"),
            Receipt(unpaid, 80, "Pending"), Receipt(unpaid, 80, "Failed"), Receipt(unpaid, 80, "Voided"),
            Receipt(dollars, 8, "Reconciled", "INR"), Receipt(foreignInvoice, 200, "Completed"),
            new FinanceAdjustment { AcademyId = academy, InvoiceId = unpaid.Id, Amount = 25m, Type = "Discount", Reason = "Synthetic pending", Status = "PendingApproval" },
            Bill(academy, "C18-INR-ISSUED", "INR", 71.12m, " Issued "),
            Bill(foreign, "C18-INR-PAID", "inr", 15.05m, "Paid"),
            Bill(academy, "C18-USD-SUBMITTED", "usd", 41.16m, " Payment submitted "),
            Bill(foreign, "C18-USD-OVERDUE", "USD", 10.23m, "oVeRdUe"));

        var mixed = await Read("academy-mixed", adminToken, finance + "/summary");
        var academyGroups = Groups(mixed, "INR", "USD");
        NullMoney(mixed, "grossBilled", "approvedAdjustments", "collected", "outstanding", "reconciled");
        Money(academyGroups["INR"], ("grossBilled", 210), ("approvedAdjustments", 0), ("collected", 158), ("outstanding", 80), ("reconciled", 8));
        Money(academyGroups["USD"], ("grossBilled", 100.55m), ("approvedAdjustments", .05m), ("collected", 30.35m), ("outstanding", 70.15m), ("reconciled", 20.10m));
        Pass("academy-mixed-exact-decimals-and-isolation");
        var queue = await Read("collection-queue", adminToken, finance + "/collections");
        var due = queue.EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid(), x => x.GetProperty("balance").GetDecimal());
        Check(due.Count == mixed.GetProperty("overdueInvoices").GetInt32() && due.Count == 2 && due[unpaid.Id] == 80m && due[dollars.Id] == 70.15m, "Collections and summary differ.");
        Pass("collection-queue-matches-summary");
        var followUp = $"{finance}/collections/{unpaid.Id}/follow-up";
        var note = new JsonObject { ["note"] = "Synthetic reminder", ["priority"] = "High" };
        await Reject("overpaid-no-follow-up", adminToken, "POST", $"{finance}/collections/{overpaid.Id}/follow-up", note, HttpStatusCode.Conflict);
        await Reject("cancelled-no-follow-up", adminToken, "POST", $"{finance}/collections/{cancelled.Id}/follow-up", note, HttpStatusCode.Conflict);
        await Reject("foreign-invoice-no-follow-up", adminToken, "POST", $"{finance}/collections/{foreignInvoice.Id}/follow-up", note, HttpStatusCode.NotFound);

        var beforeFollow = await CurrencySupportStateAsync(factory);
        using (var response = await ManualSendAsync(client, adminToken, "POST", followUp, note))
            RequireFinanceStatus(response, HttpStatusCode.OK, "follow-up");
        var afterFollow = await CurrencySupportStateAsync(factory);
        var task = ManualAdded(beforeFollow, afterFollow, "workItems", 1).Single();
        FinanceRecoveryFields(task, new { AcademyId = academy, EntityId = unpaid.Id, Priority = "High" });
        var actorAudit = ManualAdded(beforeFollow, afterFollow, "audits", 1).Single();
        FinanceRecoveryFields(actorAudit, new { AcademyId = academy, ActorUserId = admin, Action = "POST FinanceGovernance" });
        ManualSame(beforeFollow, afterFollow, "follow-up unrelated state");
        Pass("follow-up-authoritative-id-and-audit");

        var portfolio = await Read("platform-mixed", ownerToken, overview);
        var platformGroups = Groups(portfolio, "GBP", "INR", "USD");
        NullMoney(portfolio, "totalBilled", "collectedBilling", "outstandingBilling");
        Money(platformGroups["GBP"], ("totalBilled", 25.34m), ("collectedBilling", 25.34m), ("outstandingBilling", 0));
        Money(platformGroups["INR"], ("totalBilled", 86.17m), ("collectedBilling", 15.05m), ("outstandingBilling", 71.12m));
        Money(platformGroups["USD"], ("totalBilled", 51.39m), ("collectedBilling", 0), ("outstandingBilling", 51.39m));
        Money(portfolio, ("overdueInvoices", 1));
        Pass("platform-mixed-exact-sql-grouping");
        foreach (var (label, token, route, status) in new (string, string?, string, HttpStatusCode)[]
        {
            ("academy-foreign-denied", foreignAdmin, finance + "/summary", HttpStatusCode.Forbidden),
            ("academy-teacher-denied", teacher, finance + "/summary", HttpStatusCode.Forbidden),
            ("academy-module-denied", foreignAdmin, $"/api/academies/{foreign}/finance-governance/summary", HttpStatusCode.Forbidden),
            ("platform-role-only-denied", roleOnly, overview, HttpStatusCode.Forbidden),
            ("platform-anonymous-denied", null, overview, HttpStatusCode.Unauthorized)
        })
        {
            await Read(label, token, route, status);
            Pass(label);
        }

        var supportBody = new JsonObject { ["subject"] = "Synthetic Critical", ["priority"] = "Critical", ["description"] = "Synthetic support request" };
        var beforeIntake = await CurrencySupportStateAsync(factory);
        Guid caseId;
        using (var response = await ManualSendAsync(client, adminToken, "POST", intake, supportBody))
        {
            RequireFinanceStatus(response, HttpStatusCode.Created, "support-intake");
            caseId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        var afterIntake = await CurrencySupportStateAsync(factory);
        FinanceRecoveryFields(ManualAdded(beforeIntake, afterIntake, "supportCases", 1).Single(),
            new { Id = caseId, AcademyId = academy, Status = "Open", Priority = "Critical" });
        FinanceRecoveryFields(ManualAdded(beforeIntake, afterIntake, "audits", 1).Single(),
            new { AcademyId = academy, ActorUserId = admin, Action = "POST AcademyPlatformServices" });
        ManualSame(beforeIntake, afterIntake, "support intake unrelated state");
        Pass("support-intake-with-exact-audit");

        var high = new PlatformSupportCase { AcademyId = academy, Subject = "Synthetic high", Priority = "High" };
        var urgent = new PlatformSupportCase { AcademyId = foreign, Subject = "Synthetic urgent", Priority = "Urgent" };
        var normal = new PlatformSupportCase { AcademyId = foreign, Subject = "Synthetic normal", Priority = "Normal" };
        await Seed(high, urgent, normal);
        var foreignCases = await Read("foreign-academy-support-list", foreignAdmin, $"/api/academies/{foreign}/platform-services/support-cases");
        Check(!foreignCases.EnumerateArray().Any(x => x.GetProperty("id").GetGuid() == caseId), "Foreign academy saw protected support case.");
        Pass("support-foreign-academy-isolated");
        await Reject("support-foreign-write-denied", foreignAdmin, "POST", intake, supportBody, HttpStatusCode.Forbidden);
        var triageRoute = $"{triage}/{caseId}";
        await Reject("support-admin-cannot-resolve", adminToken, "PATCH", triageRoute,
            new JsonObject { ["status"] = "Resolved", ["priority"] = "Critical" }, HttpStatusCode.Forbidden);
        var beforeTriage = await Read("platform-owner-support-list", ownerToken, triage);
        var tickets = beforeTriage.EnumerateArray().ToDictionary(x => x.GetProperty("id").GetGuid());
        Check(tickets.Count == 4 && tickets[caseId].GetProperty("priority").GetString() == "Critical" &&
            tickets[high.Id].GetProperty("priority").GetString() == "High" && tickets[urgent.Id].GetProperty("priority").GetString() == "Urgent" &&
            tickets[normal.Id].GetProperty("priority").GetString() == "Normal", "Platform triage payload differs.");
        Pass("support-platform-owner-list-contains-all-priorities");

        foreach (var status in new[] { "Resolved", "Closed" })
        {
            var before = await CurrencySupportStateAsync(factory);
            using var response = await ManualSendAsync(client, ownerToken, "PATCH", triageRoute,
                new JsonObject { ["status"] = status, ["priority"] = "Critical" });
            RequireFinanceStatus(response, HttpStatusCode.OK, "support " + status);
            var after = await CurrencySupportStateAsync(factory);
            var row = ManualRow(after, "supportCases", caseId);
            FinanceRecoveryFields(row, new { Status = status, Priority = "Critical", AcademyId = academy });
            Check(row["ResolvedAtUtc"] is not null && row["UpdatedAtUtc"] is not null, "Support resolution timestamps missing.");
            var old = ManualRow(before, "supportCases", caseId);
            foreach (var field in new[] { "Status", "ResolvedAtUtc", "UpdatedAtUtc" }) row[field] = old[field]?.DeepClone();
            FinanceRecoveryFields(ManualAdded(before, after, "platformAudits", 1).Single(),
                new { ActorUserId = owner, EntityId = caseId, Action = "Support case updated" });
            ManualSame(before, after, "support transition unrelated state");
            var fresh = await Read("support fresh " + status, ownerToken, triage);
            Check(fresh.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == caseId).GetProperty("status").GetString() == status, "Support status did not persist.");
            Pass("support-" + status.ToLowerInvariant() + "-audit-and-fresh-read");
        }

        await SetOwnerFlag(false);
        await Read("platform-revoked-owner", ownerToken, overview, HttpStatusCode.Forbidden);
        Pass("platform-revoked-owner");
        await SetOwnerFlag(true);

        async Task<bool> AuditFault(string label, string auth, string method, string route, JsonObject body, CurrencySupportAuditFault fault)
        {
            var before = await CurrencySupportStateAsync(factory);
            using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
            using var failingClient = failing.CreateClient(new() { AllowAutoRedirect = false });
            using var response = await ManualSendAsync(failingClient, auth, method, route, body);
            var unchanged = JsonNode.DeepEquals(before, await CurrencySupportStateAsync(factory));
            Console.WriteLine($"CURRENCYSUPPORT FAULT {label} status={(int)response.StatusCode} hits={fault.Hits} stateUnchanged={unchanged}");
            return response.StatusCode == HttpStatusCode.InternalServerError && fault.Hits == 1 && unchanged;
        }
        Check(await AuditFault("collection", adminToken, "POST", followUp, note, new("POST FinanceGovernance")), "Collection audit fault did not roll back.");
        Pass("collection-audit-failure-rolls-back");
        Check(await AuditFault("owner-triage", ownerToken, "PATCH", triageRoute,
            new JsonObject { ["status"] = "Open", ["priority"] = "Critical" }, new("Support case updated", platform: true)), "Owner triage audit fault did not roll back.");
        Pass("owner-triage-audit-failure-rolls-back");
        Check(await AuditFault("academy-intake", adminToken, "POST", intake, supportBody, new("POST AcademyPlatformServices")), "Support intake audit fault did not roll back.");
        Pass("academy-intake-audit-failure-rolls-back");
        Check(await AuditFault("owner-academy-intake", ownerToken, "POST", intake, supportBody, new("POST AcademyPlatformServices")), "Platform owner academy intake audit fault did not roll back.");
        Pass("owner-academy-intake-audit-failure-rolls-back");
        Console.WriteLine($"CURRENCYSUPPORT SUMMARY {count} native HTTP/SQL checks PASS");
    }

    private static async Task<JsonObject> CurrencySupportStateAsync(QaApiFactory factory)
    {
        var state = await FinanceRecoveryStateAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        state["workItems"] = JsonSerializer.SerializeToNode(await db.AdminWorkItems.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        state["supportCases"] = JsonSerializer.SerializeToNode(await db.PlatformSupportCases.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        state["platformInvoices"] = JsonSerializer.SerializeToNode(await db.PlatformBillingInvoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        state["platformAudits"] = JsonSerializer.SerializeToNode(await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        return state;
    }

    private sealed class CurrencySupportAuditFault(string action, bool platform = false) : SaveChangesInterceptor
    {
        public int Hits { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && (platform
                ? db.ChangeTracker.Entries<PlatformAuditEntry>().Any(entry => entry.State == EntityState.Added && entry.Entity.Action == action)
                : db.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State == EntityState.Added && entry.Entity.Action == action)))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required currency/support audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
