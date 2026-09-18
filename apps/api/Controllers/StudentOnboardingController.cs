using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/student-onboarding")]
public sealed class StudentOnboardingController(AcademyDeskDbContext db, UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, StudentOnboardingRequest request, CancellationToken token)
    {
        var actor = await users.GetUserAsync(User);
        if (actor?.AcademyId != academyId || (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return Forbid();
        if (string.IsNullOrWhiteSpace(request.StudentFirstName) || string.IsNullOrWhiteSpace(request.StudentLastName) || request.DateOfBirth is null) return BadRequest(new { message = "Student name and date of birth are required." });
        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(new { message = "Date of birth cannot be in the future." });
        var isMinor = request.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
        if (isMinor && (string.IsNullOrWhiteSpace(request.ParentFirstName) || string.IsNullOrWhiteSpace(request.ParentLastName) || string.IsNullOrWhiteSpace(request.ParentEmail))) return BadRequest(new { message = "A parent name and email are required for a minor student." });
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (request.AdmissionFeeAmount is < 0) return BadRequest(new { message = "Admission fee cannot be negative." });
        var validFrequencies = new[] { "Monthly", "Quarterly", "HalfYearly", "Annual" };
        var feeArrangements = request.FeeArrangements?.Where(x => !string.IsNullOrWhiteSpace(x.SubjectName) || x.Amount > 0).ToList() ?? [];
        if (feeArrangements.Any(x => string.IsNullOrWhiteSpace(x.SubjectName) || x.Amount <= 0 || !validFrequencies.Contains(x.Frequency))) return BadRequest(new { message = "Each subject fee needs a subject, positive amount, and billing frequency." });
        var student = new Student { AcademyId = academyId, FirstName = request.StudentFirstName.Trim(), LastName = request.StudentLastName.Trim(), DateOfBirth = request.DateOfBirth, Email = isMinor ? null : request.StudentEmail?.Trim(), Phone = isMinor ? null : request.StudentPhone?.Trim(), AddressLine1 = request.StudentAddressLine1?.Trim(), City = request.StudentCity?.Trim(), BranchId = request.BranchId, AdmissionFeeAmount = request.AdmissionFeeAmount, AdmissionFeeDueDate = request.AdmissionFeeDueDate };
        db.Students.Add(student);
        Guardian? parent = null;
        if (!string.IsNullOrWhiteSpace(request.ParentFirstName))
        {
            parent = new Guardian { AcademyId = academyId, FirstName = request.ParentFirstName.Trim(), LastName = request.ParentLastName!.Trim(), Email = request.ParentEmail?.Trim(), Phone = request.ParentPhone?.Trim(), AddressLine1 = request.ParentAddressLine1?.Trim(), City = request.ParentCity?.Trim() };
            db.Guardians.Add(parent);
        }
        await db.SaveChangesAsync(token);
        if (feeArrangements.Count > 0)
        {
            db.StudentFeeArrangements.AddRange(feeArrangements.Select(x => new StudentFeeArrangement { AcademyId = academyId, StudentId = student.Id, SubjectName = x.SubjectName.Trim(), Amount = x.Amount, Frequency = x.Frequency, EffectiveFrom = x.EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow) }));
            await db.SaveChangesAsync(token);
        }
        if (parent is not null) { var parentAccess = isMinor || request.AllowParentPortalAccess; db.StudentGuardians.Add(new StudentGuardian { AcademyId = academyId, StudentId = student.Id, GuardianId = parent.Id, Relationship = string.IsNullOrWhiteSpace(request.Relationship) ? "Parent" : request.Relationship.Trim(), IsPrimary = true, CanAccessPortal = parentAccess, CanViewAcademicProgress = parentAccess && request.AllowAcademicProgress, CanViewFinance = parentAccess && request.AllowFinance, CanViewDocuments = parentAccess && request.AllowDocuments, CanManageLeave = parentAccess && request.AllowLeave, AccessGrantedAtUtc = parentAccess ? DateTime.UtcNow : null }); }
        await db.SaveChangesAsync(token);
        if (!await roles.RoleExistsAsync("Student")) await roles.CreateAsync(new ApplicationRole { Name = "Student" });
        if (!await roles.RoleExistsAsync("Guardian")) await roles.CreateAsync(new ApplicationRole { Name = "Guardian" });
        if (!string.IsNullOrWhiteSpace(request.StudentUserName) && !string.IsNullOrWhiteSpace(request.StudentTemporaryPassword)) await CreateAccount(request.StudentUserName, request.StudentEmail ?? $"{request.StudentUserName}@academydesk.local", request.StudentTemporaryPassword, student.FirstName + " " + student.LastName, academyId, "Student", student.Id, null, token);
        if (parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) && !string.IsNullOrWhiteSpace(request.ParentTemporaryPassword)) await CreateAccount(request.ParentUserName, parent.Email!, request.ParentTemporaryPassword, parent.FirstName + " " + parent.LastName, academyId, "Guardian", null, parent.Id, token);
        await transaction.CommitAsync(token);
        return Ok(new { student.Id, student.FirstName, student.LastName, IsMinor = isMinor, ParentId = parent?.Id, StudentAccountCreated = !string.IsNullOrWhiteSpace(request.StudentUserName), ParentAccountCreated = parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) });
    }

    private async Task CreateAccount(string userName, string email, string password, string displayName, Guid academyId, string role, Guid? studentId, Guid? parentId, CancellationToken token)
    {
        if (await users.FindByNameAsync(userName) is not null || await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("Student or Parent username/email is already in use.");
        var user = new ApplicationUser { UserName = userName.Trim(), Email = email.Trim(), DisplayName = displayName, AcademyId = academyId, StudentId = studentId, GuardianId = parentId, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new InvalidOperationException(string.Join(" ", created.Errors.Select(x => x.Description)));
        await users.AddToRoleAsync(user, role);
    }
}

public sealed record StudentOnboardingRequest(string StudentFirstName, string StudentLastName, DateOnly? DateOfBirth, string? StudentEmail, string? StudentPhone, string? StudentAddressLine1, string? StudentCity, Guid? BranchId, string? ParentFirstName, string? ParentLastName, string? ParentEmail, string? ParentPhone, string? ParentAddressLine1, string? ParentCity, string? Relationship, string? StudentUserName, string? StudentTemporaryPassword, string? ParentUserName, string? ParentTemporaryPassword, decimal? AdmissionFeeAmount = null, DateOnly? AdmissionFeeDueDate = null, IReadOnlyList<OnboardingFeeArrangement>? FeeArrangements = null, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true);
public sealed record OnboardingFeeArrangement(string SubjectName, decimal Amount, string Frequency, DateOnly? EffectiveFrom = null);
