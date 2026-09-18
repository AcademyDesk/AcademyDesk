using AcademyDesk.Api.Data;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/grading-schemes")]
public sealed class GradingSchemeLifecycleController(AcademyDeskDbContext db):ControllerBase{[HttpPatch("{schemeId:guid}/status")]public async Task<ActionResult> Status(Guid academyId,Guid schemeId,SchemeStatusRequest r,CancellationToken t){var scheme=await db.GradingSchemes.SingleOrDefaultAsync(x=>x.Id==schemeId&&x.AcademyId==academyId,t);if(scheme is null)return NotFound();scheme.IsActive=r.IsActive;await db.SaveChangesAsync(t);return Ok(new{scheme.Id,scheme.IsActive});}}
public sealed record SchemeStatusRequest(bool IsActive);
