namespace AcademyDesk.Api.Domain.Entities;
public sealed class MakeupClass : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid BatchId { get; set; }
    public Guid? TeacherId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string DeliveryMode { get; set; } = "Offline";
    public string? Venue { get; set; }
    public string? MeetingLink { get; set; }
    public bool UsesNextScheduledClass { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Notes { get; set; }
}
