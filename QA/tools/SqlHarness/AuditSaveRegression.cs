using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAuditSaveAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, bool enforceFixed)
    {
        Guid academy, student, teacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"));
        var root = $"/api/academies/{academy}";
        var fixture = await SeedAccessFixtureAsync(factory, academy, student, teacher);
        var invoiceBefore = await AuditInvoiceCountAsync(factory);
        var auditBefore = await AccessAuditCountAsync(factory);
        await SetAuditInsertDeniedAsync(manifest, true);
        try
        {
            using var response = await client.PostAsJsonAsync(root + "/invoices", new { studentId = student, amount = 99m });
            RequireFinanceStatus(response, HttpStatusCode.InternalServerError, "audit denied invoice");
            var invoiceDelta = await AuditInvoiceCountAsync(factory) - invoiceBefore;
            if (invoiceDelta != (enforceFixed ? 0 : 1) || await AccessAuditCountAsync(factory) != auditBefore)
                throw new InvalidOperationException("Audit invoice fault did not match fresh SQL expectation.");
            Console.WriteLine($"AUDIT CASE invoice-fault {(enforceFixed ? "PASS" : "REPRODUCED")}: HTTP=500, fresh invoice delta={invoiceDelta}, audit delta=0.");

            var financialBefore = await AccessFinancialSnapshotAsync(factory);
            using var payment = await client.PostAsJsonAsync(root + "/payments", new { invoiceId = fixture.Invoice, amount = 900m, reference = "QA-AUDIT-FAULT" });
            RequireFinanceStatus(payment, HttpStatusCode.InternalServerError, "audit denied payment");
            var financialAfter = await AccessFinancialSnapshotAsync(factory);
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var rows = await db.Payments.AsNoTracking().CountAsync(x => x.Reference == "QA-AUDIT-FAULT");
                var status = await db.Invoices.AsNoTracking().Where(x => x.Id == fixture.Invoice).Select(x => x.Status).SingleAsync();
                if (rows != (enforceFixed ? 0 : 1) || status != (enforceFixed ? "PartiallyPaid" : "Paid") ||
                    enforceFixed && financialBefore != financialAfter || await AccessAuditCountAsync(factory) != auditBefore)
                    throw new InvalidOperationException("Audit payment fault persisted unexpected state.");
                Console.WriteLine($"AUDIT CASE payment-fault {(enforceFixed ? "PASS" : "REPRODUCED")}: HTTP=500, fresh payment delta={rows}, invoice={status}, audit delta=0; captured finance/notifications {(enforceFixed ? "unchanged" : "changed")}.");
            }
        }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }

        // Action-level rejections must be inspected before MVC writes their HTTP status.
        foreach (var (path, body, expected) in new (string, object, HttpStatusCode)[] {
            (root + "/invoices", new { studentId = student, amount = -1m }, HttpStatusCode.BadRequest),
            (root + "/payments", new { invoiceId = Guid.NewGuid(), amount = 1m }, HttpStatusCode.NotFound)
        })
        {
            var before = await AccessFinancialSnapshotAsync(factory);
            var count = await AccessAuditCountAsync(factory);
            using var response = await client.PostAsJsonAsync(path, body);
            RequireFinanceStatus(response, expected, "audit rejected action");
            var delta = await AccessAuditCountAsync(factory) - count;
            if (before != await AccessFinancialSnapshotAsync(factory) || delta != (enforceFixed ? 0 : 1))
                throw new InvalidOperationException("Rejected action changed financial state or audit expectation.");
            Console.WriteLine($"AUDIT CASE rejected-{(int)expected} {(enforceFixed ? "PASS" : "REPRODUCED")}: financial/notification unchanged, generic audit delta={delta}.");
        }
        invoiceBefore = await AuditInvoiceCountAsync(factory);
        auditBefore = await AccessAuditCountAsync(factory);
        using var retry = await client.PostAsJsonAsync(root + "/invoices", new { studentId = student, amount = 99m });
        RequireFinanceStatus(retry, HttpStatusCode.Created, "audit recovered invoice");
        using var json = JsonDocument.Parse(await retry.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (!await db.Invoices.AsNoTracking().AnyAsync(x => x.Id == id && x.AcademyId == academy && x.TotalAmount == 99m) ||
                await AuditInvoiceCountAsync(factory) != invoiceBefore + 1 || await AccessAuditCountAsync(factory) != auditBefore + 1)
                throw new InvalidOperationException("Recovered write/audit not persisted exactly once for this request.");
            var log = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).FirstAsync();
            if (log.ActorUserId is null || log.AcademyId != academy || log.Action != "POST Invoices" || !log.MetadataJson!.Contains(root + "/invoices"))
                throw new InvalidOperationException("Audit actor/tenant/action/route incorrect.");
        }
        Console.WriteLine("AUDIT CASE recovered-write PASS: HTTP=201; response ID/amount and one actor/tenant/route audit persisted in fresh SQL.");
        var readAuditCount = await AccessAuditCountAsync(factory);
        using var read = await client.GetAsync(root + "/invoices");
        RequireFinanceStatus(read, HttpStatusCode.OK, "audit read");
        if (await AccessAuditCountAsync(factory) != readAuditCount) throw new InvalidOperationException("Read wrote generic audit.");
        Console.WriteLine("AUDIT CASE read PASS: HTTP=200, audit unchanged.");
        Console.WriteLine($"AUDIT {(enforceFixed ? "REGRESSION PASS" : "BASELINE REPRODUCED")}: six cases; SQL DENY fault restored; finance boundary only, cross-context/media/critical/client-response-loss NOT RUN.");
    }

    private static async Task<int> AuditInvoiceCountAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Invoices.CountAsync();
    }

    private static async Task SetAuditInsertDeniedAsync(QaRunManifest manifest, bool denied)
    {
        // Main has already verified exact labelled loopback SQL and its run-owned DB.
        // Revalidate its token marker immediately before this test-only permission fault.
        await using var db = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa",
            Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD") ?? throw new InvalidOperationException("QA credential absent.")));
        await db.OpenAsync();
        var marker = await ReadMarkerAsync(db, manifest);
        if (marker.RunId != manifest.RunId || marker.TokenDigest != manifest.TokenDigest)
            throw new InvalidOperationException("Refused audit fault injection: wrong owned marker.");
        await ExecuteAsync(db, $"{(denied ? "DENY" : "REVOKE")} INSERT ON OBJECT::dbo.AuditLogs {(denied ? "TO" : "FROM")} [{manifest.RuntimeLogin}]");
        Console.WriteLine($"AUDIT FAULT {(denied ? "enabled" : "removed")}: INSERT permission on exact run-owned AuditLogs only; no live database changes.");
    }
}
