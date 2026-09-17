namespace AcademyDesk.Api.Domain.Entities;

public sealed class PlatformSupportCase : EntityBase
{
    public Guid AcademyId { get; set; }
    public required string Subject { get; set; }
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
    public string? Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}
