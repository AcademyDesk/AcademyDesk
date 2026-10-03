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

        var academy = await db.Academies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == academyId, token);
        if (academy is null) return NotFound();
        var since = DateTime.UtcNow.AddDays(-30);
        var totalInvoiced = await db.Invoices.Where(x => x.AcademyId == academyId).SumAsync(x => (decimal?)x.TotalAmount, token) ?? 0;
        var totalAdjusted = await db.Invoices.Where(x => x.AcademyId == academyId).SumAsync(x => (decimal?)x.AdjustedAmount, token) ?? 0;
        var totalPaid = await db.Payments.Where(x => x.AcademyId == academyId && (x.Status == "Completed" || x.Status == "Reconciled")).SumAsync(x => (decimal?)x.Amount, token) ?? 0;
        var attendance = await db.AttendanceRecords.Where(x => x.AcademyId == academyId && x.MarkedAtUtc >= since).Select(x => x.Status).ToListAsync(token);

        TimeZoneInfo timeZone;
        try { timeZone = TimeZoneInfo.FindSystemTimeZoneById(academy.TimeZone); }
        catch (TimeZoneNotFoundException) { timeZone = TimeZoneInfo.Utc; }
        catch (InvalidTimeZoneException) { timeZone = TimeZoneInfo.Utc; }
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date, timeZone);
        var tomorrowStartUtc = TimeZoneInfo.ConvertTimeToUtc(localNow.Date.AddDays(1), timeZone);
        var todaySchedule = await (
            from session in db.ClassSessions.AsNoTracking()
            join batch in db.Batches.AsNoTracking() on session.BatchId equals batch.Id
            join teacherJoin in db.Teachers.AsNoTracking() on session.TeacherId equals teacherJoin.Id into teachers
            from teacher in teachers.DefaultIfEmpty()
            where session.AcademyId == academyId
                && session.StartUtc >= todayStartUtc
                && session.StartUtc < tomorrowStartUtc
                && session.Status != "Cancelled"
            orderby session.StartUtc
            select new DashboardScheduleItem(session.Id, batch.Name, teacher == null ? null : teacher.FirstName + " " + teacher.LastName, session.StartUtc, session.EndUtc, session.Status, session.RoomName, session.DeliveryMode))
            .ToListAsync(token);
        var activity = await db.AuditLogs.AsNoTracking()
            .Where(x => x.AcademyId == academyId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(3)
            .Select(x => new DashboardActivityItem(x.Id, x.Action, x.EntityType, x.OccurredAtUtc))
            .ToListAsync(token);

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
            totalInvoiced - totalAdjusted - totalPaid,
            todaySchedule.Count,
            todaySchedule,
            activity));
    }
}

public sealed record DashboardSummary(int ActiveStudents, int ActiveTeachers, int ActiveCourses, int ActiveBatches, int OpenLeads, int AttendanceRecordsLast30Days, int PresentAttendanceLast30Days, decimal TotalInvoiced, decimal TotalPaid, decimal OutstandingBalance, int ClassesToday, IReadOnlyList<DashboardScheduleItem> TodaySchedule, IReadOnlyList<DashboardActivityItem> RecentActivity);
public sealed record DashboardScheduleItem(Guid Id, string BatchName, string? TeacherName, DateTime StartUtc, DateTime EndUtc, string Status, string? RoomName, string DeliveryMode);
public sealed record DashboardActivityItem(Guid Id, string Action, string EntityType, DateTime OccurredAtUtc);
