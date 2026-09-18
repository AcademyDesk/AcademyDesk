namespace AcademyDesk.Api.Domain.Entities;

public sealed class StudentGuardian : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public string? Relationship { get; set; }
    public bool IsPrimary { get; set; }
    public bool CanAccessPortal { get; set; }
    public bool CanViewAcademicProgress { get; set; } = true;
    public bool CanViewFinance { get; set; } = true;
    public bool CanViewDocuments { get; set; } = true;
    public bool CanManageLeave { get; set; } = true;
    public DateTime? AccessGrantedAtUtc { get; set; }
    public DateTime? AccessRevokedAtUtc { get; set; }

    public Student? Student { get; set; }
    public Guardian? Guardian { get; set; }
}
