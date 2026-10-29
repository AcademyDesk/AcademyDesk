namespace AcademyDesk.Api.Domain.Entities;

// No Connected state or credential fields until account/token verification is qualified.
public sealed class MeetingProviderBinding : AcademyEntity
{
    public required string Provider { get; set; }
    public string Status { get; set; } = "NotConnected";
    public int Generation { get; set; }
    public int Version { get; set; }
}

public sealed class MeetingOAuthAttempt : AcademyEntity
{
    public Guid BindingId { get; set; }
    public Guid ActorUserId { get; set; }
    public required string Provider { get; set; }
    public int Generation { get; set; }
    public required string StateHash { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public int Version { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
}
