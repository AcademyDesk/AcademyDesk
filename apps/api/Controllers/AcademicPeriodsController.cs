using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/academic-periods")]
public sealed class AcademicPeriodsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AcademicPeriodsSummary>> List(Guid academyId, CancellationToken token)
    {
        var years = await db.AcademicYears.AsNoTracking().Where(x => x.AcademyId == academyId).OrderByDescending(x => x.StartDate).Select(x => new AcademicYearSummary(x.Id, x.Name, x.StartDate, x.EndDate, x.IsCurrent, x.IsClosed)).ToListAsync(token);
        var terms = await db.AcademicTerms.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.StartDate).Select(x => new AcademicTermSummary(x.Id, x.AcademicYearId, x.Name, x.StartDate, x.EndDate, x.IsClosed)).ToListAsync(token);
        return Ok(new AcademicPeriodsSummary(years, terms));
    }

    [HttpPost("years")]
    public async Task<ActionResult<AcademicYearSummary>> CreateYear(Guid academyId, AcademicYearRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.EndDate < request.StartDate) return BadRequest(new { message = "Enter a name and valid academic-year dates." });
        if (await db.AcademicYears.AnyAsync(x => x.AcademyId == academyId && x.Name == request.Name.Trim(), token)) return Conflict(new { message = "That academic year already exists." });
        if (request.IsCurrent) { var current = await db.AcademicYears.Where(x => x.AcademyId == academyId && x.IsCurrent).ToListAsync(token); current.ForEach(x => x.IsCurrent = false); }
        var year = new AcademicYear { AcademyId = academyId, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate, IsCurrent = request.IsCurrent }; db.AcademicYears.Add(year); await db.SaveChangesAsync(token); return Ok(new AcademicYearSummary(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent, year.IsClosed));
    }

    [HttpPost("terms")]
    [AtomicAcademyMutation]
    public async Task<ActionResult<AcademicTermSummary>> CreateTerm(Guid academyId, AcademicTermRequest request, CancellationToken token)
    {
        // Reuse the normal actor's save/audit boundary; platform bypass/direct
        // SQL callers still need a transaction for the coupled parent lock.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        var year = await YearForMutationAsync(academyId, request.AcademicYearId, token);
        if (year is null) return BadRequest(new { message = "Select an academic year in this academy." });
        if (year.IsClosed) return Conflict(new { message = "This academic year is closed. You cannot add a new term." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.EndDate < request.StartDate || request.StartDate < year.StartDate || request.EndDate > year.EndDate) return BadRequest(new { message = "Term dates must be within the academic year." });
        var term = new AcademicTerm { AcademyId = academyId, AcademicYearId = year.Id, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate }; db.AcademicTerms.Add(term); await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok(new AcademicTermSummary(term.Id, term.AcademicYearId, term.Name, term.StartDate, term.EndDate, term.IsClosed));
    }

    [HttpPatch("years/{yearId:guid}/close")]
    [AtomicAcademyMutation]
    public async Task<ActionResult> CloseYear(Guid academyId, Guid yearId, CancellationToken token)
    {
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        var year = await YearForMutationAsync(academyId, yearId, token);
        if (year is null) return NotFound();
        if (await db.AcademicTerms.AnyAsync(item => item.AcademicYearId == yearId && !item.IsClosed, token)) return Conflict(new { message = "Close all terms before closing the academic year." });
        year.IsClosed = true; year.IsCurrent = false; await db.SaveChangesAsync(token);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok();
    }

    [HttpPatch("terms/{termId:guid}/close")]
    public async Task<ActionResult> CloseTerm(Guid academyId, Guid termId, CancellationToken token)
    {
        var term = await db.AcademicTerms.SingleOrDefaultAsync(item => item.Id == termId && item.AcademyId == academyId, token);
        if (term is null) return NotFound();
        term.IsClosed = true; await db.SaveChangesAsync(token); return Ok();
    }

    private Task<AcademicYear?> YearForMutationAsync(Guid academyId, Guid yearId, CancellationToken token)
    {
        // Both child creation and parent closure lock the same parent until the
        // edge/state + audit commit. Parameterized SQL; no schema change.
        var query = db.Database.IsSqlServer()
            ? db.AcademicYears.FromSqlInterpolated($"SELECT * FROM [AcademicYears] WITH (UPDLOCK, HOLDLOCK) WHERE [AcademyId] = {academyId} AND [Id] = {yearId}")
            : db.AcademicYears.Where(x => x.AcademyId == academyId && x.Id == yearId);
        return query.SingleOrDefaultAsync(token);
    }
}

public sealed record AcademicYearRequest(string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
public sealed record AcademicTermRequest(Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record AcademicYearSummary(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, bool IsClosed);
public sealed record AcademicTermSummary(Guid Id, Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsClosed);
public sealed record AcademicPeriodsSummary(IReadOnlyList<AcademicYearSummary> Years, IReadOnlyList<AcademicTermSummary> Terms);
