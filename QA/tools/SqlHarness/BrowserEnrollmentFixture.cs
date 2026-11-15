using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static readonly DateOnly BrowserEnrollmentDate = new(2027, 1, 1);
    private sealed record BrowserEnrollmentFixture(Guid Actor, Guid Student, Guid Batch, string Status);

    private static async Task<BrowserEnrollmentFixture[]> SeedBrowserEnrollmentAsync(QaApiFactory factory, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var course = new ProgramCourse { AcademyId = academy, Name = "QA Browser Enrollment" };
        var fixtures = new List<BrowserEnrollmentFixture>();
        db.Courses.Add(course);
        foreach (var (name, role, status) in new[] { ("Active", "AcademyAdmin", "Active"), ("Waiting", "Owner", "Waitlisted") })
        {
            var actor = await CreateAccessActorAsync(factory, $"qa-enroll-{name.ToLowerInvariant()}@example.invalid", role, academy);
            var student = new Student { AcademyId = academy, FirstName = "Enrollment", LastName = name };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Enrollment " + name + " Piano",
                Capacity = 5, WaitlistCapacity = 3 };
            db.AddRange(student, batch);
            fixtures.Add(new(actor, student.Id, batch.Id, status));
        }
        await db.SaveChangesAsync();
        return fixtures.ToArray();
    }

    private static async Task VerifyBrowserEnrollmentAsync(QaApiFactory factory, Guid academy, BrowserEnrollmentFixture[] fixtures)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        foreach (var fixture in fixtures)
        {
            var row = await db.Enrollments.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.StudentId == fixture.Student);
            var draft = await db.PentaOnboardingDrafts.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor);
            var start = await db.PentaMiniSessions.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor);
            var audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor).ToArrayAsync();
            PentaRequire(row.BatchId == fixture.Batch && row.StartDate == BrowserEnrollmentDate && row.Status == fixture.Status &&
                draft.Status == "Succeeded" && draft.SessionId == start.Id &&
                audits.Count(x => x.Action == "PentaEnrollmentConversationStarted" && x.EntityId == start.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentPrepared" && x.EntityId == draft.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentApproved" && x.EntityId == draft.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentExecuted" && x.EntityId == draft.Id) == 1,
                "Browser did not leave one exact enrollment and audit per actor and transition.");
        }
        Console.WriteLine("PENTA ENROLLMENT BROWSER SQL PASS: exact Active and Waitlisted rows and one audit per transition.");
    }
}
