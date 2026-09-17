using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portal")]
public sealed class PortalController(UserManager<ApplicationUser> users, AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        if (user.StudentId.HasValue)
        {
            var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == user.StudentId && x.AcademyId == user.AcademyId, token);
            if (student is null) return Forbid();
            var enrollmentCount = await db.Enrollments.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id && x.Status == "Active", token);
            var invoiceCount = await db.Invoices.CountAsync(x => x.AcademyId == user.AcademyId && x.StudentId == student.Id, token);
            return Ok(new { role = "Student", displayName = $"{student.FirstName} {student.LastName}", studentId = student.Id, enrollmentCount, invoiceCount });
        }
        if (user.GuardianId.HasValue)
        {
            var children = await db.StudentGuardians.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId).Join(db.Students.AsNoTracking(), x => x.StudentId, s => s.Id, (x, s) => new { s.Id, name = s.FirstName + " " + s.LastName }).ToListAsync(token);
            return Ok(new { role = "Guardian", displayName = user.DisplayName, children });
        }
        return Forbid();
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<ActionResult<PortalStudentDetails>> Student(Guid studentId, CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user?.AcademyId is null) return Forbid();
        var permitted = user.StudentId == studentId ||
            (user.GuardianId.HasValue && await db.StudentGuardians.AnyAsync(x => x.AcademyId == user.AcademyId && x.GuardianId == user.GuardianId && x.StudentId == studentId, token));
        if (!permitted) return Forbid();
        var student = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == studentId && x.AcademyId == user.AcademyId, token);
        if (student is null) return NotFound();
        var enrollments = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId && x.Status == "Active").ToListAsync(token);
        var batchIds = enrollments.Select(x => x.BatchId).ToArray();
        var batches = await db.Batches.AsNoTracking().Where(x => batchIds.Contains(x.Id)).Select(x => new PortalBatch(x.Id, x.Name)).ToListAsync(token);
        var assignments = await db.Assignments.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && batchIds.Contains(x.BatchId) && x.IsPublished).OrderBy(x => x.DueAtUtc).Take(30).Select(x => new PortalAssignment(x.Title, x.Type, x.DueAtUtc)).ToListAsync(token);
        var attendance = await db.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.ClassSessions.AsNoTracking(), a => a.ClassSessionId, s => s.Id, (a, s) => new PortalAttendance(s.StartUtc, a.Status)).OrderByDescending(x => x.StartUtc).Take(30).ToListAsync(token);
        var music = await db.StudentMusicProgress.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).Join(db.MusicPieces.AsNoTracking(), p => p.MusicPieceId, piece => piece.Id, (p, piece) => new PortalMusicProgress(piece.Title, p.Status, p.TargetDate)).OrderBy(x => x.TargetDate).ToListAsync(token);
        var invoices = await db.Invoices.AsNoTracking().Where(x => x.AcademyId == user.AcademyId && x.StudentId == studentId).OrderByDescending(x => x.IssuedDate).ToListAsync(token);
        var invoiceIds = invoices.Select(x => x.Id).ToArray();
        var paid = await db.Payments.AsNoTracking().Where(x => invoiceIds.Contains(x.InvoiceId) && x.Status == "Completed").GroupBy(x => x.InvoiceId).Select(x => new { x.Key, Total = x.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.Key, x => x.Total, token);
        return Ok(new PortalStudentDetails($"{student.FirstName} {student.LastName}", batches, assignments, attendance, music, invoices.Select(x => new PortalInvoice(x.InvoiceNumber, x.TotalAmount, x.TotalAmount - paid.GetValueOrDefault(x.Id), x.Currency, x.DueDate, x.Status)).ToList()));
    }
}

public sealed record PortalStudentDetails(string Name, IReadOnlyList<PortalBatch> Batches, IReadOnlyList<PortalAssignment> Assignments, IReadOnlyList<PortalAttendance> Attendance, IReadOnlyList<PortalMusicProgress> Music, IReadOnlyList<PortalInvoice> Invoices);
public sealed record PortalBatch(Guid Id, string Name);
public sealed record PortalAssignment(string Title, string Type, DateTime? DueAtUtc);
public sealed record PortalAttendance(DateTime StartUtc, string Status);
public sealed record PortalMusicProgress(string Title, string Status, DateOnly? TargetDate);
public sealed record PortalInvoice(string InvoiceNumber, decimal TotalAmount, decimal Balance, string Currency, DateOnly DueDate, string Status);
