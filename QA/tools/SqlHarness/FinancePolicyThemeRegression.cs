using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyFinancePolicyThemeAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null; var route = $"/api/academies/{academy}/finance-governance/settings"; var checks = 0;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(await db.AcademyFinanceSettings.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        }
        async Task<FinanceSettingsSummary?> Send(object body, HttpStatusCode expected, string? token = null, string? path = null)
        {
            var before = await Snapshot(); await Task.Delay(750);
            using var request = new HttpRequestMessage(HttpMethod.Put, path ?? route) { Content = JsonContent.Create(body) };
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, expected, "finance policy theme");
            if (expected != HttpStatusCode.OK && before != await Snapshot()) throw new InvalidOperationException("Denied theme request changed settings.");
            checks++; Console.WriteLine($"PASS: POLICYTHEME case={checks} HTTP={(int)expected} noRejectedWrite=true");
            return expected == HttpStatusCode.OK ? await response.Content.ReadFromJsonAsync<FinanceSettingsSummary>() : null;
        }
        async Task Verify(FinanceSettingsSummary expected)
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.AcademyFinanceSettings.AsNoTracking().SingleAsync(x => x.AcademyId == academy);
                var actual = new FinanceSettingsSummary(row.TaxRegistrationNumber,row.TaxLabel,row.TaxRatePercent,row.DefaultPaymentTermsDays,row.TaxInclusivePricing,row.InvoiceLogoUrl,row.InvoiceAuthorityName,row.InvoiceAuthorityTitle,row.InvoiceSignatureUrl,row.InvoiceTemplateKey,row.PayslipTemplateKey);
                if (actual != expected) throw new InvalidOperationException("Fresh SQL settings differ from requested values.");
            }
            await Task.Delay(750); using var request = new HttpRequestMessage(HttpMethod.Get, route);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, HttpStatusCode.OK, "policy readback");
            if (await response.Content.ReadFromJsonAsync<FinanceSettingsSummary>() != expected) throw new InvalidOperationException("Fresh HTTP policy readback mismatch.");
        }
        FinanceSettingsRequest Body(string? theme, string label = "GST", decimal rate = 18) => new("Synthetic",label,rate,7,false,"https://example.invalid/logo.png","Synthetic signatory","Finance","https://example.invalid/sign.png","Modern",theme);
        foreach (var theme in new[] { "Standard", "Compact", "Professional" })
        {
            await Verify((await Send(Body(theme),HttpStatusCode.OK,admin))!);
            var changed = (await Send(Body(theme,"Edited tax label"),HttpStatusCode.OK,admin))!;
            if (changed.PayslipTemplateKey != theme) throw new InvalidOperationException("Ordinary policy save reset hidden theme.");
            await Verify(changed);
            await Send(Body(theme,rate:101),HttpStatusCode.BadRequest,admin);
            await Verify(changed);
        }
        // Existing direct API NULL/omission reset semantics remain explicit controls.
        var reset = (await Send(Body(null),HttpStatusCode.OK,admin))!;
        if (reset.PayslipTemplateKey != "Standard") throw new InvalidOperationException("Existing null reset semantics changed."); await Verify(reset);
        var omitted = JsonSerializer.SerializeToElement(Body("Professional"),new JsonSerializerOptions { PropertyNamingPolicy=JsonNamingPolicy.CamelCase })
            .EnumerateObject().Where(x=>x.Name!="payslipTemplateKey").ToDictionary(x=>x.Name,x=>x.Value);
        reset = (await Send(omitted,HttpStatusCode.OK,admin))!;
        if (reset.PayslipTemplateKey != "Standard") throw new InvalidOperationException("Existing omission reset semantics changed."); await Verify(reset);
        await Send(Body("Compact"),HttpStatusCode.Unauthorized);
        await Send(Body("Compact"),HttpStatusCode.Forbidden,teacher);
        await Send(Body("Compact"),HttpStatusCode.Forbidden,admin,$"/api/academies/{foreign}/finance-governance/settings");
        await Send(Body("Unsupported"),HttpStatusCode.BadRequest,admin);
        // Deliberate stale editor reproduction, NOT a guarded concurrency pass.
        await Send(Body("Compact"),HttpStatusCode.OK,admin);
        var stale = Body("Compact","Stale policy edit");
        await Send(Body("Professional"),HttpStatusCode.OK,admin);
        var overwritten = (await Send(stale,HttpStatusCode.OK,admin))!; await Verify(overwritten);
        if (overwritten.PayslipTemplateKey != "Compact") throw new InvalidOperationException("Stale reproduction changed; re-evaluate before claiming this known gap.");
        Console.WriteLine("PASS: POLICYTHEME STALE REPRODUCED: newer Professional overwritten by old Compact with HTTP200; no CAS/version guard; issue remains OPEN, not concurrency acceptance.");
        Console.WriteLine($"PASS: POLICYTHEME HTTP-SQL {checks} requests checked; ordinary-save/reset/rejection/authority controls plus explicit stale-overwrite reproduction; not linked browser/device/full-critical proof.");
    }
}
