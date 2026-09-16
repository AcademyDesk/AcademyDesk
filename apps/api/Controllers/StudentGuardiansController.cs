using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/students/{studentId:guid}/guardians")]
public sealed class StudentGuardiansController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentGuardianSummary>>> List(Guid academyId, Guid studentId, CancellationToken cancellationToken)
    {
        var links = await dbContext.StudentGuardians.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.StudentId == studentId)
            .Include(x => x.Guardian)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Guardian!.LastName)
            .Select(x => new StudentGuardianSummary(x.GuardianId, x.Guardian!.FirstName, x.Guardian.LastName, x.Guardian.Email, x.Guardian.Phone, x.Relationship, x.IsPrimary))
            .ToListAsync(cancellationToken);
        return Ok(links);
    }

    [HttpPost]
    public async Task<ActionResult<StudentGuardianSummary>> Link(Guid academyId, Guid studentId, LinkGuardianRequest request, CancellationToken cancellationToken)
    {
        var validStudent = await dbContext.Students.AnyAsync(x => x.Id == studentId && x.AcademyId == academyId, cancellationToken);
        var validGuardian = await dbContext.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, cancellationToken);
        if (!validStudent || !validGuardian) return NotFound();
        if (await dbContext.StudentGuardians.AnyAsync(x => x.AcademyId == academyId && x.StudentId == studentId && x.GuardianId == request.GuardianId, cancellationToken))
            return Conflict(new { message = "This guardian is already linked to the student." });

        if (request.IsPrimary)
        {
            var current = await dbContext.StudentGuardians.Where(x => x.AcademyId == academyId && x.StudentId == studentId && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var existingLink in current) existingLink.IsPrimary = false;
        }

        var studentGuardian = new StudentGuardian { AcademyId = academyId, StudentId = studentId, GuardianId = request.GuardianId, Relationship = request.Relationship?.Trim(), IsPrimary = request.IsPrimary };
        dbContext.StudentGuardians.Add(studentGuardian);
        await dbContext.SaveChangesAsync(cancellationToken);
        var guardian = await dbContext.Guardians.AsNoTracking().SingleAsync(x => x.Id == request.GuardianId, cancellationToken);
        return Created($"/api/academies/{academyId}/students/{studentId}/guardians/{guardian.Id}", new StudentGuardianSummary(guardian.Id, guardian.FirstName, guardian.LastName, guardian.Email, guardian.Phone, studentGuardian.Relationship, studentGuardian.IsPrimary));
    }
}

public sealed record LinkGuardianRequest(Guid GuardianId, string? Relationship, bool IsPrimary);
public sealed record StudentGuardianSummary(Guid GuardianId, string FirstName, string LastName, string? Email, string? Phone, string? Relationship, bool IsPrimary);
