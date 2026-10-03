using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // A real loopback API backed only by this run's disposable SQL database.
    private static async Task ServeBrowserPaymentsAsync(QaRunManifest manifest)
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
                throw new InvalidOperationException("Backend preflight failed.");
            await VerifyTenantIsolationAsync(factory, client);
            Guid academyId, studentId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                academyId = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
                studentId = await db.Students.Where(x => x.AcademyId == academyId && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"));
            var invoicesPath = $"/api/academies/{academyId}/invoices";
            var paymentsPath = $"/api/academies/{academyId}/payments";
            async Task<Guid> Invoice(string label)
            {
                using var response = await client.PostAsJsonAsync(invoicesPath, new { studentId, amount = 1000m });
                RequireFinanceStatus(response, HttpStatusCode.Created, label);
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                return json.RootElement.GetProperty("id").GetGuid();
            }
            async Task SeedReconciled(Guid invoiceId, string label)
            {
                using var payment = await client.PostAsJsonAsync(paymentsPath, new { invoiceId, amount = 600m, method = "UPI", reference = label });
                RequireFinanceStatus(payment, HttpStatusCode.Created, label + " payment");
                using var json = JsonDocument.Parse(await payment.Content.ReadAsStringAsync());
                var paymentId = json.RootElement.GetProperty("id").GetGuid();
                using var reconcile = await client.PatchAsJsonAsync($"{paymentsPath}/{paymentId}/reconcile", new { reference = label });
                RequireFinanceStatus(reconcile, HttpStatusCode.OK, label + " reconciliation");
            }
            var plainId = await Invoice("plain invoice");
            await SeedReconciled(plainId, "QA-BROWSER-PLAIN");
            await Task.Delay(1100);
            var adjustedId = await Invoice("adjusted invoice");
            var adjustmentsPath = $"/api/academies/{academyId}/finance-adjustments";
            using (var adjustment = await client.PostAsJsonAsync(adjustmentsPath, new { invoiceId = adjustedId, type = "Discount", amount = 200m, reason = "Synthetic browser QA" }))
            {
                RequireFinanceStatus(adjustment, HttpStatusCode.OK, "adjustment");
                using var json = JsonDocument.Parse(await adjustment.Content.ReadAsStringAsync());
                var adjustmentId = json.RootElement.GetProperty("id").GetGuid();
                using var approval = await client.PatchAsJsonAsync($"{adjustmentsPath}/{adjustmentId}/approval", new { approve = true, notes = "QA-BROWSER" });
                RequireFinanceStatus(approval, HttpStatusCode.OK, "approval");
            }
            await SeedReconciled(adjustedId, "QA-BROWSER-ADJUSTED");
            client.DefaultRequestHeaders.Authorization = null;

            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
                {
                    context.Response.StatusCode = 400;
                    return;
                }
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
                Console.WriteLine($"PAYPORTAL HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            var stop = Path.Combine(manifest.Root, "stop-browser");
            Console.WriteLine($"PAYPORTAL READY run={manifest.RunId:N} academy={academyId:N} plain={plainId:N} adjusted={adjustedId:N} api={port} origin={origin.Port} stop={stop}");
            var deadline = DateTime.UtcNow.AddMinutes(30);
            while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
            await bridge.StopAsync();
            using var finalScope = factory.Services.CreateScope();
            var finalDb = finalScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await finalDb.Invoices.AsNoTracking().Where(x => x.Id == plainId || x.Id == adjustedId).ToListAsync();
            var payments = await finalDb.Payments.AsNoTracking().Where(x => x.InvoiceId == plainId || x.InvoiceId == adjustedId).ToListAsync();
            if (rows.Count != 2 ||
                rows.Single(x => x.Id == plainId).Status != "Paid" || rows.Single(x => x.Id == adjustedId).Status != "Paid" ||
                rows.Single(x => x.Id == plainId).AdjustedAmount != 0m || rows.Single(x => x.Id == adjustedId).AdjustedAmount != 200m ||
                payments.Count(x => x.InvoiceId == plainId) != 2 || payments.Count(x => x.InvoiceId == adjustedId) != 2 ||
                payments.Where(x => x.InvoiceId == plainId && (x.Status == "Completed" || x.Status == "Reconciled")).Sum(x => x.Amount) != 1000m ||
                payments.Where(x => x.InvoiceId == adjustedId && (x.Status == "Completed" || x.Status == "Reconciled")).Sum(x => x.Amount) != 800m)
                throw new InvalidOperationException("Browser payment/adjustment SQL ledger does not match the accepted and rejected UI actions.");
            Console.WriteLine("PAYPORTAL SQL " + JsonSerializer.Serialize(rows.Select(x => new { x.Id, x.TotalAmount, x.AdjustedAmount, x.Status, collected = payments.Where(p => p.InvoiceId == x.Id && (p.Status == "Completed" || p.Status == "Reconciled")).Sum(p => p.Amount), paymentRows = payments.Count(p => p.InvoiceId == x.Id) })));
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
}
