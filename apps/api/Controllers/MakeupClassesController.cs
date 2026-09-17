using AcademyDesk.Api.Data;using AcademyDesk.Api.Domain.Entities;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/makeup-classes")]
public sealed class MakeupClassesController(AcademyDeskDbContext db):ControllerBase
{
 [HttpGet]public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.MakeupClasses.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderBy(x=>x.StartUtc).Select(x=>new MakeupSummary(x.Id,x.StudentId,x.BatchId,x.TeacherId,x.StartUtc,x.EndUtc,x.Venue,x.Status,x.Notes)).ToListAsync(t));
 [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateMakeupRequest r,CancellationToken t){if(r.EndUtc<=r.StartUtc)return BadRequest(new{message="End time must be after start time."});if(!await db.Students.AnyAsync(x=>x.Id==r.StudentId&&x.AcademyId==academyId,t)||!await db.Batches.AnyAsync(x=>x.Id==r.BatchId&&x.AcademyId==academyId,t))return BadRequest(new{message="Student or batch is invalid."});if(r.TeacherId.HasValue&&!await db.Teachers.AnyAsync(x=>x.Id==r.TeacherId&&x.AcademyId==academyId,t))return BadRequest(new{message="Teacher is invalid."});var x=new MakeupClass{AcademyId=academyId,StudentId=r.StudentId,BatchId=r.BatchId,TeacherId=r.TeacherId,StartUtc=r.StartUtc,EndUtc=r.EndUtc,Venue=r.Venue?.Trim(),Notes=r.Notes?.Trim()};db.MakeupClasses.Add(x);await db.SaveChangesAsync(t);return Ok();}
}
public sealed record CreateMakeupRequest(Guid StudentId,Guid BatchId,Guid? TeacherId,DateTime StartUtc,DateTime EndUtc,string? Venue,string? Notes);public sealed record MakeupSummary(Guid Id,Guid StudentId,Guid BatchId,Guid? TeacherId,DateTime StartUtc,DateTime EndUtc,string? Venue,string Status,string? Notes);
