namespace AcademyDesk.Api.Domain.Entities;
public sealed class AccessReview : AcademyEntity { public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow; public string? Notes { get; set; } public Guid? ReviewerUserId { get; set; } }
