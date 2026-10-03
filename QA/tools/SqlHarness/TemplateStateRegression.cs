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
    private static async Task VerifyTemplateStateAsync(QaApiFactory factory, HttpClient client)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_TEMPLATE_STATE_BASELINE") == "1";
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Guid academy, foreign;
        var person = Guid.NewGuid(); var foreignTemplate = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            foreach (var id in new[] { academy, foreign }) (await db.Academies.SingleAsync(x => x.Id == id)).EnabledModulesJson = "[\"Core\",\"Engagement\"]";
            db.AddRange(new Guardian { Id = person, AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" }, new CommunicationTemplate { Id = foreignTemplate, AcademyId = foreign, Channel = "Email", Name = "Foreign sentinel", TemplateKey = "foreign", Category = "Utility", Status = "Approved", Body = "Preserved" }, new Notification { AcademyId = foreign, Title = "Foreign sentinel", Message = "Preserved", Channel = "InApp", RecipientType = "Academy", Status = "Queued" });
            await db.SaveChangesAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignToken = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var count = 0;
        void Pass(string label) { count++; Console.WriteLine($"TEMPLATESTATE CASE {label} PASS."); }
        async Task<HttpResponseMessage> Send(HttpMethod method, string route, object? body = null, string? auth = null)
        {
            await Task.Delay(650);
            using var request = new HttpRequestMessage(method, route);
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        string TemplateRoute(Guid target) => $"/api/academies/{target}/communication-templates";
        string NotificationRoute(Guid target) => $"/api/academies/{target}/notifications";
        async Task<JsonElement> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.SerializeToElement(new { templates = await db.CommunicationTemplates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), notifications = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), preferences = await db.CommunicationPreferences.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        static void Unchanged(JsonElement before, JsonElement after, params string[] except)
        { foreach (var field in before.EnumerateObject()) if (!except.Contains(field.Name) && !JsonElement.DeepEquals(field.Value, after.GetProperty(field.Name))) throw new InvalidOperationException("Unexpected source write: " + field.Name); }
        static void PreserveRows(JsonElement before, JsonElement after, string field, Guid? changed = null, int added = 0)
        {
            var old = before.GetProperty(field).EnumerateArray().ToArray(); var current = after.GetProperty(field).EnumerateArray().ToArray();
            if (current.Length != old.Length + added) throw new InvalidOperationException("Unexpected row count: " + field);
            foreach (var row in old.Where(x => x.GetProperty("Id").GetGuid() != changed)) if (!JsonElement.DeepEquals(row, current.Single(x => x.GetProperty("Id").GetGuid() == row.GetProperty("Id").GetGuid()))) throw new InvalidOperationException("Unrelated or foreign row modified: " + field);
        }
        CommunicationTemplateSummary Summary(CommunicationTemplate x) => new(x.Id, x.Channel, x.Name, x.TemplateKey, x.Category, x.TemplateGroup, x.Status, x.Language, x.ProviderTemplateName, x.Subject, x.Body, x.IsActive);
        NotificationSummary Notice(Notification x) => new(x.Id, x.RecipientId, x.RecipientType, x.Title, x.Message, x.Channel, x.Status, x.TemplateId, x.VariablesJson, x.FailureReason, x.ScheduledAtUtc, x.SentAtUtc);
        SaveCommunicationTemplateRequest Save(string channel, string status, bool active, string? key = null) => new(channel, "Synthetic title", key ?? Guid.NewGuid().ToString("N"), "Utility", "General", status, "en", null, null, "Hello {{name}}", active);
        async Task<CommunicationTemplateSummary> Store(SaveCommunicationTemplateRequest body, Guid? id = null)
        {
            var before = await Snapshot(); using var response = await Send(id.HasValue ? HttpMethod.Put : HttpMethod.Post, TemplateRoute(academy) + (id.HasValue ? $"/{id}" : ""), body, token);
            RequireFinanceStatus(response, id.HasValue ? HttpStatusCode.OK : HttpStatusCode.Created, "store-template");
            var summary = await response.Content.ReadFromJsonAsync<CommunicationTemplateSummary>(json) ?? throw new InvalidOperationException("Missing template summary");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.CommunicationTemplates.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
            if (Summary(row) != summary || row.AcademyId != academy || summary.Channel != body.Channel || summary.Status != body.Status || summary.IsActive != body.IsActive || summary.Body != body.Body || summary.Name != body.Name || summary.TemplateKey != body.TemplateKey) throw new InvalidOperationException("Full template state/summary mismatch");
            var after = await Snapshot(); Unchanged(before, after, "templates", "audits"); PreserveRows(before, after, "templates", id, id.HasValue ? 0 : 1); PreserveRows(before, after, "audits", added: id.HasValue ? 1 : 2);
            var oldIds = before.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy).ToListAsync(); var added = audits.Where(x => !oldIds.Contains(x.Id)).ToArray();
            if (!added.Any(x => x.Action == (id.HasValue ? "PUT" : "POST") + " CommunicationTemplates" && x.ActorUserId.HasValue) || (!id.HasValue && !added.Any(x => x.Action == "CommunicationTemplateCreated" && x.EntityId == summary.Id))) throw new InvalidOperationException("Template audit mismatch");
            return summary;
        }
        async Task Queue(string label, CommunicationTemplateSummary? template, bool available, string? expectedStatus = null, string? auth = null, Guid? target = null, HttpStatusCode? deniedStatus = null, Guid? templateId = null)
        {
            var before = await Snapshot(); var request = new CreateNotificationRequest(person, "Guardian", "Synthetic title", "Hello {{name}}", template?.Channel ?? "Email", null, templateId ?? template?.Id, new() { ["name"] = "Synthetic" });
            using var response = await Send(HttpMethod.Post, NotificationRoute(target ?? academy), request, auth ?? token);
            RequireFinanceStatus(response, available ? HttpStatusCode.Created : deniedStatus ?? HttpStatusCode.BadRequest, label);
            var after = await Snapshot();
            if (!available)
            {
                if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Rejected request wrote data/audit: " + label);
                if (deniedStatus is null && !((await response.Content.ReadAsStringAsync()).Contains("unavailable"))) throw new InvalidOperationException("Missing unavailable feedback");
            }
            else
            {
                var summary = await response.Content.ReadFromJsonAsync<NotificationSummary>(json) ?? throw new InvalidOperationException("Missing notification summary");
                using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == summary.Id);
                if (Notice(row) != summary || row.AcademyId != academy || summary.TemplateId != request.TemplateId || summary.Channel != request.Channel || summary.Status != (expectedStatus ?? "BlockedConsent") || summary.SentAtUtc is not null || summary.RecipientId != person || summary.RecipientType != "Guardian" || summary.Title != request.Title || summary.Message != "Hello Synthetic" || summary.ScheduledAtUtc is not null || summary.VariablesJson != JsonSerializer.Serialize(request.Variables)) throw new InvalidOperationException("Full notification payload/storage/response mismatch");
                Unchanged(before, after, "notifications", "audits"); PreserveRows(before, after, "notifications", added: 1); PreserveRows(before, after, "audits", added: 2);
                var oldIds = before.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet(); var added = (await db.AuditLogs.AsNoTracking().ToListAsync()).Where(x => !oldIds.Contains(x.Id)).ToArray();
                if (added.Any(x => x.AcademyId != academy) || !added.Any(x => x.Action == "NotificationQueued" && x.EntityId == row.Id) || !added.Any(x => x.Action == "POST Notifications" && x.ActorUserId.HasValue)) throw new InvalidOperationException("Notification audit mismatch");
            }
            Pass(label);
        }
        if (baseline)
        {
            foreach (var channel in new[] { "Email", "WhatsApp" }) await Queue(channel + "-disabled-active-accepted-baseline", await Store(Save(channel, "Disabled", true)), true);
            Console.WriteLine("TEMPLATESTATE BASELINE PASS:2 Disabled/active templates created through HTTP and incorrectly accepted; BlockedConsent only, no outbound delivery."); return;
        }
        foreach (var channel in new[] { "Email", "WhatsApp" }) foreach (var status in new[] { "Draft", "Approved", "Disabled" }) foreach (var active in new[] { true, false })
        { var template = await Store(Save(channel, status, active)); await Queue($"{channel}-{status}-active-{active}", template, active && status != "Disabled"); }
        foreach (var channel in new[] { "Email", "WhatsApp" }) foreach (var status in new[] { "dIsAbLeD", " Disabled " })
        {
            CommunicationTemplateSummary template;
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = new CommunicationTemplate { AcademyId = academy, Channel = channel, Name = "Legacy", TemplateKey = Guid.NewGuid().ToString("N"), Category = "Utility", Status = status, Body = "Preserved", IsActive = true }; db.Add(row); await db.SaveChangesAsync(); template = Summary(row); }
            await Queue(channel + "-legacy-" + status.Trim(), template, false);
        }
        foreach (var channel in new[] { "Email", "WhatsApp" })
        {
            var template = await Store(Save(channel, "Approved", true)); await Queue(channel + "-before-disable", template, true);
            template = await Store(Save(channel, "Disabled", true, template.TemplateKey), template.Id); await Queue(channel + "-committed-disable-stale-id", template, false);
            template = await Store(Save(channel, "Draft", true, template.TemplateKey), template.Id); await Queue(channel + "-explicit-reactivation", template, true);
            template = await Store(Save(channel, "Approved", false, template.TemplateKey), template.Id); await Queue(channel + "-committed-inactive-stale-id", template, false);
        }
        var beforeCatalogue = await Snapshot(); using var catalogueResponse = await Send(HttpMethod.Get, TemplateRoute(academy) + "/starter-templates", auth: token); RequireFinanceStatus(catalogueResponse, HttpStatusCode.OK, "catalogue");
        var catalogue = await catalogueResponse.Content.ReadFromJsonAsync<List<StarterTemplateDefinition>>(json) ?? throw new InvalidOperationException("Missing catalogue"); if (!JsonElement.DeepEquals(beforeCatalogue, await Snapshot())) throw new InvalidOperationException("Catalogue wrote data");
        var ids = new[] { "Email", "WhatsApp" }.Select(channel => catalogue.First(x => x.Channel == channel).Id).ToArray();
        var beforeStarter = await Snapshot(); using var starterResponse = await Send(HttpMethod.Post, TemplateRoute(academy) + "/starter-templates", new AddStarterTemplatesRequest(ids), token); RequireFinanceStatus(starterResponse, HttpStatusCode.OK, "starters");
        if ((await starterResponse.Content.ReadFromJsonAsync<StarterTemplateResult>(json))?.AddedCount != 2) throw new InvalidOperationException("Starter count mismatch");
        var afterStarter = await Snapshot(); Unchanged(beforeStarter, afterStarter, "templates", "audits"); PreserveRows(beforeStarter, afterStarter, "templates", added: 2); PreserveRows(beforeStarter, afterStarter, "audits", added: 2); Pass("starter-catalogue-and-add");
        foreach (var id in ids)
        {
            var definition = catalogue.Single(x => x.Id == id); CommunicationTemplateSummary template;
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.CommunicationTemplates.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Channel == definition.Channel && x.TemplateKey == definition.TemplateKey); template = Summary(row); if (template.Status != "Draft" || !template.IsActive || template.Body != definition.Body) throw new InvalidOperationException("Starter defaults changed"); }
            await Queue(template.Channel + "-starter-draft-usable", template, true);
            template = await Store(Save(template.Channel, "Disabled", true, template.TemplateKey), template.Id); await Queue(template.Channel + "-starter-disabled", template, false);
        }
        var beforeRepeat = await Snapshot(); using var repeat = await Send(HttpMethod.Post, TemplateRoute(academy) + "/starter-templates", new AddStarterTemplatesRequest(ids), token); RequireFinanceStatus(repeat, HttpStatusCode.OK, "repeat-starters");
        if ((await repeat.Content.ReadFromJsonAsync<StarterTemplateResult>(json))?.AddedCount != 0) throw new InvalidOperationException("Repeat reactivated starter"); var afterRepeat = await Snapshot(); Unchanged(beforeRepeat, afterRepeat, "audits"); PreserveRows(beforeRepeat, afterRepeat, "audits", added: 1); Pass("repeat-starters-do-not-reactivate");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); db.Add(new CommunicationPreference { AcademyId = academy, RecipientId = person, RecipientType = "Guardian", EmailAllowed = true, WhatsAppAllowed = true }); await db.SaveChangesAsync(); }
        foreach (var channel in new[] { "Email", "WhatsApp" }) await Queue(channel + "-approved-not-provider-approved", await Store(Save(channel, "Approved", true)), true, "AwaitingConnection");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); foreach (var channel in new[] { "Email", "WhatsApp" }) db.Add(new CommunicationChannel { AcademyId = academy, Channel = channel, Provider = "Synthetic", MessagesEnabled = true, HasSecureConnection = true }); await db.SaveChangesAsync(); }
        foreach (var channel in new[] { "Email", "WhatsApp" }) await Queue(channel + "-synthetic-connected-still-queued", await Store(Save(channel, "Draft", true)), true, "Queued");
        await Queue("foreign-template", null, false, templateId: foreignTemplate); await Queue("missing-template", null, false, templateId: Guid.NewGuid());
        await Queue("teacher-denied", null, false, auth: teacherToken, deniedStatus: HttpStatusCode.Forbidden); await Queue("foreign-actor-denied", null, false, auth: foreignToken, deniedStatus: HttpStatusCode.Forbidden); await Queue("foreign-route-denied", null, false, target: foreign, deniedStatus: HttpStatusCode.Forbidden);
        var beforeAnonymous = await Snapshot(); using var anonymous = await Send(HttpMethod.Post, NotificationRoute(academy), new CreateNotificationRequest(person, "Guardian", "Synthetic", "Body", "Email", null, null, null)); RequireFinanceStatus(anonymous, HttpStatusCode.Unauthorized, "anonymous"); if (!JsonElement.DeepEquals(beforeAnonymous, await Snapshot())) throw new InvalidOperationException("Anonymous write"); Pass("anonymous-denied");
        foreach (var (target, auth) in new[] { (academy, token), (foreign, foreignToken) })
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, TemplateRoute(target), auth: auth); RequireFinanceStatus(response, HttpStatusCode.OK, "template-list");
            var list = await response.Content.ReadFromJsonAsync<List<CommunicationTemplateSummary>>(json) ?? throw new InvalidOperationException("Missing list"); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var rows = await db.CommunicationTemplates.AsNoTracking().Where(x => x.AcademyId == target).ToListAsync();
            if (!list.OrderBy(x => x.Id).SequenceEqual(rows.Select(Summary).OrderBy(x => x.Id)) || !JsonElement.DeepEquals(before, await Snapshot())) throw new InvalidOperationException("Full scoped list differs"); Pass(target == academy ? "owned-list-retains-disabled-for-editing" : "foreign-list-preserved");
        }
        if (count != 42) throw new InvalidOperationException("Unexpected template case count: " + count);
        Console.WriteLine("TEMPLATESTATE REGRESSION PASS:42 cases; real Identity/global filter/model binding/HTTP/fresh SQL; Status x active matrix, full Create/Update/List/starter summaries, legacy contradictions, committed disable before submit, explicit reactivation, unchanged consent/connection status and actor audits, rejected no-write snapshots, foreign preservation. SentAtUtc null, no outbound/provider delivery. Simultaneous disable/queue race, browser/device/critical/release remain pending.");
    }
}
