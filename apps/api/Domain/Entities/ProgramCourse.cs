namespace AcademyDesk.Api.Domain.Entities;

public sealed class ProgramCourse : AcademyEntity
{
    public required string Name { get; set; }
    public string AcademyType { get; set; } = "Music";
    public string? Level { get; set; }
    public string? Description { get; set; }
    public int? DurationMonths { get; set; }
    public bool IsActive { get; set; } = true;
}
