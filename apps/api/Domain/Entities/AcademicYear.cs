namespace AcademyDesk.Api.Domain.Entities;

public sealed class AcademicYear : AcademyEntity
{
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsClosed { get; set; }
}
