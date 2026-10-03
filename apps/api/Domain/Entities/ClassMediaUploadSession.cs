namespace AcademyDesk.Api.Domain.Entities;

public sealed class ClassMediaUploadSession : AcademyEntity
{
    public Guid OwnerUserId { get; set; }
    public Guid TeacherId { get; set; }
    public Guid ClientRequestId { get; set; }
    public Guid BatchId { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? ClassSessionId { get; set; }
    public required string FileName { get; set; }
    public long Length { get; set; }
    public int ChunkBytes { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; } = "Class material";
    public DateTime? CompletedAtUtc { get; set; }
}
