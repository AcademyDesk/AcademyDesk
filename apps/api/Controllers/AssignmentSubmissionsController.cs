using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/assignment-submissions")]
public sealed class AssignmentSubmissionsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubmissionReviewSummary>>> List(Guid academyId, Guid? assignmentId, CancellationToken t)
    {
        var q = db.AssignmentSubmissions.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (assignmentId.HasValue) q = q.Where(x => x.AssignmentId == assignmentId);
        return Ok(await ContextAsync(academyId, await q.OrderByDescending(x => x.SubmittedAtUtc).ToListAsync(t), t));
    }
    [HttpPost]
    public async Task<ActionResult<AssignmentSubmission>> Submit(Guid academyId, CreateAssignmentSubmissionRequest request, CancellationToken t)
    {
        if (request.AssignmentId == Guid.Empty || request.StudentId == Guid.Empty || request.ResponseText?.Length > 4000)
            return BadRequest(new { message = "Select an assignment and student. Written responses must be 4000 characters or fewer." });
        var assignment = await db.Assignments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.AssignmentId && x.AcademyId == academyId && x.IsPublished, t);
        if (assignment is null || !await db.Batches.AnyAsync(x => x.Id == assignment.BatchId && x.AcademyId == academyId, t))
            return BadRequest(new { message = "Select a published assignment belonging to this academy and batch." });
        if (!await db.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, t)
            || assignment.StudentId.HasValue && assignment.StudentId != request.StudentId
            || !await db.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.BatchId == assignment.BatchId && x.Status == "Active", t))
            return BadRequest(new { message = "Select a student enrolled in this assignment's batch and eligible for this work." });
        if (await db.AssignmentSubmissions.AnyAsync(x => x.AcademyId == academyId && x.AssignmentId == request.AssignmentId && x.StudentId == request.StudentId, t))
            return Conflict(new { message = "A submission already exists for this student and assignment. Existing work was not changed." });
        var now = DateTime.UtcNow;
        var row = new AssignmentSubmission
        {
            AcademyId = academyId, AssignmentId = request.AssignmentId, StudentId = request.StudentId,
            ResponseText = request.ResponseText, Status = "Submitted", TeacherFeedback = null,
            SubmittedAtUtc = now, CreatedAtUtc = now, UpdatedAtUtc = null
        };
        db.AssignmentSubmissions.Add(row);
        try { await db.SaveChangesAsync(t); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The existing unique key also protects simultaneous creates. Do not hide unrelated SQL errors.
            db.Entry(row).State = EntityState.Detached;
            if (await db.AssignmentSubmissions.AnyAsync(x => x.AcademyId == academyId && x.AssignmentId == request.AssignmentId && x.StudentId == request.StudentId, t))
                return Conflict(new { message = "A submission already exists for this student and assignment. Existing work was not changed." });
            throw;
        }
        return Ok(row);
    }
    [HttpPatch("{id:guid}/review")]
    public async Task<ActionResult<SubmissionReviewSummary>> Review(Guid academyId, Guid id, ReviewRequest r, CancellationToken t)
    {
        var x = await db.AssignmentSubmissions.SingleOrDefaultAsync(x => x.Id == id && x.AcademyId == academyId, t);
        if (x is null) return NotFound();
        x.TeacherFeedback = r.Feedback; x.Status = "Reviewed"; await db.SaveChangesAsync(t);
        return Ok((await ContextAsync(academyId, [x], t))[0]);
    }
    // Tenant-scope every label, including labels behind invalid legacy references.
    private async Task<IReadOnlyList<SubmissionReviewSummary>> ContextAsync(Guid academyId, IReadOnlyList<AssignmentSubmission> rows, CancellationToken t)
    {
        if (rows.Count == 0) return [];
        var studentIds = rows.Select(x => x.StudentId).Distinct().ToArray(); var assignmentIds = rows.Select(x => x.AssignmentId).Distinct().ToArray();
        var students = await db.Students.AsNoTracking().Where(x => x.AcademyId == academyId && studentIds.Contains(x.Id)).Select(x => new { x.Id, x.FirstName, x.LastName, x.StudentNumber }).ToDictionaryAsync(x => x.Id, t);
        var assignments = await db.Assignments.AsNoTracking().Where(x => x.AcademyId == academyId && assignmentIds.Contains(x.Id)).Select(x => new { x.Id, x.Title, x.BatchId }).ToDictionaryAsync(x => x.Id, t);
        var batchIds = assignments.Values.Select(x => x.BatchId).Distinct().ToArray();
        var batches = await db.Batches.AsNoTracking().Where(x => x.AcademyId == academyId && batchIds.Contains(x.Id)).Select(x => new { x.Id, x.Name }).ToDictionaryAsync(x => x.Id, t);
        return rows.Select(x =>
        {
            var student = students.GetValueOrDefault(x.StudentId); var assignment = assignments.GetValueOrDefault(x.AssignmentId); var batch = assignment is null ? null : batches.GetValueOrDefault(assignment.BatchId);
            return new SubmissionReviewSummary(x.Id, x.AcademyId, x.AssignmentId, x.StudentId, x.ResponseText, x.Status, x.SubmittedAtUtc, x.TeacherFeedback, x.CreatedAtUtc, x.UpdatedAtUtc, student is null ? null : $"{student.FirstName} {student.LastName}".Trim(), student?.StudentNumber, assignment?.Title, batch?.Id, batch?.Name);
        }).ToArray();
    }
}
public sealed record ReviewRequest(string? Feedback);
public sealed record CreateAssignmentSubmissionRequest(Guid AssignmentId, Guid StudentId, [StringLength(4000)] string? ResponseText);
public sealed record SubmissionReviewSummary(Guid Id, Guid AcademyId, Guid AssignmentId, Guid StudentId, string? ResponseText, string Status, DateTime SubmittedAtUtc, string? TeacherFeedback, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, string? StudentName, string? StudentNumber, string? AssignmentTitle, Guid? BatchId, string? BatchName);
