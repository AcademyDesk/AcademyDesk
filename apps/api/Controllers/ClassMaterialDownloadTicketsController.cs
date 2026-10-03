using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize, Route("api/class-material-downloads")]
public sealed class ClassMaterialDownloadTicketsController(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    ClassMaterialAccess access, ClassMaterialDownloadTickets tickets, IMediaBlobStore store, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("tickets")]
    public async Task<IActionResult> Create(DownloadRequest request, CancellationToken token)
    {
        Response.Headers.CacheControl = "private, no-store";
        var origin = Request.Headers.Origin.ToString();
        if (!tickets.IsSafeTransport(Request) || !tickets.IsApprovedOrigin(origin) || !ClassMaterialDownloadTickets.IsContentPath(request.Path)) return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive) return Forbid();
        var resources = await db.LearningResources.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.Url == request.Path).Take(2).ToArrayAsync(token);
        if (resources.Length != 1 || !await access.CanReadAsync(user, (await users.GetRolesAsync(user)).ToArray(), resources[0], token)) return NotFound();
        var resource = resources[0];
        if (request.Path.StartsWith("/api/class-media/", StringComparison.Ordinal))
        {
            if (request.Path != $"/api/class-media/{resource.Id}/content" || !await db.ClassMediaUploadSessions.AnyAsync(x => x.Id == resource.Id && x.AcademyId == resource.AcademyId && x.CompletedAtUtc != null, token)) return NotFound();
            if (!store.IsConfigured) return StatusCode(503);
        }
        else if (!System.IO.File.Exists(Path.Combine(environment.WebRootPath, request.Path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)))) return NotFound();
        return Ok(new { ticket = tickets.Create(user, request.Path, origin), expiresInSeconds = ClassMaterialDownloadTickets.LifetimeSeconds });
    }
    public sealed record DownloadRequest(string Path);
}
