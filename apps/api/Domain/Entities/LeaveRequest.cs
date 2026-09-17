namespace AcademyDesk.Api.Domain.Entities;
public sealed class LeaveRequest : AcademyEntity
{
    public string RequesterType { get; set; } = "Student";
    public Guid? StudentId { get; set; }
    public Guid? TeacherId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public required string Reason { get; set; }
    public string Status { get; set; } = "Requested";
    public string? DecisionNotes { get; set; }
}
