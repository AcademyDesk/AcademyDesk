using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyInvoiceSettingsAsync(QaApiFactory factory, HttpClient client, bool baseline)
    {
        Guid academyA, academyB, studentA; AcademyFinanceSettings settings;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academy = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A"); academyA = academy.Id; academy.EnabledModulesJson = "[\"Finance\"]";
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            studentA = await db.Students.Where(x => x.AcademyId == academyA).Select(x => x.Id).SingleAsync();
            settings = new AcademyFinanceSettings { AcademyId = academyA, TaxRegistrationNumber = "QA-TAX", TaxLabel = "GST",
                InvoiceLogoUrl = "https://example.invalid/qa-logo.png", InvoiceSignatureUrl = "https://example.invalid/qa-sign.png",
                InvoiceAuthorityName = "Synthetic Finance", InvoiceAuthorityTitle = "Authorised signatory", InvoiceTemplateKey = "Modern",
                TaxRatePercent = 18m, DefaultPaymentTermsDays = 30, TaxInclusivePricing = true, PayslipTemplateKey = "Professional" };
            db.AddRange(settings, new AcademyFinanceSettings { AcademyId = academyB, TaxRegistrationNumber = "FOREIGN-TAX-MARKER", InvoiceAuthorityName = "FOREIGN-AUTHORITY-MARKER" });
            await db.SaveChangesAsync();
        }
        var root = $"/api/academies/{academyA}"; var route = root + "/invoices/document-settings"; var count = 0;
        var financeId = await CreateAccessActorAsync(factory, "qa-invoice-settings-finance@example.invalid", "FinanceUser", academyA);
        var financeToken = await LoginAsync(client, "qa-invoice-settings-finance@example.invalid", "Synthetic!39Ab");
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        async Task<string> Send(string label, string? token, string method, string url, HttpStatusCode expected, bool projection = false, bool defaults = false, string? theme = null)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = token is null ? null : new AuthenticationHeaderValue("Bearer", token);
            var before = JsonSerializer.Serialize(await GovernanceStateAsync(factory));
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (method != "GET") request.Content = JsonContent.Create(new { invoiceTemplateKey = "Minimal", taxRatePercent = 90m, academyId = academyB });
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, "invoice settings " + label);
            var json = await response.Content.ReadAsStringAsync();
            if (before != JsonSerializer.Serialize(await GovernanceStateAsync(factory))) throw new InvalidOperationException("Invoice settings read/rejection changed captured state: " + label);
            if (projection)
            {
                using var parsed = JsonDocument.Parse(json); var row = parsed.RootElement;
                var fields = new[] { "invoiceAuthorityName", "invoiceAuthorityTitle", "invoiceLogoUrl", "invoiceSignatureUrl", "invoiceTemplateKey", "taxLabel", "taxRegistrationNumber" };
                if (!row.EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(fields)) throw new InvalidOperationException("Settings projection widened");
                if (row.GetProperty("taxRegistrationNumber").GetString() != (defaults ? "" : "QA-TAX") || row.GetProperty("taxLabel").GetString() != "GST" ||
                    row.GetProperty("invoiceTemplateKey").GetString() != (defaults ? "Classic" : theme ?? "Modern")) throw new InvalidOperationException("Settings tax/template/default mismatch");
                foreach (var pair in new[] { ("invoiceAuthorityName", settings.InvoiceAuthorityName), ("invoiceAuthorityTitle", settings.InvoiceAuthorityTitle),
                    ("invoiceLogoUrl", settings.InvoiceLogoUrl), ("invoiceSignatureUrl", settings.InvoiceSignatureUrl) })
                    if (row.GetProperty(pair.Item1).GetString() != (defaults ? null : pair.Item2)) throw new InvalidOperationException("Settings invoice field mismatch");
                if (json.Contains("FOREIGN-", StringComparison.Ordinal)) throw new InvalidOperationException("Foreign branding leaked");
            }
            count++; Console.WriteLine($"INVOICESETTINGS CASE {label} PASS: HTTP={(int)expected}; captured state unchanged{(projection ? "; exact seven-field own/default invoice projection" : "")}."); return json;
        }
        foreach (var dependency in new[] { "academies", "invoices/student-options", "fee-plans", "invoices" })
            await Send("Finance-only/prerequisite/" + dependency, financeToken, "GET", dependency == "academies" ? "/api/academies" : root + "/" + dependency, HttpStatusCode.OK);
        await Send("Finance-only/governance-read-denied", financeToken, "GET", root + "/finance-governance/settings", HttpStatusCode.Forbidden);
        if (baseline)
        {
            await Send("new-route-missing", financeToken, "GET", route, HttpStatusCode.NotFound);
            Console.WriteLine("INVOICESETTINGS BASELINE REPRODUCED: Finance-only invoice prerequisites 200; required Governance settings 403; new invoice route absent 404."); return;
        }
        await Send("Finance-only/governance-write-denied", financeToken, "PUT", root + "/finance-governance/settings", HttpStatusCode.Forbidden);
        foreach (var actor in new[] { ("AcademyAdmin", true), ("Owner", true), ("FinanceUser", true), ("QA-InvoiceFinance", true),
            ("Teacher", false), ("Student", false), ("FrontDesk", false), ("Operations", false), ("QA-InvoiceStudents", false), ("QA-InvoiceNone", false) })
        {
            var email = "qa-setting-" + actor.Item1.ToLowerInvariant() + "@example.invalid";
            string token;
            if (actor.Item1 == "AcademyAdmin") token = adminToken;
            else if (actor.Item1 == "FinanceUser") token = financeToken;
            else
            {
                await CreateAccessActorAsync(factory, email, actor.Item1, academyA, actor.Item1 == "Student" ? studentA : null,
                    actor.Item1 == "QA-InvoiceFinance" ? "[\"finance.manage\"]" : actor.Item1 == "QA-InvoiceStudents" ? "[\"students.manage\"]" : "[]");
                token = await LoginAsync(client, email, "Synthetic!39Ab");
            }
            await Send(actor.Item1, token, "GET", route, actor.Item2 ? HttpStatusCode.OK : HttpStatusCode.Forbidden, actor.Item2);
        }
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" }) await Send("read-only/" + method, financeToken, method, route, HttpStatusCode.MethodNotAllowed);
        await Send("FinanceUser/foreign-route", financeToken, "GET", $"/api/academies/{academyB}/invoices/document-settings", HttpStatusCode.Forbidden);
        await Send("Admin/foreign-route", adminToken, "GET", $"/api/academies/{academyB}/invoices/document-settings", HttpStatusCode.Forbidden);
        await Send("anonymous", null, "GET", route, HttpStatusCode.Unauthorized);
        // No-settings is a pure default response, not insertion or a foreign fallback.
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Remove(await db.AcademyFinanceSettings.SingleAsync(x => x.Id == settings.Id)); await db.SaveChangesAsync(); }
        await Send("no-row/defaults", financeToken, "GET", route, HttpStatusCode.OK, true, true);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Add(settings); await db.SaveChangesAsync(); }
        foreach (var theme in new[] { "Classic", "Modern", "Minimal", "Formal" })
        {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.AcademyFinanceSettings.SingleAsync(x => x.Id == settings.Id)).InvoiceTemplateKey = theme; await db.SaveChangesAsync(); }
            await Send("template/" + theme, financeToken, "GET", route, HttpStatusCode.OK, true, theme: theme);
        }
        foreach (var modules in new[] { "[]", "[\"FinanceControls\"]", "[\"Finance\",\"FinanceControls\"]" })
        {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academyA)).EnabledModulesJson = modules; await db.SaveChangesAsync(); }
            await Send("modules/" + modules, financeToken, "GET", route, modules.Contains("\"Finance\"") ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
                modules.Contains("\"Finance\""), theme: "Formal");
        }
        await Send("both-modules/governance-retained", financeToken, "GET", root + "/finance-governance/settings", HttpStatusCode.OK);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academyA)).IsActive = false; await db.SaveChangesAsync(); }
        await Send("inactive-academy", financeToken, "GET", route, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academyA)).IsActive = true; await db.SaveChangesAsync(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>(); (await identity.Users.SingleAsync(x => x.Id == financeId)).IsActive = false; await identity.SaveChangesAsync(); }
        await Send("inactive-user", financeToken, "GET", route, HttpStatusCode.Forbidden);
        Console.WriteLine($"INVOICESETTINGS REGRESSION PASS: {count} bounded cases; existing Finance role/tenant/module/inactive gates; seven-field defaults/templates; read-only; Governance restrictions retained.");
    }
}
