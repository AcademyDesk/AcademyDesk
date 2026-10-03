using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMarketingConsentAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_MARKETING_BASELINE") == "1";
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web); Guid academy, foreign;
        var student = Guid.NewGuid(); var guardian = Guid.NewGuid(); var templates = new Dictionary<string, Guid> { ["Email"] = Guid.NewGuid(), ["WhatsApp"] = Guid.NewGuid() };
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync(); foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            db.AddRange(new Student { Id = student, AcademyId = academy, FirstName = "Synthetic", LastName = "Student" }, new Guardian { Id = guardian, AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" }, new Notification { AcademyId = foreign, RecipientType = "Academy", Title = "Foreign sentinel", Message = "Preserve", Channel = "InApp", Status = "Queued" });
            foreach (var channel in templates.Keys) { db.Add(new CommunicationTemplate { Id = templates[channel], AcademyId = academy, Channel = channel, Name = "Synthetic title", TemplateKey = "synthetic-" + channel, Category = "Marketing", Status = "Approved", Body = "Hello {{name}}" }); }
            await db.SaveChangesAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"); var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab"); var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        var count = 0; void Pass(string label) { count++; Console.WriteLine($"MARKETING CASE {label} PASS."); }
        string Route(Guid target) => $"/api/academies/{target}/notifications";
        async Task<HttpResponseMessage> Send(HttpMethod method, string route, object? body, string? auth)
        { await Task.Delay(650); using var request = new HttpRequestMessage(method, route); if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth); if (body is not null) request.Content = JsonContent.Create(body); return await client.SendAsync(request); }
        async Task<JsonElement> Snapshot()
        { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.SerializeToElement(new { notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), templates = await db.CommunicationTemplates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), preferences = await db.CommunicationPreferences.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        static void Unchanged(JsonElement before, JsonElement after, params string[] except)
        { foreach (var field in before.EnumerateObject()) if (!except.Contains(field.Name) && !JsonElement.DeepEquals(field.Value, after.GetProperty(field.Name))) throw new InvalidOperationException("Unexpected source write " + field.Name); }
        static void Rows(JsonElement before, JsonElement after, string field, int added, Guid? changed = null)
        { var old = before.GetProperty(field).EnumerateArray().ToArray(); var current = after.GetProperty(field).EnumerateArray().ToArray(); if (current.Length != old.Length + added) throw new InvalidOperationException("Row count " + field); foreach (var x in old.Where(x => x.GetProperty("Id").GetGuid() != changed)) if (!JsonElement.DeepEquals(x, current.Single(y => y.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid()))) throw new InvalidOperationException("Unrelated/foreign row changed " + field); }
        NotificationSummary Summary(Notification x) => new(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.TemplateId, x.VariablesJson, x.FailureReason, x.ScheduledAtUtc, x.SentAtUtc);
        async Task Configure(string channel, string category, bool consent, bool marketing, string connection, string type = "Student", string preferenceMode = "Owned")
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var template = await db.CommunicationTemplates.SingleAsync(x => x.Id == templates[channel]); template.Channel = channel; template.Category = category; template.Status = "Approved"; template.IsActive = true;
            db.CommunicationPreferences.RemoveRange(await db.CommunicationPreferences.ToListAsync()); db.CommunicationChannels.RemoveRange(await db.CommunicationChannels.ToListAsync());
            if (preferenceMode != "Missing") db.Add(new CommunicationPreference { AcademyId = preferenceMode == "Foreign" ? foreign : academy, RecipientId = type == "Guardian" ? guardian : student, RecipientType = preferenceMode == "OtherType" ? "Teacher" : type, EmailAllowed = channel == "Email" && consent, WhatsAppAllowed = channel == "WhatsApp" && consent, MarketingAllowed = marketing });
            if (connection != "Missing") db.Add(new CommunicationChannel { AcademyId = academy, Channel = channel, Provider = "Synthetic", MessagesEnabled = connection != "Disabled", HasSecureConnection = connection != "Unsecured" }); await db.SaveChangesAsync();
        }
        async Task<Guid> Create(string label, string channel, string expected, string? reason, string type = "Student", bool manual = false, string? requested = null, string? auth = null, HttpStatusCode? denied = null, bool anonymous = false)
        {
            var before = await Snapshot(); var request = new CreateNotificationRequest(type == "Guardian" ? guardian : student, type, " Synthetic title ", " Hello {{name}} ", requested ?? channel, null, manual ? null : templates[channel], new() { ["name"] = "Synthetic" });
            using var response = await Send(HttpMethod.Post, Route(academy), request, anonymous ? null : auth ?? token); RequireFinanceStatus(response, denied ?? HttpStatusCode.Created, label); var after = await Snapshot();
            if (denied.HasValue) { if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Denied create wrote data"); Pass(label); return Guid.Empty; }
            var summary = await response.Content.ReadFromJsonAsync<NotificationSummary>(json) ?? throw new InvalidOperationException("Missing summary"); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
            if (Summary(row) != summary || row.AcademyId != academy || summary.Channel != channel || summary.Status != expected || summary.FailureReason != reason || summary.TemplateId != request.TemplateId || summary.RecipientId != request.RecipientId || summary.RecipientType != type || summary.Title != "Synthetic title" || summary.Message != "Hello Synthetic" || summary.SentAtUtc is not null || summary.ScheduledAtUtc is not null || summary.VariablesJson != JsonSerializer.Serialize(request.Variables) || row.AttemptCount != 0) throw new InvalidOperationException("Full creation contract differs: " + label);
            Unchanged(before, after, "notices", "audits"); Rows(before, after, "notices", 1); Rows(before, after, "audits", 2);
            var oldIds = before.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var added = (await db.AuditLogs.AsNoTracking().ToListAsync()).Where(x => !oldIds.Contains(x.Id)).ToArray();
            if (!added.Any(x => x.AcademyId == academy && x.Action == "NotificationQueued" && x.EntityId == row.Id) || !added.Any(x => x.AcademyId == academy && x.ActorUserId.HasValue && x.Action == "POST Notifications")) throw new InvalidOperationException("Creation audit mismatch"); Pass(label); return summary.Id;
        }
        async Task Patch(string label, Guid id, string target, bool allowed, string? error = null)
        {
            var before = await Snapshot(); Notification old;
            using (var scope = factory.Services.CreateScope()) { old = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Notifications.AsNoTracking().SingleAsync(x => x.Id == id); }
            using var response = await Send(HttpMethod.Patch, Route(academy) + $"/{id}/status", new UpdateNotificationStatusRequest(target), token); RequireFinanceStatus(response, allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest, label); var after = await Snapshot();
            if (!allowed) { if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Blocked requeue changed data/audit"); if (error is not null && !(await response.Content.ReadAsStringAsync()).Contains(error)) throw new InvalidOperationException("Missing rejection message"); }
            else
            {
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); old.Status = target; if (target == "RetryRequested") old.FailureReason = null; var row = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == id);
                if (JsonSerializer.Serialize(old) != JsonSerializer.Serialize(row)) throw new InvalidOperationException("Unrelated notification fields modified"); Unchanged(before, after, "notices", "audits"); Rows(before, after, "notices", 0, id); Rows(before, after, "audits", 1);
                var oldIds = before.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audit = (await db.AuditLogs.AsNoTracking().ToListAsync()).Single(x => !oldIds.Contains(x.Id)); if (audit.AcademyId != academy || audit.Action != "PATCH Notifications" || !audit.ActorUserId.HasValue) throw new InvalidOperationException("Retry actor audit mismatch");
            }
            Pass(label);
        }
        async Task OptOut(bool channelAllowed)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Put, $"/api/academies/{academy}/communication-preferences/Student/{student}", new SaveCommunicationPreferenceRequest(channelAllowed, channelAllowed, false, " Synthetic opt-out "), token); RequireFinanceStatus(response, HttpStatusCode.OK, "committed-opt-out");
            var summary = await response.Content.ReadFromJsonAsync<CommunicationPreferenceSummary>(json) ?? throw new InvalidOperationException("Missing preference summary"); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.CommunicationPreferences.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
            if (row.MarketingAllowed || summary.MarketingAllowed || row.EmailAllowed != channelAllowed || row.WhatsAppAllowed != channelAllowed || row.Notes != "Synthetic opt-out" || summary.Notes != row.Notes || row.RecipientId != student || row.AcademyId != academy || row.UpdatedAtUtc is null) throw new InvalidOperationException("Opt-out not persisted");
            var after = await Snapshot(); Unchanged(before, after, "preferences", "audits"); Rows(before, after, "preferences", 0, row.Id); Rows(before, after, "audits", 1);
        }
        if (baseline)
        {
            foreach (var channel in templates.Keys) { await Configure(channel, "Marketing", true, false, "Ready"); var id = await Create(channel + "-opt-out-incorrectly-queued", channel, "Queued", null); foreach (var target in new[] { "Queued", "RetryRequested" }) await Patch(channel + "-opt-out-bypass-" + target, id, target, true); }
            Console.WriteLine("MARKETING BASELINE PASS:6 opt-out bypasses reproduced at creation/queue/retry through real HTTP/SQL; no provider/outbound delivery."); return;
        }
        foreach (var channel in templates.Keys) foreach (var category in new[] { "Marketing", "Utility", "Authentication", "Transactional" }) foreach (var consent in new[] { true, false }) foreach (var marketing in new[] { true, false }) foreach (var connection in new[] { "Missing", "Disabled", "Unsecured", "Ready" })
        {
            await Configure(channel, category, consent, marketing, connection); var blocked = !consent || category == "Marketing" && !marketing;
            await Create($"{channel}-{category}-consent-{consent}-marketing-{marketing}-{connection}", channel, blocked ? "BlockedConsent" : connection != "Ready" ? "AwaitingConnection" : "Queued", !consent ? $"No recorded {channel} consent for this recipient." : category == "Marketing" && !marketing ? "No recorded marketing consent for this recipient." : connection != "Ready" ? $"{channel} is not securely connected for this academy." : null);
        }
        foreach (var channel in templates.Keys) foreach (var consent in new[] { true, false }) foreach (var marketing in new[] { true, false })
        { await Configure(channel, "Marketing", consent, marketing, "Ready", "Guardian"); await Create($"{channel}-Guardian-consent-{consent}-marketing-{marketing}", channel, consent && marketing ? "Queued" : "BlockedConsent", !consent ? $"No recorded {channel} consent for this recipient." : !marketing ? "No recorded marketing consent for this recipient." : null, "Guardian"); }
        foreach (var channel in templates.Keys) foreach (var mode in new[] { "Missing", "Foreign", "OtherType" }) { await Configure(channel, "Marketing", true, true, "Ready", preferenceMode: mode); await Create(channel + "-preference-" + mode, channel, "BlockedConsent", $"No recorded {channel} consent for this recipient."); }
        foreach (var channel in templates.Keys) foreach (var category in new[] { "mArKeTiNg", " Marketing " }) { await Configure(channel, category, true, false, "Ready"); await Create(channel + "-legacy-" + category.Trim(), channel, "BlockedConsent", "No recorded marketing consent for this recipient."); }
        foreach (var channel in templates.Keys)
        {
            await Configure(channel, "Marketing", true, false, "Ready");
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.CommunicationTemplates.SingleAsync(x => x.Id == templates[channel])).Channel = channel.ToLowerInvariant(); await db.SaveChangesAsync(); }
            await Create(channel + "-legacy-template-channel", channel, "BlockedConsent", "No recorded marketing consent for this recipient.");
            await Configure(channel, "Marketing", true, true, "Ready"); var id = await Create(channel + "-pre-opt-out", channel, "Queued", null); await OptOut(true);
            foreach (var target in new[] { "Queued", "RetryRequested" }) await Patch(channel + "-committed-marketing-opt-out-" + target, id, target, false, "marketing consent");
            await OptOut(false); foreach (var target in new[] { "Queued", "RetryRequested" }) await Patch(channel + "-committed-channel-opt-out-" + target, id, target, false, channel + " consent"); await Patch(channel + "-cancel-still-allowed", id, "Cancelled", true);
            await Configure(channel, "Utility", true, false, "Ready"); var utilityId = await Create(channel + "-utility-control", channel, "Queued", null); foreach (var target in new[] { "Queued", "RetryRequested" }) await Patch(channel + "-utility-requeue-" + target, utilityId, target, true);
            foreach (var state in new[] { "Disabled", "Inactive", "Foreign" })
            {
                using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var template = await db.CommunicationTemplates.SingleAsync(x => x.Id == templates[channel]); template.Status = state == "Disabled" ? "Disabled" : "Approved"; template.IsActive = state != "Inactive"; template.AcademyId = state == "Foreign" ? foreign : academy; await db.SaveChangesAsync(); }
                foreach (var target in new[] { "Queued", "RetryRequested" }) await Patch(channel + "-template-" + state + "-" + target, id, target, false, "unavailable");
            }
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.CommunicationTemplates.SingleAsync(x => x.Id == templates[channel])).AcademyId = academy; await db.SaveChangesAsync(); }
            await Configure(channel, "Utility", false, false, "Ready"); foreach (var requested in new[] { channel.ToLowerInvariant(), " " + channel.ToUpperInvariant() + " " }) await Create(channel + "-manual-variant-" + requested.Trim(), channel, "BlockedConsent", $"No recorded {channel} consent for this recipient.", manual: true, requested: requested);
            await Configure(channel, "Utility", true, false, "Ready"); await Create(channel + "-manual-not-classified-as-marketing", channel, "Queued", null, manual: true);
        }
        Guid inApp; using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = new Notification { AcademyId = academy, RecipientType = "Academy", Title = "Synthetic", Message = "Preserve", Channel = "InApp", Status = "Cancelled" }; db.Add(row); await db.SaveChangesAsync(); inApp = row.Id; } await Patch("InApp-retry-policy-unchanged", inApp, "RetryRequested", true);
        await Create("anonymous-denied", "Email", "", null, denied: HttpStatusCode.Unauthorized, anonymous: true); await Create("foreign-actor-denied", "Email", "", null, auth: foreignToken, denied: HttpStatusCode.Forbidden); await Create("teacher-denied", "Email", "", null, auth: teacherToken, denied: HttpStatusCode.Forbidden);
        foreach (var (target, auth) in new[] { (academy, token), (foreign, foreignToken) })
        { var before = await Snapshot(); using var response = await Send(HttpMethod.Get, Route(target), null, auth); RequireFinanceStatus(response, HttpStatusCode.OK, "full-notification-list"); var list = await response.Content.ReadFromJsonAsync<List<NotificationSummary>>(json) ?? throw new InvalidOperationException("Missing scoped list"); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); if (!list.OrderBy(x => x.Id).SequenceEqual((await db.Notifications.AsNoTracking().Where(x => x.AcademyId == target).ToListAsync()).Select(Summary).OrderBy(x => x.Id)) || !JsonElement.DeepEquals(before, await Snapshot())) throw new InvalidOperationException("Full scoped read differs"); Pass(target == academy ? "owned-full-readback" : "foreign-full-readback"); }
        if (count != 190) throw new InvalidOperationException("Unexpected marketing case count " + count);
        Console.WriteLine("MARKETING REGRESSION PASS:190 cases;128 category/channel/marketing/connection creation combinations; Student/Guardian, scoped/missing consent, legacy variants, committed opt-outs, Queue/Retry no-write denials, unavailable template, unchanged cancellation/InApp/manual controls, full summaries/readbacks/actor audits and foreign preservation. Real Identity/global filter/HTTP/fresh SQL; null SentAtUtc, no provider/outbound delivery. Browser, simultaneous preference/template races, original-category snapshot and future dispatcher revalidation/critical/release pending.");
    }
}
