using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AcademyDesk.Api.Tests;

public sealed class StaffRoleBoundaryTests
{
    [Theory]
    [InlineData("UpdateRole", true, false)]
    [InlineData("Create", true, true)]
    [InlineData("List", false, false)]
    [InlineData("UpdateStatus", true, true)]
    [InlineData("ResetPassword", true, true)]
    [InlineData("Offboard", true, true)]
    public void Staff_mutations_require_their_explicit_action_boundary(string method, bool expected, bool platform)
    {
        var descriptor = new ControllerActionDescriptor { MethodInfo = typeof(StaffController).GetMethod(method)! };
        Assert.NotNull(descriptor.MethodInfo);
        Assert.Equal(expected, AcademyAccessFilter.UsesAtomicBoundary(nameof(StaffController), descriptor));
        Assert.Equal(expected, AcademyAccessFilter.IncludesIdentity(descriptor));
        Assert.Equal(platform, AcademyAccessFilter.IncludesPlatformOwner(descriptor));
    }
}
