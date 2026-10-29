namespace AcademyDesk.Api.Domain.Entities;

// Quarantine only. None of these states authorize provider actions or a connection.
public sealed class MeetingOAuthExchange : AcademyEntity
{
    public Guid AttemptId { get; set; }
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
