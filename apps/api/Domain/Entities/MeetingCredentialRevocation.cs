namespace AcademyDesk.Api.Domain.Entities;

// Local disconnect/revocation evidence. Acknowledged is not global revocation proof.
public sealed class MeetingCredentialRevocation : AcademyEntity
{
    public Guid ExchangeId { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid BindingId { get; set; }
    public required string Provider { get; set; }
    public int Generation { get; set; }
    public int BindingVersion { get; set; }
    public Guid Fence { get; set; }
    public required string RegistrationFingerprint { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public string Status { get; set; } = "Started";
    public int Version { get; set; }
}
