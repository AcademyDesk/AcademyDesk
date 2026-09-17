namespace AcademyDesk.Api.Domain.Entities;

public sealed class MusicPiece : AcademyEntity
{
    public required string Title { get; set; }
    public string? Composer { get; set; }
    public string? Instrument { get; set; }
    public string? Genre { get; set; }
    public string Difficulty { get; set; } = "Beginner";
    public int? DurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}
