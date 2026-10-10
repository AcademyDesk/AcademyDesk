using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
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
    [AtomicAcademyMutation(IncludeIdentity = true, IncludePlatformOwner = true)]
    public async Task<ActionResult> Create(Guid academyId, StudentOnboardingRequest request, CancellationToken token)
    {
        var actor = await users.GetUserAsync(User);
        if (actor?.AcademyId != academyId || (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return Forbid();
        if (string.IsNullOrWhiteSpace(request.StudentFirstName) || string.IsNullOrWhiteSpace(request.StudentLastName) || request.DateOfBirth is null) return BadRequest(new { message = "Student name and date of birth are required." });
        if (request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return BadRequest(new { message = "Date of birth cannot be in the future." });
        var isMinor = request.DateOfBirth.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
        var hasParentDetails = new[] { request.ParentFirstName, request.ParentLastName, request.ParentEmail, request.ParentPhone, request.ParentAddressLine1, request.ParentCity }
            .Any(value => !string.IsNullOrWhiteSpace(value));
        if (isMinor && (string.IsNullOrWhiteSpace(request.ParentFirstName) || string.IsNullOrWhiteSpace(request.ParentLastName) || string.IsNullOrWhiteSpace(request.ParentEmail))) return BadRequest(new { message = "A parent name and email are required for a minor student." });
        if (hasParentDetails && (string.IsNullOrWhiteSpace(request.ParentFirstName) || string.IsNullOrWhiteSpace(request.ParentLastName)))
            return BadRequest(new { message = "Enter both the parent first name and last name, or clear the optional parent details." });

        try
        {
            var student = new Student { AcademyId = academyId, FirstName = request.StudentFirstName.Trim(), LastName = request.StudentLastName.Trim(), StudentNumber = string.IsNullOrWhiteSpace(request.StudentNumber) ? null : request.StudentNumber.Trim(), PreferredName = request.PreferredName?.Trim(), Gender = request.Gender?.Trim(), DateOfBirth = request.DateOfBirth, AdmissionDate = request.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Email = request.StudentEmail?.Trim(), Phone = request.StudentPhone?.Trim(), AddressLine1 = request.StudentAddressLine1?.Trim(), City = request.StudentCity?.Trim(), State = request.StudentState?.Trim(), PostalCode = request.StudentPostalCode?.Trim(), EmergencyContactName = request.EmergencyContactName?.Trim(), EmergencyContactPhone = request.EmergencyContactPhone?.Trim(), MedicalOrAccessibilityNotes = request.MedicalOrAccessibilityNotes?.Trim(), BranchId = request.BranchId };
            db.Students.Add(student);
            Guardian? parent = null;
            if (hasParentDetails)
            {
                parent = new Guardian { AcademyId = academyId, FirstName = request.ParentFirstName!.Trim(), LastName = request.ParentLastName!.Trim(), Email = request.ParentEmail?.Trim(), Phone = request.ParentPhone?.Trim(), AddressLine1 = request.ParentAddressLine1?.Trim(), City = request.ParentCity?.Trim() };
                db.Guardians.Add(parent);
            }
            await db.SaveChangesAsync(token);
            if (parent is not null) { var parentAccess = isMinor || request.AllowParentPortalAccess; db.StudentGuardians.Add(new StudentGuardian { AcademyId = academyId, StudentId = student.Id, GuardianId = parent.Id, Relationship = string.IsNullOrWhiteSpace(request.Relationship) ? "Parent" : request.Relationship.Trim(), IsPrimary = true, CanAccessPortal = parentAccess, CanViewAcademicProgress = parentAccess && request.AllowAcademicProgress, CanViewFinance = parentAccess && request.AllowFinance, CanViewDocuments = parentAccess && request.AllowDocuments, CanManageLeave = parentAccess && request.AllowLeave, AccessGrantedAtUtc = parentAccess ? DateTime.UtcNow : null }); }
            await db.SaveChangesAsync(token);
            await EnsureRole("Student");
            await EnsureRole("Guardian");
            var studentAccountCreated = false;
            var parentAccountCreated = false;
            if (!string.IsNullOrWhiteSpace(request.StudentUserName) && !string.IsNullOrWhiteSpace(request.StudentTemporaryPassword))
            {
                await CreateAccount(request.StudentUserName, request.StudentEmail ?? $"{request.StudentUserName}@academydesk.local", request.StudentTemporaryPassword, student.FirstName + " " + student.LastName, academyId, "Student", student.Id, null, token);
                studentAccountCreated = true;
            }
            if (parent is not null && !string.IsNullOrWhiteSpace(request.ParentUserName) && !string.IsNullOrWhiteSpace(request.ParentTemporaryPassword))
            {
                await CreateAccount(request.ParentUserName, parent.Email!, request.ParentTemporaryPassword, parent.FirstName + " " + parent.LastName, academyId, "Guardian", null, parent.Id, token);
                parentAccountCreated = true;
            }
            return Ok(new { student.Id, student.FirstName, student.LastName, IsMinor = isMinor, ParentId = parent?.Id, StudentAccountCreated = studentAccountCreated, ParentAccountCreated = parentAccountCreated });
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid." });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private async Task EnsureRole(string name)
    {
        if (await roles.RoleExistsAsync(name)) return;
        var result = await roles.CreateAsync(new ApplicationRole { Name = name });
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    }

    private async Task CreateAccount(string userName, string email, string password, string displayName, Guid academyId, string role, Guid? studentId, Guid? parentId, CancellationToken token)
    {
        if (await users.FindByNameAsync(userName) is not null || await users.FindByEmailAsync(email) is not null) throw new InvalidOperationException("Student or Parent username/email is already in use.");
        var user = new ApplicationUser { UserName = userName.Trim(), Email = email.Trim(), DisplayName = displayName, AcademyId = academyId, StudentId = studentId, GuardianId = parentId, EmailConfirmed = true };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new InvalidOperationException(string.Join(" ", created.Errors.Select(x => x.Description)));
        var assigned = await users.AddToRoleAsync(user, role);
        if (!assigned.Succeeded) throw new InvalidOperationException(string.Join(" ", assigned.Errors.Select(x => x.Description)));
    }
}

public sealed record StudentOnboardingRequest(string StudentFirstName, string StudentLastName, DateOnly? DateOfBirth, string? StudentEmail, string? StudentPhone, string? StudentAddressLine1, string? StudentCity, Guid? BranchId, string? ParentFirstName, string? ParentLastName, string? ParentEmail, string? ParentPhone, string? ParentAddressLine1, string? ParentCity, string? Relationship, string? StudentUserName, string? StudentTemporaryPassword, string? ParentUserName, string? ParentTemporaryPassword, string? StudentNumber = null, string? PreferredName = null, string? Gender = null, DateOnly? AdmissionDate = null, string? StudentState = null, string? StudentPostalCode = null, string? EmergencyContactName = null, string? EmergencyContactPhone = null, string? MedicalOrAccessibilityNotes = null, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true);
