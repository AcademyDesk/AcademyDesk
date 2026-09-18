namespace AcademyDesk.Api.Domain.Entities;

public sealed class StudentFeeArrangement : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid? CourseId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "Monthly";
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
