using System.Net.Mail;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

/// <summary>Shared deterministic field checks for Manual and host-authorized onboarding.</summary>
public static class StudentOnboardingRules
{
    public static IReadOnlyList<string> RequiredFields(StudentOnboardingRequest request, DateOnly today)
    {
        var fields = new List<string> { nameof(request.StudentFirstName), nameof(request.StudentLastName), nameof(request.DateOfBirth) };
        if (IsMinor(request, today) || HasParentDetails(request)) {
            fields.Add(nameof(request.ParentFirstName)); fields.Add(nameof(request.ParentLastName));
        }
        if (IsMinor(request, today)) fields.Add(nameof(request.ParentEmail));
        return fields.AsReadOnly();
    }
    private static bool IsMinor(StudentOnboardingRequest request, DateOnly today) =>
        request.DateOfBirth is { } birth && (birth.Year > today.Year - 18 ||
            birth.Year == today.Year - 18 && birth.AddYears(18) > today);
    private static bool HasParentDetails(StudentOnboardingRequest request) =>
        new[] { request.ParentFirstName, request.ParentLastName, request.ParentEmail, request.ParentPhone, request.ParentAddressLine1, request.ParentCity }
            .Any(x => !string.IsNullOrWhiteSpace(x));

    public static string? Validate(StudentOnboardingRequest request, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(request.StudentFirstName) || string.IsNullOrWhiteSpace(request.StudentLastName) || request.DateOfBirth is null)
            return "Student name and date of birth are required.";
        if (request.DateOfBirth > today) return "Date of birth cannot be in the future.";
        var minor = IsMinor(request, today);
        var parentDetails = HasParentDetails(request);
        if (minor && (string.IsNullOrWhiteSpace(request.ParentFirstName) || string.IsNullOrWhiteSpace(request.ParentLastName) || string.IsNullOrWhiteSpace(request.ParentEmail)))
            return "A parent name and email are required for a minor student.";
        if (parentDetails && (string.IsNullOrWhiteSpace(request.ParentFirstName) || string.IsNullOrWhiteSpace(request.ParentLastName)))
            return "Enter both the parent first name and last name, or clear the optional parent details.";
        foreach (var (value, maximum) in new (string?, int)[] {
            (request.StudentFirstName,120), (request.StudentLastName,120), (request.StudentNumber,50), (request.PreferredName,120),
            (request.Gender,50), (request.StudentEmail,320), (request.StudentPhone,30), (request.StudentAddressLine1,240),
            (request.StudentCity,100), (request.StudentState,100), (request.StudentPostalCode,30), (request.EmergencyContactName,160),
            (request.EmergencyContactPhone,30), (request.MedicalOrAccessibilityNotes,4000),
            (request.ParentFirstName,120), (request.ParentLastName,120), (request.ParentEmail,320), (request.ParentPhone,30),
            (request.ParentAddressLine1,240), (request.ParentCity,100), (request.Relationship,50) })
            if (value is not null && (value.Trim().Length > maximum || value.Any(c => char.IsControl(c) && c is not '\n' and not '\r' and not '\t')))
                return "An onboarding field is too long or contains unsupported characters.";
        foreach (var email in new[] { request.StudentEmail, request.ParentEmail })
            if (!string.IsNullOrWhiteSpace(email) && (!MailAddress.TryCreate(email.Trim(), out var parsed) || parsed.Address != email.Trim()))
                return "Enter a valid student or parent email address, or leave the optional email blank.";
        return null;
    }

    public static async Task<string?> ValidateAsync(AcademyDeskDbContext db, Guid academyId,
        StudentOnboardingRequest request, DateOnly today, CancellationToken token)
    {
        if (Validate(request, today) is { } error) return error;
        if (request.BranchId is { } branch && !await db.Branches.AsNoTracking().AnyAsync(
            x => x.Id == branch && x.AcademyId == academyId && x.IsActive, token))
            return "Select an active branch belonging to this academy.";
        var number = request.StudentNumber?.Trim();
        if (!string.IsNullOrEmpty(number) && await db.Students.AsNoTracking().AnyAsync(
            x => x.AcademyId == academyId && x.StudentNumber == number, token))
            return "Student onboarding could not be saved. Check that the student number is unique and the entered values are valid.";
        return null;
    }
}
