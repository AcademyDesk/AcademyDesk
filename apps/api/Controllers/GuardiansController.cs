using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/guardians")]
public sealed class GuardiansController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuardianSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var guardians = await dbContext.Guardians.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.IsActive)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new GuardianSummary(x.Id, x.FirstName, x.LastName, x.Email, x.Phone, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(guardians);
    }

    [HttpPost]
    public async Task<ActionResult<GuardianSummary>> Create(Guid academyId, CreateGuardianRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });

        var guardian = new Guardian { AcademyId = academyId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = request.Email?.Trim(), Phone = request.Phone?.Trim() };
        dbContext.Guardians.Add(guardian);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/guardians/{guardian.Id}", new GuardianSummary(guardian.Id, guardian.FirstName, guardian.LastName, guardian.Email, guardian.Phone, guardian.IsActive));
    }
}

public sealed record CreateGuardianRequest(string FirstName, string LastName, string? Email, string? Phone);
public sealed record GuardianSummary(Guid Id, string FirstName, string LastName, string? Email, string? Phone, bool IsActive);
