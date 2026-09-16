namespace AcademyDesk.Api.Domain.Entities;

public sealed class Enrollment : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid BatchId { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "Active";
}
