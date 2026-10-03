namespace AcademyDesk.Api.Domain.Entities;

public sealed class AssessmentResult : AcademyEntity
{
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Score { get; set; }
    public string? Grade { get; set; }
    // Null means legacy provenance is unknown; preserve the recorded grade.
    public bool? IsGradeManual { get; set; }
    public string? Remarks { get; set; }
    public bool IsPublished { get; set; }
}
