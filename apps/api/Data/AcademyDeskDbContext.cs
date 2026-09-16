using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Data;

public sealed class AcademyDeskDbContext(DbContextOptions<AcademyDeskDbContext> options)
    : DbContext(options)
{
    public DbSet<Academy> Academies => Set<Academy>();
    public DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Academy>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(250);
            entity.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(x => x.TimeZone).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.State).HasMaxLength(120);
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.HasIndex(x => new { x.AcademyId, x.Name }).IsUnique();
            entity.HasOne(x => x.Academy)
                .WithMany(x => x.Branches)
                .HasForeignKey(x => x.AcademyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
