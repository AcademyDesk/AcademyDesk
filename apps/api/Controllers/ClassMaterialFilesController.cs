using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize(Policy = "PrivateMaterialRead")]
public sealed class ClassMaterialFilesController(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    ClassMaterialAccess access, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("/uploads/teacher-materials/{fileName}")]
    [HttpHead("/uploads/teacher-materials/{fileName}")]
    [HttpPost("/uploads/teacher-materials/{fileName}")]
    public Task<IActionResult> TeacherMaterial(string fileName, CancellationToken token) => Read("teacher-materials", fileName, token);

    [HttpGet("/uploads/learning-resources/{fileName}")]
    [HttpHead("/uploads/learning-resources/{fileName}")]
    [HttpPost("/uploads/learning-resources/{fileName}")]
    public Task<IActionResult> LearningMaterial(string fileName, CancellationToken token) => Read("learning-resources", fileName, token);

    private async Task<IActionResult> Read(string folder, string fileName, CancellationToken token)
    {
        if (HttpMethods.IsPost(Request.Method) && User.Identity?.AuthenticationType != ClassMaterialDownloadTickets.Scheme) return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // Both legacy uploaders generate GUID filenames. Do not accept filesystem
        // paths, URL parameters, active-content names or alternate separators.
        var extension = Path.GetExtension(fileName);
        if (fileName.Length > 50 || extension.Length < 2 || extension.Any(x => !char.IsAsciiLetterOrDigit(x) && x != '.') ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _) ||
            fileName.Any(x => !char.IsAsciiLetterOrDigit(x) && x != '.')) return NotFound();
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive) return Forbid();
        var url = $"/uploads/{folder}/{fileName}";
        // Require one current, same-tenant resource binding, even for known files.
        var resources = await db.LearningResources.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.Url == url)
            .Take(2).ToArrayAsync(token);
        if (resources.Length != 1 || !await access.CanReadAsync(user, (await users.GetRolesAsync(user)).ToArray(), resources[0], token))
            return NotFound();
        var path = Path.Combine(environment.WebRootPath, "uploads", folder, fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        // Never redirect to a public URL. Attachments cannot execute in this origin.
        // Framework range/HEAD handling runs only after current access checks.
        return PhysicalFile(path, "application/octet-stream", fileName, enableRangeProcessing: true);
    }
}
