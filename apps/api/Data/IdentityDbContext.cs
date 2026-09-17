using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Email });
            entity.HasIndex(x => new { x.AcademyId, x.TeacherId });
            entity.HasIndex(x => new { x.AcademyId, x.StudentId });
            entity.HasIndex(x => new { x.AcademyId, x.GuardianId });
        });
    }
}
