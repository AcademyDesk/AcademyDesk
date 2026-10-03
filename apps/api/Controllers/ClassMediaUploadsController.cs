using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize, Route("api/teacher/media-uploads")]
public sealed class ClassMediaUploadsController(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    ClassMediaUploadPolicy policy, IMediaBlobStore store, MediaStorageOptions options, ILogger<ClassMediaUploadsController> logger) : ControllerBase
{
    [HttpPost, RequestSizeLimit(16384)]
    public async Task<IActionResult> Create(CreateClassMediaUploadRequest request, CancellationToken token)
    {
        var user = await CurrentTeacher();
        if (user is null) return Forbid();
        if (!store.IsConfigured) return Unavailable();
        if (request.ClientRequestId == Guid.Empty || !await policy.CanUploadAsync(user, request.BatchId, request.StudentId, request.ClassSessionId, token))
            return BadRequest(new { message = "Select an active assigned batch, student and class session where applicable." });
        MediaUpload manifest;
        try { manifest = MediaUpload.Create(user.AcademyId!.Value, Guid.NewGuid(), request.FileName ?? "", request.Length, options); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        var row = new ClassMediaUploadSession
        {
            Id = manifest.ResourceId, AcademyId = manifest.AcademyId, OwnerUserId = user.Id, TeacherId = user.TeacherId!.Value,
            ClientRequestId = request.ClientRequestId, BatchId = request.BatchId, StudentId = request.StudentId,
            ClassSessionId = request.ClassSessionId, FileName = manifest.FileName, Length = manifest.Length, ChunkBytes = manifest.ChunkBytes,
            Title = string.IsNullOrWhiteSpace(request.Title) ? manifest.FileName : request.Title.Trim(),
            Description = request.Description?.Trim(), Type = string.IsNullOrWhiteSpace(request.Type) ? "Class material" : request.Type.Trim()
        };
        if (row.Title.Length > 250 || row.Description?.Length > 2000 || row.Type.Length > 40)
            return BadRequest(new { message = "Title, description or type exceeds the supported length." });
        var existing = await FindByRequest(user, request.ClientRequestId, token);
        if (existing is not null) return ClassMediaUploadPolicy.Matches(existing, row) ? Ok(Summary(existing)) : Conflict(new { message = "This upload request ID belongs to different file details." });
        try { _ = await store.ProgressAsync(manifest, token); }
        catch (RequestFailedException) { return Unavailable(); }
        catch (InvalidOperationException) { return Unavailable(); }
        db.ClassMediaUploadSessions.Add(row);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.ChangeTracker.Clear();
            existing = await FindByRequest(user, request.ClientRequestId, token);
            return existing is not null && ClassMediaUploadPolicy.Matches(existing, row) ? Ok(Summary(existing)) : Conflict(new { message = "The upload request conflicts with existing details." });
        }
        return Created($"/api/teacher/media-uploads/{row.Id}", Summary(row));
    }

    [HttpGet("{uploadId:guid}")]
    public async Task<IActionResult> Progress(Guid uploadId, CancellationToken token)
    {
        var row = await AuthorizedSession(uploadId, token);
        if (row is null) return NotFound();
        if (!store.IsConfigured) return Unavailable();
        try
        {
            var progress = await store.ProgressAsync(Manifest(row), token);
            return Ok(new { upload = Summary(row), progress.UploadedBlocks, blobCommitted = progress.Committed });
        }
        catch (RequestFailedException) { return Unavailable(); }
        catch (InvalidOperationException) { return Conflict(new { message = "Stored media state is unavailable or inconsistent. Retry without starting a new upload." }); }
    }

    [HttpPut("{uploadId:guid}/chunks/{blockIndex:int}"), Consumes("application/octet-stream")]
    [RequestSizeLimit(MediaStorageOptions.DefaultChunkBytes)]
    public async Task<IActionResult> Chunk(Guid uploadId, int blockIndex, CancellationToken token)
    {
        var row = await AuthorizedSession(uploadId, token);
        if (row is null) return NotFound();
        if (!store.IsConfigured) return Unavailable();
        if (row.CompletedAtUtc.HasValue) return Conflict(new { message = "This upload is already complete." });
        var manifest = Manifest(row);
        long expected;
        try { expected = manifest.BlockLength(blockIndex); }
        catch (ArgumentOutOfRangeException) { return BadRequest(new { message = "The chunk index is invalid." }); }
        if (Request.ContentLength != expected) return BadRequest(new { message = "The chunk length does not match the upload manifest." });
        // Only one bounded chunk is buffered. The whole media file is never loaded.
        using var content = new MemoryStream(checked((int)expected));
        var buffer = new byte[65536];
        while (content.Length < expected)
        {
            var count = await Request.Body.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, expected - content.Length)), token);
            if (count == 0) return BadRequest(new { message = "The chunk was interrupted. Retry this chunk." });
            await content.WriteAsync(buffer.AsMemory(0, count), token);
        }
        content.Position = 0;
        try { await store.StageAsync(manifest, blockIndex, content, token); }
        catch (RequestFailedException) { return Unavailable(); }
        catch (InvalidOperationException) { return Conflict(new { message = "The upload cannot accept this chunk. Check its progress before retrying." }); }
        return Ok(new { message = "Chunk saved.", blockIndex });
    }

    [HttpPost("{uploadId:guid}/complete")]
    public async Task<IActionResult> Complete(Guid uploadId, CancellationToken token)
    {
        var row = await AuthorizedSession(uploadId, token);
        if (row is null) return NotFound();
        if (!store.IsConfigured) return Unavailable();
        try { _ = await store.CommitAsync(Manifest(row), token); }
        catch (RequestFailedException error)
        {
            // No endpoint, key, user data or filename is logged.
            logger.LogWarning("Private media commit failed: {Status}/{Code}", error.Status, error.ErrorCode);
            return Unavailable();
        }
        catch (InvalidOperationException) { return Conflict(new { message = "Upload all chunks and retry completion. Existing upload progress is retained." }); }

        // Blob and SQL cannot share a transaction. A committed Blob with failed SQL
        // completion stays bound to this session and is safely resumed on retry.
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var now = DateTime.UtcNow;
        var won = await db.ClassMediaUploadSessions.Where(x => x.Id == row.Id && x.CompletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CompletedAtUtc, now), token);
        if (won == 1)
        {
            db.LearningResources.Add(new LearningResource
            {
                Id = row.Id, AcademyId = row.AcademyId, BatchId = row.BatchId, StudentId = row.StudentId,
                ClassSessionId = row.ClassSessionId, Title = row.Title, Description = row.Description, Type = row.Type,
                Url = $"/api/class-media/{row.Id}/content", IsPublished = true
            });
            var recipients = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == row.AcademyId && x.BatchId == row.BatchId &&
                x.Status == "Active" && (!row.StudentId.HasValue || x.StudentId == row.StudentId)).Select(x => x.StudentId).Distinct().ToArrayAsync(token);
            foreach (var recipient in recipients) db.Notifications.Add(new Notification
            {
                AcademyId = row.AcademyId, RecipientId = recipient, RecipientType = "Student", Title = "New class material",
                Message = $"{row.Title} is available in your class history.", Channel = "InApp", Status = "Queued"
            });
            await db.SaveChangesAsync(token);
        }
        var resource = await db.LearningResources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == row.Id && x.AcademyId == row.AcademyId, token);
        if (resource is null) { await transaction.RollbackAsync(token); return Conflict(new { message = "Upload completion is inconsistent. Retry or contact support." }); }
        await transaction.CommitAsync(token);
        return Ok(new { message = "Class material uploaded successfully.", resource.Id, resource.Url });
    }

    private async Task<ApplicationUser?> CurrentTeacher()
    {
        var user = await users.GetUserAsync(User);
        return user is not null && user.IsActive && user.AcademyId.HasValue && user.TeacherId.HasValue &&
            await users.IsInRoleAsync(user, "Teacher") ? user : null;
    }
    private async Task<ClassMediaUploadSession?> AuthorizedSession(Guid id, CancellationToken token)
    {
        var user = await CurrentTeacher();
        if (user is null) return null;
        var row = await db.ClassMediaUploadSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id &&
            x.AcademyId == user.AcademyId && x.OwnerUserId == user.Id && x.TeacherId == user.TeacherId, token);
        return row is not null && await policy.CanUploadAsync(user, row.BatchId, row.StudentId, row.ClassSessionId, token) ? row : null;
    }
    private Task<ClassMediaUploadSession?> FindByRequest(ApplicationUser user, Guid id, CancellationToken token) =>
        db.ClassMediaUploadSessions.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == user.AcademyId && x.OwnerUserId == user.Id && x.ClientRequestId == id, token);
    private static MediaUpload Manifest(ClassMediaUploadSession row) => new(row.AcademyId, row.Id, row.FileName, row.Length, row.ChunkBytes);
    private static object Summary(ClassMediaUploadSession row) => new
    {
        row.Id, row.FileName, row.Length, row.ChunkBytes, blockCount = (row.Length + row.ChunkBytes - 1) / row.ChunkBytes,
        row.BatchId, row.StudentId, row.ClassSessionId, completed = row.CompletedAtUtc.HasValue
    };
    private ObjectResult Unavailable()
    {
        Response.Headers.RetryAfter = "30";
        return StatusCode(503, new { message = "Private media storage is unavailable. Keep your file and retry later." });
    }
}

public sealed record CreateClassMediaUploadRequest(Guid ClientRequestId, Guid BatchId, Guid? StudentId, Guid? ClassSessionId,
    string? FileName, long Length, string? Title, string? Description, string? Type);
