namespace AcademyDesk.Api.Domain.Entities;
public sealed class AssignmentSubmission : AcademyEntity { public Guid AssignmentId { get; set; } public Guid StudentId { get; set; } public string? ResponseText { get; set; } public string Status { get; set; } = "Submitted"; public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow; public string? TeacherFeedback { get; set; } }
