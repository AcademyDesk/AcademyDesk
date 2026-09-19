namespace AcademyDesk.Api.Domain.Entities;
public sealed class TrialClassBooking : AcademyEntity { public Guid LeadId { get; set; } public Guid? BatchId { get; set; } public Guid? TeacherId { get; set; } public DateTime ScheduledAtUtc { get; set; } public string Status { get; set; } = "Booked"; public string? Notes { get; set; } }
