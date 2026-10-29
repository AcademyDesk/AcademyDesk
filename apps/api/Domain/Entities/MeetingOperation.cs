namespace AcademyDesk.Api.Domain.Entities;

// Persistence foundation only. No production admission or dispatch path is enabled.
public sealed class MeetingOperation : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid ReceiptId { get; set; }
    public string Kind { get; set; } = "Create";
    public required string Provider { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestHash { get; set; }
    public required string SourceVersion { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public string Status { get; set; } = "Held";
    public int Version { get; set; }
}

public sealed class MeetingOutbox : AcademyEntity
{
    public Guid OperationId { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public int Version { get; set; }
}
