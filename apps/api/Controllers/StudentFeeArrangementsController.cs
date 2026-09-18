using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/students/{studentId:guid}/fee-arrangements")]
public sealed class StudentFeeArrangementsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(Guid academyId, Guid studentId, CancellationToken token) => Ok(await db.StudentFeeArrangements.AsNoTracking().Where(x => x.AcademyId == academyId && x.StudentId == studentId).OrderByDescending(x => x.EffectiveFrom).Select(x => new FeeArrangementSummary(x.Id, x.CourseId, x.SubjectName, x.Amount, x.Frequency, x.EffectiveFrom, x.EffectiveTo, x.IsActive)).ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, Guid studentId, FeeArrangementRequest request, CancellationToken token)
    {
        if (!await db.Students.AnyAsync(x => x.Id == studentId && x.AcademyId == academyId, token)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.SubjectName) || request.Amount <= 0 || !new[] { "Monthly", "Quarterly", "HalfYearly", "Annual" }.Contains(request.Frequency)) return BadRequest(new { message = "Subject, amount, and a valid billing frequency are required." });
        if (request.CourseId.HasValue && !await db.Courses.AnyAsync(x => x.Id == request.CourseId && x.AcademyId == academyId, token)) return BadRequest(new { message = "The course does not belong to this academy." });
        var arrangement = new StudentFeeArrangement { AcademyId = academyId, StudentId = studentId, CourseId = request.CourseId, SubjectName = request.SubjectName.Trim(), Amount = request.Amount, Frequency = request.Frequency, EffectiveFrom = request.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow) };
        db.StudentFeeArrangements.Add(arrangement); await db.SaveChangesAsync(token);
        return Ok(new FeeArrangementSummary(arrangement.Id, arrangement.CourseId, arrangement.SubjectName, arrangement.Amount, arrangement.Frequency, arrangement.EffectiveFrom, arrangement.EffectiveTo, arrangement.IsActive));
    }
}
public sealed record FeeArrangementRequest(Guid? CourseId, string SubjectName, decimal Amount, string Frequency, DateOnly? EffectiveFrom);
public sealed record FeeArrangementSummary(Guid Id, Guid? CourseId, string SubjectName, decimal Amount, string Frequency, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsActive);
