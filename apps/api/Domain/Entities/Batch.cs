namespace AcademyDesk.Api.Domain.Entities;

public sealed class Batch : AcademyEntity
{
    public required string Name { get; set; }
    public Guid CourseId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? BranchId { get; set; }
    public int Capacity { get; set; } = 10;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}
