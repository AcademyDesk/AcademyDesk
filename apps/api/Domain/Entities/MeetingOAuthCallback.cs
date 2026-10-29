namespace AcademyDesk.Api.Domain.Entities;

// A quarantined code awaiting authenticated resume, never a connected account.
// Code material exists only inside the protected envelope and is erased on consume.
public sealed class MeetingOAuthCallback : AcademyEntity
{
    public Guid AttemptId { get; set; }
    public Guid ActorUserId { get; set; }
    public required string Provider { get; set; }
    public required string ProtectedEnvelope { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Status { get; set; } = "Captured";
    public int Version { get; set; }
}
