using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record InvoiceIntegrityState(Invoice[] Invoices, Payment[] Payments, FinanceAdjustment[] Adjustments, AuditLog[] Audits);

    private static async Task VerifyInvoiceStatusIntegrityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        // Persisted decimals may serialize with SQL scale (400.00). Equality
        // must be exact numeric equality, never rounding or string-scale equality.
        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(400m), JsonSerializer.SerializeToElement(400.00m)) ||
            JsonElement.DeepEquals(JsonSerializer.SerializeToElement(400m), JsonSerializer.SerializeToElement(400.01m)))
            throw new InvalidOperationException("Invoice snapshot exact decimal comparison self-check failed.");
        Guid academy, foreign, student, foreignStudent, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\",\"Finance\",\"FinanceControls\"]";
            await db.SaveChangesAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}";
        string StatusPath(Guid id) => $"{root}/invoices/{id}/status";
        async Task<InvoiceIntegrityState> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            // SQL uniqueidentifier ordering differs from CLR Guid ordering. Use
            // one canonical CLR order for snapshots AND appended expected rows;
            // compare every persisted field and row, not query presentation order.
            return new((await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                (await db.FinanceAdjustments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray());
        }
        async Task<(Invoice Invoice, Payment? Payment, FinanceAdjustment? Adjustment)> Seed(decimal paid, string status, bool proposal = false, bool sentinel = false)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = new Invoice { AcademyId = sentinel ? foreign : academy, StudentId = sentinel ? foreignStudent : student,
                InvoiceNumber = Guid.NewGuid().ToString("N"), TotalAmount = 1000m, Status = status,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)) };
            Payment? payment = paid > 0m ? new() { AcademyId = invoice.AcademyId, InvoiceId = invoice.Id, Amount = paid,
                Status = "Reconciled", Reference = "Keep payment reference", ReconciliationReference = "Keep reconciliation evidence", ReconciledAtUtc = DateTime.UtcNow } : null;
            FinanceAdjustment? adjustment = proposal ? new() { AcademyId = academy, InvoiceId = invoice.Id, Amount = 400m, Reason = "Synthetic discount proposal" } : null;
            db.Add(invoice); if (payment is not null) db.Add(payment); if (adjustment is not null) db.Add(adjustment);
            await db.SaveChangesAsync(); return (invoice, payment, adjustment);
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object body)
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        void Exact(InvoiceIntegrityState expected, InvoiceIntegrityState actual)
        {
            if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), JsonSerializer.SerializeToElement(actual)))
            {
                var expectedJson = JsonSerializer.SerializeToElement(expected);
                var actualJson = JsonSerializer.SerializeToElement(actual);
                // Diagnostics expose table/field names only, never stored values.
                foreach (var table in expectedJson.EnumerateObject())
                {
                    var expectedRows = table.Value.EnumerateArray().ToArray();
                    var actualRows = actualJson.GetProperty(table.Name).EnumerateArray().ToArray();
                    if (expectedRows.Length != actualRows.Length) Console.WriteLine($"AUDIT INVOICEINTEGRITY snapshot mismatch table={table.Name} rowCount");
                    for (var row = 0; row < Math.Min(expectedRows.Length, actualRows.Length); row++)
                        foreach (var field in expectedRows[row].EnumerateObject())
                            if (!JsonElement.DeepEquals(field.Value, actualRows[row].GetProperty(field.Name)))
                                Console.WriteLine($"AUDIT INVOICEINTEGRITY snapshot mismatch table={table.Name} row={row} field={field.Name}");
                }
                throw new InvalidOperationException("Invoice integrity whole-database snapshot differs from permitted changes.");
            }
        }
        void Audits(InvoiceIntegrityState before, InvoiceIntegrityState after, params (string Action, string Route)[] expected)
        {
            var added = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray();
            if (added.Length != expected.Length || JsonSerializer.Serialize(before.Audits) != JsonSerializer.Serialize(after.Audits.Where(x => added.All(y => y.Id != x.Id))))
                throw new InvalidOperationException("Invoice integrity success audit count/history differs.");
            foreach (var (action, route) in expected)
            {
                var matches = added.Where(x => x.Action == action && x.AcademyId == academy && x.ActorUserId == actor &&
                    JsonSerializer.Deserialize<JsonElement>(x.MetadataJson!).GetProperty("Route").GetString() == route).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("Invoice integrity audit actor/tenant/action/route differs.");
            }
        }
        async Task Register(Guid id, decimal paid, decimal adjusted, string status)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, root + "/invoices"); request.Headers.Authorization = new("Bearer", token);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, HttpStatusCode.OK, "integrity register");
            var row = (await response.Content.ReadFromJsonAsync<InvoiceSummary[]>())!.Single(x => x.Id == id);
            if (row.Status != status || row.PaidAmount != paid || row.AdjustedAmount != adjusted || row.Balance != 1000m - paid - adjusted)
                throw new InvalidOperationException("Invoice integrity register/SQL ledger parity differs.");
        }
        await Seed(123m, "PartiallyPaid", sentinel: true); // Positive foreign sentinel in every whole-database comparison.
        var pairs = 0;
        foreach (var mode in new[] { "collection", "void", "approval", "restore" })
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await Task.Delay(650); // Respect the unchanged global limiter; no test override/retry.
            var fixture = await Seed(mode is "collection" or "restore" ? 0m : 600m,
                mode is "collection" or "restore" ? "Issued" : "PartiallyPaid", proposal: mode == "approval");
            if (mode == "restore")
            {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                fixture.Payment = new Payment { AcademyId = academy, InvoiceId = fixture.Invoice.Id, Amount = 600m,
                    Status = "Voided", Reference = "Keep payment reference", ReconciliationReference = "Old evidence", ReconciledAtUtc = DateTime.UtcNow };
                db.Add(fixture.Payment); await db.SaveChangesAsync();
            }
            var before = await State();
            var requested = mode is "collection" or "restore" ? "Issued" : "PartiallyPaid";
            var writePath = mode switch { "collection" => root + "/payments", "approval" => $"{root}/finance-adjustments/{fixture.Adjustment!.Id}/approval",
                "void" => $"{root}/payments/{fixture.Payment!.Id}/status", _ => $"{root}/payments/{fixture.Payment!.Id}/reconcile" };
            object body = mode switch { "collection" => new { invoiceId = fixture.Invoice.Id, amount = 600m, reference = "QA-INVOICE-INTEGRITY" },
                "approval" => new { approve = true, notes = "QA-approved" }, "void" => new { status = "Voided" }, _ => new { reference = "QA-RESTORED" } };
            // Hold the exact invoice externally until BOTH HTTP commands are observed
            // waiting in SQL. A Task.WhenAll launch alone is not overlap evidence.
            await using var blocker = await OpenInvoiceIntegrityAdminAsync(manifest);
            await using var transaction = (SqlTransaction)await blocker.BeginTransactionAsync();
            await using (var hold = new SqlCommand("SELECT [Id] FROM [Invoices] WITH (UPDLOCK,HOLDLOCK) WHERE [AcademyId]=@academy AND [Id]=@id", blocker, transaction))
            { hold.Parameters.AddWithValue("@academy", academy); hold.Parameters.AddWithValue("@id", fixture.Invoice.Id); if ((Guid)(await hold.ExecuteScalarAsync())! != fixture.Invoice.Id) throw new InvalidOperationException("Invoice lock fixture missing."); }
            var statusTask = Send(HttpMethod.Patch, StatusPath(fixture.Invoice.Id), new { status = requested });
            var writeTask = Send(mode == "collection" ? HttpMethod.Post : HttpMethod.Patch, writePath, body);
            Exception? overlapFailure = null;
            try { await RequireInvoiceIntegrityWaitersAsync(manifest, blocker.ServerProcessId); }
            catch (Exception failure) { overlapFailure = failure; }
            finally { await transaction.RollbackAsync(); }
            var results = await Task.WhenAll(statusTask, writeTask);
            try
            {
                if (overlapFailure is not null)
                {
                    Console.WriteLine($"AUDIT INVOICEINTEGRITY diagnostic-only overlap not accepted; drained HTTP={(int)results[0].StatusCode}/{(int)results[1].StatusCode}");
                    throw new InvalidOperationException("SQL overlap coordination failed; responses drained, not accepted.", overlapFailure);
                }
                var statusResult = results[0]; var writeResult = results[1];
                if (statusResult.StatusCode is not (HttpStatusCode.OK or HttpStatusCode.BadRequest)) throw new InvalidOperationException("Status race expected200/400, including no429.");
                RequireFinanceStatus(writeResult, mode == "collection" ? HttpStatusCode.Created : HttpStatusCode.OK, "concurrent ledger operation");
                if (statusResult.StatusCode == HttpStatusCode.BadRequest && (await statusResult.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString() != "Invoice status must match the collected payments and adjusted balance.")
                    throw new InvalidOperationException("Race rejection message differs.");
                var after = await State(); var expected = JsonSerializer.Deserialize<InvoiceIntegrityState>(JsonSerializer.Serialize(before))!;
                var invoice = expected.Invoices.Single(x => x.Id == fixture.Invoice.Id);
                invoice.Status = mode switch { "void" => "Issued", "approval" => "Paid", _ => "PartiallyPaid" };
                if (mode == "collection")
                {
                    var added = after.Payments.Where(x => before.Payments.All(y => y.Id != x.Id)).Single();
                    var summary = (await writeResult.Content.ReadFromJsonAsync<PaymentSummary>())!;
                    if (added.Id != summary.Id || added.InvoiceId != invoice.Id || added.AcademyId != academy || added.Amount != 600m || added.Status != "Completed" ||
                        added.Currency != invoice.Currency || added.Method != "Offline" || added.Reference != "QA-INVOICE-INTEGRITY" || added.ReconciliationReference is not null || added.ReconciledAtUtc is not null)
                        throw new InvalidOperationException("Concurrent collection row differs.");
                    expected = expected with { Payments = expected.Payments.Append(added).OrderBy(x => x.Id).ToArray() };
                }
                else if (mode == "approval")
                {
                    invoice.AdjustedAmount = 400m;
                    var adjustment = expected.Adjustments.Single(x => x.Id == fixture.Adjustment!.Id);
                    var stored = after.Adjustments.Single(x => x.Id == adjustment.Id);
                    if (stored.ApprovedAtUtc is null || stored.AppliedAtUtc != stored.ApprovedAtUtc) throw new InvalidOperationException("Approval timestamp invariant differs.");
                    adjustment.Status = "Approved"; adjustment.ApprovalNotes = "QA-approved"; adjustment.AppliedAtUtc = stored.AppliedAtUtc; adjustment.ApprovedAtUtc = stored.ApprovedAtUtc;
                }
                else
                {
                    var payment = expected.Payments.Single(x => x.Id == fixture.Payment!.Id);
                    payment.Status = mode == "void" ? "Voided" : "Reconciled";
                    if (mode == "restore")
                    {
                        payment.ReconciliationReference = "QA-RESTORED";
                        payment.ReconciledAtUtc = after.Payments.Single(x => x.Id == payment.Id).ReconciledAtUtc;
                        if (payment.ReconciledAtUtc is null) throw new InvalidOperationException("Restored evidence timestamp absent.");
                    }
                }
                Exact(expected with { Audits = after.Audits }, after);
                var auditExpectations = new List<(string, string)> { (mode == "collection" ? "POST Payments" : mode == "approval" ? "PATCH FinanceAdjustments" : "PATCH Payments", writePath) };
                if (statusResult.StatusCode == HttpStatusCode.OK) auditExpectations.Add(("PATCH Invoices", StatusPath(invoice.Id)));
                Audits(before, after, auditExpectations.ToArray());
                await Register(invoice.Id, mode == "void" ? 0m : 600m, mode == "approval" ? 400m : 0m, invoice.Status);
                Console.WriteLine($"PASS: INVOICEINTEGRITY pair={++pairs} mode={mode} attempt={attempt} SQLwaiters=2 statuses={(int)statusResult.StatusCode}/{(int)writeResult.StatusCode} invoice={invoice.Status} exactRowsAuditForeign=true no429=true");
            }
            finally { foreach (var result in results) result.Dispose(); }
        }
        var controls = 0;
        foreach (var (paid, original, target) in new[] { (0m, "Issued", "Overdue"), (600m, "Issued", "PartiallyPaid"), (1000m, "Issued", "Paid"), (600m, "PartiallyPaid", "Cancelled") })
        {
            var fixture = await Seed(paid, original); var before = await State();
            await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                using var failed = await Send(HttpMethod.Patch, StatusPath(fixture.Invoice.Id), new { status = target });
                RequireFinanceStatus(failed, HttpStatusCode.InternalServerError, "invoice status audit rollback");
                Exact(before, await State());
                if (paid == 0m)
                {
                    using var rejected = await Send(HttpMethod.Patch, StatusPath(fixture.Invoice.Id), new { status = "Paid" });
                    RequireFinanceStatus(rejected, HttpStatusCode.BadRequest, "invalid status with audit denied"); Exact(before, await State()); controls++;
                }
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }
            using var recovered = await Send(HttpMethod.Patch, StatusPath(fixture.Invoice.Id), new { status = target });
            RequireFinanceStatus(recovered, HttpStatusCode.OK, "invoice status recovery");
            var after = await State(); var expected = JsonSerializer.Deserialize<InvoiceIntegrityState>(JsonSerializer.Serialize(before))!;
            expected.Invoices.Single(x => x.Id == fixture.Invoice.Id).Status = target;
            Exact(expected with { Audits = after.Audits }, after); Audits(before, after, ("PATCH Invoices", StatusPath(fixture.Invoice.Id)));
            await Register(fixture.Invoice.Id, paid, 0m, target); controls += 2;
            Console.WriteLine($"PASS: INVOICEINTEGRITY audit target={target} HTTP500/noWrite then200/oneSuccessAudit exactForeign=true no429=true");
        }
        Console.WriteLine($"PASS: INVOICEINTEGRITY {pairs}/20 SQL-overlapped pairs +{controls}/9 audit rollback/rejection/recovery controls; no application changes. Restoration/cancellation policy, Platform Owner, browser/device/critical/release remain OPEN.");
    }

    private static async Task<SqlConnection> OpenInvoiceIntegrityAdminAsync(QaRunManifest manifest)
    {
        var connection = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD") ?? throw new InvalidOperationException("QA credential absent.")));
        try
        {
            await connection.OpenAsync(); var marker = await ReadMarkerAsync(connection, manifest);
            if (marker.RunId != manifest.RunId || marker.TokenDigest != manifest.TokenDigest) throw new InvalidOperationException("Refused invoice integrity coordination: wrong owned marker.");
            return connection;
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static async Task RequireInvoiceIntegrityWaitersAsync(QaRunManifest manifest, int blocker)
    {
        await using var monitor = await OpenInvoiceIntegrityAdminAsync(manifest);
        for (var poll = 0; poll < 150; poll++)
        {
            // A queued U-lock can report another waiting request as its immediate
            // blocker. Require BOTH real invoice commands in this lock chain,
            // not necessarily two direct children of the external lock holder.
            await using var command = new SqlCommand("WITH chain AS (SELECT @blocker AS session_id UNION ALL SELECT CAST(r.session_id AS int) FROM sys.dm_exec_requests r INNER JOIN chain c ON r.blocking_session_id=c.session_id WHERE r.database_id=DB_ID()) SELECT COUNT(DISTINCT r.session_id),COUNT(DISTINCT CASE WHEN r.blocking_session_id=@blocker THEN r.session_id END) FROM chain c INNER JOIN sys.dm_exec_requests r ON r.session_id=c.session_id CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.session_id<>@blocker AND r.database_id=DB_ID() AND r.wait_type LIKE 'LCK_M_%' AND t.text LIKE '%Invoices%' AND t.text LIKE '%UPDLOCK%' AND t.text LIKE '%HOLDLOCK%' OPTION(MAXRECURSION 30)", monitor);
            command.Parameters.AddWithValue("@blocker", blocker);
            await using (var reader = await command.ExecuteReaderAsync())
            {
                await reader.ReadAsync();
                if (reader.GetInt32(0) == 2)
                {
                    Console.WriteLine($"PASS: INVOICEINTEGRITY overlapProof chainWaiters=2 directWaiters={reader.GetInt32(1)} root={blocker}");
                    return;
                }
            }
            await Task.Delay(100);
        }
        await using (var diagnostic = new SqlCommand("SELECT r.session_id,r.blocking_session_id,r.wait_type,CASE WHEN t.text LIKE '%Invoices%' AND t.text LIKE '%UPDLOCK%' AND t.text LIKE '%HOLDLOCK%' THEN 1 ELSE 0 END FROM sys.dm_exec_requests r CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.database_id=DB_ID() AND r.wait_type LIKE 'LCK_M_%'", monitor))
        await using (var reader = await diagnostic.ExecuteReaderAsync())
            while (await reader.ReadAsync()) Console.WriteLine($"AUDIT INVOICEINTEGRITY diagnostic-only waiter={reader.GetInt16(0)} blocker={reader.GetInt16(1)} root={blocker} wait={reader.GetString(2)} invoiceLock={reader.GetInt32(3)}; not acceptance");
        throw new InvalidOperationException("Both HTTP invoice-lock waiters were not observed in owned SQL within15s; no overlap acceptance.");
    }
}
