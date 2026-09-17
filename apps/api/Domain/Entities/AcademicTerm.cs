namespace AcademyDesk.Api.Domain.Entities;

public sealed class AcademicTerm : AcademyEntity
{
    public Guid AcademicYearId { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
}
