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
    private static async Task VerifyExpenseBranchAcceptanceAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        const string financeEmail = "qa-expense-finance@example.invalid";
        const string ownerEmail = "qa-expense-owner@example.invalid";
        const string emptyEmail = "qa-expense-empty@example.invalid";
        const string password = "Synthetic!39Ab";
        Guid academy, foreign;
        var empty = new Academy { Name = "Synthetic empty expense academy", EnabledModulesJson = """["Finance"]""" };
        Branch alpha, zulu, outside;
        Expense legacy;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            alpha = new Branch { AcademyId = academy, Name = "Alpha", AddressLine1 = "Synthetic private address" };
            zulu = new Branch { AcademyId = academy, Name = "Zulu", IsActive = false };
            outside = new Branch { AcademyId = foreign, Name = "Foreign" };
            legacy = new Expense { AcademyId = academy, BranchId = zulu.Id, Description = "Legacy synthetic expense",
                Amount = 25.10m, Currency = "USD", Status = "Historical" };
            db.AddRange(empty, alpha, zulu, outside, legacy,
                new Expense { AcademyId = foreign, Description = "Foreign synthetic expense", Amount = 999 });
            await db.SaveChangesAsync();
        }

        var financeActor = await CreateAccessActorAsync(factory, financeEmail, "FinanceUser", academy);
        await CreateAccessActorAsync(factory, ownerEmail, "Owner", academy);
        await CreateAccessActorAsync(factory, emptyEmail, "FinanceUser", empty.Id);
        var finance = await LoginAsync(client, financeEmail, password);
        var owner = await LoginAsync(client, ownerEmail, password);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", password);
        var emptyFinance = await LoginAsync(client, emptyEmail, password);
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", password);
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", password);
        client.DefaultRequestHeaders.Authorization = null;

        var root = $"/api/academies/{academy}";
        var expenses = root + "/expenses";
        var options = expenses + "/branch-options";
        var count = 0;
        void Pass(string label)
        {
            count++;
            Console.WriteLine("EXPENSEACCEPT CASE " + label + " PASS");
        }
        static void Assert(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        async Task<JsonObject> Snapshot()
        {
            var state = await FinanceRecoveryStateAsync(factory);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            state["expenses"] = JsonSerializer.SerializeToNode(await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            state["branches"] = JsonSerializer.SerializeToNode(await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            state["academies"] = JsonSerializer.SerializeToNode(await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
            return state;
        }
        async Task<JsonElement> Read(string label, string? token, string route, HttpStatusCode status = HttpStatusCode.OK)
        {
            var before = await Snapshot();
            using var response = await ManualSendAsync(client, token, "GET", route);
            RequireFinanceStatus(response, status, label);
            var result = status == HttpStatusCode.OK ? await response.Content.ReadFromJsonAsync<JsonElement>() : default;
            ManualSame(before, await Snapshot(), label);
            return result;
        }
        foreach (var (label, token) in new[] { ("finance", finance), ("owner", owner), ("admin", admin) })
        {
            var rows = (await Read(label + " branch options", token, options)).EnumerateArray().ToArray();
            Assert(rows.Select(row => row.GetProperty("id").GetGuid()).SequenceEqual(new[] { alpha.Id, zulu.Id }), "Options are not ordered and scoped.");
            Assert(rows.Select(row => row.GetProperty("name").GetString()).SequenceEqual(new[] { "Alpha", "Zulu" }), "Branch labels differ.");
            Assert(rows.All(row => row.EnumerateObject().Select(field => field.Name).Order().SequenceEqual(new[] { "id", "name" })), "Branch options expose extra data.");
            var existing = (await Read(label + " expense register", token, expenses)).EnumerateArray().Single();
            Assert(existing.GetProperty("id").GetGuid() == legacy.Id && existing.GetProperty("currency").GetString() == "USD" &&
                existing.GetProperty("amount").GetDecimal() == 25.10m && existing.GetProperty("status").GetString() == "Historical", "Legacy expense readback differs.");
            Pass(label + "-register-and-minimal-options");
        }
        foreach (var (label, token, route, status) in new (string, string?, string, HttpStatusCode)[]
        {
            ("finance-no-branch-management", finance, root + "/branches", HttpStatusCode.Forbidden),
            ("anonymous-denied", null, options, HttpStatusCode.Unauthorized),
            ("teacher-denied", teacher, options, HttpStatusCode.Forbidden),
            ("foreign-denied", foreignAdmin, options, HttpStatusCode.Forbidden),
            ("missing-finance-module-denied", foreignAdmin, $"/api/academies/{foreign}/expenses/branch-options", HttpStatusCode.Forbidden)
        })
        {
            await Read(label, token, route, status);
            Pass(label);
        }
        Assert((await Read("empty branch list", emptyFinance, $"/api/academies/{empty.Id}/expenses/branch-options")).GetArrayLength() == 0,
            "A finance-only academy without branches did not receive an empty list.");
        Pass("empty-options");

        JsonObject Entry(Guid? branch = null) => new()
        {
            ["description"] = "Synthetic expense",
            ["amount"] = 19.37m,
            ["currency"] = "INR",
            ["category"] = null,
            ["branchId"] = JsonValue.Create(branch),
            ["expenseDate"] = null
        };
        async Task Reject(string label, string method, string route, JsonObject entry, HttpStatusCode status,
            HttpClient? target = null)
        {
            var before = await Snapshot();
            using var response = await ManualSendAsync(target ?? client, finance, method, route, entry);
            RequireFinanceStatus(response, status, label);
            ManualSame(before, await Snapshot(), label);
            Pass(label);
        }
        await Reject("create-foreign-branch-denied", "POST", expenses, Entry(outside.Id), HttpStatusCode.BadRequest);
        await Reject("create-missing-branch-denied", "POST", expenses, Entry(Guid.NewGuid()), HttpStatusCode.BadRequest);

        var beforeCreate = await Snapshot();
        var started = DateOnly.FromDateTime(DateTime.UtcNow);
        Guid savedId;
        using (var response = await ManualSendAsync(client, finance, "POST", expenses, Entry(alpha.Id)))
        {
            RequireFinanceStatus(response, HttpStatusCode.Created, "create scoped expense");
            savedId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        }
        var created = await Snapshot();
        var added = ManualAdded(beforeCreate, created, "expenses", 1).Single();
        FinanceRecoveryFields(added, new { Id = savedId, AcademyId = academy, BranchId = alpha.Id, Amount = 19.37m, Currency = "INR",
            Description = "Synthetic expense", Category = "General", Status = "Recorded" });
        var actualDate = DateOnly.ParseExact(added["ExpenseDate"]!.GetValue<string>(), "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture);
        Assert(actualDate >= started && actualDate <= DateOnly.FromDateTime(DateTime.UtcNow), "The omitted expense date did not default to UTC today.");
        FinanceRecoveryFields(ManualAdded(beforeCreate, created, "audits", 1).Single(),
            new { AcademyId = academy, ActorUserId = financeActor, Action = "POST Expenses" });
        ManualSame(beforeCreate, created, "create unrelated state");
        Pass("create-exact-with-attributed-audit");

        var fresh = (await Read("fresh expense register", finance, expenses)).EnumerateArray().Single(row => row.GetProperty("id").GetGuid() == savedId);
        Assert(fresh.GetProperty("amount").GetDecimal() == 19.37m && fresh.GetProperty("branchId").GetGuid() == alpha.Id, "Fresh expense response differs.");
        Pass("fresh-expense-readback");

        var editPath = $"{expenses}/{savedId}";
        var change = Entry(outside.Id);
        change["expenseDate"] = "2026-11-01";
        await Reject("edit-foreign-branch-denied", "PUT", editPath, change, HttpStatusCode.BadRequest);
        change["branchId"] = null;
        change["description"] = "Synthetic expense edited";
        change["amount"] = 20.08m;
        var beforeEdit = await Snapshot();
        using (var response = await ManualSendAsync(client, finance, "PUT", editPath, change))
            RequireFinanceStatus(response, HttpStatusCode.OK, "edit expense with no branch");
        var edited = await Snapshot();
        var row = ManualRow(edited, "expenses", savedId);
        FinanceRecoveryFields(row, new { Amount = 20.08m, BranchId = (Guid?)null, Description = "Synthetic expense edited",
            ExpenseDate = new DateOnly(2026, 11, 1), Category = "General", Currency = "INR", Status = "Recorded" });
        var old = ManualRow(beforeEdit, "expenses", savedId);
        foreach (var field in new[] { "Amount", "BranchId", "Description", "ExpenseDate" }) row[field] = old[field]?.DeepClone();
        FinanceRecoveryFields(ManualAdded(beforeEdit, edited, "audits", 1).Single(),
            new { AcademyId = academy, ActorUserId = financeActor, Action = "PUT Expenses" });
        ManualSame(beforeEdit, edited, "edit unrelated state");
        Pass("edit-null-branch-with-attributed-audit");

        var fault = new ExpenseRequiredAuditFault();
        using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
        using var failingClient = failing.CreateClient(new() { AllowAutoRedirect = false });
        await Reject("required-audit-failure-rolls-back-create", "POST", expenses, Entry(alpha.Id), HttpStatusCode.InternalServerError, failingClient);
        change["amount"] = 21.09m;
        await Reject("required-audit-failure-rolls-back-edit", "PUT", editPath, change, HttpStatusCode.InternalServerError, failingClient);
        Assert(fault.Hits == 2, "Both required expense audits must have been reached.");
        Console.WriteLine($"EXPENSEACCEPT SUMMARY {count} native HTTP/SQL checks PASS");
    }

    private sealed class ExpenseRequiredAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State == EntityState.Added &&
                entry.Entity.Action is "POST Expenses" or "PUT Expenses"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required expense audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
