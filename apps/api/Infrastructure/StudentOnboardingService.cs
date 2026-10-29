using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Infrastructure;

public sealed record StudentOnboardingResult(Guid Id, string FirstName, string LastName, bool IsMinor,
    Guid? ParentId, bool StudentAccountCreated, bool ParentAccountCreated);
public sealed record StudentOnboardingOutcome(int HttpStatus, StudentOnboardingResult? Result = null, string? Error = null);

/// <summary>
/// Existing onboarding domain implementation, shared with future host-authorized
/// workflows. Does not interpret prompts, approve actions or own the caller's
/// transaction/audit/idempotency ledger. Credentials belong only to secure Manual
/// input; they must never enter a conversational tool payload.
/// The existing request type is retained for API source/wire compatibility.
/// </summary>
public sealed class StudentOnboardingService(AcademyDeskDbContext db, IdentityDbContext identity,
    UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles)
{
    public async Task<StudentOnboardingOutcome> CreateAsync(System.Security.Claims.ClaimsPrincipal principal,
        Guid academyId, StudentOnboardingRequest request, CancellationToken token)
    {
        // A caller must supply the shared SQL/Identity unit of work. Domain service
        // success is not a committed/audited receipt until that caller commits it.
        if ((db.Database.IsRelational() || identity.Database.IsRelational()) && (!db.Database.IsSqlServer() || !identity.Database.IsSqlServer() ||
            db.Database.CurrentTransaction is not { } domainTransaction ||
            identity.Database.CurrentTransaction is not { } identityTransaction ||
            !ReferenceEquals(domainTransaction.GetDbTransaction(), identityTransaction.GetDbTransaction())))
            return new(503, Error: "Student onboarding requires an atomic host transaction.");
        if (!Guid.TryParse(users.GetUserId(principal), out var actorId)) return new(403);
        var actor = await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actorId, token);
        if (actor is null || !actor.IsActive || actor.AcademyId != academyId ||
            (!await users.IsInRoleAsync(actor, "Owner") && !await users.IsInRoleAsync(actor, "AcademyAdmin"))) return new(403);
        if (!actor.IsPlatformOwner)
        {
            var academy = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, token);
            if (academy is null || !academy.IsActive || !SubscriptionPlanCatalog.Allows(academy.EnabledModulesJson,
                SubscriptionPlanCatalog.ModuleForController(nameof(StudentOnboardingController)))) return new(403);
        }
        // Serialize both Manual and conversational onboarding against the same academy.
        // The caller still owns the shared SQL/Identity transaction and required audit.
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academyId}", token);
        if (await StudentOnboardingRules.ValidateAsync(db, academyId, request, DateOnly.FromDateTime(DateTime.UtcNow), token) is { } validation)
            return new(400, Error: validation);
        var isMinor = request.DateOfBirth!.Value.AddYears(18) > DateOnly.FromDateTime(DateTime.UtcNow);
        var hasParentDetails = new[] { request.ParentFirstName, request.ParentLastName, request.ParentEmail, request.ParentPhone, request.ParentAddressLine1, request.ParentCity }
            .Any(value => !string.IsNullOrWhiteSpace(value));

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
            return new(200, new StudentOnboardingResult(student.Id, student.FirstName, student.LastName, isMinor, parent?.Id, studentAccountCreated, parentAccountCreated));
        }
        catch (DbUpdateException)
        {
            return new(400, Error: "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid.");
        }
        catch (InvalidOperationException exception)
        {
            return new(400, Error: exception.Message);
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
