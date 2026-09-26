using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace AcademyDesk.Api.Infrastructure;

public static class ProductionIdentityBootstrapper
{
    private static readonly string[] SystemRoles = ["PlatformOwner", "AcademyAdmin", "Manager", "FinanceUser", "Teacher", "Student", "Guardian", "Owner"];

    public static async Task EnsurePlatformOwnerAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["Bootstrap:PlatformOwnerEmail"]?.Trim();
        var password = configuration["Bootstrap:PlatformOwnerPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in SystemRoles)
        {
            if (await roles.RoleExistsAsync(roleName)) continue;
            var roleResult = await roles.CreateAsync(new ApplicationRole { Name = roleName, IsSystemRole = true });
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not create the required {roleName} role.");
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            var displayName = configuration["Bootstrap:PlatformOwnerDisplayName"]?.Trim();
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
                IsActive = true,
                IsPlatformOwner = true
            };
            var createResult = await users.CreateAsync(user, password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException("Could not create the initial Platform Owner account.");
        }
        else if (!user.IsPlatformOwner || !user.IsActive)
        {
            user.IsPlatformOwner = true;
            user.IsActive = true;
            user.EmailConfirmed = true;
            var updateResult = await users.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException("Could not configure the initial Platform Owner account.");
        }

        if (!await users.IsInRoleAsync(user, "PlatformOwner"))
        {
            var roleResult = await users.AddToRoleAsync(user, "PlatformOwner");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException("Could not assign the Platform Owner role.");
        }
    }
}
