namespace AcademyDesk.Api.Domain.Entities;

public sealed class Assignment : AcademyEntity
{
    public Guid BatchId { get; set; }
    // Null means the whole class. A value makes the work private to one student.
    public Guid? StudentId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string Type { get; set; } = "Homework";
    public bool IsPublished { get; set; }
}
