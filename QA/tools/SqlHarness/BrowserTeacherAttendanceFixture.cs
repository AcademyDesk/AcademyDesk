using System.Net;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private const string AttendanceBrowserEmail = "qa-attendance-browser@example.invalid";
    private sealed record AttendanceBrowserFixture(Guid Actor, Guid Teacher, Guid Session, Guid UnassignedSession, Guid Learner);

    private static async Task<AttendanceBrowserFixture> SeedBrowserTeacherAttendanceAsync(QaApiFactory factory, Guid academy)
    {
        var actor = await CreateAccessActorAsync(factory, AttendanceBrowserEmail, "Teacher", academy);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var teacher = new Teacher { AcademyId = academy, FirstName = "Browser", LastName = "Attendance teacher" };
        var other = new Teacher { AcademyId = academy, FirstName = "Browser", LastName = "Other teacher" };
        var batch = new Batch { AcademyId = academy, Name = "Browser Attendance Piano", CourseId = Guid.NewGuid(), TeacherId = other.Id };
        var day = DateTime.UtcNow.Date.AddDays(-1);
        var session = new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = teacher.Id,
            StartUtc = day.AddHours(9), EndUtc = day.AddHours(10), Status = "Scheduled" };
        var unassigned = new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = other.Id,
            StartUtc = day.AddHours(11), EndUtc = day.AddHours(12), Status = "Scheduled" };
        var learner = new Student { AcademyId = academy, FirstName = "Mira", LastName = "Attendance Test" };
        var outsider = new Student { AcademyId = academy, FirstName = "Mira", LastName = "Paused Test" };
        db.AddRange(teacher, other, batch, session, unassigned, learner, outsider,
            new Enrollment { AcademyId = academy, BatchId = batch.Id, StudentId = learner.Id, Status = "Active" },
            new Enrollment { AcademyId = academy, BatchId = batch.Id, StudentId = outsider.Id, Status = "Paused" });
        await db.SaveChangesAsync();
        (await identity.Users.SingleAsync(x => x.Id == actor)).TeacherId = teacher.Id;
        await identity.SaveChangesAsync();
        return new(actor, teacher.Id, session.Id, unassigned.Id, learner.Id);
    }

    private static async Task VerifyTeacherAttendanceBrowserOptionsAsync(QaApiFactory factory, Guid academy, AttendanceBrowserFixture f)
    {
        const string root = "/api/teacher/penta/attendance";
        using var teacher = await PentaBrowserSignIn(factory, AttendanceBrowserEmail);
        using var admin = await PentaBrowserSignIn(factory, "qa-admin-a@example.invalid");
        using var guest = factory.CreateClient();
        await PentaBrowserStatus(guest.GetAsync(root + "/readiness"), HttpStatusCode.Unauthorized, "anonymous teacher readiness");
        await PentaBrowserStatus(admin.GetAsync(root + "/readiness"), HttpStatusCode.Forbidden, "admin teacher readiness");
        var ready = await PentaBrowserRead<PentaTeacherAttendanceReadiness>(teacher.GetAsync(root + "/readiness"), HttpStatusCode.OK, "assigned teacher readiness");
        PentaRequire(ready.AcademyId == academy && ready.RequiresConfirmation, "Teacher readiness scope differs.");
        await PentaBrowserStatus(teacher.GetAsync(root + "/sessions?q=x"), HttpStatusCode.BadRequest, "short class lookup");
        var classes = await PentaBrowserRead<PentaTeacherAttendanceOptions<PentaTeacherAttendanceSession>>(
            teacher.GetAsync(root + "/sessions?q=Browser%20Attendance"), HttpStatusCode.OK, "assigned class lookup");
        PentaRequire(classes.AcademyId == academy && classes.SessionId is null && classes.Rows.Single().Id == f.Session,
            "Attendance options exposed a class assigned to another teacher.");
        await PentaBrowserStatus(teacher.GetAsync(root + $"/sessions/{f.UnassignedSession:D}/roster?q=Mira"), HttpStatusCode.Forbidden, "unassigned roster");
        var roster = await PentaBrowserRead<PentaTeacherAttendanceOptions<PentaTeacherAttendanceLearner>>(
            teacher.GetAsync(root + $"/sessions/{f.Session:D}/roster?q=Mira"), HttpStatusCode.OK, "active enrolled roster");
        PentaRequire(roster.AcademyId == academy && roster.SessionId == f.Session && roster.Rows.Single().Id == f.Learner,
            "Attendance options exposed an inactive enrollment.");
        Console.WriteLine("PENTA TEACHER OPTIONS SQL PASS: private readiness, assigned class, active roster and denied scopes.");
    }

    private static async Task VerifyBrowserTeacherAttendanceAsync(QaApiFactory factory, Guid academy, AttendanceBrowserFixture f)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var rows = await db.AttendanceRecords.AsNoTracking().Where(x => x.AcademyId == academy && x.ClassSessionId == f.Session).ToArrayAsync();
        var audit = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == f.Actor &&
            x.EntityId == f.Session && x.Action == "PentaTeacherAttendanceMarked").ToArrayAsync();
        var notices = await db.Notifications.AsNoTracking().Where(x => x.AcademyId == academy && x.RecipientId == f.Learner).ToArrayAsync();
        PentaRequire(rows.Length == 1 && rows[0].StudentId == f.Learner && rows[0].Status == "Absent" &&
            rows[0].Notes == "Browser attendance note" && audit.Length == 1 &&
            notices.Length == 1 && notices[0].Channel == "InApp" && notices[0].Status == "Queued" &&
            notices[0].Message == "Your attendance for the class has been marked Absent." &&
            !await db.AttendanceRecords.AnyAsync(x => x.ClassSessionId == f.UnassignedSession),
            "Teacher browser did not commit exactly one scoped mark, notice and audit.");
        Console.WriteLine("PENTA TEACHER BROWSER SQL PASS: one attendance row, queued notice and assigned-teacher audit.");
    }
}
