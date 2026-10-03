using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcademyDesk.Api.Security;

// Keep Identity's password, lockout, confirmation, 2FA and stamp checks.
// Add the application's inactive-account policy at their shared extension points.
public sealed class AcademySignInManager(
    UserManager<ApplicationUser> users,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IOptions<IdentityOptions> options,
    ILogger<SignInManager<ApplicationUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<ApplicationUser> confirmation)
    : SignInManager<ApplicationUser>(users, contextAccessor, claimsFactory, options, logger, schemes, confirmation)
{
    public override async Task<bool> CanSignInAsync(ApplicationUser user) =>
        user.IsActive && await base.CanSignInAsync(user);

    // Used by native refresh and cookie/two-factor stamp validation as well.
    public override async Task<bool> ValidateSecurityStampAsync(ApplicationUser? user, string? securityStamp) =>
        user is { IsActive: true } && await base.ValidateSecurityStampAsync(user, securityStamp);
}
