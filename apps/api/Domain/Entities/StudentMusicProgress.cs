namespace AcademyDesk.Api.Domain.Entities;

public sealed class StudentMusicProgress : AcademyEntity
{
    public Guid StudentId { get; set; }
    public Guid MusicPieceId { get; set; }
    public string Status { get; set; } = "Assigned";
    public DateOnly? TargetDate { get; set; }
    public decimal? Score { get; set; }
    public string? Notes { get; set; }
}
