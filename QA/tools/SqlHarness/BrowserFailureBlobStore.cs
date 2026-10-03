using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using Azure;
using Microsoft.EntityFrameworkCore;

// Test-only, guarded emulator decorator. The real controller catches this provider
// exception and generates its own HTTP 503; no bridge response is fabricated.
internal sealed class BrowserFailureBlobStore(IMediaBlobStore inner) : IMediaBlobStore
{
    private int failures;
    public Guid FailedUploadId { get; private set; }
    public bool IsConfigured => inner.IsConfigured;
    public Task StageAsync(MediaUpload upload, int blockIndex, Stream content, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (blockIndex == 0 && Interlocked.CompareExchange(ref failures, 1, 0) == 0)
        {
            FailedUploadId = upload.ResourceId;
            Console.WriteLine($"BROWSER FAULT provider stage rejected once before Blob write; upload={FailedUploadId}");
            throw new RequestFailedException(503, "Synthetic isolated provider stage failure.");
        }
        return inner.StageAsync(upload, blockIndex, content, token);
    }
    public Task<MediaUploadProgress> ProgressAsync(MediaUpload upload, CancellationToken token) => inner.ProgressAsync(upload, token);
    public Task<MediaBlobInfo> CommitAsync(MediaUpload upload, CancellationToken token) => inner.CommitAsync(upload, token);
    public Task<MediaBlobInfo> PropertiesAsync(Guid academyId, Guid resourceId, CancellationToken token) => inner.PropertiesAsync(academyId, resourceId, token);
    public Task<MediaBlobRead> ReadAsync(Guid academyId, Guid resourceId, long offset, long? length, CancellationToken token) => inner.ReadAsync(academyId, resourceId, offset, length, token);

    public async Task AssertStateAsync(AcademyDeskDbContext db, bool completed)
    {
        if (failures != 1 || FailedUploadId == Guid.Empty) throw new InvalidOperationException("Expected one provider failure not observed.");
        var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == FailedUploadId);
        var sessions = await db.ClassMediaUploadSessions.AsNoTracking().CountAsync();
        var resources = await db.LearningResources.AsNoTracking().CountAsync();
        var matchingResources = await db.LearningResources.AsNoTracking().CountAsync(x => x.Id == FailedUploadId);
        var progress = await inner.ProgressAsync(new MediaUpload(row.AcademyId, row.Id, row.FileName, row.Length, row.ChunkBytes), CancellationToken.None);
        if (sessions != 1 || row.CompletedAtUtc.HasValue != completed || resources != (completed ? 1 : 0) || matchingResources != resources ||
            progress.Committed != completed || progress.UploadedBlocks.Count != (completed ? 1 : 0))
            throw new InvalidOperationException("Failure/retry SQL or Blob state did not match bounded one-chunk case.");
        Console.WriteLine($"BROWSER FAILURE STATE PASS completed={completed} upload={row.Id} sessions={sessions} resources={resources} blocks={progress.UploadedBlocks.Count}");
    }
}
