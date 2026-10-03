using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCommunicationPreferenceAccessAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student, foreignStudent, guardian, inactiveGuardian; int count = 0;
        var actors = new Dictionary<string, (Guid Id, string Token)>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync(); foreignStudent = await db.Students.Where(x => x.AcademyId == foreign).Select(x => x.Id).SingleAsync();
            var parent = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Preference", Email = "recipient@example.invalid", Phone = "DO-NOT-EXPOSE" }; guardian = parent.Id; db.Add(parent);
            var inactive = new Guardian { AcademyId = academy, FirstName = "Inactive", LastName = "Preference", IsActive = false }; inactiveGuardian = inactive.Id; db.Add(inactive);
            db.Add(new Guardian { AcademyId = foreign, FirstName = "Foreign", LastName = "Secret" });
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            db.Add(new CommunicationPreference { AcademyId = foreign, RecipientId = foreignStudent, RecipientType = "Student", Notes = "Foreign sentinel" }); await db.SaveChangesAsync();
        }
        foreach (var pair in new[] { ("owner", "Owner", "[]"), ("manager", "Manager", "[]"), ("operations", "Operations", "[]"), ("finance", "FinanceUser", "[]"), ("custom", "QA-ConsentAllowed", "[\"communications.manage\"]"), ("denied", "QA-ConsentDenied", "[]"), ("grant", "QA-ConsentGrant", "[]"), ("permanent", "QA-ConsentPermanent", "[]"), ("expired", "QA-ConsentExpired", "[]"), ("revoked", "QA-ConsentRevoked", "[]"), ("foreignGrant", "QA-ConsentForeignGrant", "[]"), ("platform", "PlatformOwner", "[]") })
        {
            var email = $"qa-consent-{pair.Item1}@example.invalid"; var id = await CreateAccessActorAsync(factory, email, pair.Item2, academy, permissions: pair.Item3);
            actors[pair.Item1] = (id, await LoginAsync(client, email, "Synthetic!39Ab"));
        }
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            foreach (var name in new[] { "grant", "permanent", "expired", "revoked", "foreignGrant" }) identity.AccessGrants.Add(new AccessGrant { AcademyId = name == "foreignGrant" ? foreign : academy, UserId = actors[name].Id, PermissionsJson = "[\"communications.manage\"]", IsPermanent = name == "permanent", ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(name == "expired" ? -1 : 1), RevokedAtUtc = name == "revoked" ? DateTimeOffset.UtcNow : null, GrantedByUserId = actors["owner"].Id });
            var platform = (await users.FindByIdAsync(actors["platform"].Id.ToString()))!; platform.IsPlatformOwner = true; if (!(await users.UpdateAsync(platform)).Succeeded) throw new InvalidOperationException("Platform fixture failed");
            await identity.SaveChangesAsync();
            foreach (var pair in new[] { ("admin", "qa-admin-a@example.invalid"), ("teacher", "qa-teacher-a@example.invalid"), ("foreign", "qa-admin-b@example.invalid") }) { var u = (await users.FindByEmailAsync(pair.Item2))!; actors[pair.Item1] = (u.Id, await LoginAsync(client, pair.Item2, "Synthetic!39Ab")); }
        }
        client.DefaultRequestHeaders.Authorization = null;
        async Task<string> Snapshot()
        { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { preferences = await db.CommunicationPreferences.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? actor, object? payload = null)
        { await Task.Delay(650); using var request = new HttpRequestMessage(method, path); if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actors[actor].Token); if (payload is not null) request.Content = JsonContent.Create(payload); return await client.SendAsync(request); }
        var body = new SaveCommunicationPreferenceRequest(true, false, true, "  Synthetic consent  "); string Route(Guid target) => $"/api/academies/{target}/communication-preferences";
        void Pass(string label) { count++; Console.WriteLine($"CONSENTACCESS CASE {label} PASS."); }
        async Task Deny(string label, string? actor, string suffix, HttpMethod method, HttpStatusCode status = HttpStatusCode.Forbidden, Guid? target = null, object? payload = null)
        { var before = await Snapshot(); using var response = await Send(method, Route(target ?? academy) + suffix, actor, method == HttpMethod.Put ? payload ?? body : null); RequireFinanceStatus(response, status, label); if (before != await Snapshot()) throw new InvalidOperationException("Denied consent request wrote data/audit " + label); Pass(label); }
        if (Environment.GetEnvironmentVariable("QA_CONSENT_BASELINE") == "1")
        {
            await Deny("baseline-manager-list-403", "manager", "", HttpMethod.Get);
            await Deny("baseline-manager-save-403", "manager", $"/Student/{student}", HttpMethod.Put);
            foreach (var endpoint in new[] { "students", "guardians" }) { using var response = await Send(HttpMethod.Get, $"/api/academies/{academy}/{endpoint}", "manager"); RequireFinanceStatus(response, HttpStatusCode.Forbidden, endpoint); Pass("baseline-manager-" + endpoint + "-403"); }
            Console.WriteLine("CONSENTACCESS BASELINE PASS:4 blocked Manager paths reproduced through unchanged global filter/action policy. No customer data/outbound."); return;
        }
        async Task List(string actor, string? type = null)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, Route(academy) + (type is null ? "" : "?recipientType=" + type), actor); RequireFinanceStatus(response, HttpStatusCode.OK, actor + " list");
            var summaries = await response.Content.ReadFromJsonAsync<List<CommunicationPreferenceSummary>>(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var rows = await db.CommunicationPreferences.AsNoTracking().Where(x => x.AcademyId == academy && (type == null || x.RecipientType == type)).OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).ToListAsync();
            if (!summaries!.SequenceEqual(rows.Select(x => new CommunicationPreferenceSummary(x.Id, x.RecipientId, x.RecipientType, x.EmailAllowed, x.WhatsAppAllowed, x.MarketingAllowed, x.EmailOptedInAtUtc, x.WhatsAppOptedInAtUtc, x.OptedOutAtUtc, x.Notes))) || before != await Snapshot()) throw new InvalidOperationException("Scoped summary/read-only mismatch"); Pass(actor + "-list-" + (type ?? "all"));
        }
        async Task Lookup(string actor)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, Route(academy) + "/recipients", actor); RequireFinanceStatus(response, HttpStatusCode.OK, actor + " lookup"); var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            if (!data.EnumerateObject().Select(x => x.Name).Order().SequenceEqual(new[] { "guardians", "students" })) throw new InvalidOperationException("Unexpected lookup groups");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var group in new[] { "students", "guardians" })
            {
                var expected = group == "students" ? await db.Students.AsNoTracking().Where(x => x.AcademyId == academy).OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Select(x => new { x.Id, x.FirstName, x.LastName, x.Email }).ToListAsync() : await db.Guardians.AsNoTracking().Where(x => x.AcademyId == academy).OrderBy(x => x.LastName).ThenBy(x => x.FirstName).Select(x => new { x.Id, x.FirstName, x.LastName, x.Email }).ToListAsync();
                var got = data.GetProperty(group); if (got.GetRawText() != JsonSerializer.Serialize(expected, new JsonSerializerOptions(JsonSerializerDefaults.Web))) throw new InvalidOperationException("Lookup is not exact minimal scoped data");
                foreach (var item in got.EnumerateArray()) if (!item.EnumerateObject().Select(x => x.Name).Order().SequenceEqual(new[] { "email", "firstName", "id", "lastName" })) throw new InvalidOperationException("Unneeded contact fields exposed");
            }
            if (before != await Snapshot()) throw new InvalidOperationException("Lookup wrote data"); Pass(actor + "-minimal-lookup");
        }
        async Task Save(string actor, string type, Guid recipient, SaveCommunicationPreferenceRequest request)
        {
            using var before = JsonDocument.Parse(await Snapshot()); using var response = await Send(HttpMethod.Put, Route(academy) + $"/{type}/{recipient}", actor, request); RequireFinanceStatus(response, HttpStatusCode.OK, actor + " save"); var summary = await response.Content.ReadFromJsonAsync<CommunicationPreferenceSummary>();
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var canonical = type.Equals("Student", StringComparison.OrdinalIgnoreCase) ? "Student" : "Guardian"; var saved = await db.CommunicationPreferences.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.RecipientId == recipient && x.RecipientType == canonical);
            var expected = new CommunicationPreferenceSummary(saved.Id, saved.RecipientId, saved.RecipientType, saved.EmailAllowed, saved.WhatsAppAllowed, saved.MarketingAllowed, saved.EmailOptedInAtUtc, saved.WhatsAppOptedInAtUtc, saved.OptedOutAtUtc, saved.Notes);
            if (summary != expected || saved.EmailAllowed != request.EmailAllowed || saved.WhatsAppAllowed != request.WhatsAppAllowed || saved.MarketingAllowed != request.MarketingAllowed || saved.Notes != (string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()) || (saved.EmailOptedInAtUtc.HasValue != request.EmailAllowed) || (saved.WhatsAppOptedInAtUtc.HasValue != request.WhatsAppAllowed) || (saved.OptedOutAtUtc.HasValue != (!request.EmailAllowed && !request.WhatsAppAllowed))) throw new InvalidOperationException("Consent response/storage/timestamp mismatch");
            using var after = JsonDocument.Parse(await Snapshot()); foreach (var field in new[] { "students", "guardians", "notifications" }) if (!JsonElement.DeepEquals(before.RootElement.GetProperty(field), after.RootElement.GetProperty(field))) throw new InvalidOperationException("Consent save changed other source data");
            var previousRows = before.RootElement.GetProperty("preferences").EnumerateArray().Where(x => x.GetProperty("Id").GetGuid() != saved.Id).Select(x => x.GetRawText()); var otherRows = after.RootElement.GetProperty("preferences").EnumerateArray().Where(x => x.GetProperty("Id").GetGuid() != saved.Id).Select(x => x.GetRawText()); if (!previousRows.SequenceEqual(otherRows)) throw new InvalidOperationException("Unrelated preference changed");
            var oldAudits = before.RootElement.GetProperty("audits"); var oldIds = oldAudits.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var all = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var added = all.Where(x => !oldIds.Contains(x.Id)).ToArray();
            if (added.Length != 1 || added[0].ActorUserId != actors[actor].Id || added[0].AcademyId != academy || added[0].Action != "PUT CommunicationPreferences" || JsonSerializer.Serialize(all.Where(x => oldIds.Contains(x.Id))) != oldAudits.GetRawText()) throw new InvalidOperationException("Existing actor/route audit contract not preserved"); Pass(actor + "-save-" + type);
        }
        foreach (var actor in new[] { "owner", "admin", "manager", "custom", "grant", "permanent" }) { await List(actor); await Lookup(actor); await Save(actor, "Student", student, body); }
        await Save("manager", "gUaRdIaN", guardian, new(false, true, false, null)); await Save("manager", "Guardian", inactiveGuardian, new(false, false, false, "")); await List("manager", "Guardian"); await List("manager", "Teacher");
        foreach (var actor in new[] { "operations", "finance", "teacher", "denied", "expired", "revoked", "foreignGrant", "foreign", "platform" }) foreach (var spec in new[] { ("", HttpMethod.Get), ("/recipients", HttpMethod.Get), ($"/Student/{student}", HttpMethod.Put) }) await Deny(actor + "-" + spec.Item1, actor, spec.Item1, spec.Item2);
        foreach (var spec in new[] { ("", HttpMethod.Get), ("/recipients", HttpMethod.Get), ($"/Student/{student}", HttpMethod.Put) }) { await Deny("anonymous-" + spec.Item1, null, spec.Item1, spec.Item2, HttpStatusCode.Unauthorized); await Deny("foreign-route-" + spec.Item1, "manager", spec.Item1, spec.Item2, target: foreign); }
        foreach (var endpoint in new[] { "students", "guardians", "notifications", "communication-settings", "communication-templates" }) { var before = await Snapshot(); using var response = await Send(HttpMethod.Get, $"/api/academies/{academy}/{endpoint}", "manager"); RequireFinanceStatus(response, HttpStatusCode.Forbidden, "no expanded " + endpoint); if (before != await Snapshot()) throw new InvalidOperationException("Denied broad lookup changed data"); Pass("manager-no-broad-" + endpoint); }
        await Deny("foreign-recipient", "manager", $"/Student/{foreignStudent}", HttpMethod.Put, HttpStatusCode.NotFound); await Deny("missing-recipient", "manager", $"/Student/{Guid.NewGuid()}", HttpMethod.Put, HttpStatusCode.NotFound); await Deny("unsupported-type", "manager", $"/Teacher/{student}", HttpMethod.Put, HttpStatusCode.BadRequest);
        async Task State(string mode, bool enabled)
        { using var scope = factory.Services.CreateScope(); if (mode == "user") { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await users.FindByIdAsync(actors["manager"].Id.ToString()))!; u.IsActive = enabled; if (!(await users.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("User fixture change failed"); } else { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var a = await db.Academies.SingleAsync(x => x.Id == academy); if (mode == "academy") a.IsActive = enabled; else a.EnabledModulesJson = enabled ? "[\"Core\",\"Engagement\"]" : "[\"Core\"]"; await db.SaveChangesAsync(); } }
        foreach (var mode in new[] { "user", "academy", "module" }) { await State(mode, false); foreach (var spec in new[] { ("", HttpMethod.Get), ("/recipients", HttpMethod.Get), ($"/Student/{student}", HttpMethod.Put) }) await Deny("disabled-" + mode + "-" + spec.Item1, "manager", spec.Item1, spec.Item2); await State(mode, true); }
        await List("manager"); await Lookup("manager");
        Console.WriteLine($"CONSENTACCESS REGRESSION PASS:{count} cases; real Identity/full global filter/HTTP/fresh SQL; Manager/Owner/Admin/custom role/current grants; denied/expired/revoked/foreign/suspended/module; narrow exact recipient fields; broad data/other communications not granted; complete preference/audit preservation. Browser/mobile/general feedback/concurrency/audit rollback/critical/release pending; no outbound/dev/Azure.");
    }
}
