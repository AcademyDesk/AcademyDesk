using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure.Meetings;

// Draft preparation only. No executable preview/approval/dispatch is enabled.
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MeetingPreviewRequest(string? Provider, Guid BatchId, Guid OrganizerId,
    string? Title, [property: JsonConverter(typeof(MeetingStartConverter))] DateTimeOffset Start, string? TimeZone, int DurationMinutes,
    Guid[]? StudentIds, string? Recurrence, string? Access, string? Chat, string? Recording);
public sealed record MeetingDraftPreview(string Provider, Guid BatchId, Guid OrganizerId,
    string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string TimeZone,
    IReadOnlyList<Guid> StudentIds, IReadOnlyList<string> AttendeeEmails,
    string RequestHash, string SourceVersion, DateTimeOffset ExpiresAtUtc,
    bool CanExecute, string ApprovalStatus);
public sealed record MeetingPreviewDecision(int StatusCode, string ReasonCode, MeetingDraftPreview? Draft);

public sealed class MeetingPreviewService(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    MeetingReadinessService readiness, TimeProvider clock)
{
    public async Task<MeetingPreviewDecision> PrepareAsync(ClaimsPrincipal actor, Guid academyId,
        MeetingPreviewRequest request, CancellationToken token)
    {
        var authority = await readiness.ReadAsync(actor, academyId, token);
        if (authority.StatusCode != 200) return new(authority.StatusCode, "AccessDenied", null);
        if (request.Provider is not ("GoogleWorkspace" or "Zoom" or "Microsoft365") ||
            request.BatchId == Guid.Empty || request.OrganizerId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200 ||
            request.Title.Any(char.IsControl) ||
            request.DurationMinutes is not (30 or 45 or 60 or 90 or 120) ||
            request.StudentIds is null || request.StudentIds.Length is < 1 or > 100 ||
            request.StudentIds.Any(x => x == Guid.Empty) ||
            request.StudentIds.Distinct().Count() != request.StudentIds.Length)
            return new(400, "InvalidDraft", null);
        // No policy or provider capability approval exists for recurrence, guests,
        // open joining or recording. Never silently downgrade requested controls.
        if (request.Recurrence != "Once" || request.Access != "InviteesOnly" ||
            request.Chat != "Disabled" || request.Recording != "Off")
            return new(400, "UnqualifiedControls", null);
        var academy = await db.Academies.AsNoTracking().SingleAsync(x => x.Id == academyId, token);
        if (request.TimeZone != academy.TimeZone) return new(400, "InvalidTimeZone", null);
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(academy.TimeZone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        { return new(400, "InvalidTimeZone", null); }
        var now = clock.GetUtcNow();
        if (request.Start <= now || request.Start.Second != 0 || request.Start.Ticks % TimeSpan.TicksPerMinute != 0 ||
            request.Start > DateTimeOffset.MaxValue.AddMinutes(-request.DurationMinutes) ||
            TimeZoneInfo.ConvertTime(request.Start, zone).Offset != request.Start.Offset)
            return new(400, "InvalidStart", null);

        var batch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token);
        var teacher = await db.Teachers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.OrganizerId && x.AcademyId == academyId, token);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(request.Start, zone).DateTime);
        if (batch is null || !batch.IsActive || batch.DeliveryMode is not ("Online" or "Hybrid") ||
            !string.IsNullOrWhiteSpace(batch.MeetingLink) || teacher is null || !teacher.IsActive || batch.TeacherId != teacher.Id ||
            batch.StartDate > localDate || batch.EndDate < localDate)
            return new(400, "IneligibleSource", null);
        var ids = request.StudentIds.Order().ToArray();
        var students = await db.Students.AsNoTracking().Where(x => x.AcademyId == academyId && ids.Contains(x.Id) && x.IsActive).OrderBy(x => x.Id).ToListAsync(token);
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == academyId && x.BatchId == batch.Id && ids.Contains(x.StudentId) && x.Status == "Active").OrderBy(x => x.Id).ToListAsync(token);
        if (students.Count != ids.Length || ids.Any(id => !enrollments.Any(x => x.StudentId == id && x.StartDate <= localDate && (x.EndDate is null || x.EndDate >= localDate))))
            return new(400, "IneligibleSource", null);
        static bool Email(string? value) => value is not null && value.Length <= 254 && MailAddress.TryCreate(value, out var address) && address.Address == value;
        if (!Email(teacher.Email) || students.Any(x => !Email(x.Email))) return new(400, "InvalidRecipient", null);
        var emails = students.Select(x => x.Email!).Append(teacher.Email!).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var user = await users.GetUserAsync(actor);
        if (user is null || (await readiness.ReadAsync(actor, academyId, token)).StatusCode != 200) return new(403, "AccessDenied", null);
        // These digests detect changes. They are NOT signed approval tokens and
        // must never be accepted as authorization for execution.
        static string Hash(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
        var canonical = request with { Title = request.Title.Trim(), StudentIds = ids };
        var requestHash = Hash(canonical);
        var sourceVersion = Hash(new { actor = user.Id, academyId, user.SecurityStamp, requestHash,
            academy.TimeZone, academy.EnabledModulesJson,
            batch = new { batch.Id, batch.TeacherId, batch.IsActive, batch.DeliveryMode, batch.MeetingLink, batch.StartDate, batch.EndDate, batch.UpdatedAtUtc },
            teacher = new { teacher.Id, teacher.Email, teacher.IsActive, teacher.UpdatedAtUtc },
            students = students.Select(x => new { x.Id, x.Email, x.IsActive, x.UpdatedAtUtc }),
            enrollments,
            channels = await db.CommunicationChannels.AsNoTracking().Where(x => x.AcademyId == academyId && x.Channel == "Meeting")
                .OrderBy(x => x.Id).Select(x => new { x.Id, x.Provider, x.Status, x.HasSecureConnection, x.UpdatedAtUtc }).ToArrayAsync(token) });
        return new(200, "DraftOnlyIntegrationUnavailable", new MeetingDraftPreview(request.Provider, batch.Id,
            teacher.Id, canonical.Title!, request.Start.ToUniversalTime(), request.Start.AddMinutes(request.DurationMinutes).ToUniversalTime(),
            academy.TimeZone, ids, emails, requestHash, sourceVersion, now.AddMinutes(5), false, "NotIssued"));
    }
}
