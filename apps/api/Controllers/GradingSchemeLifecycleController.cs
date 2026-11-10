using AcademyDesk.Api.Data;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/grading-schemes")]
public sealed class GradingSchemeLifecycleController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult> Active(Guid academyId, CancellationToken t) =>
        Ok(await db.GradingSchemes.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive).OrderBy(x => x.Name).ToListAsync(t));

    [HttpPatch("{schemeId:guid}/status")]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult> Status(Guid academyId, Guid schemeId, SchemeStatusRequest r, CancellationToken t)
    {
        if (r.IsActive is null) return BadRequest(new { message = "Provide an explicit active or inactive state." });
        var scheme = await db.GradingSchemes.SingleOrDefaultAsync(x => x.Id == schemeId && x.AcademyId == academyId, t);
        if (scheme is null) return NotFound();
        scheme.IsActive = r.IsActive.Value;
        await db.SaveChangesAsync(t);
        return Ok(new { scheme.Id, scheme.IsActive });
    }
}

public sealed record SchemeStatusRequest(bool? IsActive);
