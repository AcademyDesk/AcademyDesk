using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace AcademyDesk.Api.Controllers;
[ApiController]
[Route("api/academies/{academyId:guid}/leave-requests")]
public sealed class LeaveRequestsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet] public async Task<ActionResult> List(Guid academyId,CancellationToken t)=>Ok(await db.LeaveRequests.AsNoTracking().Where(x=>x.AcademyId==academyId).OrderByDescending(x=>x.CreatedAtUtc).Select(x=>new LeaveSummary(x.Id,x.RequesterType,x.StudentId,x.TeacherId,x.StartDate,x.EndDate,x.Reason,x.Status,x.DecisionNotes)).ToListAsync(t));
    [HttpPost]
    public async Task<ActionResult> Create(Guid academyId, CreateLeaveRequest r, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(r.Reason) || r.EndDate < r.StartDate)
            return BadRequest(new { message = "Reason and valid dates are required." });

        var type = r.RequesterType?.Trim();
        var requesterType = string.Equals(type, "Student", StringComparison.OrdinalIgnoreCase) ? "Student"
            : string.Equals(type, "Teacher", StringComparison.OrdinalIgnoreCase) ? "Teacher" : null;
        if (requesterType is null)
            return BadRequest(new { message = "Requester type must be Student or Teacher." });

        if (requesterType == "Student" && (r.TeacherId.HasValue || !r.StudentId.HasValue ||
            !await db.Students.AnyAsync(x => x.Id == r.StudentId && x.AcademyId == academyId, t)))
            return BadRequest(new { message = "Select one valid student from this academy and do not include a teacher." });
        if (requesterType == "Teacher" && (r.StudentId.HasValue || !r.TeacherId.HasValue ||
            !await db.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, t)))
            return BadRequest(new { message = "Select one valid teacher from this academy and do not include a student." });

        var request = new LeaveRequest { AcademyId = academyId, RequesterType = requesterType,
            StudentId = r.StudentId, TeacherId = r.TeacherId, StartDate = r.StartDate,
            EndDate = r.EndDate, Reason = r.Reason.Trim() };
        db.LeaveRequests.Add(request);
        await db.SaveChangesAsync(t);
        return Ok();
    }
    [HttpPatch("{id:guid}")] public async Task<ActionResult> Decide(Guid academyId,Guid id,DecideLeaveRequest r,CancellationToken t){if(r.Status is not("Approved" or "Rejected"))return BadRequest();var x=await db.LeaveRequests.SingleOrDefaultAsync(v=>v.Id==id&&v.AcademyId==academyId,t);if(x is null)return NotFound();x.Status=r.Status;x.DecisionNotes=r.Notes?.Trim();await db.SaveChangesAsync(t);return Ok();}
}
public sealed record CreateLeaveRequest(string RequesterType,Guid? StudentId,Guid? TeacherId,DateOnly StartDate,DateOnly EndDate,string Reason);
public sealed record DecideLeaveRequest(string Status,string? Notes);
public sealed record LeaveSummary(Guid Id,string RequesterType,Guid? StudentId,Guid? TeacherId,DateOnly StartDate,DateOnly EndDate,string Reason,string Status,string? DecisionNotes);
