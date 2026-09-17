namespace AcademyDesk.Api.Domain.Entities;

/// <summary>
/// A tenant-owned sender configuration. Provider credentials are intentionally
/// not stored here; they are connected through a secure provider flow.
/// </summary>
public sealed class CommunicationChannel : AcademyEntity
{
    public required string Channel { get; set; }
    public required string Provider { get; set; }
    public string Status { get; set; } = "NotConfigured";
    public string? SenderName { get; set; }
    public string? SenderAddress { get; set; }
    public string? ReplyToAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ExternalAccountReference { get; set; }
    public bool MessagesEnabled { get; set; }
    public bool HasSecureConnection { get; set; }
}
