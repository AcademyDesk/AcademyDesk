using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies")]
public sealed class AcademiesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AcademySummary>>> List(CancellationToken cancellationToken)
    {
        var academies = await dbContext.Academies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new AcademySummary(x.Id, x.Name, x.LegalName, x.CountryCode, x.TimeZone, x.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(academies);
    }

    [HttpPost]
    public async Task<ActionResult<AcademySummary>> Create(
        CreateAcademyRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Academy name is required." });
        }

        var academy = new Academy
        {
            Name = request.Name.Trim(),
            LegalName = string.IsNullOrWhiteSpace(request.LegalName) ? null : request.LegalName.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? "IN" : request.CountryCode.Trim().ToUpperInvariant(),
            TimeZone = string.IsNullOrWhiteSpace(request.TimeZone) ? "Asia/Kolkata" : request.TimeZone.Trim()
        };

        dbContext.Academies.Add(academy);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new AcademySummary(
            academy.Id,
            academy.Name,
            academy.LegalName,
            academy.CountryCode,
            academy.TimeZone,
            academy.IsActive);

        return CreatedAtAction(nameof(List), new { id = academy.Id }, response);
    }
}

public sealed record CreateAcademyRequest(
    string Name,
    string? LegalName,
    string? CountryCode,
    string? TimeZone);

public sealed record AcademySummary(
    Guid Id,
    string Name,
    string? LegalName,
    string CountryCode,
    string TimeZone,
    bool IsActive);
