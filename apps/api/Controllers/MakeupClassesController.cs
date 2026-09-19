using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/makeup-classes")]
public sealed class MakeupClassesController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, CancellationToken token) => Ok(await db.MakeupClasses.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.StartUtc).Select(x => new MakeupSummary(x.Id, x.StudentId, x.BatchId, x.TeacherId, x.StartUtc, x.EndUtc, x.DeliveryMode, x.Venue, x.MeetingLink, x.UsesNextScheduledClass, x.Status, x.Notes)).ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, CreateMakeupRequest request, CancellationToken token)
    {
        var batch = await db.Batches.SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token);
        if (batch is null || !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or class/batch is invalid." });
        var deliveryMode = request.DeliveryMode?.Trim() ?? "Offline";
        if (!new[] { "Online", "Offline", "Hybrid" }.Contains(deliveryMode, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Venue must be Online, Offline, or Hybrid." });
        DateTime startUtc; DateTime endUtc; Guid? teacherId = request.TeacherId; string? venue = request.Venue?.Trim(); string? meetingLink = request.MeetingLink?.Trim();
        if (request.UseNextScheduledClass)
        {
            var session = await db.ClassSessions.Where(x => x.AcademyId == academyId && x.BatchId == batch.Id && x.Status == "Scheduled" && x.StartUtc > DateTime.UtcNow).OrderBy(x => x.StartUtc).FirstOrDefaultAsync(token);
            if (session is null) return BadRequest(new { message = "There is no upcoming scheduled class for this batch. Select a make-up date and time instead." });
            startUtc = session.StartUtc; endUtc = session.EndUtc; teacherId = session.TeacherId ?? batch.TeacherId; deliveryMode = session.DeliveryMode; venue = session.DeliveryMode == "Offline" ? session.RoomName : null; meetingLink = session.DeliveryMode is "Online" or "Hybrid" ? session.RoomName : null;
        }
        else { if (!request.StartUtc.HasValue) return BadRequest(new { message = "Select a make-up date and time." }); startUtc = request.StartUtc.Value; endUtc = startUtc.AddMinutes(batch.SessionMinutes is > 0 ? batch.SessionMinutes : 60); }
        if (deliveryMode is "Online" or "Hybrid" && string.IsNullOrWhiteSpace(meetingLink)) return BadRequest(new { message = "A meeting link is required for online and hybrid make-up classes." });
        if (teacherId.HasValue && !await db.Teachers.AnyAsync(x => x.Id == teacherId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Teacher is invalid." });
        var makeup = new MakeupClass { AcademyId = academyId, StudentId = request.StudentId, BatchId = batch.Id, TeacherId = teacherId, StartUtc = startUtc, EndUtc = endUtc, DeliveryMode = deliveryMode, Venue = venue, MeetingLink = meetingLink, UsesNextScheduledClass = request.UseNextScheduledClass, Notes = request.Notes?.Trim() };
        db.MakeupClasses.Add(makeup); QueueNotifications(academyId, request.StudentId, batch.Name, startUtc, deliveryMode, venue, meetingLink); await db.SaveChangesAsync(token);
        return Ok(new MakeupSummary(makeup.Id, makeup.StudentId, makeup.BatchId, makeup.TeacherId, makeup.StartUtc, makeup.EndUtc, makeup.DeliveryMode, makeup.Venue, makeup.MeetingLink, makeup.UsesNextScheduledClass, makeup.Status, makeup.Notes));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> UpdateStatus(Guid academyId, Guid id, UpdateMakeupRequest request, CancellationToken token)
    { if (request.Status is not ("Scheduled" or "Completed" or "Cancelled")) return BadRequest(); var makeup = await db.MakeupClasses.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, token); if (makeup is null) return NotFound(); makeup.Status = request.Status; await db.SaveChangesAsync(token); return Ok(); }

    private void QueueNotifications(Guid academyId, Guid studentId, string batchName, DateTime startUtc, string deliveryMode, string? venue, string? meetingLink)
    {
        var ist = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startUtc, DateTimeKind.Utc), TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"));
        var location = deliveryMode is "Online" or "Hybrid" ? meetingLink : venue;
        var message = $"A make-up class for {batchName} is scheduled on {ist:ddd, d MMM h:mm tt} IST. {deliveryMode}{(string.IsNullOrWhiteSpace(location) ? "" : $" · {location}")}";
        db.Notifications.Add(new Notification { AcademyId = academyId, RecipientId = studentId, RecipientType = "Student", Title = "Make-up class scheduled", Message = message, Channel = "InApp" });
        var parentIds = db.StudentGuardians.Where(x => x.AcademyId == academyId && x.StudentId == studentId && x.CanAccessPortal && x.AccessRevokedAtUtc == null).Select(x => x.GuardianId).ToList();
        foreach (var parentId in parentIds) db.Notifications.Add(new Notification { AcademyId = academyId, RecipientId = parentId, RecipientType = "Parent", Title = "Make-up class scheduled", Message = message, Channel = "InApp" });
    }
}

public sealed record CreateMakeupRequest(Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime? StartUtc, string? DeliveryMode, string? Venue, string? MeetingLink, bool UseNextScheduledClass, string? Notes);
public sealed record MakeupSummary(Guid Id, Guid StudentId, Guid BatchId, Guid? TeacherId, DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? Venue, string? MeetingLink, bool UsesNextScheduledClass, string Status, string? Notes);
public sealed record UpdateMakeupRequest(string Status);
