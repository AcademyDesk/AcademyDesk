using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyNotificationChannelAsync(QaApiFactory factory, HttpClient client)
    {
        var fixtures = File.ReadLines("QA/EVIDENCE/logs/phase-2b-communication-channel-ui.log").Where(x => x.StartsWith("CHANNELCHOICE PAYLOAD ")).Select(x => JsonDocument.Parse(x["CHANNELCHOICE PAYLOAD ".Length..]).RootElement.Clone()).ToArray();
        if (fixtures.Length != 22 || fixtures.Select(x => x.GetProperty("label").GetString()).Distinct().Count() != 22) throw new InvalidOperationException("Capture actual compose payloads first");
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web); var count = 0;
        Guid academy, foreign;
        var people = new Dictionary<string, Guid> { ["Guardian"] = Guid.Parse("40000000-0000-0000-0000-000000000001"), ["Student"] = Guid.Parse("40000000-0000-0000-0000-000000000002"), ["Teacher"] = Guid.Parse("40000000-0000-0000-0000-000000000003") };
        var templateIds = new Dictionary<string, Guid> { ["Email"] = Guid.Parse("50000000-0000-0000-0000-000000000001"), ["WhatsApp"] = Guid.Parse("50000000-0000-0000-0000-000000000002") };
        var foreignTemplate = Guid.NewGuid(); var inactiveTemplate = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            db.AddRange(new Guardian { Id = people["Guardian"], AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" }, new Student { Id = people["Student"], AcademyId = academy, FirstName = "Synthetic", LastName = "Student" }, new Teacher { Id = people["Teacher"], AcademyId = academy, FirstName = "Synthetic", LastName = "Teacher" });
            foreach (var channel in templateIds.Keys) db.Add(new CommunicationTemplate { Id = templateIds[channel], AcademyId = academy, Channel = channel, Name = "Synthetic " + channel, TemplateKey = "synthetic-" + channel.ToLowerInvariant(), Category = "Utility", Status = "Approved", Body = "Hello {{name}}" });
            db.AddRange(new CommunicationTemplate { Id = foreignTemplate, AcademyId = foreign, Channel = "Email", Name = "Foreign", TemplateKey = "foreign", Category = "Utility", Status = "Approved", Body = "Foreign" }, new CommunicationTemplate { Id = inactiveTemplate, AcademyId = academy, Channel = "Email", Name = "Inactive", TemplateKey = "inactive", Category = "Utility", Status = "Approved", Body = "Inactive", IsActive = false }, new Notification { AcademyId = foreign, RecipientType = "Academy", Title = "Foreign sentinel", Message = "Preserved", Channel = "InApp", Status = "Queued" });
            await db.SaveChangesAsync();
        }
        await CreateAccessActorAsync(factory, "qa-compose-grant@example.invalid", "QA-ComposeGrant", academy, permissions: "[\"communications.manage\"]");
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"); var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab"); var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab"); var grantToken = await LoginAsync(client, "qa-compose-grant@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        NotificationSummary Summary(Notification x) => new(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.TemplateId, x.VariablesJson, x.FailureReason, x.ScheduledAtUtc, x.SentAtUtc);
        async Task<string> Snapshot()
        { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), templates = await db.CommunicationTemplates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), preferences = await db.CommunicationPreferences.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        async Task<HttpResponseMessage> Send(HttpMethod method, Guid target, object? body, string? auth)
        { await Task.Delay(650); using var request = new HttpRequestMessage(method, $"/api/academies/{target}/notifications"); if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth); if (body is not null) request.Content = JsonContent.Create(body); return await client.SendAsync(request); }
        void Pass(string label) { count++; Console.WriteLine($"CHANNELCHOICE CASE {label} PASS."); }
        async Task Read(Guid target, string auth)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, target, null, auth); RequireFinanceStatus(response, HttpStatusCode.OK, "owned-notification-read");
            var summaries = await response.Content.ReadFromJsonAsync<List<NotificationSummary>>(options) ?? throw new InvalidOperationException("Missing scoped summaries");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var rows = await db.Notifications.AsNoTracking().Where(x => x.AcademyId == target).ToListAsync();
            if (!summaries.OrderBy(x => x.Id).SequenceEqual(rows.Select(Summary).OrderBy(x => x.Id)) || before != await Snapshot()) throw new InvalidOperationException("Full GET scope/read-only mismatch");
        }
        async Task Valid(string label, CreateNotificationRequest request, string expectedChannel, string expectedStatus)
        {
            using var before = JsonDocument.Parse(await Snapshot()); using var response = await Send(HttpMethod.Post, academy, request, token); RequireFinanceStatus(response, HttpStatusCode.Created, label);
            var summary = await response.Content.ReadFromJsonAsync<NotificationSummary>(options) ?? throw new InvalidOperationException("Missing full summary");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var saved = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
            var vars = request.Variables ?? new Dictionary<string, string>(); var body = Regex.Replace(request.Message!.Trim(), "\\{\\{([a-zA-Z0-9_]+)\\}\\}", m => vars.TryGetValue(m.Groups[1].Value, out var value) ? value : m.Value);
            if (Summary(saved) != summary || saved.AcademyId != academy || summary.Channel != expectedChannel || summary.Status != expectedStatus || summary.SentAtUtc is not null || summary.TemplateId != request.TemplateId || summary.Message != body || summary.Title != request.Title!.Trim() || summary.RecipientId != request.RecipientId || summary.RecipientType != request.RecipientType!.Trim() || summary.ScheduledAtUtc != request.ScheduledAtUtc) throw new InvalidOperationException("Full created row/payload/response differs: " + label);
            if (request.IsImportant) { using var metadata = JsonDocument.Parse(summary.VariablesJson!); if (summary.Channel != "InApp" || summary.Status != "Queued" || metadata.RootElement.GetProperty("audiences").GetString() != request.Variables!["audiences"] || metadata.RootElement.GetProperty("expiresAtUtc").GetDateTime() - metadata.RootElement.GetProperty("startsAtUtc").GetDateTime() != TimeSpan.FromHours(request.DisplayHours!.Value)) throw new InvalidOperationException("Banner audience/duration mismatch"); }
            using var after = JsonDocument.Parse(await Snapshot());
            var oldNotices = before.RootElement.GetProperty("notices"); var oldIds = oldNotices.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
            var all = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); if (all.Count != oldIds.Count + 1 || JsonSerializer.Serialize(all.Where(x => oldIds.Contains(x.Id))) != oldNotices.GetRawText()) throw new InvalidOperationException("Unrelated/foreign notification changed");
            foreach (var field in new[] { "templates", "channels", "preferences" }) if (!JsonElement.DeepEquals(before.RootElement.GetProperty(field), after.RootElement.GetProperty(field))) throw new InvalidOperationException("Source settings changed");
            var oldAudits = before.RootElement.GetProperty("audits"); var auditIds = oldAudits.EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(); var added = audits.Where(x => !auditIds.Contains(x.Id)).ToArray();
            if (added.Length != 2 || JsonSerializer.Serialize(audits.Where(x => auditIds.Contains(x.Id))) != oldAudits.GetRawText() || added.Single(x => x.Action == "NotificationQueued").EntityId != saved.Id || added.Any(x => x.AcademyId != academy) || !added.Any(x => x.Action == "POST Notifications" && x.ActorUserId.HasValue)) throw new InvalidOperationException("Existing success audit contract mismatch");
            await Read(academy, token); Pass(label);
        }
        async Task Denied(string label, CreateNotificationRequest request, string? auth, HttpStatusCode expected = HttpStatusCode.BadRequest, Guid? target = null)
        { var before = await Snapshot(); using var response = await Send(HttpMethod.Post, target ?? academy, request, auth); RequireFinanceStatus(response, expected, label); if (before != await Snapshot()) throw new InvalidOperationException("Rejected notification/audit/source write: " + label); Pass(label); }
        foreach (var fixture in fixtures) { var request = fixture.GetProperty("payload").Deserialize<CreateNotificationRequest>(options)!; await Valid(fixture.GetProperty("label").GetString()!, request, request.Channel!, request.Channel == "InApp" ? "Queued" : "BlockedConsent"); }
        CreateNotificationRequest Body(string type = "Guardian", string? channel = "InApp", Guid? template = null, bool important = false) => new(important ? null : people.GetValueOrDefault(type, people["Guardian"]), type, "Synthetic title", "Hello {{name}}", channel, null, template, new() { ["name"] = "Synthetic", ["audiences"] = "Student,Teacher" }, important, important ? 4 : null);
        foreach (var type in people.Keys) foreach (var template in templateIds.Keys) foreach (var channel in new[] { "InApp", template == "Email" ? "WhatsApp" : "Email" }) await Denied(type + "-" + template + "-conflict-" + channel, Body(type, channel, templateIds[template]), token);
        foreach (var channel in templateIds.Keys) { await Denied("banner-external-channel-" + channel, Body("Academy", channel, null, true), token); await Denied("banner-hidden-external-template-" + channel, Body("Academy", null, templateIds[channel], true), token); }
        foreach (var item in new[] { ("foreign-template", foreignTemplate), ("inactive-template", inactiveTemplate), ("missing-template", Guid.NewGuid()) }) await Denied(item.Item1, Body(channel: "Email", template: item.Item2), token);
        foreach (var channel in templateIds.Keys) await Valid("legacy-unset-channel-" + channel, Body(channel: null, template: templateIds[channel]), channel, "BlockedConsent");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Add(new CommunicationPreference { AcademyId = academy, RecipientId = people["Guardian"], RecipientType = "Guardian", EmailAllowed = true, WhatsAppAllowed = true }); await db.SaveChangesAsync(); }
        foreach (var channel in templateIds.Keys) await Valid("consented-awaiting-connection-" + channel, Body(channel: channel, template: templateIds[channel]), channel, "AwaitingConnection");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); foreach (var channel in templateIds.Keys) db.Add(new CommunicationChannel { AcademyId = academy, Channel = channel, Provider = "Synthetic", MessagesEnabled = true, HasSecureConnection = true }); await db.SaveChangesAsync(); }
        foreach (var channel in templateIds.Keys) await Valid("synthetic-connected-remains-queued-" + channel, Body(channel: channel, template: templateIds[channel]), channel, "Queued");
        await Denied("foreign-route", Body(), token, HttpStatusCode.Forbidden, foreign); await Denied("foreign-actor", Body(), foreignToken, HttpStatusCode.Forbidden); await Denied("anonymous", Body(), null, HttpStatusCode.Unauthorized); await Denied("teacher-no-grant", Body(), teacherToken, HttpStatusCode.Forbidden); await Denied("custom-communications-grant-existing-denial", Body(), grantToken, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = "[\"Core\"]"; await db.SaveChangesAsync(); }
        await Denied("engagement-disabled", Body(), token, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = "[\"Core\",\"Engagement\"]"; row.IsActive = false; await db.SaveChangesAsync(); }
        await Denied("academy-inactive", Body(), token, HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = true; await db.SaveChangesAsync(); }
        await Read(academy, token); Pass("owned-full-list-readback"); await Read(foreign, foreignToken); Pass("foreign-full-list-preserved");
        if (count != 56) throw new InvalidOperationException("Unexpected channel-choice case count: " + count);
        Console.WriteLine("CHANNELCHOICE REGRESSION PASS:56 cases;22 actual compose payloads through real Identity/model binding/HTTP/fresh SQL/complete GET; conflicts reject without notification/audit/source writes; intended channel/body/audience/status/duration; existing consent/connection/access/two-log audits preserved; SentAtUtc stays null, no provider/outbound delivery. Browser/lifecycle/concurrency/audit rollback/critical/release pending.");
    }
}
