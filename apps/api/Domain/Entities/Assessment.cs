namespace AcademyDesk.Api.Domain.Entities;

public sealed class Assessment : AcademyEntity
{
    public Guid BatchId { get; set; }
    public required string Title { get; set; }
    public string Type { get; set; } = "Assessment";
    public decimal MaxScore { get; set; } = 100;
    public Guid? GradingSchemeId { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public bool IsPublished { get; set; }
}
