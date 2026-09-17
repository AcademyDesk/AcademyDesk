namespace AcademyDesk.Api.Domain.Entities;

/// <summary>
/// Per-contact consent records, kept separately from the contact profile so that
/// the academy has an auditable communication history.
/// </summary>
public sealed class CommunicationPreference : AcademyEntity
{
    public Guid RecipientId { get; set; }
    public required string RecipientType { get; set; }
    public bool EmailAllowed { get; set; }
    public bool WhatsAppAllowed { get; set; }
    public bool MarketingAllowed { get; set; }
    public DateTime? EmailOptedInAtUtc { get; set; }
    public DateTime? WhatsAppOptedInAtUtc { get; set; }
    public DateTime? OptedOutAtUtc { get; set; }
    public string? Notes { get; set; }
}
