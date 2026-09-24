using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/students")]
public sealed class StudentsController(AcademyDeskDbContext dbContext, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<StudentOverviewSummary>> Overview(Guid academyId, CancellationToken cancellationToken)
    {
        var students = dbContext.Students.AsNoTracking().Where(x => x.AcademyId == academyId);
        var invoices = dbContext.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId);
        var subjectFees = dbContext.StudentFeeArrangements.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(new StudentOverviewSummary(
            await students.CountAsync(cancellationToken),
            await students.CountAsync(x => x.IsActive, cancellationToken),
            await students.CountAsync(x => !x.IsActive, cancellationToken),
            await students.SumAsync(x => x.AdmissionFeeAmount ?? 0m, cancellationToken),
            await subjectFees.SumAsync(x => x.Amount, cancellationToken),
            await invoices.Where(x => x.Status != "Paid").SumAsync(x => x.TotalAmount, cancellationToken),
            await invoices.Where(x => x.Status != "Paid" && x.DueDate < today).SumAsync(x => x.TotalAmount, cancellationToken),
            await subjectFees.CountAsync(cancellationToken)));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StudentSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        var students = await dbContext.Students.AsNoTracking()
            .Where(x => x.AcademyId == academyId)
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
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == academyId, token);
        if (student is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First and last name are required." });

        student.FirstName = request.FirstName.Trim();
        student.LastName = request.LastName.Trim();
        student.Email = request.Email?.Trim();
        student.Phone = request.Phone?.Trim();
        student.BranchId = request.BranchId;
        student.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(token);

        var accounts = await userManager.Users
            .Where(user => user.AcademyId == academyId && user.StudentId == studentId)
            .ToListAsync(token);
        foreach (var account in accounts)
        {
            account.IsActive = student.IsActive;
            account.DisplayName = $"{student.FirstName} {student.LastName}";
            if (!string.IsNullOrWhiteSpace(student.Email)) account.Email = student.Email;
            account.PhoneNumber = student.Phone;
            var update = await userManager.UpdateAsync(account);
            if (!update.Succeeded)
                return BadRequest(new { message = "Student record saved, but the linked portal account could not be updated." });
        }

        return Ok(new StudentSummary(student.Id, student.FirstName, student.LastName, student.Email, student.Phone, student.BranchId, student.IsActive));
    }
}

public sealed record CreateStudentRequest(string FirstName, string LastName, DateOnly? DateOfBirth, string? Email, string? Phone, Guid? BranchId);
public sealed record StudentSummary(Guid Id, string FirstName, string LastName, string? Email, string? Phone, Guid? BranchId, bool IsActive);
public sealed record UpdateStudentRequest(string FirstName,string LastName,string? Email,string? Phone,Guid? BranchId,bool IsActive);
public sealed record StudentOverviewSummary(int TotalStudents, int ActiveStudents, int InactiveStudents, decimal TotalAdmissionFees, decimal TotalSubjectFees, decimal OutstandingFees, decimal OverdueFees, int ActiveSubjectFeeArrangements);
