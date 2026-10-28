using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyFinanceIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"));
        var payments = $"/api/academies/{academy}/payments";
        var adjustments = $"/api/academies/{academy}/finance-adjustments";
        var invoices = $"/api/academies/{academy}/invoices";
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine($"FINANCEINTEGRITY CASE {label} PASS."); }
        async Task<(Guid Invoice, Guid[] Payments, Guid[] Adjustments)> Seed(string mode)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-INTEGRITY-" + Guid.NewGuid().ToString("N"), TotalAmount = 1000m };
            db.Invoices.Add(invoice);
            var rows = new List<Payment>(); var credits = new List<FinanceAdjustment>();
            var paymentCount = mode == "restore-pair" ? 2 : mode is "restore-collect" or "void-reconcile" ? 1 : 0;
            for (var i = 0; i < paymentCount; i++) rows.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = mode == "void-reconcile" ? "Completed" : "Voided" });
            if (mode == "void-reconcile") invoice.Status = "PartiallyPaid";
            if (mode is "duplicate-approval" or "two-approvals")
            {
                credits.Add(new FinanceAdjustment { AcademyId = academy, InvoiceId = invoice.Id, Type = "Discount", Amount = 100m, Reason = "Synthetic guarded concurrency" });
                if (mode == "two-approvals") credits.Add(new FinanceAdjustment { AcademyId = academy, InvoiceId = invoice.Id, Type = "Scholarship", Amount = 200m, Reason = "Synthetic second adjustment" });
            }
            db.AddRange(rows); db.AddRange(credits); await db.SaveChangesAsync();
            return (invoice.Id, rows.Select(x => x.Id).ToArray(), credits.Select(x => x.Id).ToArray());
        }
        async Task<FinanceIntegrityState> Read(Guid id)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return new(await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == id),
                await db.Payments.AsNoTracking().Where(x => x.InvoiceId == id).OrderBy(x => x.Id).ToArrayAsync(),
                await db.FinanceAdjustments.AsNoTracking().Where(x => x.InvoiceId == id).OrderBy(x => x.Id).ToArrayAsync(),
                await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy).OrderBy(x => x.Id).ToArrayAsync());
        }
        foreach (var mode in new[] { "collect-pair", "restore-pair", "restore-collect", "duplicate-approval", "two-approvals", "void-reconcile" })
        {
            var f = await Seed(mode); var before = await Read(f.Invoice);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            await using var held = await db.Database.BeginTransactionAsync();
            _ = await db.Invoices.FromSqlInterpolated($"SELECT * FROM [Invoices] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academy} AND [Id] = {f.Invoice}").SingleAsync();
            var pending = mode switch
            {
                "collect-pair" => new[] { client.PostAsJsonAsync(payments, new { invoiceId = f.Invoice, amount = 600m }), client.PostAsJsonAsync(payments, new { invoiceId = f.Invoice, amount = 600m }) },
                "restore-pair" => f.Payments.Select(id => client.PatchAsJsonAsync($"{payments}/{id}/status", new { status = "Completed" })).ToArray(),
                "restore-collect" => new[] { client.PatchAsJsonAsync($"{payments}/{f.Payments[0]}/status", new { status = "Completed" }), client.PostAsJsonAsync(payments, new { invoiceId = f.Invoice, amount = 600m }) },
                "duplicate-approval" => new[] { client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve = true }), client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve = true }) },
                "two-approvals" => f.Adjustments.Select(id => client.PatchAsJsonAsync($"{adjustments}/{id}/approval", new { approve = true })).ToArray(),
                _ => new[] { client.PatchAsJsonAsync($"{payments}/{f.Payments[0]}/status", new { status = "Voided" }), client.PatchAsJsonAsync($"{payments}/{f.Payments[0]}/reconcile", new { reference = "QA-RACE" }) },
            };
            await RequireInvoiceWaitersAsync(manifest, 2);
            if (pending.Any(x => x.IsCompleted)) throw new InvalidOperationException("Invoice race completed before held lock release.");
            await held.CommitAsync(); var responses = await Task.WhenAll(pending);
            try
            {
                var successes = responses.Count(x => x.IsSuccessStatusCode);
                var expected = mode is "two-approvals" or "void-reconcile" ? 2 : 1;
                if (successes != expected || responses.Any(x => !x.IsSuccessStatusCode && x.StatusCode != (mode == "duplicate-approval" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest)))
                    throw new InvalidOperationException("Invoice race response mismatch: " + mode);
                var after = await Read(f.Invoice);
                var paid = after.Payments.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount);
                var credited = after.Adjustments.Where(x => x.Status == "Approved").Sum(x => x.Amount);
                if (after.Invoice.TotalAmount != 1000m || after.Invoice.AdjustedAmount != credited || paid > 1000m - credited)
                    throw new InvalidOperationException("Invoice race overcollection or lost/duplicate adjustment: " + mode);
                if (mode is "collect-pair" or "restore-pair" or "restore-collect" && paid != 600m)
                    throw new InvalidOperationException("Invoice race lost the successful collection.");
                if (mode == "duplicate-approval" && credited != 100m || mode == "two-approvals" && credited != 300m)
                    throw new InvalidOperationException("Invoice adjustment race total mismatch.");
                var expectedStatus = paid > 0m ? "PartiallyPaid" : "Issued";
                if (after.Invoice.Status != expectedStatus) throw new InvalidOperationException("Invoice race status differs from ledger.");
                var added = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
                if (added.Length != expected || JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => added.All(y => y.Id != x.Id))))
                    throw new InvalidOperationException("Invoice race audit boundary mismatch.");
                if (added.Any(x => x.Action != (mode.Contains("approval", StringComparison.Ordinal) ? "PATCH FinanceAdjustments" : x.Action.StartsWith("POST", StringComparison.Ordinal) ? "POST Payments" : "PATCH Payments")))
                    throw new InvalidOperationException("Invoice race unexpected audit action.");
                Pass(mode + "-two-SQL-lock-waiters-ledger-and-audit");
            }
            finally { foreach (var result in responses) result.Dispose(); }
        }
        foreach (var mode in new[] { "collect-pair", "restore-collect", "duplicate-approval" })
        {
            var f = await Seed(mode); var before = await Read(f.Invoice);
            await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                using var result = mode switch
                {
                    "collect-pair" => await client.PostAsJsonAsync(payments, new { invoiceId = f.Invoice, amount = 600m }),
                    "restore-collect" => await client.PatchAsJsonAsync($"{payments}/{f.Payments[0]}/status", new { status = "Completed" }),
                    _ => await client.PatchAsJsonAsync($"{adjustments}/{f.Adjustments[0]}/approval", new { approve = true }),
                };
                RequireFinanceStatus(result, HttpStatusCode.InternalServerError, "invoice audit fault " + mode);
                if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await Read(f.Invoice)))
                    throw new InvalidOperationException("Invoice write survived failed audit.");
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }
            Pass(mode + "-audit-failure-rolls-back-ledger");
        }
        var statusFixture = await Seed("collect-pair");
        var statusBefore = await Read(statusFixture.Invoice);
        using var falsePaid = await client.PatchAsJsonAsync($"{invoices}/{statusFixture.Invoice}/status", new { status = "paid" });
        RequireFinanceStatus(falsePaid, HttpStatusCode.BadRequest, "unpaid invoice cannot become Paid");
        if (JsonSerializer.Serialize(statusBefore) != JsonSerializer.Serialize(await Read(statusFixture.Invoice)))
            throw new InvalidOperationException("Rejected invoice label changed ledger/audit.");
        Pass("unpaid-Paid-label-rejected-no-write");
        using var cancel = await client.PatchAsJsonAsync($"{invoices}/{statusFixture.Invoice}/status", new { status = "cancelled" });
        RequireFinanceStatus(cancel, HttpStatusCode.OK, "canonical explicit cancellation");
        var cancelled = await Read(statusFixture.Invoice);
        if (cancelled.Invoice.Status != "Cancelled") throw new InvalidOperationException("Cancellation not canonical.");
        using var paymentOnCancelled = await client.PostAsJsonAsync(payments, new { invoiceId = statusFixture.Invoice, amount = 1m });
        RequireFinanceStatus(paymentOnCancelled, HttpStatusCode.BadRequest, "cancelled invoice collection");
        if (JsonSerializer.Serialize(cancelled) != JsonSerializer.Serialize(await Read(statusFixture.Invoice)))
            throw new InvalidOperationException("Cancelled collection changed ledger/audit.");
        Pass("canonical-cancellation-denies-collection-no-write");
        var cachedFixture = await Seed("duplicate-approval");
        using (var cachedScope = factory.Services.CreateScope())
        {
            var cachedDb = cachedScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            _ = await cachedDb.Invoices.SingleAsync(x => x.Id == cachedFixture.Invoice);
            using var approve = await client.PatchAsJsonAsync($"{adjustments}/{cachedFixture.Adjustments[0]}/approval", new { approve = true });
            RequireFinanceStatus(approve, HttpStatusCode.OK, "stale tracked context adjustment");
            var before = await Read(cachedFixture.Invoice);
            var rejected = await new AcademyDesk.Api.Controllers.PaymentsController(cachedDb).Create(academy,
                new(cachedFixture.Invoice, 950m, null, null), default);
            if (rejected.Result is not Microsoft.AspNetCore.Mvc.BadRequestObjectResult ||
                JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await Read(cachedFixture.Invoice)))
                throw new InvalidOperationException("Cached invoice bypassed newly committed adjusted balance.");
        }
        Pass("stale-tracked-invoice-refreshed-under-lock");
        if (count != 12) throw new InvalidOperationException("Finance integrity case inventory changed.");
        Console.WriteLine("FINANCEINTEGRITY REGRESSION PASS:12 guarded cases; six deterministic real SQL races, three audit-fault rollbacks, unpaid status rejection, cancellation collection guard and stale tracked invoice refresh; browser/device/idempotency/full release still pending.");
    }

    private static async Task RequireInvoiceWaitersAsync(QaRunManifest manifest, int expected)
    {
        await using var sql = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!));
        await sql.OpenAsync();
        for (var attempt = 0; attempt < 80; attempt++)
        {
            await using var command = new SqlCommand("SELECT COUNT(*) FROM sys.dm_exec_requests r JOIN sys.dm_exec_sessions s ON r.session_id=s.session_id CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) q WHERE r.database_id=DB_ID() AND r.blocking_session_id>0 AND r.wait_type LIKE 'LCK_M_%' AND s.login_name=@login AND q.text LIKE '%UPDLOCK%' AND q.text LIKE '%Invoices%'", sql);
            command.Parameters.AddWithValue("@login", manifest.RuntimeLogin);
            var waits = (int)(await command.ExecuteScalarAsync())!;
            if (waits >= expected) { Console.WriteLine($"FINANCEINTEGRITY SQL invoice-lock waiters={waits} observed before release."); return; }
            await Task.Delay(100);
        }
        throw new InvalidOperationException("HTTP requests did not reach the held invoice read/update lock; concurrency not proved.");
    }

    private sealed record FinanceIntegrityState(Invoice Invoice, Payment[] Payments, FinanceAdjustment[] Adjustments, AuditLog[] Audits);
}
