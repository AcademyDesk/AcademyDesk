using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/assessments")]
public sealed class AssessmentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.Assessments.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.ScheduledAtUtc).Select(x => new AssessmentSummary(x.Id, x.BatchId, x.Title, x.Type, x.MaxScore, x.ScheduledAtUtc, x.IsPublished)).ToListAsync(cancellationToken));

    [HttpGet("options")]
    public async Task<ActionResult<AssessmentOptions>> Options(Guid academyId, CancellationToken cancellationToken)
    {
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new AssessmentBatchOption(x.Id, x.Name)).ToListAsync(cancellationToken);
        var roster = await dbContext.Enrollments.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.Status == "Active" && dbContext.Batches.Any(b => b.Id == x.BatchId && b.AcademyId == academyId))
            .Join(dbContext.Students.AsNoTracking().Where(x => x.AcademyId == academyId), x => x.StudentId, s => s.Id,
                (x, s) => new { x.BatchId, StudentId = s.Id, s.FirstName, s.LastName })
            .Distinct().OrderBy(x => x.BatchId).ThenBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.StudentId).ToListAsync(cancellationToken);
        var students = roster.Select(x => new AssessmentStudentOption(x.StudentId, x.FirstName, x.LastName)).Distinct()
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).ToList();
        var enrollments = roster.Select(x => new AssessmentEnrollmentOption(x.StudentId, x.BatchId, "Active")).ToList();
        var schemes = await dbContext.GradingSchemes.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).Select(x => new AssessmentSchemeOption(x.Id, x.Name, x.PassingPercent)).ToListAsync(cancellationToken);
        return Ok(new AssessmentOptions(batches, students, enrollments, schemes));
    }

    [HttpPost]
    public async Task<ActionResult<AssessmentSummary>> Create(Guid academyId, CreateAssessmentRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Batches.AnyAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The selected batch does not belong to this academy." });
        if (string.IsNullOrWhiteSpace(request.Title) || request.MaxScore <= 0) return BadRequest(new { message = "Title and a positive maximum score are required." });
        if (request.GradingSchemeId.HasValue && !await dbContext.GradingSchemes.AnyAsync(x => x.Id == request.GradingSchemeId && x.AcademyId == academyId && x.IsActive, cancellationToken)) return BadRequest(new { message = "The grading scheme does not belong to this academy." });
        var assessment = new Assessment { AcademyId = academyId, BatchId = request.BatchId, Title = request.Title.Trim(), Type = string.IsNullOrWhiteSpace(request.Type) ? "Assessment" : request.Type.Trim(), MaxScore = request.MaxScore, GradingSchemeId = request.GradingSchemeId, ScheduledAtUtc = request.ScheduledAtUtc, IsPublished = request.IsPublished };
        dbContext.Assessments.Add(assessment); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/assessments/{assessment.Id}", new AssessmentSummary(assessment.Id, assessment.BatchId, assessment.Title, assessment.Type, assessment.MaxScore, assessment.ScheduledAtUtc, assessment.IsPublished));
    }
    [HttpPatch("{assessmentId:guid}/publish")]
    public async Task<ActionResult> Publish(Guid academyId, Guid assessmentId, PublishAssessmentRequest request, CancellationToken token)
    { var x=await dbContext.Assessments.SingleOrDefaultAsync(v=>v.Id==assessmentId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsPublished=request.IsPublished; await dbContext.SaveChangesAsync(token); return Ok(); }
}

[ApiController]
[Route("api/academies/{academyId:guid}/assessments/{assessmentId:guid}/results")]
public sealed class AssessmentResultsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentResultSummary>>> List(Guid academyId, Guid assessmentId, CancellationToken cancellationToken) => Ok(await dbContext.AssessmentResults.AsNoTracking().Where(x => x.AcademyId == academyId && x.AssessmentId == assessmentId).Select(x => new AssessmentResultSummary(x.Id, x.StudentId, x.Score, x.Grade, x.Remarks, x.IsPublished, x.IsGradeManual)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AssessmentResultSummary>> Upsert(Guid academyId, Guid assessmentId, RecordAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var assessment = await dbContext.Assessments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assessmentId && x.AcademyId == academyId, cancellationToken);
        if (assessment is null) return NotFound();
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." });
        if (!await dbContext.Enrollments.AnyAsync(x => x.AcademyId == academyId && x.BatchId == assessment.BatchId && x.StudentId == request.StudentId, cancellationToken))
            return BadRequest(new { message = "The student is not enrolled in this assessment's batch." });
        if (request.Score < 0 || request.Score > assessment.MaxScore) return BadRequest(new { message = "Score must be within the assessment range." });
        var grading = await AssessmentGrading.ResolveAsync(dbContext, assessment, request.Score, request.Grade, request.IsGradeManual, cancellationToken);
        if (grading.Error is not null) return BadRequest(new { message = grading.Error });
        var result = await dbContext.AssessmentResults.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.AssessmentId == assessmentId && x.StudentId == request.StudentId, cancellationToken);
        var isNew = result is null;
        result ??= new AssessmentResult { AcademyId = academyId, AssessmentId = assessmentId, StudentId = request.StudentId };
        result.Score = request.Score; result.Grade = grading.Grade; result.IsGradeManual = grading.IsManual; result.Remarks = request.Remarks?.Trim(); result.IsPublished = request.IsPublished;
        if (isNew) dbContext.AssessmentResults.Add(result); await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AssessmentResultSummary(result.Id, result.StudentId, result.Score, result.Grade, result.Remarks, result.IsPublished, result.IsGradeManual));
    }
}

public sealed record CreateAssessmentRequest(Guid BatchId, string Title, string? Type, decimal MaxScore, Guid? GradingSchemeId, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record AssessmentSummary(Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record PublishAssessmentRequest(bool IsPublished);
public sealed record RecordAssessmentResultRequest(Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished, bool? IsGradeManual = null);
public sealed record AssessmentResultSummary(Guid Id, Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished, bool? IsGradeManual = null);
public sealed record AssessmentOptions(IReadOnlyList<AssessmentBatchOption> Batches, IReadOnlyList<AssessmentStudentOption> Students, IReadOnlyList<AssessmentEnrollmentOption> Enrollments, IReadOnlyList<AssessmentSchemeOption> GradingSchemes);
public sealed record AssessmentBatchOption(Guid Id, string Name);
public sealed record AssessmentStudentOption(Guid Id, string FirstName, string LastName);
public sealed record AssessmentEnrollmentOption(Guid StudentId, Guid BatchId, string Status);
public sealed record AssessmentSchemeOption(Guid Id, string Name, decimal PassingPercent);
