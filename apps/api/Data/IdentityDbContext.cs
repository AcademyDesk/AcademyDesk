using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Data;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(300);
            entity.HasIndex(x => new { x.AcademyId, x.Email });
            entity.HasIndex(x => new { x.AcademyId, x.TeacherId });
            entity.HasIndex(x => new { x.AcademyId, x.StudentId });
            entity.HasIndex(x => new { x.AcademyId, x.GuardianId });
        });
        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(x => x.PermissionsJson).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique().HasFilter("[AcademyId] IS NOT NULL");
        });
        builder.Entity<AccessGrant>(entity =>
        {
            entity.ToTable("AccessGrants");
            entity.Property(x => x.PermissionsJson).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(500);
            entity.HasIndex(x => new { x.AcademyId, x.UserId, x.RevokedAtUtc });
            entity.HasIndex(x => new { x.AcademyId, x.ExpiresAtUtc });
        });
    }
}
