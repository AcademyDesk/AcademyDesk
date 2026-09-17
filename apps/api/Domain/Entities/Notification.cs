namespace AcademyDesk.Api.Domain.Entities;

public sealed class Notification : AcademyEntity
{
    public Guid? RecipientId { get; set; }
    public required string RecipientType { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Status { get; set; } = "Queued";
    public Guid? TemplateId { get; set; }
    public string? VariablesJson { get; set; }
    public string? FailureReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
