namespace AcademyDesk.Api.Domain.Entities;

// Reviewed draft only; never an executable provider authorization.
public sealed class MeetingPreviewReceipt : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string RequestHash { get; set; }
    public required string SourceVersion { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public string Status { get; set; } = "AwaitingReview";
    public int Version { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
