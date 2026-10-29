using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RoleDb = AcademyDesk.Api.Data.IdentityDbContext;

namespace AcademyDesk.Api.Tests;

public sealed class StaffRolePreparationTests
{
    [Theory]
    [InlineData("Manager")]
    [InlineData("Operations")]
    [InlineData("Sales")]
    [InlineData("Marketing")]
    [InlineData("FinanceUser")]
    [InlineData("FrontDesk")]
    public async Task Supported_role_missing_from_bootstrap_can_be_prepared(string name)
    {
        await using var db = CreateDb(); using var roles = Manager(db);
        Assert.True((await StaffController.EnsureSupportedRoleAsync(roles, name.ToLowerInvariant())).Succeeded);
        var saved = Assert.Single(await db.Roles.AsNoTracking().ToListAsync());
        Assert.Equal(name, saved.Name); Assert.Equal(name.ToUpperInvariant(), saved.NormalizedName);
        Assert.Null(saved.AcademyId);
    }

    [Fact]
    public async Task Existing_supported_role_is_reused_without_overwriting_its_id_or_metadata()
    {
        await using var db = CreateDb(); using var roles = Manager(db);
        var existing = new ApplicationRole { Name = "FrontDesk", PermissionsJson = "[\"Synthetic retained metadata\"]" };
        Assert.True((await roles.CreateAsync(existing)).Succeeded);
        var stamp = existing.ConcurrencyStamp;
        Assert.True((await StaffController.EnsureSupportedRoleAsync(roles, "frontdesk")).Succeeded);
        var saved = Assert.Single(await db.Roles.AsNoTracking().ToListAsync());
        Assert.Equal(existing.Id, saved.Id); Assert.Equal(stamp, saved.ConcurrencyStamp);
        Assert.Equal(existing.PermissionsJson, saved.PermissionsJson);
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("AcademyAdmin")]
    [InlineData("UnapprovedRole")]
    public async Task Unsupported_privileged_or_custom_roles_are_not_created(string name)
    {
        await using var db = CreateDb(); using var roles = Manager(db);
        var result = await StaffController.EnsureSupportedRoleAsync(roles, name);
        Assert.False(result.Succeeded); Assert.Equal("UnsupportedStaffRole", Assert.Single(result.Errors).Code);
        Assert.Empty(await db.Roles.ToListAsync());
    }

    [Fact]
    public async Task Identity_validator_rejection_is_returned_without_a_saved_role()
    {
        await using var db = CreateDb(); using var roles = Manager(db, new RejectRole());
        var result = await StaffController.EnsureSupportedRoleAsync(roles, "FrontDesk");
        Assert.False(result.Succeeded); Assert.Equal("SyntheticRoleFailure", Assert.Single(result.Errors).Code);
        Assert.Empty(await db.Roles.ToListAsync());
    }

    private static RoleDb CreateDb() => new(new DbContextOptionsBuilder<RoleDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static RoleManager<ApplicationRole> Manager(RoleDb db, IRoleValidator<ApplicationRole>? validator = null) =>
        new(new RoleStore<ApplicationRole, RoleDb, Guid>(db), [validator ?? new RoleValidator<ApplicationRole>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), NullLogger<RoleManager<ApplicationRole>>.Instance);
    private sealed class RejectRole : IRoleValidator<ApplicationRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "SyntheticRoleFailure", Description = "Synthetic validation failure." }));
    }
}
