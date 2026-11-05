using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaOnboardingCoreContextTests
{
    private static string Payload(object fields) => JsonSerializer.Serialize(new { tool_key = PentaOnboardingDraftService.Tool, tool_revision = 1, fields });

    [Theory]
    [InlineData("{}", 3)]
    [InlineData("{\"DateOfBirth\":{\"kind\":\"date\",\"value\":\"2015-01-01\"}}", 6)]
    [InlineData("{\"DateOfBirth\":{\"kind\":\"date\",\"value\":\"2000-01-01\"}}", 3)]
    [InlineData("{\"ParentPhone\":{\"kind\":\"text\",\"value\":\"123456\"}}", 5)]
    public void Complete_catalog_uses_the_shared_conditional_required_rules(string fields, int expected)
    {
        var payload = Payload(JsonDocument.Parse(fields).RootElement);
        var catalog = JsonSerializer.SerializeToElement(PentaOnboardingFields.Describe(payload, new DateOnly(2026,10,18), []));
        Assert.Equal(29, catalog.GetArrayLength());
        var names = catalog.EnumerateArray().Select(x => x.GetProperty("key").GetString()).ToArray();
        Assert.Equal(PentaOnboardingFields.Names.Order(), names.Order());
        Assert.Equal(expected, catalog.EnumerateArray().Count(x => x.GetProperty("required").GetBoolean()));
        Assert.All(catalog.EnumerateArray(), field => {
            Assert.Equal(!field.GetProperty("required").GetBoolean(), field.GetProperty("clearable").GetBoolean());
            Assert.InRange(field.GetProperty("max_length").GetInt32(), 1, 2000);
        });
        Assert.DoesNotContain(names, x => x!.Contains("Password") || x.Contains("UserName"));
    }

    [Fact]
    public void Branch_catalog_uses_stable_host_ids_not_labels_and_only_reference_fields_have_choices()
    {
        var choice = new PentaOnboardingBranchChoice(Guid.NewGuid().ToString("D"), "Synthetic branch");
        var catalog = JsonSerializer.SerializeToElement(PentaOnboardingFields.Describe(Payload(new { }), new DateOnly(2026,10,18), [choice]));
        foreach (var field in catalog.EnumerateArray()) {
            var choices = field.GetProperty("choices");
            if (field.GetProperty("key").GetString() == "BranchId") {
                Assert.Equal(choice.id, choices[0].GetProperty("id").GetString());
                Assert.Equal(choice.label, choices[0].GetProperty("label").GetString());
            } else Assert.Equal(0, choices.GetArrayLength());
        }
    }

    [Theory]
    [InlineData("2010-01-01", "", "", 6)]
    [InlineData("2000-01-01", "", "", 3)]
    [InlineData("2000-01-01", "Parent", "", 5)]
    [InlineData(null, "", "parent@example.invalid", 5)]
    public void Required_field_projection_has_the_same_parent_condition_as_domain_validation(string? dob, string parent, string email, int expected)
    {
        var request = JsonSerializer.Deserialize<StudentOnboardingRequest>(JsonSerializer.Serialize(new {
            StudentFirstName = "Synthetic", StudentLastName = "Test", DateOfBirth = dob, ParentFirstName = parent, ParentEmail = email }))!;
        Assert.Equal(expected, StudentOnboardingRules.RequiredFields(request, new DateOnly(2026,10,18)).Count);
    }
}
