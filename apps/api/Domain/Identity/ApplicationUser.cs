using Microsoft.AspNetCore.Identity;

namespace AcademyDesk.Api.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? AcademyId { get; set; }
    public Guid? TeacherId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? GuardianId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPlatformOwner { get; set; }
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public Guid? AcademyId { get; set; }
    public bool IsSystemRole { get; set; }
    public string PermissionsJson { get; set; } = "[]";
}
