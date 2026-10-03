using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class MarketingConsentTests
{
    public static IEnumerable<object[]> CreationMatrix()
    {
        foreach (var channel in new[] { "Email", "WhatsApp" })
        foreach (var category in new[] { "Marketing", "Utility", "Authentication", "Transactional" })
        foreach (var channelAllowed in new[] { true, false })
        foreach (var marketingAllowed in new[] { true, false })
        foreach (var connection in new[] { "Missing", "Disabled", "Unsecured", "Ready" })
            yield return [channel, category, channelAllowed, marketingAllowed, connection];
    }

    [Theory, MemberData(nameof(CreationMatrix))]
    public async Task Category_channel_marketing_and_connection_matrix(string channel, string category, bool channelAllowed, bool marketingAllowed, string connection)
    {
        var f = await Seed(channel, category, channelAllowed, marketingAllowed, connection);
        var reason = !channelAllowed ? $"No recorded {channel} consent for this recipient." : category == "Marketing" && !marketingAllowed ? "No recorded marketing consent for this recipient." : connection != "Ready" ? $"{channel} is not securely connected for this academy." : null;
        var status = !channelAllowed || category == "Marketing" && !marketingAllowed ? "BlockedConsent" : connection != "Ready" ? "AwaitingConnection" : "Queued";
        await VerifyCreate(f, channel, status, reason);
    }

    public static IEnumerable<object[]> RetryMatrix()
    {
        foreach (var channel in new[] { "Email", "WhatsApp" })
        foreach (var target in new[] { "Queued", "RetryRequested" })
        foreach (var category in new[] { "Marketing", "Utility" })
        foreach (var channelAllowed in new[] { true, false })
        foreach (var marketingAllowed in new[] { true, false })
            yield return [channel, target, category, channelAllowed, marketingAllowed];
    }

    [Theory, MemberData(nameof(RetryMatrix))]
    public async Task Queue_and_retry_recheck_current_consent_without_rewriting_rejected_rows(string channel, string target, string category, bool channelAllowed, bool marketingAllowed)
    {
        var f = await Seed(channel, category, channelAllowed, marketingAllowed, "Ready");
        await VerifyRetry(f, target, channelAllowed && (category != "Marketing" || marketingAllowed));
    }

    [Theory]
    [InlineData("Email", "Queued")]
    [InlineData("Email", "RetryRequested")]
    [InlineData("WhatsApp", "Queued")]
    [InlineData("WhatsApp", "RetryRequested")]
    public async Task Marketing_opt_out_committed_after_creation_blocks_requeue(string channel, string target)
    {
        var f = await Seed(channel, "Marketing", true, true, "Ready");
        await using (var db = new AcademyDeskDbContext(f.Options)) { (await db.CommunicationPreferences.SingleAsync()).MarketingAllowed = false; await db.SaveChangesAsync(); }
        await VerifyRetry(f, target, false);
    }

    [Theory]
    [InlineData("Email", "mArKeTiNg")]
    [InlineData("Email", " Marketing ")]
    [InlineData("WhatsApp", "mArKeTiNg")]
    [InlineData("WhatsApp", " Marketing ")]
    public async Task Legacy_marketing_category_variants_cannot_bypass_opt_out(string channel, string category)
    { var f = await Seed(channel, category, true, false, "Ready"); await VerifyCreate(f, channel, "BlockedConsent", "No recorded marketing consent for this recipient."); }

    [Theory]
    [InlineData("Email", "email")]
    [InlineData("Email", " eMaIl ")]
    [InlineData("WhatsApp", "whatsapp")]
    [InlineData("WhatsApp", " wHaTsApP ")]
    public async Task Manual_external_channel_variants_still_require_channel_consent(string canonical, string requested)
    { var f = await Seed(canonical, "Utility", false, false, "Ready"); await VerifyCreate(f, requested, "BlockedConsent", $"No recorded {canonical} consent for this recipient.", manual: true); }

    [Theory]
    [InlineData("Email", "Missing")]
    [InlineData("WhatsApp", "Missing")]
    [InlineData("Email", "Foreign")]
    [InlineData("WhatsApp", "Foreign")]
    [InlineData("Email", "OtherType")]
    [InlineData("WhatsApp", "OtherType")]
    public async Task No_matching_scoped_recipient_consent_is_fail_closed(string channel, string mode)
    {
        var f = await Seed(channel, "Marketing", true, true, "Ready");
        await using (var db = new AcademyDeskDbContext(f.Options)) { var row = await db.CommunicationPreferences.SingleAsync(); if (mode == "Missing") db.Remove(row); else if (mode == "Foreign") row.AcademyId = Guid.NewGuid(); else row.RecipientType = "Guardian"; await db.SaveChangesAsync(); }
        await VerifyCreate(f, channel, "BlockedConsent", $"No recorded {channel} consent for this recipient.");
    }

    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Template, Guid Person, Guid Notice, string Channel);
    [Theory]
    [InlineData("Email", "email")]
    [InlineData("WhatsApp", "whatsapp")]
    public async Task Legacy_template_channel_case_cannot_skip_marketing_consent(string channel, string legacy)
    {
        var f = await Seed(channel, "Marketing", true, false, "Ready");
        await using (var db = new AcademyDeskDbContext(f.Options)) { (await db.CommunicationTemplates.SingleAsync()).Channel = legacy; await db.SaveChangesAsync(); }
        await VerifyCreate(f, channel, "BlockedConsent", "No recorded marketing consent for this recipient.");
    }

    private static async Task<Fixture> Seed(string channel, string category, bool channelAllowed, bool marketingAllowed, string connection)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options);
        var academy = new Academy { Name = "Synthetic marketing consent" }; var person = Guid.NewGuid();
        var template = new CommunicationTemplate { AcademyId = academy.Id, Channel = channel, Name = "Synthetic title", TemplateKey = "synthetic", Category = category, Status = "Approved", Body = "Hello {{name}}" };
        var notice = new Notification { AcademyId = academy.Id, RecipientId = person, RecipientType = "Student", TemplateId = template.Id, Title = "Preserve", Message = "Preserve", Channel = channel, Status = "BlockedConsent", FailureReason = "Preserve existing failure", AttemptCount = 3 };
        db.AddRange(academy, template, notice, new CommunicationPreference { AcademyId = academy.Id, RecipientId = person, RecipientType = "Student", EmailAllowed = channel == "Email" && channelAllowed, WhatsAppAllowed = channel == "WhatsApp" && channelAllowed, MarketingAllowed = marketingAllowed });
        if (connection != "Missing") db.Add(new CommunicationChannel { AcademyId = academy.Id, Channel = channel, Provider = "Synthetic", MessagesEnabled = connection != "Disabled", HasSecureConnection = connection != "Unsecured" });
        await db.SaveChangesAsync(); return new(options, academy.Id, template.Id, person, notice.Id, channel);
    }
    private static async Task<string> Sources(AcademyDeskDbContext db) => JsonSerializer.Serialize(new { templates = await db.CommunicationTemplates.AsNoTracking().ToListAsync(), preferences = await db.CommunicationPreferences.AsNoTracking().ToListAsync(), channels = await db.CommunicationChannels.AsNoTracking().ToListAsync() });
    private static async Task VerifyCreate(Fixture f, string channel, string status, string? reason, bool manual = false)
    {
        await using var db = new AcademyDeskDbContext(f.Options); var before = await Sources(db); var old = JsonSerializer.Serialize(await db.Notifications.AsNoTracking().SingleAsync());
        var request = new CreateNotificationRequest(f.Person, "Student", "Synthetic title", "Hello {{name}}", channel, null, manual ? null : f.Template, new() { ["name"] = "Synthetic" });
        var summary = Assert.IsType<NotificationSummary>(Assert.IsType<CreatedResult>((await new NotificationsController(db).Create(f.Academy, request, default)).Result).Value);
        Assert.Equal(status, summary.Status); Assert.Equal(reason, summary.FailureReason); Assert.Equal(f.Channel, summary.Channel); Assert.Null(summary.SentAtUtc); Assert.Equal("Hello Synthetic", summary.Message);
        await using var fresh = new AcademyDeskDbContext(f.Options); Assert.Equal(2, await fresh.Notifications.CountAsync()); Assert.Equal(old, JsonSerializer.Serialize(await fresh.Notifications.AsNoTracking().SingleAsync(x => x.Id == f.Notice))); Assert.Equal(before, await Sources(fresh)); Assert.Single(await fresh.AuditLogs.ToListAsync());
    }
    private static async Task VerifyRetry(Fixture f, string target, bool allowed)
    {
        await using var db = new AcademyDeskDbContext(f.Options); var before = await Sources(db); var notice = await db.Notifications.AsNoTracking().SingleAsync(); var old = JsonSerializer.Serialize(notice);
        var result = await new NotificationsController(db).UpdateStatus(f.Academy, f.Notice, new(target), default);
        if (allowed) Assert.IsType<OkResult>(result); else { Assert.IsType<BadRequestObjectResult>(result); Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted); }
        await using var fresh = new AcademyDeskDbContext(f.Options); var saved = await fresh.Notifications.AsNoTracking().SingleAsync();
        if (allowed) { notice.Status = target; if (target == "RetryRequested") notice.FailureReason = null; Assert.Equal(JsonSerializer.Serialize(notice), JsonSerializer.Serialize(saved)); }
        else Assert.Equal(old, JsonSerializer.Serialize(saved));
        Assert.Equal(before, await Sources(fresh)); Assert.Empty(await fresh.AuditLogs.ToListAsync());
    }
}
