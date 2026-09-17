using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
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
    public async Task<ActionResult<AcademicTermSummary>> CreateTerm(Guid academyId, AcademicTermRequest request, CancellationToken token)
    {
        var year = await db.AcademicYears.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == request.AcademicYearId, token);
        if (year is null) return BadRequest(new { message = "Select an academic year in this academy." });
        if (string.IsNullOrWhiteSpace(request.Name) || request.EndDate < request.StartDate || request.StartDate < year.StartDate || request.EndDate > year.EndDate) return BadRequest(new { message = "Term dates must be within the academic year." });
        var term = new AcademicTerm { AcademyId = academyId, AcademicYearId = year.Id, Name = request.Name.Trim(), StartDate = request.StartDate, EndDate = request.EndDate }; db.AcademicTerms.Add(term); await db.SaveChangesAsync(token); return Ok(new AcademicTermSummary(term.Id, term.AcademicYearId, term.Name, term.StartDate, term.EndDate, term.IsClosed));
    }
}

public sealed record AcademicYearRequest(string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);
public sealed record AcademicTermRequest(Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate);
public sealed record AcademicYearSummary(Guid Id, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent, bool IsClosed);
public sealed record AcademicTermSummary(Guid Id, Guid AcademicYearId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsClosed);
public sealed record AcademicPeriodsSummary(IReadOnlyList<AcademicYearSummary> Years, IReadOnlyList<AcademicTermSummary> Terms);
