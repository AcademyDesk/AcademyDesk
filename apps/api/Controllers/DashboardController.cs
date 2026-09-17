using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/dashboard")]
public sealed class DashboardController(AcademyDeskDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummary>> Summary(Guid academyId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId != academyId) return Forbid();
        var since = DateTime.UtcNow.AddDays(-30);
        var totalInvoiced = await db.Invoices.Where(x => x.AcademyId == academyId).SumAsync(x => (decimal?)x.TotalAmount, token) ?? 0;
        var totalPaid = await db.Payments.Where(x => x.AcademyId == academyId && x.Status == "Completed").SumAsync(x => (decimal?)x.Amount, token) ?? 0;
        var attendance = await db.AttendanceRecords.Where(x => x.AcademyId == academyId && x.MarkedAtUtc >= since).Select(x => x.Status).ToListAsync(token);
        return Ok(new DashboardSummary(
            await db.Students.CountAsync(x => x.AcademyId == academyId && x.IsActive, token),
            await db.Teachers.CountAsync(x => x.AcademyId == academyId && x.IsActive, token),
            await db.Courses.CountAsync(x => x.AcademyId == academyId && x.IsActive, token),
            await db.Batches.CountAsync(x => x.AcademyId == academyId && x.IsActive, token),
            await db.Leads.CountAsync(x => x.AcademyId == academyId && x.Stage != "Converted" && x.Stage != "Lost", token),
            attendance.Count,
            attendance.Count(x => x is "Present" or "Online"),
            totalInvoiced,
            totalPaid,
            totalInvoiced - totalPaid));
    }
}

public sealed record DashboardSummary(int ActiveStudents, int ActiveTeachers, int ActiveCourses, int ActiveBatches, int OpenLeads, int AttendanceRecordsLast30Days, int PresentAttendanceLast30Days, decimal TotalInvoiced, decimal TotalPaid, decimal OutstandingBalance);
