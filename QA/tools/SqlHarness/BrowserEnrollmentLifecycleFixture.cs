using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static readonly DateOnly LifecycleBrowserStart = new(2027, 1, 1);
    private static readonly DateOnly LifecycleBrowserChange = new(2027, 2, 1);
    private const string LifecycleBrowserReason = "Synthetic family break";
    private sealed record BrowserLifecycleFixture(Guid Actor, Guid Student, Guid Source, Guid SourceBatch, Guid? Target);

    private static async Task<BrowserLifecycleFixture[]> SeedBrowserEnrollmentLifecycleAsync(QaApiFactory factory, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var course = new ProgramCourse { AcademyId = academy, Name = "QA Browser Lifecycle" };
        db.Courses.Add(course);
        var fixtures = new List<BrowserLifecycleFixture>();
        foreach (var (kind, role) in new[] { ("Update", "AcademyAdmin"), ("Transfer", "Owner") })
        {
            var actor = await CreateAccessActorAsync(factory, $"qa-lifecycle-{kind.ToLowerInvariant()}@example.invalid", role, academy);
            var student = new Student { AcademyId = academy, FirstName = "Lifecycle", LastName = kind };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = $"Lifecycle {kind} Piano", Capacity = 5 };
            var source = new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = batch.Id,
                StartDate = LifecycleBrowserStart, Status = "Active" };
            db.AddRange(student, batch, source);
            Guid? target = null;
            if (kind == "Transfer")
            {
                var destination = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Lifecycle Target Violin", Capacity = 5 };
                db.Batches.Add(destination);
                target = destination.Id;
            }
            fixtures.Add(new(actor, student.Id, source.Id, batch.Id, target));
        }
        await db.SaveChangesAsync();
        return fixtures.ToArray();
    }

    private static async Task VerifyBrowserEnrollmentLifecycleAsync(QaApiFactory factory, Guid academy, BrowserLifecycleFixture[] fixtures)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        foreach (var fixture in fixtures)
        {
            var rows = await db.Enrollments.AsNoTracking().Where(x => x.AcademyId == academy && x.StudentId == fixture.Student).ToArrayAsync();
            var source = rows.Single(x => x.Id == fixture.Source);
            PentaRequire(source.BatchId == fixture.SourceBatch && source.StartDate == LifecycleBrowserStart &&
                source.EndDate == LifecycleBrowserChange, "Lifecycle modified the wrong source or date.");
            if (fixture.Target.HasValue)
            {
                PentaRequire(source.Status == "Transferred" && rows.Length == 2 && rows.Any(x => x.Id != source.Id &&
                    x.BatchId == fixture.Target && x.Status == "Active" && x.StartDate == LifecycleBrowserChange && x.EndDate == null),
                    "Transfer did not preserve its source and create one active target.");
            }
            else
            {
                PentaRequire(rows.Length == 1 && source.Status == "Paused" && source.LifecycleReason == LifecycleBrowserReason,
                    "Status update did not store the exact reason and status on the original row.");
            }
            var draft = await db.PentaOnboardingDrafts.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor);
            var session = await db.PentaMiniSessions.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor);
            var audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == fixture.Actor).ToArrayAsync();
            PentaRequire(draft.Status == "Succeeded" && draft.SessionId == session.Id &&
                audits.Count(x => x.Action == "PentaEnrollmentLifecycleSearched") == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentLifecycleConversationStarted" && x.EntityId == session.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentPrepared" && x.EntityId == draft.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentApproved" && x.EntityId == draft.Id) == 1 &&
                audits.Count(x => x.Action == "PentaEnrollmentExecuted" && x.EntityId == draft.Id) == 1,
                "Lifecycle browser did not store one audit per transition.");
        }
        Console.WriteLine("PENTA LIFECYCLE BROWSER SQL PASS: exact pause and transfer, preserved history, one audit per transition.");
    }
}
