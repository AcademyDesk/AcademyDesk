using AcademyDesk.Api.Security;

namespace AcademyDesk.Api.Tests;

public sealed class AssessmentAccessTests
{
    [Theory]
    [InlineData("AssessmentsController")]
    [InlineData("AssessmentResultsController")]
    public void Setup_and_results_require_the_same_academic_permission_and_module(string controller)
    {
        Assert.Equal(new[] { "academics.manage" }, PermissionCatalog.RequiredFor(controller));
        Assert.Equal("AcademicGovernance", SubscriptionPlanCatalog.ModuleForController(controller));
    }

    [Theory]
    [InlineData("[\"AcademicGovernance\"]", true)]
    [InlineData("[\"academicgovernance\"]", true)]
    [InlineData("[\"Core\"]", false)]
    [InlineData("[]", false)]
    [InlineData(null, false)]
    [InlineData("invalid-json", false)]
    public void Academic_results_do_not_fall_back_to_always_enabled_Core(string? modules, bool allowed)
    {
        Assert.Equal(allowed, SubscriptionPlanCatalog.Allows(modules, SubscriptionPlanCatalog.ModuleForController("AssessmentResultsController")));
    }

    [Fact]
    public void Result_repair_does_not_grant_general_student_or_batch_management()
    {
        Assert.Equal(new[] { "students.manage" }, PermissionCatalog.RequiredFor("StudentsController"));
        Assert.Equal(new[] { "students.manage" }, PermissionCatalog.RequiredFor("EnrollmentsController"));
        Assert.Equal(new[] { "batches.manage" }, PermissionCatalog.RequiredFor("BatchesController"));
        Assert.DoesNotContain("academics.manage", PermissionCatalog.ForSystemRole("FinanceUser"));
        Assert.DoesNotContain("academics.manage", PermissionCatalog.ForSystemRole("Operations"));
    }
}
