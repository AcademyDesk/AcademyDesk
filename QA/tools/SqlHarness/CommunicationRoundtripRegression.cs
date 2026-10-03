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
    private static async Task VerifyCommunicationRoundtripAsync(QaApiFactory factory, HttpClient client)
    {
        var fixtures = File.ReadLines("QA/EVIDENCE/logs/phase-2b-communication-roundtrip-ui.log").Where(x => x.StartsWith("CHANNELROUNDTRIP PAYLOAD ", StringComparison.Ordinal)).Select(x => JsonDocument.Parse(x["CHANNELROUNDTRIP PAYLOAD ".Length..]).RootElement.Clone()).ToArray();
        if (fixtures.Length != 24 || fixtures.Select(x => x.GetProperty("label").GetString()).Distinct().Count() != 24) throw new InvalidOperationException("Run the actual channel TSX payload tests first");
        Guid academy, foreign, actor;
        var count = 0; var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            db.Add(new CommunicationChannel { AcademyId = foreign, Channel = "Email", Provider = "SMTP", Status = "Configured", SenderAddress = "foreign@example.invalid", ReplyToAddress = "foreign-reply@example.invalid", PhoneNumber = "foreign-number", ExternalAccountReference = "foreign-reference", MessagesEnabled = true, HasSecureConnection = true });
            await db.SaveChangesAsync();
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        var ownerActor = await CreateAccessActorAsync(factory, "qa-channel-owner@example.invalid", "Owner", academy);
        await CreateAccessActorAsync(factory, "qa-channel-grant@example.invalid", "QA-CommunicationGrant", academy, permissions: "[\"communications.manage\"]");
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var ownerToken = await LoginAsync(client, "qa-channel-owner@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var grantToken = await LoginAsync(client, "qa-channel-grant@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        CommunicationChannelSummary Summary(CommunicationChannel x) => new(x.Id, x.Channel, x.Provider, x.Status, x.SenderName, x.SenderAddress, x.ReplyToAddress, x.PhoneNumber, x.ExternalAccountReference, x.MessagesEnabled, x.HasSecureConnection);
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, Guid target, string? channel, object? payload, string? auth)
        {
            await Task.Delay(650); using var request = new HttpRequestMessage(method, $"/api/academies/{target}/communication-settings" + (channel is null ? "" : "/" + channel));
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (payload is not null) request.Content = JsonContent.Create(payload); return await client.SendAsync(request);
        }
        async Task Seed(JsonElement initial)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            // Only synthetic rows in the generated owned database; no customer/dev database is reachable here.
            db.RemoveRange(await db.CommunicationChannels.Where(x => x.AcademyId == academy).ToListAsync()); await db.SaveChangesAsync();
            foreach (var fixture in initial.EnumerateArray())
            {
                var row = fixture.Deserialize<CommunicationChannel>(options) ?? throw new InvalidOperationException("Incomplete fixture"); row.AcademyId = academy; db.Add(row);
            }
            await db.SaveChangesAsync();
        }
        async Task<JsonElement> Read(Guid target, string auth)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, target, null, null, auth); RequireFinanceStatus(response, HttpStatusCode.OK, "owned-read");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.CommunicationChannels.AsNoTracking().Where(x => x.AcademyId == target).OrderBy(x => x.Channel).ToListAsync();
            var summaries = json.RootElement.Deserialize<List<CommunicationChannelSummary>>(options)!;
            if (!summaries.SequenceEqual(rows.Select(Summary)) || before != await Snapshot()) throw new InvalidOperationException("GET scope/complete fields/read-only mismatch");
            return json.RootElement.Clone();
        }
        void Pass(string label) { count++; Console.WriteLine($"CHANNELROUNDTRIP CASE {label} PASS."); }
        async Task Valid(string label, string channel, JsonElement payload, JsonElement expected, bool secure, string? auth = null, Guid? expectedActor = null)
        {
            using var before = JsonDocument.Parse(await Snapshot());
            using var response = await Send(HttpMethod.Put, academy, channel, payload, auth ?? token); RequireFinanceStatus(response, HttpStatusCode.OK, label);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var all = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var row = all.Single(x => x.AcademyId == academy && x.Channel == channel);
            var oldRows = before.RootElement.GetProperty("channels"); var oldTarget = oldRows.EnumerateArray().SingleOrDefault(x => x.GetProperty("AcademyId").GetGuid() == academy && x.GetProperty("Channel").GetString() == channel);
            var oldOther = oldRows.EnumerateArray().Where(x => !(x.GetProperty("AcademyId").GetGuid() == academy && x.GetProperty("Channel").GetString() == channel)).Select(x => x.GetRawText());
            var other = all.Where(x => x.Id != row.Id).Select(x => JsonSerializer.Serialize(x));
            if (!other.SequenceEqual(oldOther) || row.HasSecureConnection != secure || row.UpdatedAtUtc is null || oldTarget.ValueKind != JsonValueKind.Undefined && (row.Id != oldTarget.GetProperty("Id").GetGuid() || row.CreatedAtUtc != oldTarget.GetProperty("CreatedAtUtc").GetDateTime())) throw new InvalidOperationException("Unrelated/readonly/identity field changed: " + label);
            var summary = Summary(row); if (body.RootElement.Deserialize<CommunicationChannelSummary>(options) != summary) throw new InvalidOperationException("Response differs from fresh SQL");
            var projected = JsonSerializer.SerializeToElement(summary, options);
            foreach (var field in expected.EnumerateObject()) if (!JsonElement.DeepEquals(projected.GetProperty(field.Name), field.Value)) throw new InvalidOperationException("Actual TSX payload normalization mismatch: " + label + "/" + field.Name);
            var audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var oldAudits = before.RootElement.GetProperty("audits"); var oldIds = oldAudits.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
            var added = audits.Where(x => !oldIds.Contains(x.Id)).ToArray();
            if (added.Length != 2 || JsonSerializer.Serialize(audits.Where(x => oldIds.Contains(x.Id))) != oldAudits.GetRawText()) throw new InvalidOperationException("Existing two-log audit contract changed");
            var domain = added.Single(x => x.Action == "CommunicationChannelSaved"); var filtered = added.Single(x => x.Action == "PUT CommunicationSettings");
            if (domain.AcademyId != academy || domain.EntityType != "CommunicationChannel" || domain.EntityId != row.Id || filtered.AcademyId != academy || filtered.ActorUserId != (expectedActor ?? actor)) throw new InvalidOperationException("Audit ownership/entity mismatch");
            using var metadata = JsonDocument.Parse(domain.MetadataJson!);
            if (metadata.RootElement.EnumerateObject().Count() != 4 || metadata.RootElement.GetProperty("Channel").GetString() != channel || metadata.RootElement.GetProperty("Provider").GetString() != row.Provider || metadata.RootElement.GetProperty("Status").GetString() != row.Status || metadata.RootElement.GetProperty("MessagesEnabled").GetBoolean() != row.MessagesEnabled) throw new InvalidOperationException("Audit metadata contract changed");
            using var after = JsonDocument.Parse(await Snapshot()); if (before.RootElement.GetProperty("notifications").GetRawText() != after.RootElement.GetProperty("notifications").GetRawText()) throw new InvalidOperationException("Configuration save created outbound/notification rows");
            var readback = await Read(academy, auth ?? token); var returned = readback.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == row.Id);
            if (returned.Deserialize<CommunicationChannelSummary>(options) != summary) throw new InvalidOperationException("GET after PUT mismatch"); Pass(label);
        }
        foreach (var fixture in fixtures)
        {
            await Seed(fixture.GetProperty("initial")); await Read(academy, token);
            await Valid(fixture.GetProperty("label").GetString()!, fixture.GetProperty("channel").GetString()!, fixture.GetProperty("payload"), fixture.GetProperty("expected"), fixture.GetProperty("secureConnection").GetBoolean());
        }
        // Direct callers retain deliberate full-replacement clears, including hidden fields.
        foreach (var channel in new[] { "Email", "WhatsApp", "Meeting" })
        {
            await Seed(fixtures[0].GetProperty("initial"));
            var payload = JsonSerializer.SerializeToElement(new { provider = "SyntheticProvider", status = "NotConfigured", senderName = "", senderAddress = (string?)null, replyToAddress = (string?)null, phoneNumber = "", externalAccountReference = "  ", messagesEnabled = true }, options);
            var expected = JsonSerializer.SerializeToElement(new { provider = "SyntheticProvider", status = "NotConfigured", senderName = (string?)null, senderAddress = (string?)null, replyToAddress = (string?)null, phoneNumber = (string?)null, externalAccountReference = (string?)null, messagesEnabled = false }, options);
            await Valid("direct-explicit-clear-" + channel, channel, payload, expected, true);
        }
        await Seed(fixtures[0].GetProperty("initial"));
        await Valid("owner-existing-access", "Email", fixtures[0].GetProperty("payload"), fixtures[0].GetProperty("expected"), true, ownerToken, ownerActor);
        async Task Denied(string label, string channel, object? payload, Guid? target = null, string? auth = null, HttpStatusCode expected = HttpStatusCode.BadRequest, HttpMethod? method = null)
        {
            var before = await Snapshot(); using var response = await Send(method ?? HttpMethod.Put, target ?? academy, (method == HttpMethod.Get) ? null : channel, payload, auth); RequireFinanceStatus(response, expected, label);
            if (before != await Snapshot()) throw new InvalidOperationException("Denied settings request changed channels/audits/notifications: " + label); Pass(label);
        }
        object Body(string? provider = "Synthetic", string? status = "Configured", string? address = "sender@example.invalid", string? phone = "synthetic-number", string? reference = "synthetic-reference") => new { provider, status, senderName = "Synthetic", senderAddress = address, replyToAddress = "reply@example.invalid", phoneNumber = phone, externalAccountReference = reference, messagesEnabled = true };
        await Denied("unsupported-channel", "SMS", Body(), auth: token);
        await Denied("blank-provider", "Email", Body(provider: "  "), auth: token);
        await Denied("unknown-status", "Email", Body(status: "Unknown"), auth: token);
        await Denied("configured-email-missing-address", "Email", Body(address: null), auth: token);
        await Denied("configured-whatsapp-missing-number", "WhatsApp", Body(phone: null), auth: token);
        await Denied("configured-meeting-missing-organizer", "Meeting", Body(address: null), auth: token);
        await Denied("configured-meeting-missing-reference", "Meeting", Body(reference: null), auth: token);
        await Denied("null-provider-binding", "Email", Body(provider: null), auth: token);
        await Denied("null-status-binding", "Email", Body(status: null), auth: token);
        await Denied("foreign-route", "Email", Body(), foreign, token, HttpStatusCode.Forbidden);
        await Denied("foreign-actor", "Email", Body(), academy, foreignToken, HttpStatusCode.Forbidden);
        await Denied("anonymous-save", "Email", Body(), expected: HttpStatusCode.Unauthorized);
        await Denied("teacher-existing-denial", "Email", Body(), auth: teacherToken, expected: HttpStatusCode.Forbidden);
        await Denied("communications-grant-does-not-broaden-owner-rule", "Email", Body(), auth: grantToken, expected: HttpStatusCode.Forbidden);
        await Denied("foreign-read", "Email", null, foreign, token, HttpStatusCode.Forbidden, HttpMethod.Get);
        await Denied("anonymous-read", "Email", null, expected: HttpStatusCode.Unauthorized, method: HttpMethod.Get);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\"]"; await db.SaveChangesAsync(); }
        await Denied("engagement-disabled", "Email", Body(), auth: token, expected: HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = "[\"Core\",\"Engagement\"]"; row.IsActive = false; await db.SaveChangesAsync(); }
        await Denied("academy-inactive", "Email", Body(), auth: token, expected: HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = true; await db.SaveChangesAsync(); }
        var owned = await Read(academy, token); Pass("owned-full-list-readback"); await Read(foreign, foreignToken); Pass("foreign-owned-list-preserved-configuration");
        Console.WriteLine("CHANNELROUNDTRIP FIXTURE " + JsonSerializer.Serialize(new { rows = owned }, options));
        if (count != 48) throw new InvalidOperationException("Unexpected channel case count: " + count);
        Console.WriteLine("CHANNELROUNDTRIP REGRESSION PASS:48 cases;24 actual TSX payloads through real Identity/HTTP/fresh SQL/full GET; all hidden/readonly/unrelated channel fields preserved, intentional status normalization/direct clears, existing Owner/Admin and tenant/module/auth gates, unchanged two-log success audit and no rejected data/audit/notification writes; no credentials/secure connection/outbound provider contacted; browser/drafts/concurrency/audit rollback/release pending.");
    }
}
