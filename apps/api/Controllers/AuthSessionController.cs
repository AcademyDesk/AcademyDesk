using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth/session")]
public sealed class AuthSessionController(UserManager<ApplicationUser> users, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SessionSummary>> Current(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var roles = await users.GetRolesAsync(user);
        var workspace = user.IsPlatformOwner ? "Platform" : roles.Contains("Teacher") ? "Teacher" : roles.Contains("Student") || roles.Contains("Guardian") ? "Portal" : "AcademyAdmin";
        return Ok(new SessionSummary(user.DisplayName, roles.ToArray(), user.AcademyId, user.IsPlatformOwner, workspace, user.ProfileImageUrl));
    }

    [HttpPost("profile-image")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<ProfileImageSummary>> UploadProfileImage(IFormFile image, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Unauthorized();
        if (image.Length is <= 0 or > 2_000_000)
            return BadRequest(new { message = "Choose an image smaller than 2 MB." });

        var extension = image.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty,
        };
        if (extension.Length == 0)
            return BadRequest(new { message = "Use a JPG, PNG, or WebP image." });

        var webRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var folder = Path.Combine(webRootPath, "uploads", "profile-images");
        Directory.CreateDirectory(folder);
        var fileName = $"{user.Id:N}-{Guid.NewGuid():N}{extension}";
        var diskPath = Path.Combine(folder, fileName);
        await using (var stream = System.IO.File.Create(diskPath))
            await image.CopyToAsync(stream, token);

        if (!string.IsNullOrWhiteSpace(user.ProfileImageUrl) && user.ProfileImageUrl.StartsWith("/uploads/profile-images/", StringComparison.Ordinal))
        {
            var previousName = Path.GetFileName(user.ProfileImageUrl);
            var previousPath = Path.Combine(folder, previousName);
            if (System.IO.File.Exists(previousPath)) System.IO.File.Delete(previousPath);
        }

        user.ProfileImageUrl = $"/uploads/profile-images/{fileName}";
        var update = await users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            if (System.IO.File.Exists(diskPath)) System.IO.File.Delete(diskPath);
            return BadRequest(new { message = "Profile image could not be saved." });
        }
        return Ok(new ProfileImageSummary(user.ProfileImageUrl));
    }
}

public sealed record SessionSummary(string DisplayName, IReadOnlyList<string> Roles, Guid? AcademyId, bool IsPlatformOwner, string Workspace, string? ProfileImageUrl);
public sealed record ProfileImageSummary(string ProfileImageUrl);
