namespace AcademyDesk.Api.Domain.Entities;

public sealed class AttendanceRecord : AcademyEntity
{
    public Guid ClassSessionId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = "Present";
    public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
