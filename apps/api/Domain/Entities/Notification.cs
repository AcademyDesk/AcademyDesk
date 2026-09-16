namespace AcademyDesk.Api.Domain.Entities;

public sealed class Notification : AcademyEntity
{
    public Guid? RecipientId { get; set; }
    public required string RecipientType { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Status { get; set; } = "Queued";
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
