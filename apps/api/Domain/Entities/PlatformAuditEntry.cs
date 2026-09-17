namespace AcademyDesk.Api.Domain.Entities;

public sealed class PlatformAuditEntry : EntityBase
{
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = "System";
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
