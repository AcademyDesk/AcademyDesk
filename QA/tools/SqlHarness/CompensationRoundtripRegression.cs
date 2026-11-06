using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record CompensationSqlState(List<Teacher> Teachers, List<AuditLog> Audits, string Money, string Identity);

    private static async Task VerifyCompensationRoundtripAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var receiptPath = Environment.GetEnvironmentVariable("QA_COMPENSATION_BROWSER_RECEIPT");
        if (string.IsNullOrWhiteSpace(receiptPath)) throw new InvalidOperationException("A genuine controlled-browser compensation receipt is required.");
        var receiptBytes = await File.ReadAllBytesAsync(receiptPath);
        if (receiptBytes.Length > 2_000_000) throw new InvalidOperationException("Receipt exceeds the bounded input limit.");
        using var receipt = JsonDocument.Parse(receiptBytes);
        if (receipt.RootElement.GetProperty("status").GetString() != "PASS" ||
            receipt.RootElement.GetProperty("results").GetArrayLength() != 44 ||
            receipt.RootElement.GetProperty("runtimeErrors").GetArrayLength() != 0)
            throw new InvalidOperationException("The controlled-browser input did not pass its declared scope.");
        var bodies = receipt.RootElement.GetProperty("results").EnumerateArray()
            .Where(row => row.GetProperty("pass").GetBoolean() && row.TryGetProperty("writes", out _))
            .SelectMany(row => row.GetProperty("writes").EnumerateArray())
            .Select(write => JsonSerializer.Serialize(write.GetProperty("body"))).Distinct().ToArray();
        if (bodies.Length < 5 || bodies.Length > 12) throw new InvalidOperationException("Expected the bounded distinct browser value matrix.");

        Guid academy, foreign, teacherId, foreignTeacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacherId = await db.Teachers.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            var other = new Teacher { AcademyId = foreign, FirstName = "Synthetic", LastName = "Compensation foreign" };
            db.Teachers.Add(other);
            await db.SaveChangesAsync();
            foreignTeacher = other.Id;
        }
        var platformId = await CreateAccessActorAsync(factory, "qa-comp-platform@example.invalid", "Owner", academy);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByIdAsync(platformId.ToString()))!;
            user.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Synthetic platform actor setup failed.");
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var platform = await LoginAsync(client, "qa-comp-platform@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        Guid adminId;
        using (var scope = factory.Services.CreateScope())
            adminId = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        var route = $"/api/academies/{academy}/teachers/{teacherId}/compensation";
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var checks = 0;
        void Pass(string label) { checks++; Console.WriteLine("FINANCECOMP CASE " + label + " PASS."); }
        async Task<CompensationSqlState> State()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var money = JsonSerializer.Serialize(new {
                invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                profiles = await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                payouts = await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync()
            });
            var identities = JsonSerializer.Serialize(new {
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.IsActive, x.SecurityStamp, x.IsPlatformOwner }).ToArrayAsync(),
                memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync()
            });
            return new(await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
                await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), money, identities);
        }
        async Task<HttpResponseMessage> Send(HttpClient target, string? token, string method, string url, string? body = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            return await target.SendAsync(request);
        }
        async Task VerifySaved(string body, string label, string? access = null, Guid? actorId = null)
        {
            var request = JsonSerializer.Deserialize<UpdateTeacherCompensationRequest>(body, jsonOptions)!;
            var expected = request.Model == "Monthly"
                ? new TeacherCompensationSummary(teacherId, "Monthly", request.MonthlySalary, null, null, null, null, request.EffectiveFrom)
                : new TeacherCompensationSummary(teacherId, "Hourly", null, request.StandardHourlyRate, request.BeginnerHourlyRate, request.IntermediateHourlyRate, request.AdvancedHourlyRate, request.EffectiveFrom);
            for (var save = 1; save <= 2; save++)
            {
                var before = await State();
                using var response = await Send(client, access ?? admin, "PUT", route, body);
                RequireFinanceStatus(response, HttpStatusCode.OK, label);
                if (await response.Content.ReadFromJsonAsync<TeacherCompensationSummary>() != expected) throw new InvalidOperationException("Compensation response changed zero/null/decimal/date values.");
                var after = await State();
                var stored = JsonSerializer.Deserialize<TeacherCompensationSummary>(after.Teachers.Single(x => x.Id == teacherId).CompensationJson!, jsonOptions);
                if (stored != expected) throw new InvalidOperationException("Fresh SQL compensation differs from the exact browser payload.");
                before.Teachers.Single(x => x.Id == teacherId).CompensationJson = after.Teachers.Single(x => x.Id == teacherId).CompensationJson;
                if (JsonSerializer.Serialize(before.Teachers) != JsonSerializer.Serialize(after.Teachers) || before.Money != after.Money || before.Identity != after.Identity || after.Audits.Count != before.Audits.Count + 1)
                    throw new InvalidOperationException("Compensation save changed another teacher, payroll, Identity or audit count.");
                var audit = after.Audits.Single(x => before.Audits.All(old => old.Id != x.Id));
                if (audit.ActorUserId != (actorId ?? adminId) || audit.AcademyId != academy || audit.Action != "PUT TeacherCompensation" || !audit.MetadataJson!.Contains(route, StringComparison.Ordinal))
                    throw new InvalidOperationException("Compensation audit actor/tenant/route mismatch.");
                using var read = await Send(client, access ?? admin, "GET", route);
                RequireFinanceStatus(read, HttpStatusCode.OK, "compensation readback");
                if (await read.Content.ReadFromJsonAsync<TeacherCompensationSummary>() != expected || JsonSerializer.Serialize(after) != JsonSerializer.Serialize(await State()))
                    throw new InvalidOperationException("Compensation readback changed data or returned different values.");
                Pass(label + " save/reload " + save + ": exact one teacher, one audit, money/Identity unchanged");
            }
        }
        foreach (var body in bodies) await VerifySaved(body, "captured browser payload");
        var valid = "{\"model\":\"Hourly\",\"monthlySalary\":null,\"standardHourlyRate\":0,\"beginnerHourlyRate\":0,\"intermediateHourlyRate\":0,\"advancedHourlyRate\":0,\"effectiveFrom\":null}";
        foreach (var field in new[] { "monthlySalary", "standardHourlyRate", "beginnerHourlyRate", "intermediateHourlyRate", "advancedHourlyRate" })
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(valid)!;
            values[field] = JsonSerializer.SerializeToElement(-1m);
            await Denied(admin, "PUT", route, JsonSerializer.Serialize(values), HttpStatusCode.BadRequest, "negative " + field);
        }
        await Denied(admin, "PUT", route, "{\"model\":\"Monthly\",\"monthlySalary\":null}", HttpStatusCode.BadRequest, "missing monthly primary");
        await Denied(admin, "PUT", route, "{\"model\":\"Hourly\",\"standardHourlyRate\":null}", HttpStatusCode.BadRequest, "missing hourly primary");
        await Denied(admin, "PUT", route, "{\"model\":\"Other\",\"monthlySalary\":0}", HttpStatusCode.BadRequest, "invalid model");
        await Denied(null, "PUT", route, valid, HttpStatusCode.Unauthorized, "anonymous write");
        await Denied(teacher, "GET", route, null, HttpStatusCode.Forbidden, "Teacher financial read");
        await Denied(teacher, "PUT", route, valid, HttpStatusCode.Forbidden, "Teacher financial write");
        await Denied(admin, "GET", $"/api/academies/{foreign}/teachers/{foreignTeacher}/compensation", null, HttpStatusCode.Forbidden, "foreign tenant read");
        await Denied(admin, "PUT", $"/api/academies/{foreign}/teachers/{foreignTeacher}/compensation", valid, HttpStatusCode.Forbidden, "foreign tenant write");
        await Denied(admin, "PUT", $"/api/academies/{academy}/teachers/{foreignTeacher}/compensation", valid, HttpStatusCode.NotFound, "foreign teacher in own route");
        await VerifySaved(valid, "Platform Owner authorized save", platform, platformId);
        async Task Denied(string? token, string method, string url, string? body, HttpStatusCode status, string label)
        {
            var before = JsonSerializer.Serialize(await State());
            using var response = await Send(client, token, method, url, body);
            RequireFinanceStatus(response, status, label);
            if (before != JsonSerializer.Serialize(await State())) throw new InvalidOperationException("Rejected compensation request changed state: " + label);
            Pass(label + ": exact HTTP rejection and no SQL/audit/money/Identity changes");
        }
        var auditFailures = new List<object>();
        foreach (var actor in new[] { (Name: "Admin", Token: admin), (Name: "PlatformOwner", Token: platform) })
        {
            var fault = new CompensationAuditFault();
            using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
            using var failureClient = failing.CreateClient(new() { AllowAutoRedirect = false });
            var before = JsonSerializer.Serialize(await State());
            using var response = await Send(failureClient, actor.Token, "PUT", route, "{\"model\":\"Monthly\",\"monthlySalary\":125.5}");
            var unchanged = before == JsonSerializer.Serialize(await State());
            if (response.StatusCode != HttpStatusCode.InternalServerError || fault.Hits != 1 || !unchanged)
            {
                auditFailures.Add(new { actor = actor.Name, status = (int)response.StatusCode, fault.Hits, unchanged });
                Console.WriteLine($"FINANCECOMP AUDIT FAILURE actor={actor.Name} status={(int)response.StatusCode} faultHits={fault.Hits} unchanged={unchanged}");
            }
            else Pass(actor.Name + " required audit failure: native500 and full SQL rollback");
        }
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "QA", "EVIDENCE", "compensation-sql-" + manifest.RunId.ToString("N"));
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(new {
            run = manifest.RunId.ToString("N"), passed = auditFailures.Count == 0, checks,
            browserReceiptSha256 = Convert.ToHexString(SHA256.HashData(receiptBytes)).ToLowerInvariant(),
            distinctBrowserPayloads = bodies.Length, auditFailures, modelInference = false, physicalDevices = "NOT RUN"
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (auditFailures.Count != 0) throw new InvalidOperationException("Compensation required-audit boundary failed; retained native status/fault/SQL evidence.");
        Console.WriteLine($"FINANCECOMP SUMMARY {checks} real HTTP/SQL checks PASS; exact browser payloads, independent authority and audit rollback.");
    }

    private sealed class CompensationAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(x => x.State == EntityState.Added && x.Entity.Action == "PUT TeacherCompensation"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required compensation audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
