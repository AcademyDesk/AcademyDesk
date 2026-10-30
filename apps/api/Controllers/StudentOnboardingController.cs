using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/student-onboarding")]
public sealed class StudentOnboardingController(StudentOnboardingService onboarding) : ControllerBase
{
    [HttpPost]
    [AtomicAcademyMutation(IncludeIdentity = true, IncludePlatformOwner = true)]
    public async Task<ActionResult> Create(Guid academyId, StudentOnboardingRequest request, CancellationToken token)
    {
        var outcome = await onboarding.CreateAsync(User, academyId, request, token);
        return outcome.HttpStatus switch
        {
            403 => Forbid(),
            200 => Ok(outcome.Result),
            _ => StatusCode(outcome.HttpStatus, new { message = outcome.Error })
        };
    }
}

public sealed record StudentOnboardingRequest(string StudentFirstName, string StudentLastName, DateOnly? DateOfBirth, string? StudentEmail, string? StudentPhone, string? StudentAddressLine1, string? StudentCity, Guid? BranchId, string? ParentFirstName, string? ParentLastName, string? ParentEmail, string? ParentPhone, string? ParentAddressLine1, string? ParentCity, string? Relationship, string? StudentUserName, string? StudentTemporaryPassword, string? ParentUserName, string? ParentTemporaryPassword, string? StudentNumber = null, string? PreferredName = null, string? Gender = null, DateOnly? AdmissionDate = null, string? StudentState = null, string? StudentPostalCode = null, string? EmergencyContactName = null, string? EmergencyContactPhone = null, string? MedicalOrAccessibilityNotes = null, bool AllowParentPortalAccess = false, bool AllowAcademicProgress = true, bool AllowFinance = true, bool AllowDocuments = true, bool AllowLeave = true);
