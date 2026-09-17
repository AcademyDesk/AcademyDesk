using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/music-progress")]
public sealed class MusicProgressController(AcademyDeskDbContext dbContext) : ControllerBase
{
    private static readonly string[] Statuses = ["Assigned", "Learning", "ReadyForReview", "Mastered", "Paused"];
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MusicProgressSummary>>> List(Guid academyId, Guid? studentId, CancellationToken token)
    {
        var query = dbContext.StudentMusicProgress.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (studentId.HasValue) query = query.Where(x => x.StudentId == studentId);
        return Ok(await query.OrderByDescending(x => x.UpdatedAtUtc).Select(x => new MusicProgressSummary(x.Id, x.StudentId, x.MusicPieceId, x.Status, x.TargetDate, x.Score, x.Notes)).ToListAsync(token));
    }
    [HttpPost]
    public async Task<ActionResult<MusicProgressSummary>> Assign(Guid academyId, AssignMusicPieceRequest request, CancellationToken token)
    {
        if (!await dbContext.Students.AnyAsync(x => x.Id == request.StudentId && x.AcademyId == academyId, token) || !await dbContext.MusicPieces.AnyAsync(x => x.Id == request.MusicPieceId && x.AcademyId == academyId, token)) return BadRequest(new { message = "Student or music piece is invalid." });
        if (await dbContext.StudentMusicProgress.AnyAsync(x => x.AcademyId == academyId && x.StudentId == request.StudentId && x.MusicPieceId == request.MusicPieceId, token)) return Conflict(new { message = "This piece is already assigned to the student." });
        var progress = new StudentMusicProgress { AcademyId = academyId, StudentId = request.StudentId, MusicPieceId = request.MusicPieceId, TargetDate = request.TargetDate, Notes = request.Notes?.Trim() };
        dbContext.StudentMusicProgress.Add(progress); await dbContext.SaveChangesAsync(token); return Ok(new MusicProgressSummary(progress.Id, progress.StudentId, progress.MusicPieceId, progress.Status, progress.TargetDate, progress.Score, progress.Notes));
    }
    [HttpPatch("{progressId:guid}")]
    public async Task<ActionResult<MusicProgressSummary>> Update(Guid academyId, Guid progressId, UpdateMusicProgressRequest request, CancellationToken token)
    {
        if (!Statuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid progress status." });
        var progress = await dbContext.StudentMusicProgress.SingleOrDefaultAsync(x => x.Id == progressId && x.AcademyId == academyId, token); if (progress is null) return NotFound();
        progress.Status = Statuses.Single(x => x.Equals(request.Status, StringComparison.OrdinalIgnoreCase)); progress.Score = request.Score; progress.Notes = request.Notes?.Trim(); progress.TargetDate = request.TargetDate; await dbContext.SaveChangesAsync(token); return Ok(new MusicProgressSummary(progress.Id, progress.StudentId, progress.MusicPieceId, progress.Status, progress.TargetDate, progress.Score, progress.Notes));
    }
}
public sealed record AssignMusicPieceRequest(Guid StudentId, Guid MusicPieceId, DateOnly? TargetDate, string? Notes);
public sealed record UpdateMusicProgressRequest(string Status, DateOnly? TargetDate, decimal? Score, string? Notes);
public sealed record MusicProgressSummary(Guid Id, Guid StudentId, Guid MusicPieceId, string Status, DateOnly? TargetDate, decimal? Score, string? Notes);
