using AcademyDesk.Api.Security;

namespace AcademyDesk.Api.Tests;

public sealed class CommunicationPreferenceAccessTests
{
    [Fact]
    public void Preference_actions_use_the_existing_communication_permission()
        => Assert.Equal(new[] { "communications.manage" }, PermissionCatalog.RequiredFor("CommunicationPreferencesController"));

    [Fact]
    public void Manager_can_manage_consent_without_general_people_management()
    {
        var allowed = PermissionCatalog.ForSystemRole("Manager").ToArray();
        Assert.Contains("communications.manage", allowed);
        Assert.DoesNotContain("students.manage", allowed);
        Assert.DoesNotContain("workforce.manage", allowed);
        Assert.Equal(new[] { "batches.manage", "scheduling.manage", "attendance.manage", "makeup.manage", "communications.manage" }, allowed);
    }

    [Theory]
    [InlineData("Operations")][InlineData("Teacher")][InlineData("Student")][InlineData("Guardian")]
    [InlineData("FrontDesk")][InlineData("FinanceUser")][InlineData("Sales")][InlineData("Marketing")]
    public void Other_system_roles_gain_no_consent_management(string role)
        => Assert.DoesNotContain("communications.manage", PermissionCatalog.ForSystemRole(role));

    [Theory]
    [InlineData("StudentsController")][InlineData("GuardiansController")]
    public void Broad_people_endpoints_keep_their_original_permission(string controller)
        => Assert.Equal(new[] { "students.manage" }, PermissionCatalog.RequiredFor(controller));

    [Theory]
    [InlineData("NotificationsController")][InlineData("CommunicationSettingsController")][InlineData("CommunicationTemplatesController")]
    public void This_slice_does_not_remap_other_communication_controllers(string controller)
        => Assert.Null(PermissionCatalog.RequiredFor(controller));

    [Fact]
    public void Consent_management_keeps_Engagement_module_gate_and_catalog_entry()
    {
        Assert.Equal("Engagement", SubscriptionPlanCatalog.ModuleForController("CommunicationPreferencesController"));
        Assert.Single(PermissionCatalog.All, x => x == "communications.manage");
        Assert.Equal(14, PermissionCatalog.All.Length);
    }
}
