using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaManualCapabilityIndexTests
{
    [Theory]
    [InlineData("PlatformAcademiesController", "Onboard", "platform")]
    [InlineData("PlatformControlController", "CreateSupportCase", "platform")]
    [InlineData("StudentsController", "List", "academy-administration")]
    [InlineData("TeacherPortalController", "ReviewSubmission", "teacher")]
    [InlineData("TeacherPortalController", "MarkAttendanceBulk", "teacher")]
    [InlineData("PortalController", "SubmitAssignment", "student-guardian")]
    [InlineData("PortalController", "RequestStudentLeave", "student-guardian")]
    [InlineData("PortalController", "UpdateGuardianProfile", "student-guardian")]
    [InlineData("AssessmentResultsController", "Upsert", "academy-administration")]
    public void Includes_role_specific_and_nested_controller_actions(string controller, string action, string surface)
    {
        var row = Assert.Single(PentaManualCapabilityIndex.Discover(), x => x.Controller == controller && x.Action == action);
        Assert.Equal(surface, row.Surface); Assert.StartsWith("/api/", row.Route);
    }

    [Fact]
    public void Whole_product_index_is_not_a_claim_of_model_qualification_or_permission()
    {
        var rows = PentaManualCapabilityIndex.Discover();
        Assert.True(rows.Count > 400);
        Assert.Equal(9, rows.Count(x => x.DraftToolKey is not null));
        Assert.Contains(rows, x => x.Controller == "CommunicationTemplatesController" && x.DraftToolKey is null);
        Assert.Contains(rows, x => x.Controller == "StaffController" && x.Action == "UpdateRole" && x.DraftToolKey is null);
        Assert.Contains(rows, x => x.Controller == "PlatformControlController" && x.Action == "ResetAdminPassword" && x.DraftToolKey is null);
        Assert.DoesNotContain(rows, x => x.Integration.Contains("model-qualified", StringComparison.Ordinal));
    }

    [Fact]
    public void Module_metadata_and_absolute_route_overrides_match_existing_manual_declarations()
    {
        var rows = PentaManualCapabilityIndex.Discover();
        Assert.Equal("Finance", Assert.Single(rows, x => x.Controller == "PaymentsController" && x.Action == "Create").Module);
        Assert.Equal("MultiBranch", Assert.Single(rows, x => x.Controller == "BranchesController" && x.Action == "Create").Module);
        var overrideRoute = Assert.Single(rows, x => x.Controller == "PentaOnboardingController" && x.Action == "StartConversation").Route;
        Assert.Equal("/api/academies/{academyId:guid}/penta/onboarding/conversations", overrideRoute);
        Assert.DoesNotContain(rows, x => x.Route.Contains("~/", StringComparison.Ordinal));
        Assert.Equal("/WeatherForecast", Assert.Single(rows, x => x.Controller == "WeatherForecastController").Route);
    }
}
