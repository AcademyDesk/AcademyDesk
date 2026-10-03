using AcademyDesk.Api.Data;using AcademyDesk.Api.Domain.Entities;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController][Route("api/academies/{academyId:guid}/resources")]
public sealed class LearningResourcesController(AcademyDeskDbContext db):ControllerBase
{
 [HttpGet]public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.LearningResources.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new ResourceSummary(x.Id,x.Title,x.Description,x.Type,x.Url,x.BatchId,x.CourseId,x.IsPublished)).ToListAsync(t));
 [HttpPost]public async Task<ActionResult> Create(Guid academyId,CreateResourceRequest r,CancellationToken t){if(string.IsNullOrWhiteSpace(r.Title)||!Uri.TryCreate(r.Url,UriKind.Absolute,out _))return BadRequest(new{message="Title and a valid full URL are required."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var x=new LearningResource{AcademyId=academyId,Title=r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Link":r.Type.Trim(),Url=r.Url.Trim(),BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished};db.LearningResources.Add(x);await db.SaveChangesAsync(t);return Ok();}
 [HttpPost("upload")][RequestSizeLimit(50_000_000)]public async Task<ActionResult> Upload(Guid academyId,[FromForm]UploadResourceRequest r,IWebHostEnvironment environment,CancellationToken t){if(r.File is null||r.File.Length==0)return BadRequest(new{message="Choose a file to upload."});var problem=await ValidateScope(academyId,r.BatchId,r.CourseId,t);if(problem is not null)return BadRequest(new{message=problem});var allowed=new[]{".pdf",".doc",".docx",".jpg",".jpeg",".png",".webp",".mp3",".wav",".mp4"};var ext=Path.GetExtension(r.File.FileName).ToLowerInvariant();if(!allowed.Contains(ext))return BadRequest(new{message="Upload a PDF, document, image, audio, or video file."});var folder=Path.Combine(environment.WebRootPath,"uploads","learning-resources");Directory.CreateDirectory(folder);var fileName=$"{Guid.NewGuid():N}{ext}";await using(var stream=System.IO.File.Create(Path.Combine(folder,fileName))){await r.File.CopyToAsync(stream,t);}db.LearningResources.Add(new LearningResource{AcademyId=academyId,Title=string.IsNullOrWhiteSpace(r.Title)?Path.GetFileNameWithoutExtension(r.File.FileName):r.Title.Trim(),Description=r.Description?.Trim(),Type=string.IsNullOrWhiteSpace(r.Type)?"Document":r.Type.Trim(),Url=$"/uploads/learning-resources/{fileName}",BatchId=r.BatchId,CourseId=r.CourseId,IsPublished=r.IsPublished});await db.SaveChangesAsync(t);return Ok();}
 [HttpPatch("{resourceId:guid}/publish")]public async Task<ActionResult> Publish(Guid academyId,Guid resourceId,PublishResourceRequest r,CancellationToken t){var x=await db.LearningResources.SingleOrDefaultAsync(v=>v.Id==resourceId&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.IsPublished=r.IsPublished;await db.SaveChangesAsync(t);return Ok();}
 private async Task<string?> ValidateScope(Guid academyId,Guid? batchId,Guid? courseId,CancellationToken t)
 {
     Guid? batchCourseId = null;
     if (batchId.HasValue)
     {
         batchCourseId = await db.Batches.AsNoTracking()
             .Where(x => x.Id == batchId.Value && x.AcademyId == academyId)
             .Select(x => (Guid?)x.CourseId).SingleOrDefaultAsync(t);
         if (!batchCourseId.HasValue) return "Invalid batch.";
     }
     if (courseId.HasValue && !await db.Courses.AnyAsync(x => x.Id == courseId.Value && x.AcademyId == academyId, t))
         return "Invalid subject.";
     if (batchId.HasValue && courseId.HasValue && batchCourseId != courseId)
         return "The selected subject does not belong to the selected batch.";
     return null;
 }
}
public sealed record CreateResourceRequest(string Title,string? Description,string? Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished);public sealed record ResourceSummary(Guid Id,string Title,string? Description,string Type,string Url,Guid? BatchId,Guid? CourseId,bool IsPublished);public sealed record PublishResourceRequest(bool IsPublished);public sealed class UploadResourceRequest{public string? Title{get;set;}public string? Description{get;set;}public string? Type{get;set;}public Guid? BatchId{get;set;}public Guid? CourseId{get;set;}public bool IsPublished{get;set;}=true;public IFormFile? File{get;set;}}
