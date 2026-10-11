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
            return JsonSerializer.Serialize(new { Settings = await db.AcademyFinanceSettings.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<FinanceSettingsSummary> Read()
        {
            await Task.Delay(750); using var request = new HttpRequestMessage(HttpMethod.Get, route);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response,HttpStatusCode.OK,"settings version read");
            var result = (await response.Content.ReadFromJsonAsync<FinanceSettingsSummary>())!;
            if (result.SettingsVersion?.Length != 64) throw new InvalidOperationException("Missing authoritative settings version.");
            return result;
        }
        async Task<FinanceSettingsSummary?> Send(object body, HttpStatusCode expected, string? token = null, string? path = null, bool attachVersion = true)
        {
            if (attachVersion)
            {
                var version = (await Read()).SettingsVersion;
                if (body is FinanceSettingsRequest typed) body = typed with { SettingsVersion = version };
                else if (body is Dictionary<string, JsonElement> omitted) omitted["settingsVersion"] = JsonSerializer.SerializeToElement(version);
            }
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
                if (actual != expected with { SettingsVersion = null }) throw new InvalidOperationException("Fresh SQL settings differ from requested values.");
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
        // Original stale failure now requires conflict and a completely unchanged snapshot.
        var compact = (await Send(Body("Compact"),HttpStatusCode.OK,admin))!;
        var stale = Body("Compact","Stale policy edit") with { SettingsVersion = compact.SettingsVersion };
        var professional = (await Send(Body("Professional"),HttpStatusCode.OK,admin))!;
        await Send(stale,HttpStatusCode.Conflict,admin,attachVersion:false); await Verify(professional);
        await Send(Body("Compact"), (HttpStatusCode)428, admin,attachVersion:false);
        await Send(Body("Compact") with { SettingsVersion = "*" },HttpStatusCode.Conflict,admin,attachVersion:false);
        await Send(Body("Compact") with { SettingsVersion = (await Read()).SettingsVersion },HttpStatusCode.Forbidden,admin,$"/api/academies/{foreign}/finance-governance/settings",attachVersion:false);
        // Five simultaneous pairs; first tests absent-row insertion, others replacement.
        for (var pair = 0; pair < 5; pair++)
        {
            if (pair == 0)
            {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                db.Remove(await db.AcademyFinanceSettings.SingleAsync(x => x.AcademyId == academy)); await db.SaveChangesAsync();
            }
            var version = (await Read()).SettingsVersion; int auditBefore;
            using (var scope = factory.Services.CreateScope()) auditBefore = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.CountAsync();
            var bodies = new[] { Body("Compact",$"Policy pair{pair}") with { SettingsVersion=version }, Body("Professional",$"Document pair{pair}") with { SettingsVersion=version } };
            await Task.Delay(1000);
            async Task<(HttpStatusCode Status, FinanceSettingsSummary? Result)> Race(FinanceSettingsRequest body)
            {
                using var request = new HttpRequestMessage(HttpMethod.Put,route) { Content=JsonContent.Create(body) };
                request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",admin); using var response=await client.SendAsync(request);
                return(response.StatusCode,response.IsSuccessStatusCode?await response.Content.ReadFromJsonAsync<FinanceSettingsSummary>():null);
            }
            var results = await Task.WhenAll(bodies.Select(Race));
            if (results.Count(x=>x.Status==HttpStatusCode.OK)!=1 || results.Count(x=>x.Status==HttpStatusCode.Conflict)!=1) throw new InvalidOperationException($"Settings race{pair} expected200/409; got {string.Join(',',results.Select(x=>(int)x.Status))}.");
            var winnerIndex = Array.FindIndex(results,x=>x.Status==HttpStatusCode.OK); var winner = results[winnerIndex].Result!;
            if(winner.TaxLabel!=bodies[winnerIndex].TaxLabel || winner.PayslipTemplateKey!=bodies[winnerIndex].PayslipTemplateKey)throw new InvalidOperationException("Race mixed writer fields.");
            await Verify(winner);
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                if(await db.AcademyFinanceSettings.CountAsync(x=>x.AcademyId==academy)!=1 || await db.AuditLogs.CountAsync()!=auditBefore+1)throw new InvalidOperationException("Race inserted extra rows/audits or lost success audit.");
            }
            Console.WriteLine($"PASS: POLICYTHEME race={pair+1} exactly200/409 oneRow=true oneSuccessAudit=true noMixedFields=true");
        }
        Console.WriteLine($"PASS: POLICYTHEME HTTP-SQL {checks} sequential controls plus5 guarded simultaneous pairs; not linked browser/device/full-critical proof.");
    }
}
