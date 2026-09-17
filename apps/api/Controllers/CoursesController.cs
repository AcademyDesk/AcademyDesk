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
    public async Task<ActionResult<IReadOnlyList<CourseSummary>>> List(Guid academyId, CancellationToken token) => Ok(await dbContext.Courses.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.Name).Select(SummaryProjection).ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult<CourseSummary>> Create(Guid academyId, CreateCourseRequest request, CancellationToken token)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound();
        var problem = await Validate(academyId, request.Name, request.AcademyType, request.CourseCode, request.WeeklySessions, request.SessionMinutes, request.MinimumAge, request.MaximumAge, token);
        if (problem is not null) return BadRequest(new { message = problem });
        var course = new ProgramCourse { AcademyId = academyId, Name = request.Name.Trim() }; Apply(course, request);
        dbContext.Courses.Add(course); await dbContext.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/courses/{course.Id}", ToSummary(course));
    }

    [HttpPut("{courseId:guid}")]
    public async Task<ActionResult<CourseSummary>> Update(Guid academyId, Guid courseId, UpdateCourseRequest request, CancellationToken token)
    {
        var course = await dbContext.Courses.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == courseId, token); if (course is null) return NotFound();
        var problem = await Validate(academyId, request.Name, request.AcademyType, request.CourseCode, request.WeeklySessions, request.SessionMinutes, request.MinimumAge, request.MaximumAge, token, courseId);
        if (problem is not null) return BadRequest(new { message = problem });
        Apply(course, request); course.IsActive = request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(ToSummary(course));
    }

    private async Task<string?> Validate(Guid academyId, string name, string? type, string? courseCode, int? weeklySessions, int? sessionMinutes, int? minimumAge, int? maximumAge, CancellationToken token, Guid? ignore = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Course name is required.";
        if (!new[] { "Music", "Tuition", "Coaching" }.Contains((type ?? "Music").Trim(), StringComparer.OrdinalIgnoreCase)) return "Academy type must be Music, Tuition, or Coaching.";
        if (weeklySessions is < 1 or > 14 || sessionMinutes is < 15 or > 480) return "Weekly sessions must be 1–14 and session duration 15–480 minutes.";
        if (minimumAge is < 0 or > 120 || maximumAge is < 0 or > 120 || (minimumAge.HasValue && maximumAge.HasValue && minimumAge > maximumAge)) return "Check the minimum and maximum age range.";
        var code = Clean(courseCode); if (code is not null && await dbContext.Courses.AnyAsync(x => x.AcademyId == academyId && x.Id != ignore && x.CourseCode == code, token)) return "That course code is already in use.";
        return null;
    }
    private static void Apply(ProgramCourse x, CourseRequest request) { x.Name = request.Name.Trim(); x.CourseCode = Clean(request.CourseCode); x.AcademyType = string.IsNullOrWhiteSpace(request.AcademyType) ? "Music" : request.AcademyType.Trim(); x.SubjectArea = Clean(request.SubjectArea); x.Level = Clean(request.Level); x.Description = Clean(request.Description); x.DurationMonths = request.DurationMonths; x.WeeklySessions = request.WeeklySessions; x.SessionMinutes = request.SessionMinutes; x.MinimumAge = request.MinimumAge; x.MaximumAge = request.MaximumAge; x.DeliveryMode = Clean(request.DeliveryMode); x.Prerequisites = Clean(request.Prerequisites); x.LearningOutcomes = Clean(request.LearningOutcomes); x.IsPublished = request.IsPublished; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static readonly System.Linq.Expressions.Expression<Func<ProgramCourse, CourseSummary>> SummaryProjection = x => new(x.Id, x.Name, x.CourseCode, x.AcademyType, x.SubjectArea, x.Level, x.Description, x.DurationMonths, x.WeeklySessions, x.SessionMinutes, x.MinimumAge, x.MaximumAge, x.DeliveryMode, x.Prerequisites, x.LearningOutcomes, x.IsPublished, x.IsActive);
    private static CourseSummary ToSummary(ProgramCourse x) => new(x.Id, x.Name, x.CourseCode, x.AcademyType, x.SubjectArea, x.Level, x.Description, x.DurationMonths, x.WeeklySessions, x.SessionMinutes, x.MinimumAge, x.MaximumAge, x.DeliveryMode, x.Prerequisites, x.LearningOutcomes, x.IsPublished, x.IsActive);
}

public abstract record CourseRequest(string Name, string? CourseCode, string? AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished);
public sealed record CreateCourseRequest(string Name, string? CourseCode, string? AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished) : CourseRequest(Name, CourseCode, AcademyType, SubjectArea, Level, Description, DurationMonths, WeeklySessions, SessionMinutes, MinimumAge, MaximumAge, DeliveryMode, Prerequisites, LearningOutcomes, IsPublished);
public sealed record UpdateCourseRequest(string Name, string? CourseCode, string? AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished, bool IsActive) : CourseRequest(Name, CourseCode, AcademyType, SubjectArea, Level, Description, DurationMonths, WeeklySessions, SessionMinutes, MinimumAge, MaximumAge, DeliveryMode, Prerequisites, LearningOutcomes, IsPublished);
public sealed record CourseSummary(Guid Id, string Name, string? CourseCode, string AcademyType, string? SubjectArea, string? Level, string? Description, int? DurationMonths, int? WeeklySessions, int? SessionMinutes, int? MinimumAge, int? MaximumAge, string? DeliveryMode, string? Prerequisites, string? LearningOutcomes, bool IsPublished, bool IsActive);
