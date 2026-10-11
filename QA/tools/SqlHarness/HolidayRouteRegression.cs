using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyHolidayRouteAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_HOLIDAY_ROUTE_BASELINE") == "1";
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>().Where(x =>
                x.ControllerTypeInfo.Name is "HolidaysController" or "HolidayDeleteController" &&
                x.ActionConstraints?.OfType<HttpMethodActionConstraint>().Any(c => c.HttpMethods.Contains("DELETE")) == true).ToArray();
        if (actions.Length != (baseline ? 2 : 1)) throw new InvalidOperationException("Unexpected holiday DELETE endpoint count.");
        if (!baseline && actions[0].ControllerTypeInfo.Name != "HolidaysController") throw new InvalidOperationException("Canonical endpoint changed.");
        Console.WriteLine($"PASS: HOLIDAYROUTE {(baseline ? "baseline two candidates" : "one canonical DELETE endpoint")}");
        Guid academy, foreign, localId, sentinelId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var local = new AcademyHoliday { AcademyId = academy, Name = "Synthetic local holiday", HolidayDate = new(2026, 10, 11), Notes = "Local sentinel" };
            var other = new AcademyHoliday { AcademyId = foreign, Name = "Synthetic foreign holiday", HolidayDate = new(2026, 10, 12), Notes = "Foreign sentinel" };
            db.AddRange(local, other); await db.SaveChangesAsync(); localId = local.Id; sentinelId = other.Id;
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var otherAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                Holidays = await db.AcademyHolidays.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            });
        }
        async Task<HttpResponseMessage> Delete(Guid tenant, Guid record, string? token)
        {
            await Task.Delay(650); using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/academies/{tenant}/holidays/{record}");
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        if (baseline)
        {
            var before = await Snapshot(); var reproduced = false;
            try { using var response = await Delete(academy, localId, admin); reproduced = response.StatusCode == HttpStatusCode.InternalServerError; }
            catch (Exception error) when (error.GetType().FullName == "Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException") { reproduced = true; }
            if (!reproduced || before != await Snapshot()) throw new InvalidOperationException("Expected HTTP ambiguity with no row/audit changes.");
            Console.WriteLine("PASS: HOLIDAYROUTE baseline HTTP ambiguity reproduced; holidays/audit unchanged."); return;
        }
        var count = 1;
        var denied = new (string Label, Guid Tenant, Guid Record, string? Token, HttpStatusCode Status)[] {
            ("anonymous", academy, localId, null, HttpStatusCode.Unauthorized),
            ("teacher", academy, localId, teacher, HttpStatusCode.Forbidden),
            ("foreign actor", academy, localId, otherAdmin, HttpStatusCode.Forbidden),
            ("foreign route", foreign, sentinelId, admin, HttpStatusCode.Forbidden),
            ("foreign record", academy, sentinelId, admin, HttpStatusCode.NotFound),
            ("missing record", academy, Guid.NewGuid(), admin, HttpStatusCode.NotFound)
        };
        foreach (var item in denied)
        {
            var before = await Snapshot(); using var response = await Delete(item.Tenant, item.Record, item.Token);
            RequireFinanceStatus(response, item.Status, "holiday " + item.Label);
            if (before != await Snapshot()) throw new InvalidOperationException("Denied holiday request changed SQL/audit: " + item.Label);
            count++; Console.WriteLine("PASS: HOLIDAYROUTE no-write " + item.Label);
        }
        foreach (var mode in new[] { "suspended", "module disabled" })
        {
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy);
                if (mode == "suspended") row.IsActive = false; else row.EnabledModulesJson = "[]"; await db.SaveChangesAsync();
            }
            var before = await Snapshot(); using var response = await Delete(academy, localId, admin);
            RequireFinanceStatus(response, HttpStatusCode.Forbidden, mode); if (before != await Snapshot()) throw new InvalidOperationException("Lifecycle denial changed SQL/audit.");
            count++; Console.WriteLine("PASS: HOLIDAYROUTE no-write " + mode);
            using var restore = factory.Services.CreateScope(); var dbRestore = restore.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var academyRow = await dbRestore.Academies.SingleAsync(x => x.Id == academy); academyRow.IsActive = true; academyRow.EnabledModulesJson = "[\"Certificates\",\"Finance\",\"FinanceControls\"]"; await dbRestore.SaveChangesAsync();
        }
        int auditsBefore;
        using (var scope = factory.Services.CreateScope()) auditsBefore = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.CountAsync();
        using (var response = await Delete(academy, localId, admin)) RequireFinanceStatus(response, HttpStatusCode.NoContent, "authorized deletion");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AcademyHolidays.AnyAsync(x => x.Id == localId) || await db.AcademyHolidays.CountAsync() != 1 ||
                (await db.AcademyHolidays.AsNoTracking().SingleAsync()).Id != sentinelId || await db.AuditLogs.CountAsync() != auditsBefore + 1 ||
                !await db.AuditLogs.AnyAsync(x => x.AcademyId == academy && x.ActorUserId != null && x.Action == "DELETE Holidays"))
                throw new InvalidOperationException("Deletion/audit exact persistence differs.");
        }
        count++; Console.WriteLine("PASS: HOLIDAYROUTE authorized 204, exactly one row removed, foreign sentinel and actor audit retained.");
        var after = await Snapshot(); using (var repeated = await Delete(academy, localId, admin)) RequireFinanceStatus(repeated, HttpStatusCode.NotFound, "repeated deletion");
        if (after != await Snapshot()) throw new InvalidOperationException("Repeated deletion changed SQL/audit."); count++;
        Console.WriteLine("PASS: HOLIDAYROUTE repeated 404 no-write.");
        if (count != 11) throw new InvalidOperationException("Incomplete holiday matrix.");
        Console.WriteLine($"PASS: HOLIDAYROUTE all {count} endpoint/SQL/Identity/HTTP checks; no 429.");
    }
}
