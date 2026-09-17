using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portal")]
public sealed class PortalController(UserManager<ApplicationUser> users, AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        if (user.StudentId.HasValue)
        {
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.StudentId && x.AcademyId == user.AcademyId, token);
            if (student is null) return Forbid();
            var enrollmentCount = await db.Enrollments.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id && x.Status == "Active", token);
            var invoiceCount = await db.Invoices.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id, token);
            return Ok(new { role = "Student", displayName = $"{student.FirstName} {student.LastName}", enrollmentCount, invoiceCount });
        }
        if (user.GuardianId.HasValue)
        {
            var children = await db.StudentGuardians.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId).Join(db.Students.AsNoTracking(), x => x.StudentId, s => s.Id, (x, s) => new { s.Id, name = s.FirstName + " " + s.LastName }).ToListAsync(token);
            return Ok(new { role = "Guardian", displayName = user.DisplayName, children });
        }
        return Forbid();
    }
}
