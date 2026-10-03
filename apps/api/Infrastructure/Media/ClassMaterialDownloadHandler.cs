using System.Security.Claims;
using System.Text.Encodings.Web;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcademyDesk.Api.Infrastructure.Media;

public sealed class ClassMaterialDownloadHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, ClassMaterialDownloadTickets tickets, UserManager<ApplicationUser> users)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // This handler is selected only on the three attachment read actions.
        // No GET/query/cookie/account-token fallback and no chunked or large body.
        if (!HttpMethods.IsPost(Request.Method)) return AuthenticateResult.NoResult();
        if (!tickets.IsSafeTransport(Request) || !ClassMaterialDownloadTickets.IsContentPath(Request.Path.Value) ||
            Request.QueryString.HasValue || Request.ContentLength is null or <= 0 or > 4096 ||
            !string.Equals(Request.ContentType?.Split(';')[0].Trim(), "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)) return Reject();
        try
        {
            var form = await Request.ReadFormAsync(new FormOptions { ValueCountLimit = 1, KeyLengthLimit = 16, ValueLengthLimit = 3000 }, Context.RequestAborted);
            if (form.Count != 1 || !form.TryGetValue("ticket", out var value) || value.Count != 1) return Reject();
            var scope = tickets.Read(value.ToString(), Request.Path.Value!, Request.Headers.Origin.ToString());
            if (scope is null) return Reject();
            var user = await users.FindByIdAsync(scope.UserId.ToString());
            if (user is null || !user.IsActive || user.AcademyId != scope.AcademyId || user.SecurityStamp != scope.SecurityStamp) return Reject();
            // Content actions recheck current database roles, tenant, enrollment,
            // recipient and publication before streaming any bytes.
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], Scheme.Name);
            return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
        }
        catch (InvalidDataException) { return Reject(); }
        catch (BadHttpRequestException) { return Reject(); }
    }
    private static AuthenticateResult Reject() => AuthenticateResult.Fail("Invalid or expired private-download credential.");
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Task.CompletedTask;
    }
}
