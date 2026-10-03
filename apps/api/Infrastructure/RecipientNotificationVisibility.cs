using System.Linq.Expressions;
using AcademyDesk.Api.Domain.Entities;

namespace AcademyDesk.Api.Infrastructure;

public static class RecipientNotificationVisibility
{
    // InApp needs no external transport; due queued rows are available locally.
    // External messages enter history only after delivery. Legacy Read rows are
    // retained, but their former delivery status cannot safely be reconstructed.
    public static Expression<Func<Notification, bool>> At(DateTime nowUtc) => x =>
        (x.ScheduledAtUtc == null || x.ScheduledAtUtc <= nowUtc) &&
        (x.Channel == "InApp" || x.Channel == "Email" || x.Channel == "WhatsApp") &&
        (x.Status == "Sent" || x.Status == "Read" || (x.Channel == "InApp" && x.Status == "Queued"));
}
