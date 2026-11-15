using System.Security.Claims;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

// Guard-only direct controller checks. The real provisioning transaction requires
// matching SQL Server contexts; these tests deliberately never bypass that boundary.
public sealed class GuardianProvisioningGuardTests
{
    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    public async Task Inactive_guardian_is_refused_before_any_account_or_role_write(string actor)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(adminRole: actor); await f.Change(db => db.Guardians.Single(x => x.Id == f.Guardian).IsActive = false); var before = await f.Snapshot();
        await using var scope = f.Factory.Services.CreateAsyncScope(); var controller = Controller(f, scope.ServiceProvider);
        var result = await controller.Create(f.Academy, new("Guardian", "new-guardian@example.invalid", "Synthetic!39Ab", "Synthetic", null, f.Guardian, null), default);
        Assert.IsType<BadRequestObjectResult>(result); Assert.Equal(before, await f.Snapshot());
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("foreign")]
    public async Task Missing_or_foreign_guardian_is_refused_without_identity_changes(string kind)
    {
        using var f = new GuardianAccessFixture(); await f.Seed(); if (kind == "foreign") await f.Change(db => db.Guardians.Single(x => x.Id == f.Guardian).AcademyId = f.Foreign); var before = await f.Snapshot();
        await using var scope = f.Factory.Services.CreateAsyncScope(); var controller = Controller(f, scope.ServiceProvider);
        var result = await controller.Create(f.Academy, new("Guardian", "new-guardian@example.invalid", "Synthetic!39Ab", "Synthetic", null, kind == "missing" ? Guid.NewGuid() : f.Guardian, null), default);
        Assert.IsType<BadRequestObjectResult>(result); Assert.Equal(before, await f.Snapshot());
    }
    [Fact]
    public async Task Unprivileged_creator_is_still_forbidden_before_domain_checks()
    {
        using var f = new GuardianAccessFixture(); await f.Seed(adminRole: "FrontDesk"); var before = await f.Snapshot(); await using var scope = f.Factory.Services.CreateAsyncScope();
        var result = await Controller(f, scope.ServiceProvider).Create(f.Academy, new("Guardian", "new-guardian@example.invalid", "Synthetic!39Ab", "Synthetic", null, f.Guardian, null), default);
        Assert.IsType<ForbidResult>(result); Assert.Equal(before, await f.Snapshot());
    }
    private static PortalAccountsController Controller(GuardianAccessFixture f, IServiceProvider services) => new(services.GetRequiredService<UserManager<ApplicationUser>>(), services.GetRequiredService<RoleManager<ApplicationRole>>(), services.GetRequiredService<AcademyDeskDbContext>(), services.GetRequiredService<IdentityDbContext>()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, f.Admin.ToString()) }, "Synthetic")) } } };
}
