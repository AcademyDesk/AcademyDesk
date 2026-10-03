namespace AcademyDesk.Api.Infrastructure.Media;

public sealed record MediaBlobInfo(long Length, string FileName, string ETag);
public sealed record MediaUploadProgress(bool Committed, IReadOnlyList<int> UploadedBlocks);
public sealed record MediaBlobRead(Stream Content, long Length, string ETag) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public interface IMediaBlobStore
{
    bool IsConfigured { get; }
    Task StageAsync(MediaUpload upload, int blockIndex, Stream content, CancellationToken token);
    Task<MediaUploadProgress> ProgressAsync(MediaUpload upload, CancellationToken token);
    Task<MediaBlobInfo> CommitAsync(MediaUpload upload, CancellationToken token);
    Task<MediaBlobInfo> PropertiesAsync(Guid academyId, Guid resourceId, CancellationToken token);
    Task<MediaBlobRead> ReadAsync(Guid academyId, Guid resourceId, long offset, long? length, CancellationToken token);
}
