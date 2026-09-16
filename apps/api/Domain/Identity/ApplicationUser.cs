using Microsoft.AspNetCore.Identity;

namespace AcademyDesk.Api.Domain.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? AcademyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
}
