using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Tests;

public sealed class StudentOnboardingRulesTests
{
    private static readonly DateOnly Today = new(2026, 10, 29);
    private static StudentOnboardingRequest Adult() => JsonSerializer.Deserialize<StudentOnboardingRequest>(
        """{"StudentFirstName":"Synthetic","StudentLastName":"Adult","DateOfBirth":"2000-01-01"}""")!;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Parent_account_requires_email_before_any_Identity_write(string? email)
    {
        var request = Adult() with { ParentFirstName = "Synthetic", ParentLastName = "Guardian", ParentEmail = email,
            ParentUserName = "synthetic-parent", ParentTemporaryPassword = "Synthetic!39Ab" };
        Assert.Equal("Enter a parent email address to create a parent portal account.", StudentOnboardingRules.Validate(request, Today));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Adult_guardian_without_portal_account_does_not_require_email(string? email)
    {
        Assert.Null(StudentOnboardingRules.Validate(Adult() with { ParentFirstName = "Synthetic", ParentLastName = "Guardian", ParentEmail = email }, Today));
    }

    [Theory]
    [InlineData("2008-10-29", false)]
    [InlineData("2008-10-30", true)]
    [InlineData("2015-01-01", true)]
    public void Guardian_requirement_uses_exact_eighteenth_birthday(string birth, bool minor)
    {
        var request = Adult() with { DateOfBirth = DateOnly.Parse(birth) };
        Assert.Equal(minor, StudentOnboardingRules.RequiredFields(request, Today).Contains(nameof(request.ParentEmail)));
        Assert.Equal(minor, StudentOnboardingRules.Validate(request, Today) is not null);
    }
}
