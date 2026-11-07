using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task ServePaymentRecoveryBrowserAsync(QaRunManifest manifest)
    {
        const string api = "http://127.0.0.1:54999";
        const string origin = "http://127.0.0.1:54998";
        const string reference = "C9 payment recovery";
        var run = manifest.RunId.ToString("N");
        using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin });
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException("Owned payment browser backend preflight failed.");

        Guid academy, admin;
        var invoices = new List<Invoice>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            var student = await db.Students.Where(x => x.AcademyId == academy && x.FirstName == "Isolated-A").Select(x => x.Id).SingleAsync();
            admin = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            foreach (var number in new[] { "C9-COMMITTED", "C9-UNSENT" })
            {
                var invoice = new Invoice { AcademyId = academy, StudentId = student, InvoiceNumber = number, TotalAmount = 1000m,
                    Status = "PartiallyPaid", DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) };
                invoices.Add(invoice);
                db.AddRange(invoice, new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 600m, Status = "Reconciled",
                    ReconciledAtUtc = DateTime.UtcNow, ReconciliationReference = "C9 opening reconciliation" });
            }
            await db.SaveChangesAsync();
        }

        await File.WriteAllTextAsync(Path.Combine(manifest.Root, "payment-recovery-fixture.json"), JsonSerializer.Serialize(new
        {
            run, origin, api, academy, reference, invoices = invoices.Select(x => new { id = x.Id, number = x.InvoiceNumber })
        }));
        var state = await ManualStateAsync(factory);
        var completed = 0;
        var failed = false;
        var paymentPath = $"/api/academies/{academy}/payments";
        using var checkLock = new SemaphoreSlim(1);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(api);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
        await using var bridge = builder.Build();
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) ||
                context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != 54999)
            {
                context.Response.StatusCode = 400;
                return;
            }
            if (context.Request.Path == "/__qa/payment-recovery")
            {
                if (context.Request.Method != "POST" || context.Request.ContentLength is not (> 0 and <= 2048))
                {
                    context.Response.StatusCode = 400;
                    return;
                }
                await checkLock.WaitAsync(context.RequestAborted);
                try
                {
                    var input = await context.Request.ReadFromJsonAsync<JsonObject>(context.RequestAborted);
                    if (input?["run"]?.GetValue<string>() != run || failed || completed >= 4 || input["phase"]?.GetValue<int>() != completed + 1)
                        throw new InvalidOperationException("Unexpected payment recovery phase.");
                    var actual = await ManualStateAsync(factory);
                    if (completed == 0)
                    {
                        var normalized = actual.DeepClone().AsObject();
                        var payment = ManualAdded(state, normalized, "payments", 1).Single();
                        if (!Guid.TryParse(input["paymentId"]?.GetValue<string>(), out var paymentId) ||
                            payment["Id"]!.GetValue<Guid>() != paymentId || payment["InvoiceId"]!.GetValue<Guid>() != invoices[0].Id ||
                            payment["AcademyId"]!.GetValue<Guid>() != academy || payment["Amount"]!.GetValue<decimal>() != 400m ||
                            payment["Status"]!.GetValue<string>() != "Completed" || payment["Currency"]!.GetValue<string>() != "INR" ||
                            payment["Method"]!.GetValue<string>() != "UPI" || payment["Reference"]?.GetValue<string>() != reference)
                            throw new InvalidOperationException("Committed payment differs from the lost 201 response or submitted form.");
                        var invoice = ManualRow(normalized, "invoices", invoices[0].Id);
                        if (invoice["Status"]!.GetValue<string>() != "Paid") throw new InvalidOperationException("Invoice must be paid at exactly 1000.");
                        invoice["Status"] = "PartiallyPaid";
                        var audit = ManualAdded(state, normalized, "audits", 1).Single();
                        if (audit["ActorUserId"]!.GetValue<Guid>() != admin || audit["AcademyId"]!.GetValue<Guid>() != academy ||
                            audit["Action"]!.GetValue<string>() != "POST Payments" ||
                            !audit["MetadataJson"]!.GetValue<string>().Contains(paymentPath, StringComparison.Ordinal))
                            throw new InvalidOperationException("Payment audit does not match the signed-in actor and route.");
                        ManualSame(state, normalized, "other invoice, existing reconciled rows and unrelated SQL state");
                    }
                    else
                    {
                        ManualSame(state, actual, "payment recovery cannot replay or change SQL state");
                    }
                    state = actual;
                    completed++;
                    Console.WriteLine($"C9PAYMENT SQL PASS phase={completed}");
                    await context.Response.WriteAsJsonAsync(new { phase = completed }, context.RequestAborted);
                }
                catch (Exception error)
                {
                    failed = true;
                    Console.WriteLine("C9PAYMENT SQL FAILED " + error.Message);
                    context.Response.StatusCode = 500;
                }
                finally { checkLock.Release(); }
                return;
            }

            // Allow real API reads, Identity, and the specific payment operation only.
            var method = context.Request.Method;
            if (!context.Request.Path.StartsWithSegments("/api") ||
                !(method is "GET" or "OPTIONS" || method == "POST" &&
                    (context.Request.Path == paymentPath || context.Request.Path == "/api/auth/login" || context.Request.Path == "/api/auth/refresh")))
            {
                context.Response.StatusCode = 405;
                return;
            }
            await ForwardFinanceBrowserRequestAsync(context, client);
        });

        await bridge.StartAsync();
        var stop = Path.Combine(manifest.Root, "stop-payment-recovery");
        Console.WriteLine($"C9PAYMENT READY run={run} api=54999 origin=54998 stop={stop}");
        var deadline = DateTime.UtcNow.AddMinutes(5);
        while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(500);
        await bridge.StopAsync();
        if (failed || completed != 4 || !File.Exists(stop))
            throw new InvalidOperationException($"Incomplete payment recovery verification: {completed}/4.");
        ManualSame(state, await ManualStateAsync(factory), "final payment recovery SQL state");
        Console.WriteLine("C9PAYMENT FINAL 4 verified UI/SQL phases.");
    }

    private static async Task ForwardFinanceBrowserRequestAsync(HttpContext context, HttpClient client)
    {
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
        if (context.Request.ContentLength > 0) request.Content = new StreamContent(context.Request.Body);
        foreach (var header in context.Request.Headers)
        {
            if (header.Key is "Host" or "Connection" or "Transfer-Encoding") continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            if (header.Key is "Transfer-Encoding" or "Connection") continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
