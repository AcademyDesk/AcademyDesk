namespace AcademyDesk.Api.Domain.Entities;

// Independent of the retained synthetic contract; no raw customer prompt stored.
public sealed class PentaMiniSession : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public long Version { get; set; }
    public Guid? PendingRequestId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public required string ProtectedState { get; set; }
}
public sealed class PentaMiniTurn : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid SessionId { get; set; }
    public Guid RequestId { get; set; }
    public long ExpectedVersion { get; set; }
    public required string InputDigest { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ProtectedReceipt { get; set; }
}
