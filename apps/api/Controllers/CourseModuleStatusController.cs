using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/course-modules")]
public sealed class CourseModuleStatusController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpPatch("{moduleId:guid}/publish")]
    public async Task<ActionResult> Publish(Guid academyId, Guid moduleId, PublishModuleRequest request, CancellationToken token)
    {
        var module = await db.CourseModules.SingleOrDefaultAsync(x => x.Id == moduleId && x.AcademyId == academyId, token);
        if (module is null) return NotFound();
        module.IsPublished = request.IsPublished;
        await db.SaveChangesAsync(token);
        return Ok();
    }
}

public sealed record PublishModuleRequest(bool IsPublished);
