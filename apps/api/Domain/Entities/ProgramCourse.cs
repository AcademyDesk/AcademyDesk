namespace AcademyDesk.Api.Domain.Entities;

public sealed class ProgramCourse : AcademyEntity
{
    public required string Name { get; set; }
    public string? CourseCode { get; set; }
    public string AcademyType { get; set; } = "Music";
    public string? SubjectArea { get; set; }
    public string? Level { get; set; }
    public string? Description { get; set; }
    public int? DurationMonths { get; set; }
    public int? WeeklySessions { get; set; }
    public int? SessionMinutes { get; set; }
    public int? MinimumAge { get; set; }
    public int? MaximumAge { get; set; }
    public string? DeliveryMode { get; set; }
    public string? Prerequisites { get; set; }
    public string? LearningOutcomes { get; set; }
    public bool IsPublished { get; set; }
    public bool IsActive { get; set; } = true;
}
