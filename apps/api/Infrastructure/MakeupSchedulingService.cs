using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

public sealed record MakeupSchedulePlan(Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime StartUtc,
    DateTime EndUtc, string DeliveryMode, string? Venue, string? MeetingLink, bool UsesNextScheduledClass,
    string? Notes, Guid? SourceSessionId, Guid[] GuardianIds, string NotificationMessage);

internal sealed record MakeupScheduleOutcome(MakeupSchedulePlan? Plan, string? Error = null);

/// <summary>Shared scheduling rules. Callers own authorization, audit and the transaction.</summary>
public sealed class MakeupSchedulingService(AcademyDeskDbContext db, TimeProvider clock)
{
    internal async Task<MakeupScheduleOutcome> PlanAsync(Guid academyId, CreateMakeupRequest request, CancellationToken token)
    {
        var batch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token);
        if (batch is null || !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token))
            return new(null, "Student or class/batch is invalid.");
        var mode = NormalizeDeliveryMode(request.DeliveryMode ?? "Offline");
        if (mode is null) return new(null, "Delivery mode must be Online, Offline, or Hybrid.");

        DateTime startUtc, endUtc;
        Guid? sourceSessionId = null;
        var teacherId = request.TeacherId;
        var venue = request.Venue?.Trim();
        var link = request.MeetingLink?.Trim();
        if (request.UseNextScheduledClass)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var session = await db.ClassSessions.AsNoTracking()
                .Where(x => x.AcademyId == academyId && x.BatchId == batch.Id && x.Status == "Scheduled" && x.StartUtc > now)
                .OrderBy(x => x.StartUtc).ThenBy(x => x.Id).FirstOrDefaultAsync(token);
            if (session is null) return new(null, "There is no upcoming scheduled class for this batch. Select a make-up date and time instead.");
            mode = NormalizeDeliveryMode(session.DeliveryMode, allowInPerson: true);
            if (mode is null) return new(null, "The next scheduled class has an unsupported delivery mode.");
            sourceSessionId = session.Id;
            startUtc = session.StartUtc;
            endUtc = session.EndUtc;
            teacherId = session.TeacherId ?? batch.TeacherId;
            venue = mode == "Offline" ? session.RoomName?.Trim() : null;
            link = mode is "Online" or "Hybrid" ? session.RoomName?.Trim() : null;
        }
        else
        {
            if (request.StartUtc is not { } start) return new(null, "Select a make-up date and time.");
            startUtc = start;
            var duration = TimeSpan.FromMinutes(batch.SessionMinutes is > 0 ? batch.SessionMinutes : 60);
            if (startUtc.Ticks > DateTime.MaxValue.Ticks - duration.Ticks)
                return new(null, "The make-up date and duration are out of range.");
            endUtc = startUtc.Add(duration);
        }

        if (mode == "Offline")
        {
            link = null;
            if (string.IsNullOrWhiteSpace(venue)) venue = null;
        }
        else
        {
            venue = null;
            if (string.IsNullOrWhiteSpace(link)) return new(null, "A meeting link is required for online and hybrid make-up classes.");
        }
        if (teacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == teacherId && x.AcademyId == academyId, token))
            return new(null, "Teacher is invalid.");

        var guardians = await (from access in db.StudentGuardians.AsNoTracking()
            join guardian in db.Guardians.AsNoTracking() on access.GuardianId equals guardian.Id
            where access.AcademyId == academyId && guardian.AcademyId == academyId && access.StudentId == request.StudentId &&
                access.CanAccessPortal && access.AccessRevokedAtUtc == null
            select guardian.Id).Distinct().OrderBy(x => x).ToArrayAsync(token);
        var ist = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        var location = link ?? venue;
        var message = $"A make-up class for {batch.Name} is scheduled on {ist:ddd, d MMM h:mm tt} IST. {mode}{(string.IsNullOrWhiteSpace(location) ? "" : $" · {location}")}";
        return new(new(request.StudentId, batch.Id, teacherId, startUtc, endUtc, mode, venue, link,
            request.UseNextScheduledClass, request.Notes?.Trim(), sourceSessionId, guardians, message));
    }

    internal async Task<MakeupSummary> ScheduleAsync(Guid academyId, MakeupSchedulePlan plan, CancellationToken token)
    {
        var row = new MakeupClass { AcademyId = academyId, StudentId = plan.StudentId, BatchId = plan.BatchId,
            TeacherId = plan.TeacherId, StartUtc = plan.StartUtc, EndUtc = plan.EndUtc, DeliveryMode = plan.DeliveryMode,
            Venue = plan.Venue, MeetingLink = plan.MeetingLink, UsesNextScheduledClass = plan.UsesNextScheduledClass, Notes = plan.Notes };
        db.MakeupClasses.Add(row);
        db.Notifications.Add(Notice(plan.StudentId, "Student"));
        foreach (var guardian in plan.GuardianIds) db.Notifications.Add(Notice(guardian, "Parent"));
        await db.SaveChangesAsync(token);
        return new(row.Id, row.StudentId, row.BatchId, row.TeacherId, row.StartUtc, row.EndUtc, row.DeliveryMode,
            row.Venue, row.MeetingLink, row.UsesNextScheduledClass, row.Status, row.Notes);

        Notification Notice(Guid recipient, string recipientType) => new() { AcademyId = academyId, RecipientId = recipient,
            RecipientType = recipientType, Title = "Make-up class scheduled", Message = plan.NotificationMessage, Channel = "InApp" };
    }

    private static string? NormalizeDeliveryMode(string? value, bool allowInPerson = false)
    {
        var mode = value?.Trim();
        if (string.Equals(mode, "Offline", StringComparison.OrdinalIgnoreCase) ||
            allowInPerson && string.Equals(mode, "InPerson", StringComparison.OrdinalIgnoreCase)) return "Offline";
        if (string.Equals(mode, "Online", StringComparison.OrdinalIgnoreCase)) return "Online";
        if (string.Equals(mode, "Hybrid", StringComparison.OrdinalIgnoreCase)) return "Hybrid";
        return null;
    }
}
