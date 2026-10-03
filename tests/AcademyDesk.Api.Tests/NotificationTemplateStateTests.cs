using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class NotificationTemplateStateTests
{
    public static IEnumerable<object[]> States()
    {
        foreach (var channel in new[] { "Email", "WhatsApp" })
        foreach (var status in new[] { "Draft", "Approved", "Disabled", "dIsAbLeD", " Disabled " })
        foreach (var active in new[] { true, false })
            yield return [channel, status, active];
    }

    [Theory, MemberData(nameof(States))]
    public async Task Disabled_overrides_availability_without_changing_draft_or_approved_policy(string channel, string status, bool active)
    {
        var f = await Seed(channel, status, active);
        await Verify(f, active && !status.Trim().Equals("Disabled", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Email", "Disabled")]
    [InlineData("WhatsApp", "Disabled")]
    [InlineData("Email", "Inactive")]
    [InlineData("WhatsApp", "Inactive")]
    public async Task Disable_committed_after_selection_is_rechecked_at_creation(string channel, string change)
    {
        var f = await Seed(channel, "Approved", true);
        await using (var db = new AcademyDeskDbContext(f.Options))
        {
            var template = await db.CommunicationTemplates.SingleAsync();
            if (change == "Disabled") template.Status = "Disabled";
            else template.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Verify(f, false);
    }

    private sealed record Fixture(DbContextOptions<AcademyDeskDbContext> Options, Guid Academy, Guid Template);
    private static async Task<Fixture> Seed(string channel, string status, bool active)
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AcademyDeskDbContext(options);
        var academy = new Academy { Name = "Synthetic template state" };
        var template = new CommunicationTemplate { AcademyId = academy.Id, Channel = channel, Name = "Synthetic title", TemplateKey = "synthetic", Category = "Utility", Status = status, IsActive = active, Body = "Hello {{name}}" };
        db.AddRange(academy, template);
        await db.SaveChangesAsync();
        return new(options, academy.Id, template.Id);
    }

    private static async Task Verify(Fixture f, bool available)
    {
        await using var db = new AcademyDeskDbContext(f.Options);
        var before = JsonSerializer.Serialize(await db.CommunicationTemplates.AsNoTracking().ToListAsync());
        var request = new CreateNotificationRequest(Guid.NewGuid(), "Guardian", null, null, null, null, f.Template, new() { ["name"] = "Synthetic" });
        var result = (await new NotificationsController(db).Create(f.Academy, request, default)).Result;
        if (available)
        {
            var summary = Assert.IsType<NotificationSummary>(Assert.IsType<CreatedResult>(result).Value);
            Assert.Equal("BlockedConsent", summary.Status);
            Assert.Equal("Hello Synthetic", summary.Message);
            Assert.Equal("Synthetic title", summary.Title);
            Assert.Equal(f.Template, summary.TemplateId);
            Assert.Null(summary.SentAtUtc);
        }
        else
        {
            var rejected = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("unavailable", JsonSerializer.Serialize(rejected.Value));
            Assert.DoesNotContain(db.ChangeTracker.Entries(), x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
        }
        await using var fresh = new AcademyDeskDbContext(f.Options);
        Assert.Equal(available ? 1 : 0, await fresh.Notifications.CountAsync());
        Assert.Equal(available ? 1 : 0, await fresh.AuditLogs.CountAsync());
        Assert.Equal(before, JsonSerializer.Serialize(await fresh.CommunicationTemplates.AsNoTracking().ToListAsync()));
        Assert.Empty(await fresh.CommunicationPreferences.ToListAsync());
        Assert.Empty(await fresh.CommunicationChannels.ToListAsync());
    }
}
