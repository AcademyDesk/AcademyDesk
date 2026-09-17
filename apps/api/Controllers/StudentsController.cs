using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/students")]
public sealed class StudentsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var students = await dbContext.Students.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.IsActive)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new StudentSummary(x.Id, x.FirstName, x.LastName, x.Email, x.Phone, x.BranchId, x.IsActive))
            .ToListAsync(cancellationToken);
        return Ok(students);
    }

    [HttpPost]
    public async Task<ActionResult<StudentSummary>> Create(Guid academyId, CreateStudentRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });
        if (request.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.AcademyId == academyId, cancellationToken))
            return BadRequest(new { message = "The selected branch does not belong to this academy." });

        var student = new Student
        {
            AcademyId = academyId,
            BranchId = request.BranchId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim()
        };
        dbContext.Students.Add(student);
        await dbContext.SaveChangesAsync(cancellationToken);
        var response = new StudentSummary(student.Id, student.FirstName, student.LastName, student.Email, student.Phone, student.BranchId, student.IsActive);
        return Created($"/api/academies/{academyId}/students/{student.Id}", response);
    }
    [HttpPut("{studentId:guid}")]
    public async Task<ActionResult<StudentSummary>> Update(Guid academyId, Guid studentId, UpdateStudentRequest request, CancellationToken token)
    { var x=await dbContext.Students.SingleOrDefaultAsync(s=>s.Id==studentId&&s.AcademyId==academyId,token); if(x is null)return NotFound(); if(string.IsNullOrWhiteSpace(request.FirstName)||string.IsNullOrWhiteSpace(request.LastName))return BadRequest(new{message="First and last name are required."}); x.FirstName=request.FirstName.Trim();x.LastName=request.LastName.Trim();x.Email=request.Email?.Trim();x.Phone=request.Phone?.Trim();x.BranchId=request.BranchId;x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(new StudentSummary(x.Id,x.FirstName,x.LastName,x.Email,x.Phone,x.BranchId,x.IsActive)); }
}

public sealed record CreateStudentRequest(string FirstName, string LastName, DateOnly? DateOfBirth, string? Email, string? Phone, Guid? BranchId);
public sealed record StudentSummary(Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? BranchId, bool IsActive);
public sealed record UpdateStudentRequest(string FirstName,string LastName,string? Email,string? Phone,Guid? BranchId,bool IsActive);
