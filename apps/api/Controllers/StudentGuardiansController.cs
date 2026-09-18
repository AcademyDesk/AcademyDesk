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
            .Select(x => new StudentGuardianSummary(x.GuardianId, x.Guardian!.FirstName, x.Guardian.LastName, x.Guardian.Email, x.Guardian.Phone, x.Relationship, x.IsPrimary, x.CanAccessPortal, x.CanViewAcademicProgress, x.CanViewFinance, x.CanViewDocuments, x.CanManageLeave, x.AccessGrantedAtUtc, x.AccessRevokedAtUtc))
            .ToListAsync(cancellationToken);
        return Ok(links);
    }

    [HttpPost]
    public async Task<ActionResult<StudentGuardianSummary>> Link(Guid academyId, Guid studentId, LinkGuardianRequest request, CancellationToken cancellationToken)
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == academyId, cancellationToken);
        var validGuardian = await dbContext.Guardians.AnyAsync(x => x.Id == request.GuardianId && x.AcademyId == academyId, cancellationToken);
        if (student is null || !validGuardian) return NotFound();
        if (await dbContext.StudentGuardians.AnyAsync(x => x.AcademyId == academyId && x.StudentId == studentId && x.GuardianId == request.GuardianId, cancellationToken))
            return Conflict(new { message = "This guardian is already linked to the student." });

        if (request.IsPrimary)
        {
            var current = await dbContext.StudentGuardians.Where(x => x.AcademyId == academyId && x.StudentId == studentId && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var existingLink in current) existingLink.IsPrimary = false;
        }

        var isMinor = student.DateOfBirth.HasValue && student.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
        var portalAccess = isMinor || request.AllowParentPortalAccess;
        var studentGuardian = new StudentGuardian { AcademyId = academyId, StudentId = studentId, GuardianId = request.GuardianId, Relationship = request.Relationship?.Trim(), IsPrimary = request.IsPrimary, CanAccessPortal = portalAccess, CanViewAcademicProgress = portalAccess && request.AllowAcademicProgress, CanViewFinance = portalAccess && request.AllowFinance, CanViewDocuments = portalAccess && request.AllowDocuments, CanManageLeave = portalAccess && request.AllowLeave, AccessGrantedAtUtc = portalAccess ? DateTime.UtcNow : null };
        dbContext.StudentGuardians.Add(studentGuardian);
        await dbContext.SaveChangesAsync(cancellationToken);
        var guardian = await dbContext.Guardians.AsNoTracking().SingleAsync(x => x.Id == request.GuardianId, cancellationToken);
        return Created($"/api/academies/{academyId}/students/{studentId}/guardians/{guardian.Id}", Summary(guardian, studentGuardian));
    }
    [HttpPatch("{guardianId:guid}/portal-access")]
    public async Task<ActionResult<StudentGuardianSummary>> SetPortalAccess(Guid academyId, Guid studentId, Guid guardianId, ParentPortalAccessRequest request, CancellationToken token)
    {
        var link = await dbContext.StudentGuardians.Include(x => x.Guardian).Include(x => x.Student).SingleOrDefaultAsync(x => x.AcademyId == academyId && x.StudentId == studentId && x.GuardianId == guardianId, token);
        if (link?.Guardian is null || link.Student is null) return NotFound();
        var isMinor = link.Student.DateOfBirth.HasValue && link.Student.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
        var access = isMinor || request.AllowPortalAccess;
        link.CanAccessPortal = access;
        link.CanViewAcademicProgress = access && request.AllowAcademicProgress;
        link.CanViewFinance = access && request.AllowFinance;
        link.CanViewDocuments = access && request.AllowDocuments;
        link.CanManageLeave = access && request.AllowLeave;
        link.AccessGrantedAtUtc = access ? DateTime.UtcNow : null;
        link.AccessRevokedAtUtc = access ? null : DateTime.UtcNow;
        await dbContext.SaveChangesAsync(token);
        return Ok(Summary(link.Guardian, link));
    }
    private static StudentGuardianSummary Summary(Guardian guardian, StudentGuardian link) => new(guardian.Id, guardian.FirstName, guardian.LastName, guardian.Email, guardian.Phone, link.Relationship, link.IsPrimary, link.CanAccessPortal, link.CanViewAcademicProgress, link.CanViewFinance, link.CanViewDocuments, link.CanManageLeave, link.AccessGrantedAtUtc, link.AccessRevokedAtUtc);

    [HttpDelete("{guardianId:guid}")]
    public async Task<ActionResult> Unlink(Guid academyId, Guid studentId, Guid guardianId, CancellationToken token)
    { var link=await dbContext.StudentGuardians.SingleOrDefaultAsync(x=>x.AcademyId==academyId&&x.StudentId==studentId&&x.GuardianId==guardianId,token); if(link is null)return NotFound(); dbContext.StudentGuardians.Remove(link); await dbContext.SaveChangesAsync(token); return NoContent(); }
}

public sealed record LinkGuardianRequest(Guid GuardianId, string? Relationship, bool IsPrimary, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true);
public sealed record ParentPortalAccessRequest(bool AllowPortalAccess, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true);
public sealed record StudentGuardianSummary(Guid GuardianId, string FirstName, string LastName, string? Email, string? Phone, string? Relationship, bool IsPrimary, bool CanAccessPortal, bool CanViewAcademicProgress, bool CanViewFinance, bool CanViewDocuments, bool CanManageLeave, DateTime? AccessGrantedAtUtc, DateTime? AccessRevokedAtUtc);
