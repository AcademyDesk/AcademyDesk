namespace AcademyDesk.Api.Domain.Entities;

// Pilot-only ledger. User prompts are never persisted here; InputDigest is a
// one-way fingerprint for repeat-request conflict detection.
public sealed class PentaTask : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public required string Capability { get; set; }
    public required string Status { get; set; }
}

public sealed class PentaExecution : AcademyEntity
{
    public Guid TaskId { get; set; }
    public Guid ActorUserId { get; set; }
    public required string ToolName { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string InputDigest { get; set; }
    public required string Status { get; set; }
    public Guid? ResultCorrelationId { get; set; }
    public string? ResultMessage { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class PentaAttempt : AcademyEntity
{
    public Guid ExecutionId { get; set; }
    public required string Outcome { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}
