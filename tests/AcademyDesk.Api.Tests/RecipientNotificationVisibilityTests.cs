using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Tests;

public sealed class RecipientNotificationVisibilityTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var channel in new[] { "InApp", "Email", "WhatsApp", "Unknown" })
        foreach (var status in new[] { "Queued", "Sent", "Read", "Cancelled", "BlockedConsent", "AwaitingConnection", "Failed", "RetryRequested", "Unknown" })
            yield return new object[] { channel, status, channel != "Unknown" && (status is "Sent" or "Read" || channel == "InApp" && status == "Queued") };
    }

    [Theory, MemberData(nameof(Cases))]
    public void Delivery_state_and_channel_gate_recipient_visibility(string channel, string status, bool visible)
    {
        var now = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        var filter = RecipientNotificationVisibility.At(now).Compile();
        var row = new Notification { RecipientType = "Student", Title = "Synthetic", Message = "Synthetic", Channel = channel, Status = status };
        Assert.Equal(visible, filter(row));
        row.ScheduledAtUtc = now.AddTicks(-1); Assert.Equal(visible, filter(row));
        row.ScheduledAtUtc = now; Assert.Equal(visible, filter(row));
        row.ScheduledAtUtc = now.AddTicks(1); Assert.False(filter(row));
        Assert.Equal(status, row.Status);
    }
}
