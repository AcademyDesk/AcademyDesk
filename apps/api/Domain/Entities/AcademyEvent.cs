namespace AcademyDesk.Api.Domain.Entities;

public sealed class AcademyEvent : AcademyEntity
{
    public required string Title { get; set; }
    public string Type { get; set; } = "Recital";
    public Guid? BranchId { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string? Venue { get; set; }
    public int? Capacity { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
}
