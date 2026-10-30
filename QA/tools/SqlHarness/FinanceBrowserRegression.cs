using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Called only after Main verifies the fresh Docker/database ownership and runtime grants.
    // Browser requests pass through real Program/Identity/SQL; this host has no seed/control routes.
    private static async Task ServeFinanceBrowserAsync(QaRunManifest manifest)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and origin required.");
        try
        {
            using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) });
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
            if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Finance browser backend preflight failed.");
            RecordRuntimeEndpoints(factory, manifest);
            await VerifyTenantIsolationAsync(factory, client);
            Guid academy, student, admin;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
                student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
                admin = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
            }
            var finance = await CreateAccessActorAsync(factory, "qa-browser-finance@example.invalid", "FinanceUser", academy);
            var cases = new List<FinanceBrowserCase>();
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                foreach (var fixture in new[] {
                    (Name: "desktop-admin", Width: 1440, Height: 900, Email: "qa-admin-a@example.invalid", Actor: admin),
                    (Name: "phone-finance", Width: 390, Height: 844, Email: "qa-browser-finance@example.invalid", Actor: finance) })
                {
                    var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-BROWSER-" + fixture.Name,
                        TotalAmount = 1000m, Currency = "INR", Status = "PartiallyPaid" };
                    db.Invoices.Add(invoice);
                    db.Payments.AddRange(
                        new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Completed" },
                        new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 200m, Status = "Reconciled", ReconciliationReference = "SYNTHETIC-SEED", ReconciledAtUtc = DateTime.UtcNow },
                        new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 999m, Status = "Voided" });
                    cases.Add(new(fixture.Name, new(fixture.Width, fixture.Height), fixture.Email, "Synthetic!39Ab", invoice.InvoiceNumber,
                        invoice.Id, 100m, 100m, "INR", fixture.Actor));
                }
                db.Invoices.Add(new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-BROWSER-USD", TotalAmount = 10m, Currency = "USD" });
                await db.SaveChangesAsync();
            }
            client.DefaultRequestHeaders.Authorization = null;
            using (var anonymous = await client.GetAsync($"/api/academies/{academy}/payments"))
                if (anonymous.StatusCode != HttpStatusCode.Unauthorized) throw new InvalidOperationException("Browser bridge retained a fixture authentication header.");
            var fixturePath = Path.Combine(manifest.Root, "finance-browser-fixture.json");
            await File.WriteAllTextAsync(fixturePath, JsonSerializer.Serialize(new {
                apiOrigin = $"http://127.0.0.1:{port}", webOrigin = origin.GetLeftPart(UriPartial.Authority), cases
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                    context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port) { context.Response.StatusCode = 400; return; }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                {
                    if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
                Console.WriteLine($"FINANCEBROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            try
            {
                using var process = new Process { StartInfo = new ProcessStartInfo("node") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false } };
                process.StartInfo.ArgumentList.Add(Path.GetFullPath("QA/tools/finance-critical-browser.cjs"));
                process.StartInfo.Environment["QA_FINANCE_BROWSER_FIXTURE"] = fixturePath;
                if (!process.Start()) throw new InvalidOperationException("Finance browser runner failed to start.");
                // Only the runner's sanitized bounded observations are public evidence; never echo HTTP headers/tokens.
                async Task Drain(StreamReader reader) { while (await reader.ReadLineAsync() is { } line) if (line.StartsWith("FINANCEBROWSER ", StringComparison.Ordinal) && line.Length <= 2000) Console.WriteLine(line); }
                var stdout = Drain(process.StandardOutput); var stderr = Drain(process.StandardError);
                using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8));
                try { await process.WaitForExitAsync(deadline.Token); }
                catch { if (!process.HasExited) process.Kill(entireProcessTree: true); await Task.WhenAll(stdout, stderr); throw; }
                await Task.WhenAll(stdout, stderr);
                if (process.ExitCode != 0) throw new InvalidOperationException("Finance real browser acceptance failed; inspect sanitized owned artifacts.");
            }
            finally { await bridge.StopAsync(); }
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                foreach (var item in cases)
                {
                    var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == item.InvoiceId && x.AcademyId == academy);
                    var rows = await db.Payments.AsNoTracking().Where(x => x.InvoiceId == item.InvoiceId && x.AcademyId == academy).ToArrayAsync();
                    if (rows.Length != 4 || rows.Count(x => x.Amount == item.PartialAmount && x.Status == "Completed" && x.Method == "UPI" && x.Currency == item.Currency) != 1 ||
                        invoice.TotalAmount - invoice.AdjustedAmount - rows.Where(x => x.Status is "Completed" or "Reconciled").Sum(x => x.Amount) != item.ExpectedBalanceAfter || invoice.Status != "PartiallyPaid")
                        throw new InvalidOperationException("Browser payment count/currency/status/independent SQL balance failed.");
                    var audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == item.ActorUserId && x.Action == "POST Payments" && x.EntityType == "Payments").ToArrayAsync();
                    if (audits.Length != 1 || audits[0].MetadataJson is null || !audits[0].MetadataJson!.Contains($"/api/academies/{academy}/payments", StringComparison.Ordinal))
                        throw new InvalidOperationException("Browser payment attributed audit failed.");
                    Console.WriteLine($"FINANCEBROWSER SQL PASS case={item.Name} payment-rows=4 collected=900 balance=100 one-attributed-audit=true");
                }
                if (await db.Payments.CountAsync() != 8 || await db.Invoices.CountAsync() != 3)
                    throw new InvalidOperationException("Finance browser fixture wrote unexpected financial rows.");
            }
            Console.WriteLine("FINANCEBROWSER FINAL PASS two real UI logins; Admin/FinanceUser partial payments, reconciled/voided arithmetic, same-tick duplicate admission and independent SQL/audit; remaining finance/device acceptance stays open.");
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
    private sealed record FinanceBrowserViewport(int Width, int Height);
    private sealed record FinanceBrowserCase(string Name, FinanceBrowserViewport Viewport, string Email, string Password,
        string InvoiceNumber, Guid InvoiceId, decimal PartialAmount, decimal ExpectedBalanceAfter, string Currency, Guid ActorUserId);
}
