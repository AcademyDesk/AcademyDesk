namespace AcademyDesk.Api.Domain.Entities;

// Inactive host operational-draft candidate. No prompt, credential or plain payload hash.
// Ordinary confirmation is review only. The inactive execution candidate uses a
// separately recorded exact creation authorization and encrypted committed receipt.
public sealed class PentaOnboardingDraft : AcademyEntity
{
    public Guid ActorUserId { get; set; }
    public Guid SessionId { get; set; }
    public long ContextVersion { get; set; }
    public long Revision { get; set; }
    public required string Status { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public required string ProtectedBody { get; set; }
}
