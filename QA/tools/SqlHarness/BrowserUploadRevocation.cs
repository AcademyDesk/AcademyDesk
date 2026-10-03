using System.Net;
using System.Net.Http.Headers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

// QA-only fixture mutation inside the already guarded run-owned database.
// Real request credentials/middleware/policy are used; never logged or bypassed.
internal sealed class BrowserUploadRevocation
{
    public Guid UploadId { get; private set; }
    public int DeniedCreates { get; private set; }
    private bool probesPassed;
    public async Task RevokeAndProbeAsync(Guid id, AcademyDeskDbContext db, HttpClient client,
        AuthenticationHeaderValue? authorization, IMediaBlobStore store)
    {
        if (UploadId != Guid.Empty) return;
        if (authorization is null) throw new InvalidOperationException("Authenticated fixture request required.");
        var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == id);
        var manifest = new MediaUpload(row.AcademyId, row.Id, row.FileName, row.Length, row.ChunkBytes);
        if (manifest.BlockCount != 3) throw new InvalidOperationException("Revocation case requires the bounded three-chunk fixture.");
        var progress = await store.ProgressAsync(manifest, CancellationToken.None);
        if (progress.Committed || !progress.UploadedBlocks.SequenceEqual(new[] { 0 })) throw new InvalidOperationException("Expected first durable block only.");
        var changed = await db.Batches.Where(x => x.Id == row.BatchId && x.AcademyId == row.AcademyId && x.TeacherId == row.TeacherId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TeacherId, (Guid?)null));
        if (changed != 1) throw new InvalidOperationException("Exact synthetic batch assignment not revoked.");
        UploadId = id;
        Console.WriteLine($"BROWSER REVOKED synthetic batch assignment after durable chunk 0; upload={id}");
        foreach (var item in new[] { (Method: HttpMethod.Get, Path: $"/api/teacher/media-uploads/{id}"),
            (Method: HttpMethod.Put, Path: $"/api/teacher/media-uploads/{id}/chunks/1"),
            (Method: HttpMethod.Post, Path: $"/api/teacher/media-uploads/{id}/complete") })
        {
            using var request = new HttpRequestMessage(item.Method, item.Path);
            request.Headers.Authorization = authorization;
            if (item.Method == HttpMethod.Put)
            {
                var bytes = new byte[checked((int)manifest.BlockLength(1))];
                Array.Fill(bytes, (byte)0x51); // Matches the inert synthetic-resume.bin chunk.
                request.Content = new ByteArrayContent(bytes);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            }
            using var response = await client.SendAsync(request);
            Console.WriteLine($"BROWSER REVOCATION PROBE {item.Method} {item.Path} {(int)response.StatusCode}");
            if (response.StatusCode != HttpStatusCode.NotFound) throw new InvalidOperationException("Revoked upload request was not denied by real policy.");
        }
        probesPassed = true;
        await AssertStateAsync(db, store, requireBrowserRetry: false);
    }
    public async Task ObserveDeniedCreateAsync(AcademyDeskDbContext db, IMediaBlobStore store)
    {
        if (UploadId == Guid.Empty) return;
        DeniedCreates++;
        await AssertStateAsync(db, store, requireBrowserRetry: true);
    }
    public async Task AssertStateAsync(AcademyDeskDbContext db, IMediaBlobStore store, bool requireBrowserRetry)
    {
        if (UploadId == Guid.Empty || !probesPassed || (requireBrowserRetry && DeniedCreates < 1))
            throw new InvalidOperationException("Revocation/browser retry not observed.");
        var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleAsync(x => x.Id == UploadId);
        var sessions = await db.ClassMediaUploadSessions.AsNoTracking().CountAsync();
        var resources = await db.LearningResources.AsNoTracking().CountAsync();
        var notices = await db.Notifications.AsNoTracking().CountAsync(x => x.Title == "New class material");
        var assigned = await db.Batches.AsNoTracking().AnyAsync(x => x.Id == row.BatchId && x.TeacherId == row.TeacherId);
        var progress = await store.ProgressAsync(new MediaUpload(row.AcademyId, row.Id, row.FileName, row.Length, row.ChunkBytes), CancellationToken.None);
        if (assigned || sessions != 1 || resources != 0 || notices != 0 || row.CompletedAtUtc.HasValue ||
            progress.Committed || !progress.UploadedBlocks.SequenceEqual(new[] { 0 }))
            throw new InvalidOperationException("Revoked retry changed session/resources/notifications/private blocks.");
        Console.WriteLine($"BROWSER REVOCATION STATE PASS upload={row.Id} sessions=1 resources=0 notifications=0 blocks=0-only committed=False browserDeniedCreates={DeniedCreates}");
    }
}
