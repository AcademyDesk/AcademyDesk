using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/teachers")]
public sealed class TeachersController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeacherSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var teachers = await dbContext.Teachers.AsNoTracking()
            .Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new TeacherSummary(x.Id, x.FirstName, x.LastName, x.Email, x.Phone, x.Specialties, x.BranchId, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(teachers);
    }

    [HttpPost]
    public async Task<ActionResult<TeacherSummary>> Create(Guid academyId, CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken))
            return BadRequest(new { message = "The selected branch does not belong to this academy." });

        var teacher = new Teacher { AcademyId = academyId, BranchId = request.BranchId, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = request.Email?.Trim(), Phone = request.Phone?.Trim(), Specialties = request.Specialties?.Trim(), Qualifications = request.Qualifications?.Trim(), EmploymentType = request.EmploymentType?.Trim(), JoiningDate = request.JoiningDate, AddressLine1 = request.AddressLine1?.Trim(), City = request.City?.Trim(), State = request.State?.Trim(), PostalCode = request.PostalCode?.Trim(), CertificationsJson = request.CertificationsJson, AvailabilityJson = request.AvailabilityJson, CompensationJson = request.CompensationJson };
        dbContext.Teachers.Add(teacher);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/teachers/{teacher.Id}", new TeacherSummary(teacher.Id, teacher.FirstName, teacher.LastName, teacher.Email, teacher.Phone, teacher.Specialties, teacher.BranchId, teacher.IsActive));
    }
    [HttpPut("{teacherId:guid}")]
    public async Task<ActionResult<TeacherSummary>> Update(Guid academyId,Guid teacherId,UpdateTeacherRequest request,CancellationToken token){var x=await dbContext.Teachers.SingleOrDefaultAsync(t=>t.Id==teacherId&&t.AcademyId==academyId,token);if(x is null)return NotFound();if(string.IsNullOrWhiteSpace(request.FirstName)||string.IsNullOrWhiteSpace(request.LastName))return BadRequest();x.FirstName=request.FirstName.Trim();x.LastName=request.LastName.Trim();x.Email=request.Email?.Trim();x.Phone=request.Phone?.Trim();x.Specialties=request.Specialties?.Trim();x.BranchId=request.BranchId;x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(new TeacherSummary(x.Id,x.FirstName,x.LastName,x.Email,x.Phone,x.Specialties,x.BranchId,x.IsActive));}
}

public sealed record CreateTeacherRequest(string FirstName, string LastName, string? Email, string? Phone, string? Specialties, Guid? BranchId, string? Qualifications = null, string? EmploymentType = null, DateOnly? JoiningDate = null, string? AddressLine1 = null, string? City = null, string? State = null, string? PostalCode = null, string? CertificationsJson = null, string? AvailabilityJson = null, string? CompensationJson = null);
public sealed record TeacherSummary(Guid Id, string FirstName, string LastName, string? Email, string? Phone, string? Specialties, Guid? BranchId, bool IsActive);
public sealed record UpdateTeacherRequest(string FirstName,string LastName,string? Email,string? Phone,string? Specialties,Guid? BranchId,bool IsActive);
