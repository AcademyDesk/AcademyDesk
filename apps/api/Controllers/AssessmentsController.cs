using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/assessments")]
public sealed class AssessmentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssessmentSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.Assessments.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.ScheduledAtUtc).Select(x => new AssessmentSummary(x.Id, x.BatchId, x.Title, x.Type, x.MaxScore, x.ScheduledAtUtc, x.IsPublished)).ToListAsync(cancellationToken));

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
    public async Task<ActionResult<IReadOnlyList<AssessmentResultSummary>>> List(Guid academyId, Guid assessmentId, CancellationToken cancellationToken) => Ok(await dbContext.AssessmentResults.AsNoTracking().Where(x => x.AcademyId == academyId && x.AssessmentId == assessmentId).Select(x => new AssessmentResultSummary(x.Id, x.StudentId, x.Score, x.Grade, x.Remarks, x.IsPublished)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AssessmentResultSummary>> Upsert(Guid academyId, Guid assessmentId, RecordAssessmentResultRequest request, CancellationToken cancellationToken)
    {
        var assessment = await dbContext.Assessments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assessmentId && x.AcademyId == academyId, cancellationToken);
        if (assessment is null) return NotFound();
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, cancellationToken)) return BadRequest(new { message = "The student does not belong to this academy." });
        if (request.Score < 0 || request.Score > assessment.MaxScore) return BadRequest(new { message = "Score must be within the assessment range." });
        var result = await dbContext.AssessmentResults.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.AssessmentId == assessmentId && x.StudentId == request.StudentId, cancellationToken);
        var isNew = result is null;
        result ??= new AssessmentResult { AcademyId = academyId, AssessmentId = assessmentId, StudentId = request.StudentId };
        result.Score = request.Score; result.Grade = request.Grade?.Trim(); result.Remarks = request.Remarks?.Trim(); result.IsPublished = request.IsPublished;
        if (assessment.GradingSchemeId.HasValue) { var scheme = await dbContext.GradingSchemes.AsNoTracking().SingleAsync(x => x.Id == assessment.GradingSchemeId && x.AcademyId == academyId, cancellationToken); result.Grade = request.Grade?.Trim() ?? (request.Score * 100m / assessment.MaxScore >= scheme.PassingPercent ? "Pass" : "Fail"); }
        if (isNew) dbContext.AssessmentResults.Add(result); await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AssessmentResultSummary(result.Id, result.StudentId, result.Score, result.Grade, result.Remarks, result.IsPublished));
    }
}

public sealed record CreateAssessmentRequest(Guid BatchId, string Title, string? Type, decimal MaxScore, Guid? GradingSchemeId, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record AssessmentSummary(Guid Id, Guid BatchId, string Title, string Type, decimal MaxScore, DateTime? ScheduledAtUtc, bool IsPublished);
public sealed record PublishAssessmentRequest(bool IsPublished);
public sealed record RecordAssessmentResultRequest(Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished);
public sealed record AssessmentResultSummary(Guid Id, Guid StudentId, decimal Score, string? Grade, string? Remarks, bool IsPublished);
