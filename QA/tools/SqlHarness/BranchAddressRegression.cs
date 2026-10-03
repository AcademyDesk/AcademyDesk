using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyBranchAddressAsync(QaApiFactory factory, HttpClient client, bool baseline)
    {
        Guid academyA, academyB;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var own = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
            academyA = own.Id; own.EnabledModulesJson = "[\"MultiBranch\"]";
            academyB = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            await db.SaveChangesAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var root = $"/api/academies/{academyA}/branches"; var count = 0; var seed = 0;
        async Task<HttpResponseMessage> Send(string method, string route, object? body, string? actor = null, bool anonymous = false)
        {
            await Task.Delay(650); client.DefaultRequestHeaders.Authorization = anonymous ? null : new AuthenticationHeaderValue("Bearer", actor ?? token);
            var request = new HttpRequestMessage(new HttpMethod(method), route);
            if (body is not null) request.Content = JsonContent.Create(body);
            using (request) return await client.SendAsync(request);
        }
        void Passed(string label) { count++; Console.WriteLine($"BRANCHADDRESS CASE {label} PASS."); }
        async Task<Branch> Row(Guid id)
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Branches.AsNoTracking().SingleAsync(x => x.Id == id);
        }
        async Task<string> Unrelated(Guid id)
        {
            var snapshot = await GovernanceStateAsync(factory);
            var stable = JsonNode.Parse(snapshot.Stable)!.AsObject();
            var rows = stable["Branches"]!.AsArray();
            stable["Branches"] = new JsonArray(rows.Where(x => x!["Id"]!.GetValue<Guid>() != id).Select(x => x!.DeepClone()).ToArray());
            return JsonSerializer.Serialize(new { Stable = stable, snapshot.Tasks });
        }
        async Task<JsonElement> Projection(Guid id)
        {
            var before = JsonSerializer.Serialize(await GovernanceStateAsync(factory));
            using var response = await Send("GET", root, null); RequireFinanceStatus(response, HttpStatusCode.OK, "branches list");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var row = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id).Clone();
            if (before != JsonSerializer.Serialize(await GovernanceStateAsync(factory))) throw new InvalidOperationException("Branch GET wrote captured state.");
            return row;
        }
        async Task<Guid> Create(bool populated, bool active = true)
        {
            using var response = await Send("POST", root, new { name = $" Synthetic branch {++seed} ", addressLine1 = populated ? " 12 Synthetic street " : null,
                city = " Synthetic city ", state = " Synthetic state ", postalCode = populated ? " 560001 " : null });
            RequireFinanceStatus(response, HttpStatusCode.Created, "create branch");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var id = json.RootElement.GetProperty("id").GetGuid();
            var row = await Row(id);
            if (row.AddressLine1 != (populated ? "12 Synthetic street" : null) || row.PostalCode != (populated ? "560001" : null)) throw new InvalidOperationException("Seed create did not persist fields.");
            if (!baseline && (json.RootElement.GetProperty("addressLine1").GetString() != row.AddressLine1 || json.RootElement.GetProperty("postalCode").GetString() != row.PostalCode))
                throw new InvalidOperationException("Create summary did not return saved fields.");
            if (!active) { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Branches.SingleAsync(x => x.Id == id)).IsActive = false; await db.SaveChangesAsync(); }
            return id;
        }
        foreach (var populated in baseline ? new[] { true } : new[] { true, false })
        foreach (var action in new[] { "edit", "deactivate", "reactivate" })
        {
            var id = await Create(populated, action != "reactivate"); var before = await Row(id); var projection = await Projection(id);
            if (baseline && projection.TryGetProperty("addressLine1", out _)) throw new InvalidOperationException("Baseline already exposes address.");
            if (!baseline && (projection.GetProperty("addressLine1").GetString() != before.AddressLine1 || projection.GetProperty("postalCode").GetString() != before.PostalCode))
                throw new InvalidOperationException("GET summary missing preservation fields.");
            var body = new Dictionary<string, object?> {
                ["name"] = action == "edit" ? $"Renamed synthetic {seed}" : before.Name, ["city"] = before.City, ["state"] = before.State,
                ["isActive"] = action == "edit" ? before.IsActive : !before.IsActive };
            if (!baseline) { body["addressLine1"] = projection.GetProperty("addressLine1").GetString(); body["postalCode"] = projection.GetProperty("postalCode").GetString(); }
            var unchanged = await Unrelated(id); var audits = await AccessAuditCountAsync(factory);
            using var response = await Send("PUT", root + "/" + id, body); RequireFinanceStatus(response, HttpStatusCode.OK, action);
            var after = await Row(id);
            before.Name = (string)body["name"]!; before.IsActive = (bool)body["isActive"]!;
            if (baseline) { before.AddressLine1 = null; before.PostalCode = null; }
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after) || unchanged != await Unrelated(id) || await AccessAuditCountAsync(factory) != audits + 1)
                throw new InvalidOperationException("Branch full-row/unrelated-state/audit assertion failed: " + action);
            if (!baseline) {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (json.RootElement.GetProperty("addressLine1").GetString() != after.AddressLine1 || json.RootElement.GetProperty("postalCode").GetString() != after.PostalCode)
                    throw new InvalidOperationException("Update summary lost preserved fields.");
                await Projection(id);
            }
            Passed((baseline ? "baseline-loss/" : "preserved/") + action + "/" + (populated ? "populated" : "null"));
        }
        if (baseline) { Console.WriteLine($"BRANCHADDRESS BASELINE REPRODUCED: {count} independent populated edit/deactivate/reactivate cases; omitted PUT fields erase address/postcode; GET omits street."); return; }
        var control = await Create(true); var controlRoute = root + "/" + control;
        var clear = new { name = "Explicit clear", addressLine1 = (string?)null, city = "Synthetic city", state = "Synthetic state", postalCode = (string?)null, isActive = true };
        using (var response = await Send("PUT", controlRoute, clear)) RequireFinanceStatus(response, HttpStatusCode.OK, "explicit null clear");
        var cleared = await Row(control); if (cleared.AddressLine1 is not null || cleared.PostalCode is not null) throw new InvalidOperationException("Explicit clear failed.");
        Passed("explicit-null-clear/current-replacement-contract");
        var invalid = new { name = " ", addressLine1 = "Changed", city = "Changed", state = "Changed", postalCode = "Changed", isActive = false };
        foreach (var rejection in new[] {
            ("blank-name", controlRoute, (object)invalid, token, false, HttpStatusCode.BadRequest),
            ("missing-branch", root + "/" + Guid.NewGuid(), (object)clear, token, false, HttpStatusCode.NotFound),
            ("foreign-actor", controlRoute, (object)clear, foreignToken, false, HttpStatusCode.Forbidden),
            ("foreign-route", $"/api/academies/{academyB}/branches/{control}", (object)clear, token, false, HttpStatusCode.Forbidden),
            ("teacher", controlRoute, (object)clear, teacherToken, false, HttpStatusCode.Forbidden),
            ("anonymous", controlRoute, (object)clear, token, true, HttpStatusCode.Unauthorized) })
        {
            var before = JsonSerializer.Serialize(await GovernanceStateAsync(factory));
            using var response = await Send("PUT", rejection.Item2, rejection.Item3, rejection.Item4, rejection.Item5);
            RequireFinanceStatus(response, rejection.Item6, rejection.Item1);
            if (before != JsonSerializer.Serialize(await GovernanceStateAsync(factory))) throw new InvalidOperationException("Rejected branch update wrote captured state.");
            Passed("rejected-no-write/" + rejection.Item1);
        }
        Console.WriteLine($"BRANCHADDRESS REGRESSION PASS: {count} cases; fresh full SQL rows, list/create/update projections, unchanged unrelated captured rows, success audit count, explicit null clear and 400/401/403/404 no-write. Browser/concurrency/audit-fault rollback not covered.");
    }
}
