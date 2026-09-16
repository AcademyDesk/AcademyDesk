using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/courses")]
public sealed class CoursesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourseSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var courses = await dbContext.Courses.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CourseSummary(x.Id, x.Name, x.AcademyType, x.Level, x.Description, x.DurationMonths, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(courses);
    }

    [HttpPost]
    public async Task<ActionResult<CourseSummary>> Create(Guid academyId, CreateCourseRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Course name is required." });
        var type = string.IsNullOrWhiteSpace(request.AcademyType) ? "Music" : request.AcademyType.Trim();
        var allowedTypes = new[] { "Music", "Tuition", "Coaching" };
        if (!allowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Academy type must be Music, Tuition, or Coaching." });

        var course = new ProgramCourse { AcademyId = academyId, Name = request.Name.Trim(), AcademyType = type, Level = request.Level?.Trim(), Description = request.Description?.Trim(), DurationMonths = request.DurationMonths };
        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/courses/{course.Id}", new CourseSummary(course.Id, course.Name, course.AcademyType, course.Level, course.Description, course.DurationMonths, course.IsActive));
    }
}

public sealed record CreateCourseRequest(string Name, string? AcademyType, string? Level, string? Description, int? DurationMonths);
public sealed record CourseSummary(Guid Id, string Name, string AcademyType, string? Level, string? Description, int? DurationMonths, bool IsActive);
