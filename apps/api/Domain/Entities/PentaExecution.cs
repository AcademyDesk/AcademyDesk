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
    public string ApprovalMode { get; set; } = "None";
    public Guid? ResultCorrelationId { get; set; }
    public string? ResultMessage { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

// No approval endpoint or authorizing transition is enabled in the pilot.
public sealed class PentaApproval : AcademyEntity
{
    public Guid ExecutionId { get; set; }
    public Guid InitiatorUserId { get; set; }
    public Guid? ApproverUserId { get; set; }
    public required string Status { get; set; }
    public required string DecisionDigest { get; set; }
    public required string PolicyVersion { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

// One reservation is claimed with each synthetic execution. Units are local
// diagnostic work quota, NOT tokens, API billing, or permission to use a model.
public sealed class PentaUsageReservation : AcademyEntity
{
    public Guid ExecutionId { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime WindowStartUtc { get; set; }
    public int UnitsReserved { get; set; }
    public required string UnitKind { get; set; }
    public required string Status { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public required string Currency { get; set; }
    public required string PriceVersion { get; set; }
}

public sealed class PentaUsageEntry : AcademyEntity
{
    public Guid ReservationId { get; set; }
    public required string EventKey { get; set; }
    public required string Outcome { get; set; }
    public int UnitsObserved { get; set; }
    public decimal ActualCost { get; set; }
    public required string Currency { get; set; }
}

public sealed class PentaAttempt : AcademyEntity
{
    public Guid ExecutionId { get; set; }
    public required string Outcome { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}
