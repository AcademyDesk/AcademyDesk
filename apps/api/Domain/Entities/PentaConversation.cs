namespace AcademyDesk.Api.Domain.Entities;

// Local synthetic contract rehearsal only. No free-text customer transcript,
// model response or academy record is stored by the C1a service.
public sealed class PentaConversation : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid RequestId { get; set; }
    public long ContextVersion { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}

public sealed class PentaConversationTurn : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid RequestId { get; set; }
    public long ExpectedContextVersion { get; set; }
    public long CompletedContextVersion { get; set; }
    public required string Capability { get; set; }
    public required string InputDigest { get; set; }
}

public sealed class PentaConversationMessage : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid TurnId { get; set; }
    public long Sequence { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
}
