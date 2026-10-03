using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyInboxReceiptMigrationAsync(AcademyDeskDbContext db)
    {
        var migrations = db.Database.GetMigrations().ToArray(); var latest = migrations.Last(); var previous = migrations[^2];
        if (!latest.EndsWith("_AddNotificationReadReceipts")) throw new InvalidOperationException("Expected pinned receipt migration");
        var migrator = db.GetService<IMigrator>(); await migrator.MigrateAsync(previous);
        var row = new Notification { AcademyId = Guid.NewGuid(), RecipientId = Guid.NewGuid(), RecipientType = "Student", Title = "Synthetic pre-upgrade", Message = "Preserve legacy delivery", Status = "Read", Channel = "InApp", AttemptCount = 3, FailureReason = "Historical synthetic metadata", ScheduledAtUtc = DateTime.UtcNow.AddDays(-1) };
        // The current model contains an unapplied receipt table, but adding a
        // Notification touches only its existing table before upgrade.
        db.Add(row); await db.SaveChangesAsync(); db.ChangeTracker.Clear(); var original = JsonSerializer.Serialize(await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == row.Id));
        await migrator.MigrateAsync(latest);
        if (original != JsonSerializer.Serialize(await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == row.Id)) || await db.NotificationReadReceipts.AnyAsync()) throw new InvalidOperationException("Upgrade modified legacy data or invented receipts");
        db.Add(new NotificationReadReceipt { NotificationId = row.Id, AcademyId = row.AcademyId, UserId = Guid.NewGuid(), ReadAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await migrator.MigrateAsync(previous);
        if (original != JsonSerializer.Serialize(await db.Notifications.AsNoTracking().SingleAsync(x => x.Id == row.Id))) throw new InvalidOperationException("Rollback changed notification");
        await migrator.MigrateAsync(latest);
        if (await db.NotificationReadReceipts.AnyAsync() || (await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Reapply receipt schema failed");
        db.Remove(await db.Notifications.SingleAsync(x => x.Id == row.Id)); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Console.WriteLine("INBOXLIFE MIGRATION PASS:upgrade preserves existing notification/legacy Read; no fabricated receipts; rollback drops only receipts; reapply clean. Disposable owned SQL only, not dev/Azure.");
    }

    private static async Task VerifyRecipientInboxAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student, teacher, guardian; var count = 0;
        var rows = new List<Notification>(); var actors = new Dictionary<string, (Guid Id, string Token)>();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            teacher = (await db.Batches.SingleAsync(x => x.AcademyId == academy)).TeacherId!.Value;
            var parent = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Inbox" }; guardian = parent.Id; db.Add(parent);
            foreach (var type in new[] { "Student", "Teacher", "Guardian", "Parent" })
            foreach (var spec in new[] { ("Queued", "InApp", false), ("Sent", "InApp", false), ("Read", "InApp", false), ("Queued", "InApp", true), ("Sent", "Email", true), ("Cancelled", "InApp", false), ("BlockedConsent", "Email", false), ("AwaitingConnection", "WhatsApp", false), ("Failed", "Email", false), ("Queued", "Email", false), ("RetryRequested", "InApp", false), ("Sent", "Email", false), ("Read", "WhatsApp", false), ("Sent", "Unknown", false) })
            {
                var row = new Notification { AcademyId = academy, RecipientId = type == "Student" ? student : type == "Teacher" ? teacher : guardian, RecipientType = type, Title = $"{type}-{spec.Item1}-{spec.Item2}-{spec.Item3}", Message = "Synthetic lifecycle body", Status = spec.Item1, Channel = spec.Item2, ScheduledAtUtc = spec.Item3 ? DateTime.UtcNow.AddDays(1) : DateTime.UtcNow.AddDays(-1), SentAtUtc = spec.Item1 == "Sent" ? DateTime.UtcNow.AddDays(-1) : null, FailureReason = spec.Item1 == "Failed" ? "Synthetic failure" : null, AttemptCount = 2 }; rows.Add(row); db.Add(row);
            }
            // Identical person GUID but another academy; never visible/readable.
            rows.Add(new Notification { AcademyId = foreign, RecipientId = student, RecipientType = "Student", Title = "Foreign sentinel", Message = "Foreign", Status = "Sent" }); db.Add(rows.Last());
            await db.SaveChangesAsync();
        }
        foreach (var name in new[] { "student", "student2", "guardian", "foreign" })
        {
            var id = await CreateAccessActorAsync(factory, $"qa-inbox-{name}@example.invalid", name == "guardian" ? "Guardian" : "Student", name == "foreign" ? foreign : academy, name == "guardian" ? null : student);
            if (name == "guardian") { using var scope = factory.Services.CreateScope(); var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var u = (await manager.FindByIdAsync(id.ToString()))!; u.GuardianId = guardian; if (!(await manager.UpdateAsync(u)).Succeeded) throw new InvalidOperationException("Guardian link failed"); }
            actors[name] = (id, await LoginAsync(client, $"qa-inbox-{name}@example.invalid", "Synthetic!39Ab"));
        }
        using (var scope = factory.Services.CreateScope()) { var u = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-teacher-a@example.invalid"))!; actors["teacher"] = (u.Id, await LoginAsync(client, u.Email!, "Synthetic!39Ab")); }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"); client.DefaultRequestHeaders.Authorization = null;
        async Task<string> Snapshot(bool receipts = true)
        { using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); return JsonSerializer.Serialize(new { notices = await db.Notifications.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), receipts = receipts ? await db.NotificationReadReceipts.AsNoTracking().OrderBy(x => x.NotificationId).ThenBy(x => x.UserId).ToListAsync() : null, audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() }); }
        async Task<HttpResponseMessage> Send(HttpMethod method, string url, string? auth)
        { await Task.Delay(650); using var req = new HttpRequestMessage(method, url); if (auth is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth); return await client.SendAsync(req); }
        bool Visible(Notification n) => !n.ScheduledAtUtc.HasValue || n.ScheduledAtUtc <= DateTime.UtcNow ? (n.Channel is "InApp" or "Email" or "WhatsApp") && (n.Status is "Sent" or "Read" || n.Channel == "InApp" && n.Status == "Queued") : false;
        void Pass(string label) { count++; Console.WriteLine($"INBOXLIFE CASE {label} PASS."); }
        var beforeSource = await Snapshot(false);
        if (Environment.GetEnvironmentVariable("QA_INBOX_BASELINE") == "1")
        {
            using var list = await Send(HttpMethod.Get, "/api/portal/notifications", actors["student"].Token); RequireFinanceStatus(list, HttpStatusCode.OK, "baseline list");
            var returned = await list.Content.ReadFromJsonAsync<JsonElement>(); var visible = returned.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
            var hidden = rows.Where(x => x.RecipientType == "Student" && x.AcademyId == academy && !Visible(x)).ToList();
            if (hidden.Count != 9 || hidden.Any(x => !visible.Contains(x.Id))) throw new InvalidOperationException("Expected baseline disclosure not reproduced");
            Console.WriteLine("INBOXLIFE BASELINE:9 future/cancelled/blocked/undelivered/unknown rows exposed.");
            var cancelled = hidden.Single(x => x.Status == "Cancelled"); using var read = await Send(HttpMethod.Patch, $"/api/portal/notifications/{cancelled.Id}/read", actors["student"].Token); RequireFinanceStatus(read, HttpStatusCode.OK, "baseline read");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); if ((await db.Notifications.SingleAsync(x => x.Id == cancelled.Id)).Status != "Read") throw new InvalidOperationException("Expected baseline overwrite not reproduced");
            Console.WriteLine("INBOXLIFE BASELINE:cancelled delivery state overwritten to Read; reproduced2 defects. No outbound."); return;
        }
        async Task List(string actor, string type)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, "/api/portal/notifications", actors[actor].Token); RequireFinanceStatus(response, HttpStatusCode.OK, actor + " list");
            var data = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToArray(); var expected = rows.Where(n => n.AcademyId == (actor == "foreign" ? foreign : academy) && (type == "Guardian" ? n.RecipientType is "Guardian" or "Parent" : n.RecipientType == type) && Visible(n)).ToArray();
            if (!data.Select(x => x.GetProperty("id").GetGuid()).Order().SequenceEqual(expected.Select(x => x.Id).Order())) throw new InvalidOperationException("Visibility/tenant/type mismatch " + actor);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var item in data) { var row = expected.Single(x => x.Id == item.GetProperty("id").GetGuid()); var receipt = await db.NotificationReadReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.NotificationId == row.Id && x.UserId == actors[actor].Id); if (item.GetProperty("status").GetString() != row.Status || item.GetProperty("message").GetString() != row.Message || item.GetProperty("isRead").GetBoolean() != (row.Status == "Read" || receipt != null) || (receipt == null ? item.GetProperty("readAtUtc").ValueKind != JsonValueKind.Null : item.GetProperty("readAtUtc").GetDateTime() != receipt.ReadAtUtc)) throw new InvalidOperationException("Read projection mismatch"); }
            if (before != await Snapshot()) throw new InvalidOperationException("GET wrote state"); Pass(actor + "-scoped-list");
        }
        foreach (var pair in new[] { ("student", "Student"), ("guardian", "Guardian"), ("teacher", "Teacher"), ("foreign", "Student") }) await List(pair.Item1, pair.Item2);
        async Task Denied(string label, Guid id, string? token, HttpStatusCode status = HttpStatusCode.NotFound)
        { var before = await Snapshot(); using var response = await Send(HttpMethod.Patch, $"/api/portal/notifications/{id}/read", token); RequireFinanceStatus(response, status, label); if (before != await Snapshot()) throw new InvalidOperationException("Rejected read wrote state " + label); Pass(label); }
        foreach (var row in rows.Where(n => n.AcademyId == academy && !Visible(n))) await Denied("hidden-" + row.Title, row.Id, actors[row.RecipientType is "Guardian" or "Parent" ? "guardian" : row.RecipientType.ToLowerInvariant()].Token);
        await Denied("foreign-academy-id", rows.Last().Id, actors["student"].Token);
        await Denied("wrong-recipient", rows.First(x => x.RecipientType == "Teacher" && Visible(x)).Id, actors["student"].Token);
        await Denied("unknown-id", Guid.NewGuid(), actors["student"].Token);
        await Denied("anonymous", rows[0].Id, null, HttpStatusCode.Unauthorized);
        await Denied("admin-no-person", rows[0].Id, admin, HttpStatusCode.Forbidden);
        async Task Acknowledge(Notification row, string actor)
        {
            using var response = await Send(HttpMethod.Patch, $"/api/portal/notifications/{row.Id}/read", actors[actor].Token); RequireFinanceStatus(response, HttpStatusCode.OK, row.Title);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(); using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var saved = await db.NotificationReadReceipts.SingleAsync(x => x.NotificationId == row.Id && x.UserId == actors[actor].Id);
            if (saved.AcademyId != row.AcademyId || !json.GetProperty("isRead").GetBoolean() || json.GetProperty("status").GetString() != row.Status || json.GetProperty("readAtUtc").GetDateTime() != saved.ReadAtUtc || beforeSource != await Snapshot(false)) throw new InvalidOperationException("Acknowledgment damaged delivery state/source/audit");
            Pass("ack-" + actor + "-" + row.Title);
        }
        foreach (var row in rows.Where(n => n.AcademyId == academy && Visible(n))) await Acknowledge(row, row.RecipientType is "Guardian" or "Parent" ? "guardian" : row.RecipientType.ToLowerInvariant());
        var first = rows.First(x => x.RecipientType == "Student" && x.Status == "Queued" && Visible(x)); var stable = await Snapshot(); await Acknowledge(first, "student"); if (stable != await Snapshot()) throw new InvalidOperationException("Repeat changed first read time"); Pass("repeat-idempotent");
        await List("student2", "Student"); await Acknowledge(first, "student2"); await List("student", "Student"); await List("student2", "Student");
        // Concurrent retries serialize on the notification row; one immutable receipt.
        var parallel = rows.First(x => x.RecipientType == "Student" && x.Channel == "InApp" && x.Status == "Sent");
        using (var scope = factory.Services.CreateScope()) if (await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().NotificationReadReceipts.AnyAsync(x => x.NotificationId == parallel.Id && x.UserId == actors["student2"].Id)) throw new InvalidOperationException("Concurrent first-read fixture already acknowledged");
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Send(HttpMethod.Patch, $"/api/portal/notifications/{parallel.Id}/read", actors["student2"].Token)));
        var readTimes = new List<DateTime>(); foreach (var response in results) { using (response) { RequireFinanceStatus(response, HttpStatusCode.OK, "concurrent first read"); readTimes.Add((await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("readAtUtc").GetDateTime()); } }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); if (await db.NotificationReadReceipts.CountAsync(x => x.NotificationId == parallel.Id && x.UserId == actors["student2"].Id) != 1 || readTimes.Distinct().Count() != 1 || beforeSource != await Snapshot(false)) throw new InvalidOperationException("Concurrent first read duplicated receipt or changed delivery/source"); }
        Pass("concurrent-first-read-idempotent");
        foreach (var pair in new[] { ("guardian", "Guardian"), ("teacher", "Teacher") }) await List(pair.Item1, pair.Item2);
        // Explicitly move a synthetic future fixture across its due boundary.
        var future = rows.Single(x => x.RecipientType == "Student" && x.Status == "Queued" && x.ScheduledAtUtc > DateTime.UtcNow);
        future.ScheduledAtUtc = DateTime.UtcNow.AddSeconds(-1);
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Notifications.SingleAsync(x => x.Id == future.Id)).ScheduledAtUtc = future.ScheduledAtUtc; await db.SaveChangesAsync(); }
        beforeSource = await Snapshot(false); await List("student", "Student"); await Acknowledge(future, "student"); Pass("future-becomes-due");
        // Cancelling an acknowledged message hides its body and forbids another
        // acknowledgment, without deleting/rewriting its prior receipt.
        first.Status = "Cancelled";
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Notifications.SingleAsync(x => x.Id == first.Id)).Status = first.Status; await db.SaveChangesAsync(); }
        beforeSource = await Snapshot(false); await List("student", "Student"); await Denied("cancelled-after-read", first.Id, actors["student"].Token); Pass("cancellation-retains-receipt-not-delivery-overwrite");
        if (count != 80) throw new InvalidOperationException("Unexpected inbox case count " + count);
        Console.WriteLine($"INBOXLIFE REGRESSION PASS:{count} cases; real Identity/HTTP/fresh SQL, all four recipient types, due/channel/state/tenant gates, separate per-account read receipts, unchanged delivery rows, idempotent/concurrent retries. Browser/devices/general announcements/sender status lifecycle/audit-fault rollback/critical/release pending. No outbound.");
    }
}
