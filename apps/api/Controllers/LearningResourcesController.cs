using AcademyDesk.Api.Data;using AcademyDesk.Api.Domain.Entities;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/resources")]
public sealed class LearningResourcesController(AcademyDeskDbContext db):ControllerBase
{
 [HttpGet]public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.LearningResources.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new ResourceSummary(x.Id,x.Title,x.Description,x.Type,x.Url,x.BatchId,x.IsPublished)).ToListAsync(t));
 [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateResourceRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)||!Uri.TryCreate(r.Url,UriKind.Absolute,out _))return BadRequest(new{message="Title and a valid URL are required."});if(r.BatchId.HasValue&&!await db.Batches.AnyAsync(x=>x.Id==r.BatchId&&x.AcademyId==academyId,t))return BadRequest(new{message="Invalid batch."});var x=new LearningResource{AcademyId=academyId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Link":r.Type.Trim(),Url=r.Url.Trim(),BatchId=r.BatchId,IsPublished=r.IsPublished};db.LearningResources.Add(x);await db.SaveChangesAsync(t);return Ok();}
}
public sealed record CreateResourceRequest(string Title,string? Description,string? Type,string Url,Guid? BatchId,bool IsPublished);public sealed record ResourceSummary(Guid Id,string Title,string? Description,string Type,string Url,Guid? BatchId,bool IsPublished);
