namespace AcademyDesk.Api.Domain.Entities;

public sealed class Lead : AcademyEntity
{
    public required string FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ParentName { get; set; }
    public string? ProgramInterest { get; set; }
    public string Source { get; set; } = "WalkIn";
    public string Stage { get; set; } = "New";
    public Guid? BranchId { get; set; }
    public Guid? AssignedTeacherId { get; set; }
    public DateTime? FollowUpAtUtc { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedStudentId { get; set; }
}
