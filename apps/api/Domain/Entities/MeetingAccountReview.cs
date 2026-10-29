namespace AcademyDesk.Api.Domain.Entities;

// Confirmation evidence only. Never a Connected/meeting-entitlement grant.
public sealed class MeetingAccountReview : AcademyEntity
{
    public Guid ExchangeId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid BindingId { get; set; }
    public required string Provider { get; set; }
    public int Generation { get; set; }
    public int BindingVersion { get; set; }
    public required string RegistrationFingerprint { get; set; }
    public required string ReviewHash { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Status { get; set; } = "AwaitingConfirmation";
    public int Version { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
}
