using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task<InvoiceIntegrityState> InvoiceConsumerStateAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return new((await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
            (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
            (await db.FinanceAdjustments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
            (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray());
    }

    private static void InvoiceConsumerExact(object expected, object actual)
    {
        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(expected), JsonSerializer.SerializeToElement(actual)))
            throw new InvalidOperationException("Invoice consumer full finance snapshot changed outside the permitted operation.");
    }

    private static async Task VerifyInvoiceConsumerBrowserAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, student, actor; var invoices = new List<Guid>(); var displayInvoices = new List<Guid>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            var identity = scope.ServiceProvider.GetRequiredService<AcademyDesk.Api.Data.IdentityDbContext>();
            actor = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            for (var i = 0; i < 4; i++)
            {
                var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = $"QA-LINKED-{i}",
                    TotalAmount = 1000m, Status = "PartiallyPaid", DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };
                db.Invoices.Add(invoice); invoices.Add(invoice.Id);
                db.Payments.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled",
                    Reference = "Keep reference", ReconciliationReference = "Keep linked reconciliation", ReconciledAtUtc = DateTime.UtcNow });
            }
            foreach (var fixture in new[] { ("CENT", 200m, 799.99m, "PartiallyPaid"), ("ADJUSTED", 1000m, 0m, "Paid"), ("SETTLED", 0m, 1000m, "Paid") })
            {
                var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = $"QA-DISPLAY-{fixture.Item1}",
                    TotalAmount = 1000m, AdjustedAmount = fixture.Item2, Status = fixture.Item4, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)) };
                db.Invoices.Add(invoice); displayInvoices.Add(invoice.Id);
                if (fixture.Item3 > 0m) db.Payments.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = fixture.Item3, Status = "Reconciled" });
            }
            // Positive foreign sentinel makes every whole-database comparison meaningful.
            var foreignStudent = await db.Students.SingleAsync(x => x.AcademyId != academy);
            var foreign = new Invoice { AcademyId = foreignStudent.AcademyId, StudentId = foreignStudent.Id,
                InvoiceNumber = "QA-LINKED-FOREIGN", TotalAmount = 1000m, Status = "PartiallyPaid" };
            db.Add(foreign); db.Payments.Add(new Payment { AcademyId = foreign.AcademyId, InvoiceId = foreign.Id, Amount = 123m, Status = "Completed" });
            await db.SaveChangesAsync();
        }
        var root = $"/api/academies/{academy}"; var failures = new ConcurrentQueue<string>(); var writes = 0; var rejected = 0; var reads = 0;
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build(); using var pacing = new SemaphoreSlim(1, 1);
        Console.WriteLine("PASS: INVOICE CONSUMERS waiting an unchanged production limiter window before linked checks.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var route = context.Request.Path.ToString();
            var statusWrite = context.Request.Method == "PATCH" && invoices.Any(id => route == root + $"/invoices/{id}/status");
            var paymentWrite = context.Request.Method == "POST" && route == root + "/payments";
            var login = context.Request.Method == "POST" && route == "/api/auth/login";
            if (context.Request.Method is not ("GET" or "OPTIONS") && !statusWrite && !paymentWrite && !login) { context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                // Serialize and pace only the QA relay, never disable/override production limiting.
                await Task.Delay(650, context.RequestAborted);
                var before = await InvoiceConsumerStateAsync(factory); var body = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
                Guid? invoiceId = null; decimal amount = 0; string? requestedStatus = null;
                if (statusWrite) { invoiceId = invoices.Single(id => route == root + $"/invoices/{id}/status"); requestedStatus = JsonDocument.Parse(body).RootElement.GetProperty("status").GetString(); }
                if (paymentWrite)
                {
                    using var parsed = JsonDocument.Parse(body); invoiceId = parsed.RootElement.GetProperty("invoiceId").GetGuid(); amount = parsed.RootElement.GetProperty("amount").GetDecimal();
                    if (!invoices.Contains(invoiceId.Value) || amount != 400m) { context.Response.StatusCode = 400; return; }
                }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (body.Length > 0) request.Content = new StringContent(body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null) { request.Content.Headers.Remove(header.Key); request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()); }
                }
                using var response = await client.SendAsync(request, context.RequestAborted); var bytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
                var after = await InvoiceConsumerStateAsync(factory);
                if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new InvalidOperationException("HTTP429 not accepted.");
                if (statusWrite || paymentWrite)
                {
                    Interlocked.Increment(ref writes);
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        if (!statusWrite || requestedStatus != "Paid") throw new InvalidOperationException("Unexpected rejected linked write.");
                        InvoiceConsumerExact(before, after); Interlocked.Increment(ref rejected);
                    }
                    else
                    {
                        if (response.StatusCode != (paymentWrite ? HttpStatusCode.Created : HttpStatusCode.OK)) throw new InvalidOperationException("Linked write failed.");
                        var expectedStatus = paymentWrite ? "Paid" : requestedStatus?.Trim().Equals("paid", StringComparison.OrdinalIgnoreCase) == true ? "Paid" : "PartiallyPaid";
                        var expectedInvoices = before.Invoices.Select(x => { if (x.Id == invoiceId) x.Status = expectedStatus!; return x; }).ToArray();
                        var newPayments = after.Payments.Where(x => before.Payments.All(y => y.Id != x.Id)).ToArray();
                        if (paymentWrite)
                        {
                            using var result = JsonDocument.Parse(bytes); var payment = newPayments.Single();
                            if (payment.Id != result.RootElement.GetProperty("id").GetGuid() || payment.AcademyId != academy || payment.InvoiceId != invoiceId ||
                                payment.Amount != amount || payment.Currency != "INR" || payment.Method != "Offline" || payment.Status != "Completed" || payment.Reference is not null || payment.ReconciliationReference is not null || payment.ReconciledAtUtc is not null)
                                throw new InvalidOperationException("Linked stored payment differs from the submitted contract.");
                        }
                        else if (newPayments.Length != 0) throw new InvalidOperationException("Status write added a payment.");
                        var newAudits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray(); var audit = newAudits.Single();
                        if (audit.AcademyId != academy || audit.ActorUserId != actor || audit.Action != (paymentWrite ? "POST Payments" : "PATCH Invoices") ||
                            JsonDocument.Parse(audit.MetadataJson!).RootElement.GetProperty("Route").GetString() != route) throw new InvalidOperationException("Linked audit actor/tenant/action/route differs.");
                        InvoiceConsumerExact(before with { Invoices = expectedInvoices, Payments = before.Payments.Concat(newPayments).OrderBy(x => x.Id).ToArray(), Audits = before.Audits.Concat(newAudits).OrderBy(x => x.Id).ToArray() }, after);
                    }
                }
                else { InvoiceConsumerExact(before, after); Interlocked.Increment(ref reads); }
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers)) if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) context.Response.Headers[header.Key] = header.Value.ToArray();
                Console.WriteLine($"PASS: INVOICE LINKED HTTP {context.Request.Method} {route} {(int)response.StatusCode}; fresh full finance snapshot verified");
                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
            catch (Exception error) { failures.Enqueue(error.Message); throw; }
            finally { pacing.Release(); }
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/invoice-consumers-live.cjs")); start.ArgumentList.Add(bridge.Urls.Single()); start.ArgumentList.Add(academy.ToString()); start.ArgumentList.Add(student.ToString());
        foreach (var id in invoices.Concat(displayInvoices)) start.ArgumentList.Add(id.ToString());
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Linked invoice browser did not start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        finally { await bridge.StopAsync(); }
        Console.Write(await output); var errorText = await errors;
        if (process.ExitCode != 0 || !failures.IsEmpty || writes != 16 || rejected != 4 || reads == 0)
            throw new InvalidOperationException("Linked invoice consumer failed: " + errorText + string.Join("; ", failures));
        var final = await InvoiceConsumerStateAsync(factory);
        foreach (var id in invoices)
        {
            var invoice = final.Invoices.Single(x => x.Id == id); var payments = final.Payments.Where(x => x.InvoiceId == id).ToArray();
            if (invoice.Status != "Paid" || payments.Length != 2 || payments.Sum(x => x.Amount) != 1000m || payments.Count(x => x.Status == "Reconciled") != 1 || payments.Count(x => x.Status == "Completed") != 1)
                throw new InvalidOperationException("Linked final invoice ledger differs.");
        }
        Console.WriteLine($"PASS: INVOICE CONSUMERS four linked journeys / three real pages / 16 mutations (12 success, four400 no-write), {reads} non-mutation snapshot controls; eight target payment rows, collected1000 each; no429; physical devices NOT RUN.");
    }
}
