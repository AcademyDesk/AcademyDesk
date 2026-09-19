namespace AcademyDesk.Api.Domain.Entities;

public sealed class Certificate : AcademyEntity
{
    public required string CertificateNumber { get; set; }
    public Guid StudentId { get; set; }
    public Guid? BatchId { get; set; }
    public required string Title { get; set; }
    public string TemplateKey { get; set; } = "music-recital";
    public string DesignKey { get; set; } = "none";
    public int ArtworkX { get; set; } = 50;
    public int ArtworkY { get; set; } = 30;
    public int ArtworkSize { get; set; } = 72;
    public required string VerificationCode { get; set; }
    public DateOnly IssuedDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Status { get; set; } = "Issued";
    public string? Notes { get; set; }
}
