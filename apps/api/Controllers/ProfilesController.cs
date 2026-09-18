using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}")]
public sealed class ProfilesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet("guardians/{guardianId:guid}/profile")]
    public async Task<ActionResult<GuardianProfileSummary>> Guardian(Guid academyId, Guid guardianId, CancellationToken token)
    {
        var guardian = await dbContext.Guardians.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == guardianId, token);
        if (guardian is null) return NotFound();
        var students = await (from link in dbContext.StudentGuardians.AsNoTracking()
                              join student in dbContext.Students.AsNoTracking() on link.StudentId equals student.Id
                              where link.AcademyId == academyId && link.GuardianId == guardianId
                              select new GuardianStudentSummary(student.Id, student.FirstName + " " + student.LastName, link.Relationship)).ToListAsync(token);
        var ids = students.Select(x => x.Id).ToArray();
        var invoices = await dbContext.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId && ids.Contains(x.StudentId)).OrderByDescending(x => x.DueDate).Take(15).Select(x => new GuardianInvoiceSummary(x.StudentId, x.InvoiceNumber, x.TotalAmount, x.Currency, x.DueDate, x.Status)).ToListAsync(token);
        var communications = await dbContext.Notifications.AsNoTracking().Where(x => x.AcademyId == academyId && x.RecipientId == guardianId).OrderByDescending(x => x.CreatedAtUtc).Take(12).Select(x => new CommunicationProfileSummary(x.Title, x.Channel, x.Status, x.CreatedAtUtc)).ToListAsync(token);
        return Ok(new GuardianProfileSummary(guardian.Id, guardian.FirstName + " " + guardian.LastName, guardian.Email, guardian.Phone, guardian.PreferredName, guardian.AddressLine1, guardian.City, guardian.State, guardian.PostalCode, guardian.PreferredLanguage, students, invoices, communications));
    }
    [HttpGet("students/{studentId:guid}/profile")]
    public async Task<ActionResult<StudentProfileSummary>> Student(Guid academyId, Guid studentId, CancellationToken token)
    {
        var student = await dbContext.Students.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == studentId, token);
        if (student is null) return NotFound();
        var guardians = await (from link in dbContext.StudentGuardians.AsNoTracking()
                               join guardian in dbContext.Guardians.AsNoTracking() on link.GuardianId equals guardian.Id
                               where link.AcademyId == academyId && link.StudentId == studentId
                               select new ContactSummary(guardian.Id, guardian.FirstName + " " + guardian.LastName, guardian.Email, guardian.Phone, link.Relationship)).ToListAsync(token);
        var enrolments = await (from enrolment in dbContext.Enrollments.AsNoTracking()
                                 join batch in dbContext.Batches.AsNoTracking() on enrolment.BatchId equals batch.Id
                                 join course in dbContext.Courses.AsNoTracking() on batch.CourseId equals course.Id
                                 where enrolment.AcademyId == academyId && enrolment.StudentId == studentId
                                 select new EnrollmentProfileSummary(batch.Name, course.Name, enrolment.Status, enrolment.StartDate, enrolment.EndDate)).ToListAsync(token);
        var attendance = await dbContext.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == academyId && x.StudentId == studentId).GroupBy(x => x.Status).Select(x => new StatusCount(x.Key, x.Count())).ToListAsync(token);
        var invoices = await dbContext.Invoices.AsNoTracking().Where(x => x.AcademyId == academyId && x.StudentId == studentId).OrderByDescending(x => x.DueDate).Take(10).Select(x => new InvoiceProfileSummary(x.InvoiceNumber, x.TotalAmount, x.Currency, x.DueDate, x.Status)).ToListAsync(token);
        var progress = await (from item in dbContext.StudentMusicProgress.AsNoTracking()
                              join piece in dbContext.MusicPieces.AsNoTracking() on item.MusicPieceId equals piece.Id
                              where item.AcademyId == academyId && item.StudentId == studentId
                              select new MusicProgressProfileSummary(piece.Title, piece.Instrument, item.Status, item.Score)).ToListAsync(token);
        var practice = await dbContext.PracticeLogs.AsNoTracking().Where(x => x.AcademyId == academyId && x.StudentId == studentId).OrderByDescending(x => x.PracticeDate).Take(10).Select(x => new PracticeProfileSummary(x.PracticeDate, x.MinutesPracticed, x.FocusArea, x.Notes, x.Status)).ToListAsync(token);
        var communications = await dbContext.Notifications.AsNoTracking().Where(x => x.AcademyId == academyId && x.RecipientId == studentId).OrderByDescending(x => x.CreatedAtUtc).Take(8).Select(x => new CommunicationProfileSummary(x.Title, x.Channel, x.Status, x.CreatedAtUtc)).ToListAsync(token);
        return Ok(new StudentProfileSummary(student.Id, student.FirstName + " " + student.LastName, student.Email, student.Phone, student.StudentNumber, student.PreferredName, student.Gender, student.DateOfBirth, student.AdmissionDate, student.AddressLine1, student.City, student.State, student.PostalCode, student.EmergencyContactName, student.EmergencyContactPhone, student.MedicalOrAccessibilityNotes, student.AdminNotes, guardians, enrolments, attendance, invoices, progress, practice, communications));
    }

    [HttpGet("teachers/{teacherId:guid}/profile")]
    public async Task<ActionResult<TeacherProfileSummary>> Teacher(Guid academyId, Guid teacherId, CancellationToken token)
    {
        var teacher = await dbContext.Teachers.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == teacherId, token);
        if (teacher is null) return NotFound();
        var batches = await (from batch in dbContext.Batches.AsNoTracking()
                             join course in dbContext.Courses.AsNoTracking() on batch.CourseId equals course.Id
                             where batch.AcademyId == academyId && batch.TeacherId == teacherId
                             select new TeacherAssignedBatchSummary(batch.Name, course.Name, batch.IsActive)).ToListAsync(token);
        var classes = await dbContext.ClassSessions.AsNoTracking().Where(x => x.AcademyId == academyId && x.TeacherId == teacherId).OrderByDescending(x => x.StartUtc).Take(12).Select(x => new TeacherClassSummary(x.StartUtc, x.EndUtc, x.DeliveryMode, x.RoomName, x.Status)).ToListAsync(token);
        var leave = await dbContext.LeaveRequests.AsNoTracking().Where(x => x.AcademyId == academyId && x.TeacherId == teacherId).OrderByDescending(x => x.StartDate).Take(8).Select(x => new LeaveProfileSummary(x.StartDate, x.EndDate, x.Status, x.Reason)).ToListAsync(token);
        return Ok(new TeacherProfileSummary(teacher.Id, teacher.FirstName + " " + teacher.LastName, teacher.Email, teacher.Phone, teacher.Specialties, teacher.EmployeeCode, teacher.PreferredName, teacher.EmploymentType, teacher.DateOfBirth, teacher.JoiningDate, teacher.Qualifications, teacher.AddressLine1, teacher.City, teacher.State, teacher.PostalCode, teacher.EmergencyContactName, teacher.EmergencyContactPhone, teacher.AdminNotes, batches, classes, leave));
    }

    [HttpPut("students/{studentId:guid}/profile")]
    public async Task<ActionResult<StudentProfileSummary>> UpdateStudentProfile(Guid academyId, Guid studentId, UpdateStudentAdminProfileRequest request, CancellationToken token)
    {
        var student = await dbContext.Students.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == studentId, token);
        if (student is null) return NotFound();
        var studentNumber = Clean(request.StudentNumber);
        if (studentNumber is not null && await dbContext.Students.AnyAsync(x => x.AcademyId == academyId && x.Id != studentId && x.StudentNumber == studentNumber, token))
            return Conflict(new { message = "That learner number is already in use." });
        student.StudentNumber = studentNumber;
        student.PreferredName = Clean(request.PreferredName);
        student.Gender = Clean(request.Gender);
        student.DateOfBirth = request.DateOfBirth;
        student.AdmissionDate = request.AdmissionDate;
        student.AddressLine1 = Clean(request.AddressLine1);
        student.City = Clean(request.City);
        student.State = Clean(request.State);
        student.PostalCode = Clean(request.PostalCode);
        student.EmergencyContactName = Clean(request.EmergencyContactName);
        student.EmergencyContactPhone = Clean(request.EmergencyContactPhone);
        student.MedicalOrAccessibilityNotes = Clean(request.MedicalOrAccessibilityNotes);
        student.AdminNotes = Clean(request.AdminNotes);
        await dbContext.SaveChangesAsync(token);
        return await Student(academyId, studentId, token);
    }

    [HttpPut("teachers/{teacherId:guid}/profile")]
    public async Task<ActionResult<TeacherProfileSummary>> UpdateTeacherProfile(Guid academyId, Guid teacherId, UpdateTeacherAdminProfileRequest request, CancellationToken token)
    {
        var teacher = await dbContext.Teachers.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == teacherId, token);
        if (teacher is null) return NotFound();
        var employeeCode = Clean(request.EmployeeCode);
        if (employeeCode is not null && await dbContext.Teachers.AnyAsync(x => x.AcademyId == academyId && x.Id != teacherId && x.EmployeeCode == employeeCode, token))
            return Conflict(new { message = "That employee code is already in use." });
        teacher.EmployeeCode = employeeCode;
        teacher.PreferredName = Clean(request.PreferredName);
        teacher.EmploymentType = Clean(request.EmploymentType);
        teacher.DateOfBirth = request.DateOfBirth;
        teacher.JoiningDate = request.JoiningDate;
        teacher.Qualifications = Clean(request.Qualifications);
        teacher.AddressLine1 = Clean(request.AddressLine1);
        teacher.City = Clean(request.City);
        teacher.State = Clean(request.State);
        teacher.PostalCode = Clean(request.PostalCode);
        teacher.EmergencyContactName = Clean(request.EmergencyContactName);
        teacher.EmergencyContactPhone = Clean(request.EmergencyContactPhone);
        teacher.AdminNotes = Clean(request.AdminNotes);
        await dbContext.SaveChangesAsync(token);
        return await Teacher(academyId, teacherId, token);
    }

    [HttpPut("guardians/{guardianId:guid}/profile")]
    public async Task<ActionResult<GuardianProfileSummary>> UpdateGuardianProfile(Guid academyId, Guid guardianId, UpdateGuardianAdminProfileRequest request, CancellationToken token)
    {
        var guardian = await dbContext.Guardians.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == guardianId, token);
        if (guardian is null) return NotFound();
        guardian.PreferredName = Clean(request.PreferredName);
        guardian.AddressLine1 = Clean(request.AddressLine1);
        guardian.City = Clean(request.City);
        guardian.State = Clean(request.State);
        guardian.PostalCode = Clean(request.PostalCode);
        guardian.PreferredLanguage = Clean(request.PreferredLanguage);
        await dbContext.SaveChangesAsync(token);
        return await Guardian(academyId, guardianId, token);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record ContactSummary(Guid Id, string Name, string? Email, string? Phone, string? Relationship);
public sealed record EnrollmentProfileSummary(string BatchName, string CourseName, string Status, DateOnly StartDate, DateOnly? EndDate);
public sealed record StatusCount(string Status, int Count);
public sealed record InvoiceProfileSummary(string InvoiceNumber, decimal TotalAmount, string Currency, DateOnly DueDate, string Status);
public sealed record MusicProgressProfileSummary(string Title, string? Instrument, string Status, decimal? Score);
public sealed record PracticeProfileSummary(DateOnly PracticeDate, int MinutesPracticed, string? FocusArea, string? Notes, string Status);
public sealed record CommunicationProfileSummary(string Title, string Channel, string Status, DateTime CreatedAtUtc);
public sealed record StudentProfileSummary(Guid Id, string Name, string? Email, string? Phone, string? StudentNumber, string? PreferredName, string? Gender, DateOnly? DateOfBirth, DateOnly? AdmissionDate, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? MedicalOrAccessibilityNotes, string? AdminNotes, IReadOnlyList<ContactSummary> Guardians, IReadOnlyList<EnrollmentProfileSummary> Enrollments, IReadOnlyList<StatusCount> Attendance, IReadOnlyList<InvoiceProfileSummary> Invoices, IReadOnlyList<MusicProgressProfileSummary> MusicProgress, IReadOnlyList<PracticeProfileSummary> PracticeLogs, IReadOnlyList<CommunicationProfileSummary> Communications);
public sealed record GuardianStudentSummary(Guid Id, string Name, string? Relationship);
public sealed record GuardianInvoiceSummary(Guid StudentId, string InvoiceNumber, decimal TotalAmount, string Currency, DateOnly DueDate, string Status);
public sealed record GuardianProfileSummary(Guid Id, string Name, string? Email, string? Phone, string? PreferredName, string? AddressLine1, string? City, string? State, string? PostalCode, string? PreferredLanguage, IReadOnlyList<GuardianStudentSummary> Students, IReadOnlyList<GuardianInvoiceSummary> Invoices, IReadOnlyList<CommunicationProfileSummary> Communications);
public sealed record TeacherAssignedBatchSummary(string BatchName, string CourseName, bool IsActive);
public sealed record TeacherClassSummary(DateTime StartUtc, DateTime EndUtc, string DeliveryMode, string? RoomName, string Status);
public sealed record LeaveProfileSummary(DateOnly StartDate, DateOnly EndDate, string Status, string Reason);
public sealed record TeacherProfileSummary(Guid Id, string Name, string? Email, string? Phone, string? Specialties, string? EmployeeCode, string? PreferredName, string? EmploymentType, DateOnly? DateOfBirth, DateOnly? JoiningDate, string? Qualifications, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? AdminNotes, IReadOnlyList<TeacherAssignedBatchSummary> Batches, IReadOnlyList<TeacherClassSummary> Classes, IReadOnlyList<LeaveProfileSummary> LeaveRequests);
public sealed record UpdateStudentAdminProfileRequest(string? StudentNumber, string? PreferredName, string? Gender, DateOnly? DateOfBirth, DateOnly? AdmissionDate, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? MedicalOrAccessibilityNotes, string? AdminNotes);
public sealed record UpdateTeacherAdminProfileRequest(string? EmployeeCode, string? PreferredName, string? EmploymentType, DateOnly? DateOfBirth, DateOnly? JoiningDate, string? Qualifications, string? AddressLine1, string? City, string? State, string? PostalCode, string? EmergencyContactName, string? EmergencyContactPhone, string? AdminNotes);
public sealed record UpdateGuardianAdminProfileRequest(string? PreferredName, string? AddressLine1, string? City, string? State, string? PostalCode, string? PreferredLanguage);
