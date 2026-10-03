using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

// Test-only observation of real durable completion before the bridge drops its
// first response. No storage, authorization or API response is fabricated.
internal sealed class BrowserCompletionLoss
{
    public Guid UploadId { get; private set; }
    public int Responses { get; private set; }
    public async Task<bool> ObserveAsync(Guid id, AcademyDeskDbContext db, IMediaBlobStore store)
    {
        if (UploadId != Guid.Empty && UploadId != id) throw new InvalidOperationException("Completion retry changed upload identity.");
        UploadId = id;
        Responses++;
        await AssertStateAsync(db, store, Responses);
        return Responses == 1;
    }
    public async Task AssertStateAsync(AcademyDeskDbContext db, IMediaBlobStore store, int expectedResponses)
    {
        if (UploadId == Guid.Empty || Responses != expectedResponses) throw new InvalidOperationException("Expected completion responses not observed.");
        var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == UploadId);
        var sessions = await db.ClassMediaUploadSessions.AsNoTracking().CountAsync();
        var resources = await db.LearningResources.AsNoTracking().CountAsync();
        var matching = await db.LearningResources.AsNoTracking().CountAsync(x => x.Id == UploadId && x.Title == row.Title && x.Description == row.Description);
        var notifications = await db.Notifications.AsNoTracking().CountAsync(x => x.AcademyId == row.AcademyId &&
            x.RecipientType == "Student" && x.Title == "New class material" && x.Message == row.Title + " is available in your class history.");
        var progress = await store.ProgressAsync(new MediaUpload(row.AcademyId, row.Id, row.FileName, row.Length, row.ChunkBytes), CancellationToken.None);
        if (sessions != 1 || resources != 1 || matching != 1 || notifications != 1 || !row.CompletedAtUtc.HasValue ||
            !progress.Committed || progress.UploadedBlocks.Count != 1)
            throw new InvalidOperationException("Completion-loss one-chunk SQL/Blob/notification state mismatch.");
        Console.WriteLine($"BROWSER COMPLETION STATE PASS responses={Responses} upload={row.Id} sessions={sessions} resources={resources} notifications={notifications} blocks=1 committed=True");
    }
}
