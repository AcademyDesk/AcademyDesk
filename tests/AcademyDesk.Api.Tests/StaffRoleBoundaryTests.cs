using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace AcademyDesk.Api.Tests;

public sealed class StaffRoleBoundaryTests
{
    [Theory]
    [InlineData("UpdateRole", true)]
    [InlineData("Create", false)]
    [InlineData("List", false)]
    [InlineData("UpdateStatus", false)]
    [InlineData("ResetPassword", false)]
    [InlineData("Offboard", false)]
    public void Role_repair_opts_in_only_the_requested_action(string method, bool expected)
    {
        var descriptor = new ControllerActionDescriptor { MethodInfo = typeof(StaffController).GetMethod(method)! };
        Assert.NotNull(descriptor.MethodInfo);
        Assert.Equal(expected, AcademyAccessFilter.UsesAtomicBoundary(nameof(StaffController), descriptor));
        Assert.Equal(expected, AcademyAccessFilter.IncludesIdentity(descriptor));
    }
}
