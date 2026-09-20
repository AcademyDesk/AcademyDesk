namespace AcademyDesk.Api.Domain.Entities;
public sealed class LearningResource : AcademyEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "Link";
    public required string Url { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? ClassSessionId { get; set; }
    public bool IsPublished { get; set; } = true;
}
