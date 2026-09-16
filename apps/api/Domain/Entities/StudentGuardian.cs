namespace AcademyDesk.Api.Domain.Entities;

public sealed class StudentGuardian : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public string? Relationship { get; set; }
    public bool IsPrimary { get; set; }

    public Student? Student { get; set; }
    public Guardian? Guardian { get; set; }
}
