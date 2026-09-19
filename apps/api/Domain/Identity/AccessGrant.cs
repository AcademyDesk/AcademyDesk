namespace AcademyDesk.Api.Domain.Identity;

/// <summary>Additional, auditable permissions granted to one academy user.</summary>
public sealed class AccessGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AcademyId { get; set; }
    public Guid UserId { get; set; }
    public string PermissionsJson { get; set; } = "[]";
    public bool IsPermanent { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
    public Guid GrantedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? RevokedByUserId { get; set; }
}
