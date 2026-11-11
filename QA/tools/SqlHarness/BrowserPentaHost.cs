using System.Net;
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
    // Opt-in browser bridge; only the runner's disposable SQL and exact loopback origin.
    private static async Task ServePentaBrowserAsync(QaRunManifest manifest, Guid academyId, bool finance = false)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and origin required.");
        var mini = !finance && Environment.GetEnvironmentVariable("QA_PENTA_MINI") == "1";
        var payment = finance && Environment.GetEnvironmentVariable("QA_PENTA_PAYMENT_BROWSER") == "1";
        var issuance = finance && Environment.GetEnvironmentVariable("QA_PENTA_INVOICE_BROWSER") == "1";
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> {
            ["Penta:Enabled"] = "true", ["Penta:Mini:Enabled"] = mini.ToString(),
            ["Penta:FinanceReadEnabled"] = finance.ToString(), ["Penta:PaymentCollectionEnabled"] = payment.ToString(),
            ["Penta:InvoiceIssuanceEnabled"] = issuance.ToString(),
            ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        client.Timeout = TimeSpan.FromSeconds(140);
        PentaRequire(factory.PreflightPassed && (await client.GetAsync("/health")).StatusCode == HttpStatusCode.OK, "Browser backend preflight failed.");
        async Task<int[]> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return [await db.Students.CountAsync(), await db.Batches.CountAsync(), await db.PentaTasks.CountAsync(), await db.PentaExecutions.CountAsync(),
                await db.PentaApprovals.CountAsync(), await db.Expenses.CountAsync(), await db.PayrollPayouts.CountAsync(),
                await db.Invoices.CountAsync(), await db.Payments.CountAsync()];
        }
        var browserInvoice = payment ? await SeedBrowserPaymentAsync(factory, academyId) : Guid.Empty;
        var issuanceFixture = issuance ? await SeedBrowserInvoiceIssuanceAsync(factory, academyId) : null;
        var before = await Snapshot();
        var paymentAuditsBefore = payment ? await BrowserFinanceAuditCountAsync(factory, academyId, "PentaPaymentRecorded") : 0;
        var invoiceAuditsBefore = issuance ? await BrowserFinanceAuditCountAsync(factory, academyId, "PentaInvoiceIssued") : 0;
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
            { context.Response.StatusCode = 400; return; }
            // The optional payment fixture admits only the protected routes in this disposable academy.
            var miniPost = mini && context.Request.Method == "POST" &&
                context.Request.Path.StartsWithSegments($"/api/academies/{academyId:D}/penta/chat/conversations");
            var financePost = finance && context.Request.Method == "POST" &&
                context.Request.Path == $"/api/academies/{academyId:D}/penta/chat/finance";
            var paymentRequest = payment && context.Request.Method is ("PUT" or "POST") &&
                context.Request.Path.StartsWithSegments($"/api/academies/{academyId:D}/penta/payment-collection/conversations");
            var invoiceRoot = $"/api/academies/{academyId:D}/penta/invoice-issuance";
            var invoiceRequest = issuance && (context.Request.Method is ("PUT" or "POST") &&
                context.Request.Path.StartsWithSegments(invoiceRoot + "/conversations") ||
                context.Request.Method == "POST" && context.Request.Path == invoiceRoot + "/options");
            if (context.Request.Method is not ("GET" or "OPTIONS") &&
                !(context.Request.Method == "POST" && context.Request.Path == "/api/auth/login") &&
                !miniPost && !financePost && !paymentRequest && !invoiceRequest)
            { context.Response.StatusCode = 405; return; }
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0) request.Content = new StreamContent(context.Request.Body);
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
            Console.WriteLine($"PENTA BROWSER HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        });
        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, finance ? "stop-finance-chat-browser" : "stop-browser");
        Console.WriteLine($"PENTA BROWSER READY run={manifest.RunId:N} academy={academyId:N} api={port} origin={origin.Port} stop={stop}");
        var deadline = DateTime.UtcNow.AddHours(mini ? 4 : 0.5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
        await bridge.StopAsync();
        var after = await Snapshot();
        if (payment) before[^1]++;
        if (issuance) before[^2]++;
        PentaRequire(before.SequenceEqual(after), "Browser changed unexpected domain or AI execution rows.");
        if (payment)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == browserInvoice && x.AcademyId == academyId);
            var rows = await db.Payments.AsNoTracking().Where(x => x.InvoiceId == browserInvoice && x.AcademyId == academyId).ToArrayAsync();
            PentaRequire(invoice.Status == "Paid" && invoice.AdjustedAmount == 200m && rows.Length == 2 &&
                rows.Count(x => x is { Amount: 200m, Currency: "USD", Status: "Completed", Method: "Cash", Reference: null }) == 1 &&
                rows.Count(x => x is { Amount: 600m, Currency: "USD", Status: "Reconciled" }) == 1 &&
                await BrowserFinanceAuditCountAsync(factory, academyId, "PentaPaymentRecorded") == paymentAuditsBefore + 1,
                "Browser payment and receipt recovery did not leave one exact audited collection.");
            Console.WriteLine("PENTA PAYMENT BROWSER SQL PASS: one exact payment and one execution audit.");
        }
        if (issuanceFixture is { } expected)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.AcademyId == academyId && x.StudentId == expected.Student);
            PentaRequire(invoice is { Status: "Issued", Currency: "USD", TotalAmount: 132.01m } &&
                invoice.FeePlanId == expected.FeePlan &&
                !await db.Payments.AnyAsync(x => x.InvoiceId == invoice.Id) &&
                await BrowserFinanceAuditCountAsync(factory, academyId, "PentaInvoiceIssued") == invoiceAuditsBefore + 1,
                "Browser invoice recovery did not leave one exact audited invoice.");
            Console.WriteLine("PENTA INVOICE BROWSER SQL PASS: one exact invoice and one execution audit.");
        }
        if (!payment && !issuance) Console.WriteLine("PENTA BROWSER PASS: domain and AI execution row counts unchanged.");
    }

    private static async Task<InvoiceBrowserFixture> SeedBrowserInvoiceIssuanceAsync(QaApiFactory factory, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var student = new Student { AcademyId = academy, FirstName = "Invoice Browser", LastName = "Learner",
            StudentNumber = "QA-INV-BROWSER", Email = "private-qa-invoice@example.invalid", Phone = "5550000123" };
        var inactive = new Student { AcademyId = academy, FirstName = "Invoice Browser", LastName = "Inactive", IsActive = false };
        var plan = new FeePlan { AcademyId = academy, Name = "QA Browser Fee Plan", Amount = 132.01m, Currency = "USD" };
        db.AddRange(student, inactive, plan);
        await db.SaveChangesAsync();
        return new(student.Id, plan.Id);
    }

    private sealed record InvoiceBrowserFixture(Guid Student, Guid FeePlan);

    private static async Task<Guid> SeedBrowserPaymentAsync(QaApiFactory factory, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).FirstAsync();
        var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = "QA-BROWSER-PAYMENT",
            TotalAmount = 1000m, AdjustedAmount = 200m, Currency = "USD", Status = "PartiallyPaid",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-999)) };
        db.Add(invoice);
        db.Add(new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Currency = "USD", Status = "Reconciled" });
        await db.SaveChangesAsync();
        return invoice.Id;
    }

    private static async Task<int> BrowserFinanceAuditCountAsync(QaApiFactory factory, Guid academy, string action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return await db.AuditLogs.CountAsync(x => x.AcademyId == academy && x.Action == action);
    }
}
