using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class NotificationChannelTests
{
    public static IEnumerable<object[]> Mismatches() { foreach (var type in new[] { "Guardian", "Student", "Teacher" }) foreach (var template in new[] { "Email", "WhatsApp" }) foreach (var channel in new[] { "InApp", template == "Email" ? "WhatsApp" : "Email" }) yield return [type, template, channel]; }
    [Theory, MemberData(nameof(Mismatches))]
    public async Task Mismatched_channel_rejects_without_notification_or_audit(string type, string template, string channel)
    { var f = await Seed(template); await Reject(f, Request(f, type, channel, f.Template)); }

    public static IEnumerable<object[]> Matches() { foreach (var type in new[] { "Guardian", "Student", "Teacher" }) foreach (var channel in new[] { "Email", "WhatsApp" }) yield return [type, channel]; }
    [Theory, MemberData(nameof(Matches))]
    public async Task Matching_template_keeps_explicit_content_substitution_and_existing_consent_status(string type, string channel)
    { var f = await Seed(channel); var result = await Accept(f, Request(f, type, channel, f.Template)); Assert.Equal(channel, result.Channel); Assert.Equal("BlockedConsent", result.Status); Assert.Equal("Visible title", result.Title); Assert.Equal("Hello Synthetic", result.Message); Assert.Equal(f.Template, result.TemplateId); }
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("  ")]
    public async Task Unspecified_channel_retains_legacy_template_channel_fallback(string? channel)
    { var f = await Seed("Email"); Assert.Equal("Email", (await Accept(f, Request(f, "Student", channel, f.Template))).Channel); }
    [Theory] [InlineData("InApp", "Queued")] [InlineData("Email", "BlockedConsent")] [InlineData("WhatsApp", "BlockedConsent")]
    public async Task Manual_channel_keeps_existing_status_without_template(string channel, string status)
    { var f = await Seed("Email"); var row = await Accept(f, Request(f, "Guardian", channel, null)); Assert.Equal(channel, row.Channel); Assert.Equal(status, row.Status); Assert.Null(row.TemplateId); }
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("InApp")] [InlineData(" inapp ")]
    public async Task Portal_banner_has_canonical_InApp_channel_audience_duration_and_no_delivery(string? channel)
    { var f = await Seed("Email"); var row = await Accept(f, Request(f, "Academy", channel, null, true)); Assert.Equal("InApp", row.Channel); Assert.Equal("Queued", row.Status); Assert.Null(row.FailureReason); using var json = JsonDocument.Parse(row.VariablesJson!); Assert.Equal("Student,Teacher", json.RootElement.GetProperty("audiences").GetString()); Assert.Equal("true", json.RootElement.GetProperty("important").GetString()); Assert.Equal(TimeSpan.FromHours(4), json.RootElement.GetProperty("expiresAtUtc").GetDateTime() - json.RootElement.GetProperty("startsAtUtc").GetDateTime()); }
    [Theory] [InlineData("Email")] [InlineData("WhatsApp")]
    public async Task Portal_banner_rejects_explicit_external_channel_without_template(string channel)
    { var f = await Seed(channel); await Reject(f, Request(f, "Academy", channel, null, true)); }
    [Theory] [InlineData("Email", null)] [InlineData("WhatsApp", null)] [InlineData("Email", "InApp")] [InlineData("WhatsApp", "InApp")]
    public async Task Portal_banner_rejects_external_template_even_when_channel_is_unspecified(string template, string? channel)
    { var f = await Seed(template); await Reject(f, Request(f, "Academy", channel, f.Template, true)); }
    [Theory] [InlineData("foreign")] [InlineData("inactive")] [InlineData("missing")]
    public async Task Existing_template_scope_and_active_guards_remain_no_write(string mode)
    { var f = await Seed("Email"); await using (var db = new AcademyDeskDbContext(f.Options)) { var t = await db.CommunicationTemplates.SingleAsync(); if (mode == "foreign") t.AcademyId = Guid.NewGuid(); if (mode == "inactive") t.IsActive = false; await db.SaveChangesAsync(); } await Reject(f, Request(f, "Guardian", "Email", mode == "missing" ? Guid.NewGuid() : f.Template)); }
    [Theory] [InlineData(0)] [InlineData(169)]
    public async Task Existing_banner_duration_guard_remains_no_write(int hours)
    { var f = await Seed("Email"); await Reject(f, Request(f, "Academy", "InApp", null, true) with { DisplayHours = hours }); }
    [Fact] public async Task Matching_template_still_falls_back_to_template_title_and_body()
    { var f = await Seed("Email"); var row = await Accept(f, Request(f, "Student", " email ", f.Template) with { Title = null, Message = null }); Assert.Equal("Template title", row.Title); Assert.Equal("Template Synthetic", row.Message); Assert.Equal("Email", row.Channel); }
    [Fact] public async Task Missing_academy_keeps_NotFound_without_writes()
    { var f = await Seed("Email"); await using var db = new AcademyDeskDbContext(f.Options); Assert.IsType<NotFoundResult>((await new NotificationsController(db).Create(Guid.NewGuid(), Request(f, "Guardian", "Email", f.Template), default)).Result); Assert.Empty(await db.Notifications.ToListAsync()); Assert.Empty(await db.AuditLogs.ToListAsync()); }

    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Template, Guid Person);
    private static CreateNotificationRequest Request(Fixture f, string type, string? channel, Guid? template, bool banner = false) => new(banner ? null : f.Person, type, " Visible title ", " Hello {{name}} ", channel, null, template, new() { ["name"] = "Synthetic", ["audiences"] = "Student,Teacher" }, banner, banner ? 4 : null);
    private static async Task<Fixture> Seed(string channel)
    { var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options; await using var db = new AcademyDeskDbContext(options); var academy = new Academy { Name = "Synthetic" }; var template = new CommunicationTemplate { AcademyId = academy.Id, Channel = channel, Name = "Template title", TemplateKey = "synthetic", Category = "Utility", Status = "Approved", Body = "Template {{name}}" }; db.AddRange(academy, template); await db.SaveChangesAsync(); return new(options, academy.Id, template.Id, Guid.NewGuid()); }
    private static async Task<string> Sources(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { Templates = await db.CommunicationTemplates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Channels = await db.CommunicationChannels.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), Preferences = await db.CommunicationPreferences.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
    private static async Task Reject(Fixture f, CreateNotificationRequest request)
    { await using var db = new AcademyDeskDbContext(f.Options); var before = await Sources(db); Assert.IsType<BadRequestObjectResult>((await new NotificationsController(db).Create(f.Academy, request, default)).Result); await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Empty(await fresh.Notifications.ToListAsync()); Assert.Empty(await fresh.AuditLogs.ToListAsync()); Assert.Equal(before, await Sources(fresh)); Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted); }
    private static async Task<NotificationSummary> Accept(Fixture f, CreateNotificationRequest request)
    { await using var db = new AcademyDeskDbContext(f.Options); var before = await Sources(db); var row = Assert.IsType<NotificationSummary>(Assert.IsType<CreatedResult>((await new NotificationsController(db).Create(f.Academy, request, default)).Result).Value); await using var fresh = new AcademyDeskDbContext(f.Options); var saved = Assert.Single(await fresh.Notifications.ToListAsync()); Assert.Equal(row.Id, saved.Id); Assert.Equal(f.Academy, saved.AcademyId); Assert.Equal(row.Channel, saved.Channel); Assert.Equal(row.Message, saved.Message); Assert.Equal(row.Status, saved.Status); Assert.Null(saved.SentAtUtc); Assert.Equal(before, await Sources(fresh)); var audit = Assert.Single(await fresh.AuditLogs.ToListAsync()); Assert.Equal("NotificationQueued", audit.Action); Assert.Equal(saved.Id, audit.EntityId); using var metadata = JsonDocument.Parse(audit.MetadataJson!); Assert.Equal(row.Channel, metadata.RootElement.GetProperty("Channel").GetString()); return row; }
}
