using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyExpenseIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, foreign, student, localBranch, foreignBranch, inactiveBranch, expenseId, sentinelId;
        var count = 0;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x => x.Id == foreign)).EnabledModulesJson = "[\"Finance\",\"FinanceControls\"]";
            var local = new Branch { AcademyId = academy, Name = "Expense local" };
            var other = new Branch { AcademyId = foreign, Name = "Expense foreign" };
            var inactive = new Branch { AcademyId = academy, Name = "Expense historical", IsActive = false };
            var expense = new Expense { AcademyId = academy, Description = "Original", Amount = 10.25m, Currency = "USD", Category = "Rent", BranchId = local.Id, ExpenseDate = new(2026, 10, 1) };
            var sentinel = new Expense { AcademyId = foreign, Description = "Foreign sentinel", Amount = 17.75m, Currency = "EUR", BranchId = other.Id, ExpenseDate = new(2026, 10, 1) };
            db.AddRange(local, other, inactive, expense, sentinel); await db.SaveChangesAsync();
            localBranch = local.Id; foreignBranch = other.Id; inactiveBranch = inactive.Id; expenseId = expense.Id; sentinelId = sentinel.Id;
        }
        await CreateAccessActorAsync(factory, "qa-expense-student@example.invalid", "Student", academy, student);
        await CreateAccessActorAsync(factory, "qa-expense-finance@example.invalid", "FinanceUser", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-expense-student@example.invalid", "Synthetic!39Ab");
        var finance = await LoginAsync(client, "qa-expense-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}/expenses";
        var updatePath = root + "/" + expenseId;
        var valid = new UpdateExpenseRequest(" Updated ", 12.50m, " Supplies ", localBranch, new(2026, 10, 11), "Recorded");
        var create = new CreateExpenseRequest(" Created ", 1.25m, " usd ", " ", localBranch, new(2026, 10, 11));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Expenses = await db.Expenses.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Branches = await db.Branches.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body, string? token, bool delay = true)
        {
            if (delay) await Task.Delay(650);
            using var request = new HttpRequestMessage(method, path);
            if (body is not null) request.Content = JsonContent.Create(body);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        void Passed(string label) { count++; Console.WriteLine("PASS: EXPENSE " + label); }
        async Task NoWrite(HttpMethod method, string path, object? body, string? token, HttpStatusCode expected, string label)
        {
            var before = await Snapshot(); using var response = await Send(method, path, body, token);
            RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Expense no-write snapshot differs: " + label);
            if (expected == HttpStatusCode.BadRequest)
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (string.IsNullOrWhiteSpace(json.RootElement.GetProperty("message").GetString())) throw new InvalidOperationException("Missing expense validation feedback.");
            }
            Passed(label + " exact status/no-write");
        }
        async Task<int> Audits()
        { using var scope = factory.Services.CreateScope(); return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.CountAsync(); }
        async Task CheckAudit(int before, string method)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.AuditLogs.AsNoTracking().ToArrayAsync();
            if (rows.Length != before + 1 || !rows.Any(x => x.AcademyId == academy && x.ActorUserId != null && x.Action == method + " Expenses" && x.MetadataJson!.Contains(root)))
                throw new InvalidOperationException("Expense actor/tenant/action audit differs.");
            var sentinel = await db.Expenses.AsNoTracking().SingleAsync(x => x.Id == sentinelId);
            if (sentinel.AcademyId != foreign || sentinel.Amount != 17.75m || sentinel.Currency != "EUR" || sentinel.BranchId != foreignBranch || sentinel.Status != "Recorded")
                throw new InvalidOperationException("Foreign expense sentinel changed.");
        }
        foreach (var mode in new[] { "foreign", "missing", "empty-guid", "Paid", "Voided", "unknown", "description", "description-long", "category-long", "zero", "negative", "precision", "overflow" })
        {
            var body = mode switch {
                "foreign" => valid with { BranchId = foreignBranch }, "missing" => valid with { BranchId = Guid.NewGuid() }, "empty-guid" => valid with { BranchId = Guid.Empty },
                "Paid" or "Voided" or "unknown" => valid with { Status = mode }, "description" => valid with { Description = " " },
                "description-long" => valid with { Description = new('x', 301) }, "category-long" => valid with { Category = new('x', 81) },
                "zero" => valid with { Amount = 0 }, "negative" => valid with { Amount = -1 }, "precision" => valid with { Amount = .001m },
                "overflow" => valid with { Amount = 10000000000000000m }, _ => throw new InvalidOperationException() };
            await NoWrite(HttpMethod.Put, updatePath, body, admin, HttpStatusCode.BadRequest, "update-" + mode);
        }
        foreach (var mode in new[] { "foreign", "missing", "description-long", "category-long", "zero", "negative", "precision", "overflow", "currency-long", "currency-symbol" })
        {
            var body = mode switch {
                "foreign" => create with { BranchId = foreignBranch }, "missing" => create with { BranchId = Guid.NewGuid() },
                "description-long" => create with { Description = new('x', 301) }, "category-long" => create with { Category = new('x', 81) },
                "zero" => create with { Amount = 0 }, "negative" => create with { Amount = -1 }, "precision" => create with { Amount = .001m },
                "overflow" => create with { Amount = 10000000000000000m }, "currency-long" => create with { Currency = "USDX" },
                "currency-symbol" => create with { Currency = "12$" }, _ => throw new InvalidOperationException() };
            await NoWrite(HttpMethod.Post, root, body, admin, HttpStatusCode.BadRequest, "create-" + mode);
        }
        foreach (var actor in new (string Label, string? Token, HttpStatusCode Status)[] { ("anonymous", null, HttpStatusCode.Unauthorized),
            ("teacher", teacher, HttpStatusCode.Forbidden), ("student", studentToken, HttpStatusCode.Forbidden), ("foreign actor", otherAdmin, HttpStatusCode.Forbidden) })
            foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put })
                await NoWrite(method, method == HttpMethod.Put ? updatePath : root, method == HttpMethod.Get ? null : method == HttpMethod.Put ? valid : create,
                    actor.Token, actor.Status, actor.Label + " " + method);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Put })
            await NoWrite(method, $"/api/academies/{foreign}/expenses" + (method == HttpMethod.Put ? "/" + sentinelId : ""),
                method == HttpMethod.Get ? null : method == HttpMethod.Put ? valid : create, admin, HttpStatusCode.Forbidden, "foreign route " + method);
        await NoWrite(HttpMethod.Put, root + "/" + sentinelId, valid, admin, HttpStatusCode.NotFound, "foreign record");
        await NoWrite(HttpMethod.Put, root + "/" + Guid.NewGuid(), valid, admin, HttpStatusCode.NotFound, "missing record");
        foreach (var mode in new[] { "suspended", "module disabled" })
        {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy);
                if (mode == "suspended") row.IsActive = false; else row.EnabledModulesJson = "[]"; await db.SaveChangesAsync(); }
            await NoWrite(HttpMethod.Post, root, create, admin, HttpStatusCode.Forbidden, mode);
            using var restore = factory.Services.CreateScope(); var dbRestore = restore.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var restored = await dbRestore.Academies.SingleAsync(x => x.Id == academy);
            restored.IsActive = true; restored.EnabledModulesJson = "[\"Certificates\",\"Finance\",\"FinanceControls\"]"; await dbRestore.SaveChangesAsync();
        }
        foreach (var body in new[] { create, create with { BranchId = null, Currency = null }, create with { BranchId = inactiveBranch },
            create with { Description = new('x', 300), Category = new('x', 80), Amount = 9999999999999999.99m } })
        {
            var before = await Audits(); using var response = await Send(HttpMethod.Post, root, body, finance); RequireFinanceStatus(response, HttpStatusCode.Created, "valid creation");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid();
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Expenses.AsNoTracking().SingleAsync(x => x.Id == id);
            if (row.AcademyId != academy || row.Description != body.Description.Trim() || row.Amount != body.Amount || row.Currency != (body.Currency is null ? "INR" : "USD") ||
                row.Category != (string.IsNullOrWhiteSpace(body.Category) ? "General" : body.Category.Trim()) || row.BranchId != body.BranchId || row.Status != "Recorded" ||
                json.RootElement.GetProperty("amount").GetDecimal() != body.Amount || json.RootElement.GetProperty("amountExact").GetString() != body.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)) throw new InvalidOperationException("Create expense SQL/response differs.");
            await CheckAudit(before, "POST"); Passed("create SQL boundary/optional/history/currency/audit");
        }
        foreach (var body in new[] { valid, valid with { BranchId = null, Category = " ", Status = null }, valid with { BranchId = inactiveBranch, Status = " " }, valid with { Status = " Recorded " } })
        {
            var before = await Audits(); using var response = await Send(HttpMethod.Put, updatePath, body, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "valid update");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Expenses.AsNoTracking().SingleAsync(x => x.Id == expenseId);
            if (row.Description != "Updated" || row.Amount != body.Amount || row.Category != (string.IsNullOrWhiteSpace(body.Category) ? "General" : "Supplies") ||
                row.BranchId != body.BranchId || row.Currency != "USD" || row.Status != "Recorded" || row.ExpenseDate != body.ExpenseDate) throw new InvalidOperationException("Valid update readback differs.");
            await CheckAudit(before, "PUT"); Passed("update optional/history/status/currency/audit");
        }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Expenses.SingleAsync(x => x.Id == expenseId)).Status = "Legacy review"; await db.SaveChangesAsync(); }
        using (var response = await Send(HttpMethod.Put, updatePath, valid with { Status = null }, admin)) RequireFinanceStatus(response, HttpStatusCode.OK, "legacy omission");
        using (var scope = factory.Services.CreateScope()) if ((await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Expenses.AsNoTracking().SingleAsync(x => x.Id == expenseId)).Status != "Legacy review") throw new InvalidOperationException("Legacy status rewritten.");
        Passed("legacy status omitted is preserved");
        foreach (var (token, tenant, expectedId) in new[] { (admin, academy, expenseId), (otherAdmin, foreign, sentinelId) })
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, $"/api/academies/{tenant}/expenses", null, token); RequireFinanceStatus(response, HttpStatusCode.OK, "scoped read");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var ids = json.RootElement.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToArray();
            if (!ids.Contains(expectedId) || ids.Contains(tenant == academy ? sentinelId : expenseId) || before != await Snapshot()) throw new InvalidOperationException("Expense read leaked/mutated.");
            Passed("tenant scoped read/no-write");
        }
        await SetAuditInsertDeniedAsync(manifest, true);
        try {
            await NoWrite(HttpMethod.Post, root, create, admin, HttpStatusCode.InternalServerError, "create audit-failure rollback");
            await NoWrite(HttpMethod.Put, updatePath, valid, admin, HttpStatusCode.InternalServerError, "update audit-failure rollback");
        } finally { await SetAuditInsertDeniedAsync(manifest, false); }
        var recoveryAudit = await Audits();
        using (var response = await Send(HttpMethod.Put, updatePath, valid, admin)) RequireFinanceStatus(response, HttpStatusCode.OK, "audit recovery");
        await CheckAudit(recoveryAudit, "PUT"); Passed("audit recovery");
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var before = await Audits(); await Task.Delay(650);
            var responses = await Task.WhenAll(Send(HttpMethod.Put, updatePath, valid with { Description = "Race " + attempt, Amount = 20 + attempt }, admin, false),
                Send(HttpMethod.Put, updatePath, valid with { BranchId = foreignBranch, Status = "Paid" }, admin, false));
            try {
                RequireFinanceStatus(responses[0], HttpStatusCode.OK, "valid concurrent edit"); RequireFinanceStatus(responses[1], HttpStatusCode.BadRequest, "invalid concurrent edit");
                using var scope = factory.Services.CreateScope(); var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Expenses.AsNoTracking().SingleAsync(x => x.Id == expenseId);
                if (row.Description != "Race " + attempt || row.Amount != 20 + attempt || row.BranchId != localBranch || row.Currency != "USD" || row.Status != "Recorded") throw new InvalidOperationException("Concurrent expense edit corrupted row.");
                await CheckAudit(before, "PUT"); Passed("concurrent edit " + attempt + " valid-only persistence");
            } finally { foreach (var response in responses) response.Dispose(); }
        }
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var before = await Audits(); await Task.Delay(650);
            var left = valid with { Description = "Valid left " + attempt, Amount = 31.25m, BranchId = null, Category = "Left", ExpenseDate = new(2026, 10, 1) };
            var right = valid with { Description = "Valid right " + attempt, Amount = 41.75m, BranchId = inactiveBranch, Category = "Right", ExpenseDate = new(2026, 10, 2) };
            var responses = await Task.WhenAll(Send(HttpMethod.Put, updatePath, left, admin, false), Send(HttpMethod.Put, updatePath, right, admin, false));
            try {
                foreach (var response in responses) RequireFinanceStatus(response, HttpStatusCode.OK, "valid concurrent replacement");
                using var scope = factory.Services.CreateScope(); var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Expenses.AsNoTracking().SingleAsync(x => x.Id == expenseId);
                bool Matches(UpdateExpenseRequest body) => row.Description == body.Description && row.Amount == body.Amount && row.BranchId == body.BranchId && row.Category == body.Category && row.ExpenseDate == body.ExpenseDate;
                var auditDelta = await Audits() - before;
                if (!(Matches(left) || Matches(right)) || row.Currency != "USD" || row.Status != "Recorded" || auditDelta != 2)
                    throw new InvalidOperationException($"Concurrent replacements: wholeRow={Matches(left) || Matches(right)}, currencyPreserved={row.Currency == "USD"}, statusPreserved={row.Status == "Recorded"}, auditDelta={auditDelta}.");
                Passed("valid concurrent replacements " + attempt + " whole-row/immutable-currency/two-audits");
            } finally { foreach (var response in responses) response.Dispose(); }
        }
        if (count != 66) throw new InvalidOperationException("Expense matrix incomplete: " + count);
        Console.WriteLine($"PASS: EXPENSE all {count} SQL/Identity/HTTP cases; exact status, decimal, tenant, currency, audit rollback and ten concurrent edit controls; no 429.");
        await VerifyExpenseBrowserAsync(factory, client, academy, foreignBranch);
    }
}
