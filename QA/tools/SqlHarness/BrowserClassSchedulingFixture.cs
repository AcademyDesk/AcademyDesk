using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private const string BrowserClassLink = "https://class.example.invalid/rehearsal";
    private sealed record BrowserClassFixture(Guid Admin, Guid Owner, Guid Batch, Guid Teacher, Guid Branch, Guid Source, DateTime Day);

    private static async Task<BrowserClassFixture> SeedBrowserClassSchedulingAsync(QaApiFactory factory, Guid academy)
    {
        var admin = await CreateAccessActorAsync(factory, "qa-class-create@example.invalid", "AcademyAdmin", academy);
        var owner = await CreateAccessActorAsync(factory, "qa-class-move@example.invalid", "Owner", academy);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        if (Environment.GetEnvironmentVariable("QA_PENTA_SHARED_CONVERSATION") == "1")
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = """["Core","Finance"]""";
        var teacher = new Teacher { AcademyId = academy, FirstName = "Synthetic", LastName = "Class browser" };
        var branch = new Branch { AcademyId = academy, Name = "Synthetic browser campus" };
        var batch = new Batch { AcademyId = academy, Name = "Browser Rehearsal", CourseId = Guid.NewGuid(), TeacherId = teacher.Id, BranchId = branch.Id };
        var day = DateTime.UtcNow.Date.AddDays(60);
        var source = new ClassSession { AcademyId = academy, BatchId = batch.Id, TeacherId = teacher.Id, BranchId = branch.Id,
            StartUtc = day.AddHours(9), EndUtc = day.AddHours(10), DeliveryMode = "InPerson", RoomName = "Before rehearsal", Status = "Scheduled" };
        db.AddRange(teacher, branch, batch, source,
            new Student { AcademyId = academy, FirstName = "Mira", LastName = "Conversation Alpha" },
            new Student { AcademyId = academy, FirstName = "Mira", LastName = "Conversation Beta" });
        await db.SaveChangesAsync();
        return new(admin, owner, batch.Id, teacher.Id, branch.Id, source.Id, day);
    }

    private static async Task VerifyBrowserClassSchedulingAsync(QaApiFactory factory, Guid academy, BrowserClassFixture fixture, int before)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var rows = await db.ClassSessions.AsNoTracking().Where(x => x.AcademyId == academy && x.BatchId == fixture.Batch).ToArrayAsync();
        PentaRequire(rows.Length == 2 && await db.ClassSessions.CountAsync() == before + 1 &&
            rows.All(x => x.TeacherId == fixture.Teacher && x.BranchId == fixture.Branch && x.Status == "Scheduled"),
            "Class browser changed the wrong rows or default assignments.");
        var added = rows.Single(x => x.Id != fixture.Source);
        var moved = rows.Single(x => x.Id == fixture.Source);
        PentaRequire(added.StartUtc == fixture.Day.AddDays(1).AddHours(11) && added.EndUtc == fixture.Day.AddDays(1).AddHours(12) &&
            added.DeliveryMode == "InPerson" && added.RoomName == "Browser studio" &&
            moved.StartUtc == fixture.Day.AddDays(2).AddHours(13) && moved.EndUtc == fixture.Day.AddDays(2).AddHours(14) &&
            moved.DeliveryMode == "Hybrid" && moved.RoomName == BrowserClassLink,
            "Class browser did not persist the exact reviewed slots.");
        var actors = new[] { fixture.Admin, fixture.Owner };
        var audits = await db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId.HasValue &&
            actors.Contains(x.ActorUserId.Value) && (x.Action == "PentaClassSessionScheduled" || x.Action == "PentaClassSessionRescheduled")).ToArrayAsync();
        PentaRequire(audits.Length == 2 && audits.Count(x => x.ActorUserId == fixture.Admin && x.Action == "PentaClassSessionScheduled") == 1 &&
            audits.Count(x => x.ActorUserId == fixture.Owner && x.Action == "PentaClassSessionRescheduled") == 1,
            "Class browser must record exactly one execution audit per acting role.");
        if (Environment.GetEnvironmentVariable("QA_PENTA_SHARED_CONVERSATION") == "1")
        {
            var reads = db.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academy && x.ActorUserId == fixture.Admin);
            PentaRequire(await reads.CountAsync(x => x.Action == "PentaStudentSearchRead") == 2 &&
                await reads.CountAsync(x => x.Action == "PentaBatchSearchRead") == 1,
                "Shared conversation context must use the exact approved student and batch read path.");
            Console.WriteLine("PENTA SHARED CONVERSATION SQL PASS: expected scoped lookup audits.");
        }
        Console.WriteLine("PENTA CLASS BROWSER SQL PASS: one new class, one moved original and one execution audit per actor.");
    }
}
