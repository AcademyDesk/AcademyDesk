using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace AcademyDesk.Api.Controllers;

[ApiController, Authorize(Policy = "PrivateMaterialRead"), Route("api/class-media")]
public sealed class ClassMediaContentController(AcademyDeskDbContext db, UserManager<ApplicationUser> users,
    ClassMaterialAccess access, IMediaBlobStore store) : ControllerBase
{
    [HttpGet("{resourceId:guid}/content"), HttpHead("{resourceId:guid}/content"), HttpPost("{resourceId:guid}/content")]
    public async Task<IActionResult> Read(Guid resourceId, CancellationToken token)
    {
        // Native downloads must not accept a normal account token POST as a
        // substitute for the path/origin-bound short-lived read credential.
        if (HttpMethods.IsPost(Request.Method) && User.Identity?.AuthenticationType != ClassMaterialDownloadTickets.Scheme) return Unauthorized();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var user = await users.GetUserAsync(User);
        if (user is null || !user.IsActive) return Forbid();
        var resource = await db.LearningResources.AsNoTracking().SingleOrDefaultAsync(x => x.Id == resourceId && x.AcademyId == user.AcademyId, token);
        if (resource is null || resource.Url != $"/api/class-media/{resourceId}/content" ||
            !await access.CanReadAsync(user, (await users.GetRolesAsync(user)).ToArray(), resource, token) ||
            !await db.ClassMediaUploadSessions.AnyAsync(x => x.Id == resourceId && x.AcademyId == resource.AcademyId && x.CompletedAtUtc != null, token)) return NotFound();
        if (!store.IsConfigured) return StatusCode(503, new { message = "Private media storage is unavailable. Retry later." });
        try
        {
            var info = await store.PropertiesAsync(resource.AcademyId, resource.Id, token);
            long offset = 0, length = info.Length;
            if (!HttpMethods.IsHead(Request.Method) && !Request.Headers.ContainsKey(HeaderNames.IfRange) && Request.Headers.TryGetValue(HeaderNames.Range, out var requested))
            {
                if (!RangeHeaderValue.TryParse(requested.ToString(), out var range) || range.Unit != "bytes" || range.Ranges.Count != 1)
                    return InvalidRange(info.Length);
                var value = range.Ranges.Single();
                if (value.From.HasValue)
                {
                    offset = value.From.Value;
                    var end = Math.Min(value.To ?? (info.Length - 1), info.Length - 1);
                    if (offset >= info.Length || end < offset) return InvalidRange(info.Length);
                    length = end - offset + 1;
                }
                else
                {
                    if (value.To is null or <= 0) return InvalidRange(info.Length);
                    length = Math.Min(value.To.Value, info.Length); offset = info.Length - length;
                }
                Response.StatusCode = 206;
                Response.Headers.ContentRange = $"bytes {offset}-{offset + length - 1}/{info.Length}";
            }
            Response.Headers.AcceptRanges = "bytes";
            Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileNameStar = info.FileName }.ToString();
            if (HttpMethods.IsHead(Request.Method))
            {
                Response.ContentType = "application/octet-stream"; Response.ContentLength = info.Length;
                return new EmptyResult();
            }
            var read = await store.ReadAsync(resource.AcademyId, resource.Id, offset, length, token);
            Response.ContentLength = read.Length;
            return File(read.Content, "application/octet-stream", info.FileName);
        }
        catch (RequestFailedException e) when (e.Status == 404) { return NotFound(); }
        catch (RequestFailedException) { return StatusCode(503, new { message = "Private media storage is unavailable. Retry later." }); }
        catch (InvalidOperationException) { return StatusCode(503, new { message = "Private media storage is unavailable. Retry later." }); }
    }
    private StatusCodeResult InvalidRange(long total)
    {
        Response.Headers.ContentRange = $"bytes */{total}";
        return StatusCode(416);
    }
}
