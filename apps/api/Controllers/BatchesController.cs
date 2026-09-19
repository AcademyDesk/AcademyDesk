using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/batches")]
public sealed class BatchesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BatchSummary>>> List(Guid academyId, CancellationToken token)
    {
        var batches = await dbContext.Batches.AsNoTracking().Where(x => x.AcademyId == academyId).OrderBy(x => x.Name).ToListAsync(token);
        var counts = await dbContext.Enrollments.AsNoTracking().Where(x => x.AcademyId == academyId && x.Status == "Active").GroupBy(x => x.BatchId).Select(x => new { BatchId = x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.BatchId, x => x.Count, token);
        var rows = batches.Select(x => Summary(x, counts.GetValueOrDefault(x.Id))).ToArray();
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<BatchSummary>> Create(Guid academyId, CreateBatchRequest request, CancellationToken token)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound();
        var problem = await Validate(academyId, request, token); if (problem is not null) return BadRequest(new { message = problem });
        var studentIds = (request.StudentIds ?? []).Distinct().ToArray();
        if (studentIds.Length > request.Capacity) return BadRequest(new { message = "Selected students cannot exceed batch capacity." });
        if (studentIds.Length > 0)
        {
            var validStudentCount = await dbContext.Students.CountAsync(x => x.AcademyId == academyId && studentIds.Contains(x.Id) && x.IsActive, token);
            if (validStudentCount != studentIds.Length) return BadRequest(new { message = "One or more selected students are inactive or do not belong to this academy." });
        }
        var batch = new Batch { AcademyId = academyId, Name = request.Name.Trim() };
        Apply(batch, request);
        dbContext.Batches.Add(batch);
        foreach (var studentId in studentIds)
            dbContext.Enrollments.Add(new Enrollment { AcademyId = academyId, StudentId = studentId, BatchId = batch.Id, StartDate = request.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active" });
        await dbContext.SaveChangesAsync(token);
        return Created($"/api/academies/{academyId}/batches/{batch.Id}", Summary(batch, studentIds.Length));
    }

    [HttpPut("{batchId:guid}")]
    public async Task<ActionResult<BatchSummary>> Update(Guid academyId, Guid batchId, UpdateBatchRequest request, CancellationToken token)
    {
        var batch = await dbContext.Batches.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == batchId, token); if (batch is null) return NotFound();
        var problem = await Validate(academyId, request, token, batchId); if (problem is not null) return BadRequest(new { message = problem });
        var active = await dbContext.Enrollments.CountAsync(x => x.AcademyId == academyId && x.BatchId == batchId && x.Status == "Active", token); if (request.Capacity < active) return BadRequest(new { message = $"Capacity cannot be lower than the {active} active enrolments." });
        Apply(batch, request); batch.IsActive = request.IsActive; await dbContext.SaveChangesAsync(token); return Ok(Summary(batch, active));
    }

    private async Task<string?> Validate(Guid academyId, BatchRequest r, CancellationToken token, Guid? ignore = null)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.Capacity is < 1 or > 1000 || r.WaitlistCapacity is < 0 or > 1000) return "Enter a batch name, capacity between 1 and 1000, and a valid waitlist capacity.";
        if (r.EndDate.HasValue && r.StartDate.HasValue && r.EndDate < r.StartDate) return "End date cannot be earlier than the start date.";
        if (!new[] { "InPerson", "Online", "Hybrid" }.Contains((r.DeliveryMode ?? "InPerson").Replace(" ", ""), StringComparer.OrdinalIgnoreCase)) return "Delivery mode must be InPerson, Online, or Hybrid.";
        if ((r.DeliveryMode ?? "").Replace(" ", "") is "Online" or "Hybrid" && string.IsNullOrWhiteSpace(r.MeetingLink)) return "A meeting link is required for online and hybrid classes.";
        if (!string.IsNullOrWhiteSpace(r.MeetingDaysJson))
        {
            try
            {
                var sessions = JsonSerializer.Deserialize<List<BatchMeetingTime>>(r.MeetingDaysJson);
                var validDays = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
                if (sessions is null || sessions.Count == 0 || sessions.Any(x => !validDays.Contains(x.Day, StringComparer.OrdinalIgnoreCase) || !TimeOnly.TryParse(x.StartTime, out _)))
                    return "Select a valid class time for every teaching day.";
            }
            catch (JsonException) { return "Class times could not be read. Please select the teaching days again."; }
        }
        if (!new[] { "Open", "Waitlist", "Closed" }.Contains((r.EnrollmentStatus ?? "Open").Trim(), StringComparer.OrdinalIgnoreCase)) return "Enrolment status must be Open, Waitlist, or Closed.";
        if (!await dbContext.Courses.AnyAsync(x => x.Id == r.CourseId && x.AcademyId == academyId, token)) return "The selected course does not belong to this academy.";
        if (r.BranchId.HasValue && !await dbContext.Branches.AnyAsync(x => x.Id == r.BranchId && x.AcademyId == academyId, token)) return "The selected branch does not belong to this academy.";
        if (r.TeacherId.HasValue && !await dbContext.Teachers.AnyAsync(x => x.Id == r.TeacherId && x.AcademyId == academyId, token)) return "The selected teacher does not belong to this academy.";
        var code = Clean(r.BatchCode); if (code is not null && await dbContext.Batches.AnyAsync(x => x.AcademyId == academyId && x.Id != ignore && x.BatchCode == code, token)) return "That batch code is already in use.";
        return null;
    }
    private static void Apply(Batch x, BatchRequest r) { x.Name = r.Name.Trim(); x.BatchCode = Clean(r.BatchCode); x.CourseId = r.CourseId; x.TeacherId = r.TeacherId; x.BranchId = r.BranchId; x.Capacity = r.Capacity; x.WaitlistCapacity = r.WaitlistCapacity; x.ClassType = r.ClassType ?? "Group"; x.SessionMinutes = r.SessionMinutes ?? 60; x.SessionsPerWeek = r.SessionsPerWeek ?? 1; x.MeetingDaysJson = Clean(r.MeetingDaysJson); x.MeetingLink = Clean(r.MeetingLink); x.DeliveryMode = string.IsNullOrWhiteSpace(r.DeliveryMode) ? "InPerson" : r.DeliveryMode.Replace(" ", "").Trim(); x.MeetingPattern = Clean(r.MeetingPattern); x.RoomName = Clean(r.RoomName); x.EnrollmentStatus = string.IsNullOrWhiteSpace(r.EnrollmentStatus) ? "Open" : r.EnrollmentStatus.Trim(); x.AdminNotes = Clean(r.AdminNotes); x.StartDate = r.StartDate; x.EndDate = r.EndDate; }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static BatchSummary Summary(Batch x, int active) => new(x.Id, x.Name, x.BatchCode, x.CourseId, x.TeacherId, x.BranchId, x.Capacity, x.WaitlistCapacity, x.DeliveryMode, x.MeetingPattern, x.RoomName, x.EnrollmentStatus, x.AdminNotes, x.StartDate, x.EndDate, x.IsActive, active);
}

public abstract record BatchRequest(string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string? DeliveryMode, string? MeetingPattern, string? RoomName, string? EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, string? ClassType = null, int? SessionMinutes = null, int? SessionsPerWeek = null, string? MeetingDaysJson = null, string? MeetingLink = null, IReadOnlyList<Guid>? StudentIds = null);
public sealed record CreateBatchRequest(string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string? DeliveryMode, string? MeetingPattern, string? RoomName, string? EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, string? ClassType = null, int? SessionMinutes = null, int? SessionsPerWeek = null, string? MeetingDaysJson = null, string? MeetingLink = null, IReadOnlyList<Guid>? StudentIds = null) : BatchRequest(Name, BatchCode, CourseId, TeacherId, BranchId, Capacity, WaitlistCapacity, DeliveryMode, MeetingPattern, RoomName, EnrollmentStatus, AdminNotes, StartDate, EndDate, ClassType, SessionMinutes, SessionsPerWeek, MeetingDaysJson, MeetingLink, StudentIds);
public sealed record UpdateBatchRequest(string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string? DeliveryMode, string? MeetingPattern, string? RoomName, string? EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, bool IsActive) : BatchRequest(Name, BatchCode, CourseId, TeacherId, BranchId, Capacity, WaitlistCapacity, DeliveryMode, MeetingPattern, RoomName, EnrollmentStatus, AdminNotes, StartDate, EndDate);
public sealed record BatchMeetingTime(string Day, string StartTime);
public sealed record BatchSummary(Guid Id, string Name, string? BatchCode, Guid CourseId, Guid? TeacherId, Guid? BranchId, int Capacity, int WaitlistCapacity, string DeliveryMode, string? MeetingPattern, string? RoomName, string EnrollmentStatus, string? AdminNotes, DateOnly? StartDate, DateOnly? EndDate, bool IsActive, int ActiveEnrolments);
