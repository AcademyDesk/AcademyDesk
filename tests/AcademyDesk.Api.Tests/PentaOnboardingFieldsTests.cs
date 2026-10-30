using System.Reflection;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingFieldsTests
{
    private static string Payload(Dictionary<string, object> fields) => JsonSerializer.Serialize(new {
        tool_key = PentaOnboardingFields.Tool, tool_revision = 1, fields });
    private static object Text(string value) => new { kind = "text", value };
    private static Dictionary<string, object> ValidFields() => new() {
        ["StudentFirstName"] = Text("Synthetic"), ["StudentLastName"] = Text("Person"),
        ["DateOfBirth"] = new { kind = "date", value = "2000-01-01" } };
    [Fact]
    public void Catalog_covers_every_request_property_except_four_secure_account_fields()
    {
        var excluded = new[] { "StudentUserName", "StudentTemporaryPassword", "ParentUserName", "ParentTemporaryPassword" };
        var expected = typeof(StudentOnboardingRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(x => x.Name).Except(excluded).Order(StringComparer.Ordinal);
        Assert.Equal(expected, PentaOnboardingFields.Names.Order(StringComparer.Ordinal));
        Assert.Equal(29, PentaOnboardingFields.Names.Count);
    }
    [Theory]
    [InlineData("StudentUserName")]
    [InlineData("StudentTemporaryPassword")]
    [InlineData("ParentUserName")]
    [InlineData("ParentTemporaryPassword")]
    [InlineData("AcademyId")]
    [InlineData("ActorId")]
    [InlineData("IsPlatformOwner")]
    [InlineData("Approved")]
    public void Credentials_and_authority_fields_are_not_conversational_inputs(string key)
    {
        var fields = ValidFields(); fields[key] = Text("Untrusted");
        Assert.False(PentaOnboardingFields.TryRead(Payload(fields), out _));
    }
    [Theory]
    [InlineData("StudentNumber",51)]
    [InlineData("StudentPhone",31)]
    [InlineData("ParentPhone",31)]
    [InlineData("StudentFirstName",121)]
    [InlineData("StudentAddressLine1",241)]
    [InlineData("StudentPostalCode",31)]
    [InlineData("MedicalOrAccessibilityNotes",2001)]
    public void Typed_field_bounds_reject_before_draft_storage(string key, int length)
    {
        var fields = ValidFields(); fields[key] = Text(new string('x', length));
        Assert.False(PentaOnboardingFields.TryRead(Payload(fields), out _));
    }
    [Fact]
    public void Optional_booleans_preserve_explicit_false_and_request_defaults_without_credentials()
    {
        var fields = ValidFields(); fields["AllowParentPortalAccess"] = new { kind = "boolean", value = true };
        fields["AllowFinance"] = new { kind = "boolean", value = false };
        fields["BranchId"] = new { kind = "reference", value = Guid.NewGuid().ToString("D") };
        Assert.True(PentaOnboardingFields.TryRead(Payload(fields), out var request));
        Assert.True(request!.AllowParentPortalAccess); Assert.False(request.AllowFinance);
        Assert.True(request.AllowAcademicProgress); Assert.Null(request.StudentTemporaryPassword); Assert.Null(request.ParentTemporaryPassword);
    }
    [Theory]
    [InlineData("not-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("Visible branch name")]
    public void Branch_requires_stable_nonempty_identity(string value)
    {
        var fields = ValidFields(); fields["BranchId"] = new { kind = "reference", value };
        Assert.False(PentaOnboardingFields.TryRead(Payload(fields), out _));
    }
    [Theory]
    [InlineData("2015-01-01",false)]
    [InlineData("2000-01-01",true)]
    [InlineData("2099-01-01",false)]
    public void Required_and_minor_rules_use_shared_deterministic_validation(string dob, bool ready)
    {
        var fields = ValidFields(); fields["DateOfBirth"] = new { kind = "date", value = dob };
        Assert.Equal(ready, PentaOnboardingFields.Ready(Payload(fields), new DateOnly(2026,10,17)));
    }
    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Name <user@example.invalid>")]
    [InlineData("user@example.invalid\r\nBcc: someone")]
    public void Invalid_email_is_not_confirmation_ready(string email)
    {
        var fields = ValidFields(); fields["StudentEmail"] = Text(email);
        Assert.False(PentaOnboardingFields.Ready(Payload(fields), new DateOnly(2026,10,17)));
    }
    [Fact]
    public void Optional_parent_partial_details_remain_invalid_but_empty_optional_fields_are_allowed()
    {
        var fields = ValidFields(); fields["ParentCity"] = Text("Synthetic City");
        Assert.False(PentaOnboardingFields.Ready(Payload(fields), new DateOnly(2026,10,17)));
        fields["ParentCity"] = Text("");
        Assert.True(PentaOnboardingFields.Ready(Payload(fields), new DateOnly(2026,10,17)));
    }
}
