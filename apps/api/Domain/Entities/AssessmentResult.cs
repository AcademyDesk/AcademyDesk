namespace AcademyDesk.Api.Domain.Entities;

public sealed class AssessmentResult : AcademyEntity
{
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Score { get; set; }
    public string? Grade { get; set; }
    public string? Remarks { get; set; }
    public bool IsPublished { get; set; }
}
