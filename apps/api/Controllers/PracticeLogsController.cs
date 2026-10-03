using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/practice-logs")]
public sealed class PracticeLogsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PracticeLog>>> List(Guid academyId, CancellationToken t) =>
        Ok(await db.PracticeLogs.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PracticeDate).ToListAsync(t));

    [HttpPost]
    public async Task<ActionResult<PracticeLog>> Create(Guid academyId, CreatePracticeLogRequest request, CancellationToken t)
    {
        if (request.MinutesPracticed is < 1 or > 1440)
            return BadRequest(new { message = "Practice time must be between 1 and 1440 minutes." });
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var date = request.PracticeDate ?? today;
        if (date > today) return BadRequest(new { message = "Practice date cannot be in the future." });
        if (request.FocusArea?.Length > 250 || request.Notes?.Length > 2000)
            return BadRequest(new { message = "Focus area must be 250 characters or fewer and notes 2000 characters or fewer." });
        if (request.StudentId == Guid.Empty || !await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId && x.IsActive, t))
            return BadRequest(new { message = "Select an active student belonging to this academy." });
        // Review metadata is owned by the separate Review action, never by create JSON.
        var row = new PracticeLog
        {
            AcademyId = academyId, StudentId = request.StudentId, PracticeDate = date,
            MinutesPracticed = request.MinutesPracticed, FocusArea = request.FocusArea, Notes = request.Notes,
            Status = "Logged", TeacherFeedback = null, ReviewedAtUtc = null,
            CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = null
        };
        db.PracticeLogs.Add(row); await db.SaveChangesAsync(t); return Ok(row);
    }

    [HttpPatch("{id:guid}/review")]
    public async Task<ActionResult<PracticeLog>> Review(Guid academyId, Guid id, ReviewPracticeLogRequest r, CancellationToken t)
    {
        var x = await db.PracticeLogs.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, t);
        if (x is null) return NotFound();
        x.TeacherFeedback = r.TeacherFeedback?.Trim(); x.Status = "Reviewed"; x.ReviewedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(t); return Ok(x);
    }
}
public sealed record ReviewPracticeLogRequest(string? TeacherFeedback);
public sealed record CreatePracticeLogRequest(Guid StudentId, DateOnly? PracticeDate, [Range(1, 1440)] int MinutesPracticed,
    [StringLength(250)] string? FocusArea, [StringLength(2000)] string? Notes);
