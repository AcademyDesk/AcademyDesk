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
    private static async Task VerifyPentaExpensePayrollReadsAsync(QaApiFactory factory, QaRunManifest manifest, Guid academy)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string ownerEmail = "qa-penta-payroll-owner@example.invalid";
        const string retiringEmail = "qa-penta-payroll-retiring@example.invalid";
        const string expenseAudit = "PentaFinanceExpensesRead";
        const string payrollAudit = "PentaFinancePayrollRead";
        const string withheld = "QA-private-preview";
        const string financeOnly = """["Core","Finance"]""";
        var flags = new Dictionary<string, string?> { ["Penta:Enabled"] = "true", ["Penta:FinanceReadEnabled"] = "true" };
        var root = $"/api/academies/{academy}";
        var expenses = root + "/penta/finance/expenses";
        var payroll = root + "/penta/finance/payroll";
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var count = 0;

        var owner = await CreateAccessActorAsync(factory, ownerEmail, "Owner", academy);
        var retiring = await CreateAccessActorAsync(factory, retiringEmail, "AcademyAdmin", academy);
        var foreign = await WithDb(db => db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync());
        var baselineModules = await WithDb(db => db.Academies.Where(x => x.Id == academy || x.Id == foreign)
            .ToDictionaryAsync(x => x.Id, x => x.EnabledModulesJson));
        await Modules(financeOnly, foreign);

        using var host = new QaApiFactory(manifest, flags);
        using var admin = await SignIn(host, adminEmail);
        using var ownerClient = await SignIn(host, ownerEmail);
        using var disabled = await SignIn(factory, adminEmail);
        using var teacher = await SignIn(host, "qa-teacher-a@example.invalid");
        using var student = await SignIn(host, "qa-consumer-student@example.invalid");
        using var finance = await SignIn(host, "qa-penta-finance-user@example.invalid");
        using var foreignAdmin = await SignIn(host, "qa-admin-b@example.invalid");
        using var anonymous = host.CreateClient();

        var emptyExpenses = await Read<PentaFinanceExpenses>(foreignAdmin, $"/api/academies/{foreign}/penta/finance/expenses");
        var emptyPayroll = await Read<PentaFinancePayroll>(foreignAdmin, $"/api/academies/{foreign}/penta/finance/payroll");
        if (emptyExpenses.Expenses.Length != 0 || emptyExpenses.HasMore || emptyPayroll.Payouts.Length != 0 || emptyPayroll.HasMore)
            throw new InvalidOperationException("Empty expense and payroll registers must not imply more records.");
        Pass("empty owned registers");

        var profile = new PayrollProfile { AcademyId = academy, WorkerType = "Staff", WorkerName = withheld + "-current" };
        var foreignProfile = new PayrollProfile { AcademyId = foreign, WorkerType = "Staff", WorkerName = withheld + "-foreign" };
        var paid = Payout(profile, "PAID", now, "USD", 120.40m, 120.39m);
        var large = Payout(profile, "LARGE", now.AddMinutes(-1), "INR", 9999999999999999.99m, 0m);
        var legacy = Payout(profile, "LEGACY", now.AddMinutes(-2), "INR", 10m, 10m);
        legacy.WorkerTypeSnapshot = null; // Partial snapshot must still be marked unverified.
        var noCashDue = Payout(foreignProfile, "NO-CASH", null, "EUR", 5m, 5m);
        noCashDue.Status = "NoCashDue";
        var otherPayout = Payout(foreignProfile, "FOREIGN", now.AddHours(1), "EUR", 100m, 0m);
        var branch = new Branch { AcademyId = academy, Name = "Synthetic expense branch" };
        var cent = Expense(academy, today, "USD", .01m);
        cent.BranchId = branch.Id;
        var otherExpense = Expense(foreign, today.AddDays(1), "EUR", 100m);

        await WithDb(db =>
        {
            db.AddRange(profile, foreignProfile, branch, cent, otherExpense, paid, large, legacy, otherPayout, noCashDue);
            db.AddRange(Enumerable.Range(0, 21).Select(i => Expense(academy, today.AddDays(-1 - i / 2), "INR", 10m)));
            db.AddRange(Enumerable.Range(0, 21).Select(i => Payout(profile, $"EXTRA-{i:D2}", now.AddDays(-1 - i / 2), "INR", 20m, 5m)));
            return db.SaveChangesAsync();
        });

        await Refuse(disabled, expenses, HttpStatusCode.NotFound, "default-disabled expenses");
        await Refuse(disabled, payroll, HttpStatusCode.NotFound, "default-disabled payroll");
        await Refuse(anonymous, payroll, HttpStatusCode.Unauthorized, "anonymous denied");
        await Refuse(teacher, payroll, HttpStatusCode.Forbidden, "teacher denied");
        await Refuse(student, expenses, HttpStatusCode.Forbidden, "student denied");
        await Refuse(finance, expenses, HttpStatusCode.Forbidden, "delegated finance denied");
        await Refuse(foreignAdmin, payroll, HttpStatusCode.Forbidden, "foreign academy denied");

        await Modules("""["Core"]""");
        await Refuse(admin, expenses, HttpStatusCode.Forbidden, "expense Finance module required");
        await Refuse(admin, payroll, HttpStatusCode.Forbidden, "payroll Finance module required");
        await Modules(financeOnly);

        var before = await State(includeAudit: false);
        var manualExpenses = await Read<ExpenseSummary[]>(admin, root + "/expenses", protect: false);
        var manualPayroll = await Read<PayrollPayoutSummary[]>(admin, root + "/payroll/payouts", protect: false);
        var expensePreview = await Read<PentaFinanceExpenses>(admin, expenses);
        var payrollPreview = await Read<PentaFinancePayroll>(ownerClient, payroll);

        if (!expensePreview.HasMore || expensePreview.Expenses.Length != 20 ||
            !expensePreview.Expenses.SequenceEqual(manualExpenses.Take(20).Select(x => new PentaExpense(x.Id, x.Amount, x.Currency, x.BranchId, x.ExpenseDate, x.Status))) ||
            expensePreview.Expenses[0] is not { AmountExact: "0.01", Currency: "USD" } ||
            expensePreview.Expenses[0].Id != cent.Id || expensePreview.Expenses.Any(x => x.Id == otherExpense.Id) ||
            !expensePreview.Expenses.Any(x => x.Currency == "INR") || manualExpenses[0].Description != withheld)
            throw new InvalidOperationException("Protected expense preview must match Manual order and amounts with bounded tenant data.");
        Pass("expense cap, branch, currencies, exact cent and Manual parity");

        if (!payrollPreview.HasMore || payrollPreview.Payouts.Length != 20 ||
            !payrollPreview.Payouts.SequenceEqual(manualPayroll.Take(20).Select(x => new PentaPayrollPayout(x.Id, x.PayslipNumber, x.SessionsCovered,
                x.GrossAmount, x.Deductions, x.NetAmount, x.Currency, x.Status, x.PaidAtUtc, x.IdentitySource))) ||
            payrollPreview.Payouts[0] is not { NetAmountExact: "0.01", Currency: "USD", IdentitySource: "PayoutSnapshot" } ||
            payrollPreview.Payouts[1] is not { NetAmountExact: "9999999999999999.99", Currency: "INR" } ||
            payrollPreview.Payouts[2] is not { NetAmount: 0m, IdentitySource: "LegacyUnverified" } ||
            payrollPreview.Payouts.Any(x => x.Id == otherPayout.Id) || manualPayroll[0].WorkerName != withheld + "-snapshot")
            throw new InvalidOperationException("Protected payroll preview must match Manual stored payouts and preserve exact cents and provenance.");
        Pass("payroll cap, mixed currencies, exact large amount, legacy provenance and Manual parity");

        var historical = await Read<PentaFinancePayroll>(foreignAdmin, $"/api/academies/{foreign}/penta/finance/payroll");
        if (historical.HasMore || historical.Payouts.Length != 2 ||
            historical.Payouts.Single(x => x.Id == noCashDue.Id) is not { Status: "NoCashDue", PaidAtUtc: null, NetAmount: 0m })
            throw new InvalidOperationException("Protected payroll must preserve historical no-cash statements.");
        Pass("historical no-cash statement and nullable paid time");

        var audits = await WithDb(db => db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy &&
            (x.Action == expenseAudit || x.Action == payrollAudit)).ToArrayAsync());
        if (before != await State(includeAudit: false) || audits.Length != 2 ||
            !audits.Any(x => x.Action == payrollAudit && x.ActorUserId == owner) ||
            !audits.Any(x => x.Action == expenseAudit && x.ActorUserId != owner && x.ActorUserId != null) ||
            audits.Any(x => x.EntityId != academy || x.MetadataJson?.Contains(withheld, StringComparison.Ordinal) == true))
            throw new InvalidOperationException("Protected reads must write only actor-bound audits.");
        Pass("committed audit and unchanged ledgers");

        foreach (var (action, path) in new[] { (expenseAudit, expenses), (payrollAudit, payroll) })
        {
            var fault = new EnrollmentSharedAuditFault(action);
            using var faulty = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: fault);
            using var faultClient = await SignIn(faulty, adminEmail);
            await Refuse(faultClient, path, HttpStatusCode.ServiceUnavailable, action + " fails closed on audit error");
        }

        var revoke = new FinanceReadBeforeLock(async () =>
        {
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(retiring.ToString("D")))!;
            if (!(await users.RemoveFromRoleAsync(user, "AcademyAdmin")).Succeeded)
                throw new InvalidOperationException("Payroll role revocation fixture failed.");
        });
        using var revokedHost = new QaApiFactory(manifest, flags, isolatedDomainInterceptor: revoke);
        using var revokedClient = await SignIn(revokedHost, retiringEmail);
        await Refuse(revokedClient, payroll, HttpStatusCode.Forbidden, "payroll role revoked between entry and lock");
        if (revoke.Hits != 1) throw new InvalidOperationException("Payroll role recheck was not exercised.");
        await Modules(baselineModules[academy]);
        await Modules(baselineModules[foreign], foreign);

        if (count != 17) throw new InvalidOperationException("Unexpected protected expense and payroll check count: " + count);
        Console.WriteLine($"FINANCE PENTA EXPENSE PAYROLL SUMMARY {count} native HTTP/SQL checks PASS.");

        Expense Expense(Guid tenant, DateOnly date, string currency, decimal amount) => new()
        {
            AcademyId = tenant, ExpenseDate = date, Currency = currency, Amount = amount,
            Description = withheld, Category = withheld + "-category"
        };

        PayrollPayout Payout(PayrollProfile worker, string code, DateTime? paidAt, string currency, decimal gross, decimal deductions) => new()
        {
            AcademyId = worker.AcademyId, PayrollProfileId = worker.Id, PayslipNumber = "QA-PENTA-PS-" + code,
            PeriodLabel = withheld + "-period", PaymentMethod = withheld + "-method", Reference = withheld + "-ref",
            WorkerNameSnapshot = withheld + "-snapshot", WorkerTypeSnapshot = "Staff", IdentityRecordedAtUtc = now,
            PaidAtUtc = paidAt, GrossAmount = gross, Deductions = deductions, NetAmount = gross - deductions, Currency = currency
        };

        static async Task<HttpClient> SignIn(QaApiFactory api, string email)
        {
            var client = api.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, email, "Synthetic!39Ab"));
            return client;
        }

        async Task<T> WithDb<T>(Func<AcademyDeskDbContext, Task<T>> action)
        {
            using var scope = factory.Services.CreateScope();
            return await action(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>());
        }

        Task<int> Modules(string json, Guid? tenant = null) => WithDb(db => db.Academies.Where(x => x.Id == (tenant ?? academy))
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.EnabledModulesJson, json)));

        Task<string> State(bool includeAudit = true) => WithDb(async db => JsonSerializer.Serialize(new
        {
            expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            profiles = await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            payouts = await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
            audits = includeAudit ? await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() : null
        }));

        void Pass(string label)
        {
            count++;
            Console.WriteLine($"FINANCE PENTA EXPENSE PAYROLL CASE {label} PASS");
        }

        async Task Refuse(HttpClient client, string route, HttpStatusCode status, string label)
        {
            var snapshot = await State();
            using var response = await client.GetAsync(route);
            RequireFinanceStatus(response, status, label);
            var payload = await response.Content.ReadAsStringAsync();
            if (snapshot != await State() || payload.Contains(withheld, StringComparison.Ordinal) ||
                payload.Contains("\"payouts\"", StringComparison.Ordinal) || payload.Contains("\"expenses\"", StringComparison.Ordinal))
                throw new InvalidOperationException(label + " wrote data or exposed a protected result.");
            Pass(label);
        }

        async Task<T> Read<T>(HttpClient client, string route, bool protect = true)
        {
            using var response = await client.GetAsync(route);
            RequireFinanceStatus(response, HttpStatusCode.OK, "expense or payroll read");
            if (protect)
            {
                var payload = await response.Content.ReadAsStringAsync();
                if (response.Headers.CacheControl?.NoStore != true || payload.Contains(withheld, StringComparison.Ordinal) ||
                    payload.Contains("\"workerName\"", StringComparison.Ordinal) || payload.Contains("\"payrollProfileId\"", StringComparison.Ordinal))
                    throw new InvalidOperationException("Protected response must be no-store and omit private ledger fields.");
            }
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }
    }
}
