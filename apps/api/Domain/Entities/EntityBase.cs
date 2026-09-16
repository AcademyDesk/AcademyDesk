namespace AcademyDesk.Api.Domain.Entities;

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public abstract class AcademyEntity : EntityBase
{
    public Guid AcademyId { get; set; }
}
