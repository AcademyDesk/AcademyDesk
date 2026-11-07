using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private static async Task VerifyTeacherAvailabilityAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        const string adminEmail = "qa-admin-a@example.invalid";
        const string ownerEmail = "qa-availability-owner@example.invalid";
        const string platformEmail = "qa-availability-platform@example.invalid";
        const string password = "Synthetic!39Ab";
        const string mixed = """[null,{"day":"Monday","from":null,"to":"11:00"}]""";
        const string legacyNull = "[null]";
        const string duplicateCode = "QA-AVAILABILITY-DUPLICATE";

        Guid academy, foreign, teacher, otherTenantTeacher, admin;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var target = await db.Teachers.SingleAsync(x => x.AcademyId == academy);
            teacher = target.Id;
            target.AvailabilityJson = legacyNull;
            target.CompensationJson = """{"Model":"Monthly","baseMonthlySalary":1800}""";
            target.Qualifications = "Synthetic qualification";
            var other = new Teacher { AcademyId = foreign, FirstName = "Synthetic", LastName = "Foreign teacher", AvailabilityJson = mixed };
            otherTenantTeacher = other.Id;
            db.Teachers.AddRange(other, new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Other teacher",
                EmployeeCode = duplicateCode, AvailabilityJson = mixed });
            await db.SaveChangesAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            admin = (await users.FindByEmailAsync(adminEmail))!.Id;
        }

        var owner = await CreateAccessActorAsync(factory, ownerEmail, "Owner", academy);
        var platform = await CreateAccessActorAsync(factory, platformEmail, "PlatformOwner", foreign);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var account = (await users.FindByIdAsync(platform.ToString()))!;
            account.IsPlatformOwner = true;
            if (!(await users.UpdateAsync(account)).Succeeded)
                throw new InvalidOperationException("Synthetic platform owner setup failed.");
        }

        var adminToken = await LoginAsync(client, adminEmail, password);
        var ownerToken = await LoginAsync(client, ownerEmail, password);
        var platformToken = await LoginAsync(client, platformEmail, password);
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", password);
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", password);
        client.DefaultRequestHeaders.Authorization = null;

        var root = $"/api/academies/{academy}/teachers";
        var route = $"{root}/{teacher}/profile";
        var checks = 0;
        var failures = 0;
        void Check(string label, bool passed, string detail = "")
        {
            checks++;
            if (!passed) failures++;
            Console.WriteLine($"AVAILABILITY CASE {label} {(passed ? "PASS" : "FAIL")} {detail}".TrimEnd());
        }

        JsonObject Request(string raw, string name, string? employeeCode = null) => new()
        {
            ["availabilityJson"] = raw,
            ["preferredName"] = name,
            ["qualifications"] = "Synthetic qualification",
            ["employeeCode"] = employeeCode
        };

        async Task AssertProjection(HttpResponseMessage response, string label, bool expectSlot, string? name = null)
        {
            RequireFinanceStatus(response, HttpStatusCode.OK, label);
            var profile = await response.Content.ReadFromJsonAsync<JsonElement>();
            var slots = profile.GetProperty("availability");
            if (profile.GetProperty("preferredName").GetString() != name || slots.GetArrayLength() != (expectSlot ? 1 : 0))
                throw new InvalidOperationException("Unexpected teacher profile projection: " + label);
            if (expectSlot && (slots[0].GetProperty("day").GetString() != "Monday" ||
                slots[0].GetProperty("from").ValueKind != JsonValueKind.Null || slots[0].GetProperty("to").GetString() != "11:00"))
                throw new InvalidOperationException("Availability slot was not preserved: " + label);
        }

        var initial = await TeacherAvailabilityStateAsync(factory);
        using (var read = await ManualSendAsync(client, adminToken, "GET", route))
            await AssertProjection(read, "legacy-null-get", false);
        ManualSame(initial, await TeacherAvailabilityStateAsync(factory), "legacy null GET");
        Check("legacy-null-get", true);

        var actors = new[] { ("admin", adminToken, admin), ("owner", ownerToken, owner), ("platform-owner", platformToken, platform) };
        foreach (var (label, token, actor) in actors)
        {
            var before = await TeacherAvailabilityStateAsync(factory);
            var name = "Synthetic " + label;
            var raw = label == "owner" ? legacyNull : mixed;
            using (var write = await ManualSendAsync(client, token, "PUT", route, Request(raw, name)))
                await AssertProjection(write, label + "-put", raw == mixed, name);
            using (var read = await ManualSendAsync(client, token, "GET", route))
                await AssertProjection(read, label + "-fresh-get", raw == mixed, name);

            var after = await TeacherAvailabilityStateAsync(factory);
            var saved = ManualRow(after, "teachers", teacher);
            FinanceRecoveryFields(saved, new { AvailabilityJson = raw, PreferredName = name });
            var old = ManualRow(before, "teachers", teacher);
            saved["AvailabilityJson"] = old["AvailabilityJson"]?.DeepClone();
            saved["PreferredName"] = old["PreferredName"]?.DeepClone();
            var count = after["audits"]!.AsArray().Count - before["audits"]!.AsArray().Count;
            var added = ManualAdded(before, after, "audits", count);
            ManualSame(before, after, label + " unrelated rows");
            var attributed = count == 1 && added[0]["ActorUserId"]!.GetValue<Guid>() == actor &&
                added[0]["AcademyId"]!.GetValue<Guid>() == academy && added[0]["Action"]!.GetValue<string>() == "PUT Profiles" &&
                added[0]["MetadataJson"]!.GetValue<string>().Contains(route, StringComparison.Ordinal);
            Check(label + "-put-get-audit", attributed, $"newAuditRows={count}");
        }

        foreach (var (label, token, target, status, employeeCode) in new (string, string?, string, HttpStatusCode, string?)[]
        {
            ("teacher-denied", teacherToken, route, HttpStatusCode.Forbidden, null),
            ("foreign-admin-denied", foreignToken, route, HttpStatusCode.Forbidden, null),
            ("anonymous-denied", null, route, HttpStatusCode.Unauthorized, null),
            ("missing-teacher", adminToken, $"{root}/{Guid.NewGuid()}/profile", HttpStatusCode.NotFound, null),
            ("foreign-teacher-id", adminToken, $"{root}/{otherTenantTeacher}/profile", HttpStatusCode.NotFound, null),
            ("duplicate-employee-code", adminToken, route, HttpStatusCode.Conflict, duplicateCode)
        })
        {
            var before = await TeacherAvailabilityStateAsync(factory);
            using var response = await ManualSendAsync(client, token, "PUT", target, Request(legacyNull, "Synthetic denied", employeeCode));
            RequireFinanceStatus(response, status, label);
            ManualSame(before, await TeacherAvailabilityStateAsync(factory), label);
            Check(label, true);
        }

        foreach (var (label, token, _) in actors)
        {
            var fault = new TeacherAvailabilityAuditFault();
            var before = await TeacherAvailabilityStateAsync(factory);
            using var failing = new QaApiFactory(manifest, isolatedDomainInterceptor: fault);
            using var failingClient = failing.CreateClient(new() { AllowAutoRedirect = false });
            using var response = await ManualSendAsync(failingClient, token, "PUT", route, Request(legacyNull, "Synthetic fault " + label));
            var unchanged = JsonNode.DeepEquals(before, await TeacherAvailabilityStateAsync(factory));
            Check(label + "-audit-failure", response.StatusCode == HttpStatusCode.InternalServerError && fault.Hits == 1 && unchanged,
                $"status={(int)response.StatusCode} injectedAuditFailures={fault.Hits} stateUnchanged={unchanged}");
        }

        Console.WriteLine($"AVAILABILITY SUMMARY {checks - failures}/{checks} native HTTP/SQL checks passed");
        if (failures != 0) throw new InvalidOperationException("Teacher availability SQL acceptance failed.");
    }

    private static async Task<JsonObject> TeacherAvailabilityStateAsync(QaApiFactory factory)
    {
        var state = await FinanceRecoveryStateAsync(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        state["teachers"] = JsonSerializer.SerializeToNode(await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        return state;
    }

    private sealed class TeacherAvailabilityAuditFault : SaveChangesInterceptor
    {
        public int Hits { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default)
        {
            if (data.Context is AcademyDeskDbContext db && db.ChangeTracker.Entries<AuditLog>().Any(entry => entry.State == EntityState.Added && entry.Entity.Action == "PUT Profiles"))
            {
                Hits++;
                throw new InvalidOperationException("Synthetic required teacher profile audit failure.");
            }
            return base.SavingChangesAsync(data, result, token);
        }
    }
}
