using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/music-pieces")]
public sealed class MusicPiecesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MusicPieceSummary>>> List(Guid academyId, CancellationToken token) => Ok(await dbContext.MusicPieces.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive).OrderBy(x => x.Title).Select(x => new MusicPieceSummary(x.Id, x.Title, x.Composer, x.Instrument, x.Genre, x.Difficulty, x.DurationMinutes)).ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult<MusicPieceSummary>> Create(Guid academyId, CreateMusicPieceRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Piece title is required." });
        var piece = new MusicPiece { AcademyId = academyId, Title = request.Title.Trim(), Composer = request.Composer?.Trim(), Instrument = request.Instrument?.Trim(), Genre = request.Genre?.Trim(), Difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "Beginner" : request.Difficulty.Trim(), DurationMinutes = request.DurationMinutes };
        dbContext.MusicPieces.Add(piece); await dbContext.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/music-pieces/{piece.Id}", new MusicPieceSummary(piece.Id, piece.Title, piece.Composer, piece.Instrument, piece.Genre, piece.Difficulty, piece.DurationMinutes));
    }
    [HttpPatch("{pieceId:guid}/active")]
    public async Task<ActionResult> SetActive(Guid academyId, Guid pieceId, SetMusicPieceActiveRequest request, CancellationToken token)
    { var x=await dbContext.MusicPieces.SingleOrDefaultAsync(v=>v.Id==pieceId&&v.AcademyId==academyId,token); if(x is null)return NotFound(); x.IsActive=request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(); }
}
public sealed record CreateMusicPieceRequest(string Title, string? Composer, string? Instrument, string? Genre, string? Difficulty, int? DurationMinutes);
public sealed record MusicPieceSummary(Guid Id, string Title, string? Composer, string? Instrument, string? Genre, string Difficulty, int? DurationMinutes);
public sealed record SetMusicPieceActiveRequest(bool IsActive);
