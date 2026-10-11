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
    private static async Task VerifyTeacherCompensationAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, teacher; string foreignBefore;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            foreignBefore = JsonSerializer.Serialize(await db.Teachers.AsNoTracking().Where(x => x.AcademyId == foreign).OrderBy(x => x.Id).ToArrayAsync());
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null; var path = $"/api/academies/{academy}/teachers/{teacher}/compensation"; var checks = 0;
        void Pass(string label) { checks++; Console.WriteLine("PASS: COMPENSATION " + label); }
        async Task<HttpResponseMessage> Send(string route, object body, string? token)
        {
            await Task.Delay(700); using var request = new HttpRequestMessage(HttpMethod.Put, route) { Content = JsonContent.Create(body) };
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audit = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task VerifyStored(TeacherCompensationSummary expected)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var raw = await db.Teachers.AsNoTracking().Where(x => x.Id == teacher).Select(x => x.CompensationJson).SingleAsync();
            if (JsonSerializer.Deserialize<TeacherCompensationSummary>(raw!) != expected) throw new InvalidOperationException("Fresh SQL compensation fields differ.");
            if (foreignBefore != JsonSerializer.Serialize(await db.Teachers.AsNoTracking().Where(x => x.AcademyId == foreign).OrderBy(x => x.Id).ToArrayAsync())) throw new InvalidOperationException("Foreign teacher changed.");
        }
        var cases = new[] {
            new UpdateTeacherCompensationRequest("Monthly",0,null,null,null,null,null), new("Monthly",125.50m,null,null,null,null,new DateOnly(2026,10,1)),
            new("Hourly",null,0,null,null,null,null), new("Hourly",null,0,0,0,0,null),
            new("Hourly",null,10,0,null,null,null), new("Hourly",null,10,null,0,null,null), new("Hourly",null,10,null,null,0,null),
            new("Hourly",null,10,null,null,null,null), new("Hourly",null,125.50m,10.25m,20.50m,30.75m,new DateOnly(2024,2,29)) };
        foreach (var body in cases)
        {
            var expected = new TeacherCompensationSummary(teacher,body.Model,body.MonthlySalary,body.StandardHourlyRate,body.BeginnerHourlyRate,body.IntermediateHourlyRate,body.AdvancedHourlyRate,body.EffectiveFrom);
            using (var response = await Send(path,body,admin))
            {
                RequireFinanceStatus(response,HttpStatusCode.OK,"compensation initial save");
                if (await response.Content.ReadFromJsonAsync<TeacherCompensationSummary>() != expected) throw new InvalidOperationException("Compensation initial response differs.");
                await VerifyStored(expected); Pass("initial save exact response/SQL zero-null-date/foreign sentinel");
            }
            using var request = new HttpRequestMessage(HttpMethod.Get,path); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",admin);
            await Task.Delay(700); using var read = await client.SendAsync(request); RequireFinanceStatus(read,HttpStatusCode.OK,"compensation reload");
            var loaded = await read.Content.ReadFromJsonAsync<TeacherCompensationSummary>(); if (loaded != expected) throw new InvalidOperationException("Compensation GET changed zero/null."); Pass("GET reload exact rates/date");
            var resave = new UpdateTeacherCompensationRequest(loaded!.Model,loaded.MonthlySalary,loaded.StandardHourlyRate,loaded.BeginnerHourlyRate,loaded.IntermediateHourlyRate,loaded.AdvancedHourlyRate,loaded.EffectiveFrom);
            using var repeated = await Send(path,resave,admin); RequireFinanceStatus(repeated,HttpStatusCode.OK,"compensation unchanged resave");
            if (await repeated.Content.ReadFromJsonAsync<TeacherCompensationSummary>() != expected) throw new InvalidOperationException("Compensation resave response differs.");
            await VerifyStored(expected); Pass("unchanged resave exact response/fresh SQL");
        }
        var valid = cases[0];
        var invalid = new[] { valid with { MonthlySalary = null }, cases[2] with { StandardHourlyRate = null }, valid with { MonthlySalary = -1 },
            cases[2] with { StandardHourlyRate = -1 }, cases[2] with { BeginnerHourlyRate = -1 }, cases[2] with { IntermediateHourlyRate = -1 }, cases[2] with { AdvancedHourlyRate = -1 }, valid with { Model = "Unsupported" } };
        async Task Rejected(string route, object body, string? token, HttpStatusCode status)
        {
            var before = await Snapshot(); using var response = await Send(route,body,token); RequireFinanceStatus(response,status,"compensation denied/invalid");
            if (before != await Snapshot()) throw new InvalidOperationException("Rejected compensation changed SQL/audit."); Pass("exact rejected status/no SQL or audit write");
        }
        foreach (var body in invalid) await Rejected(path,body,admin,HttpStatusCode.BadRequest);
        await Rejected(path,valid,null,HttpStatusCode.Unauthorized);
        await Rejected(path,valid,teacherToken,HttpStatusCode.Forbidden);
        await Rejected(path,valid,foreignAdmin,HttpStatusCode.Forbidden);
        await Rejected($"/api/academies/{foreign}/teachers/{teacher}/compensation",valid,admin,HttpStatusCode.Forbidden);
        if (checks != 39) throw new InvalidOperationException("Compensation matrix incomplete: " + checks);
        Console.WriteLine("PASS: COMPENSATION all39 native SQL/Identity/HTTP round-trip and denied/no-write checks; no429.");
    }
}
