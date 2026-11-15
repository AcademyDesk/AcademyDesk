using System.Net;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private const string BrowserPortalStudent = "qa-penta-portal-student@example.invalid";
    private const string BrowserPortalGuardian = "qa-penta-portal-guardian@example.invalid";
    private sealed record BrowserPortalFixture(Guid Academy, Guid Guardian, Guid Learner, Guid Sibling, Guid Unlinked, Guid Foreign)
    {
        public string RevokePath => $"/api/academies/{Academy:D}/students/{Learner:D}/guardians/{Guardian:D}/portal-access";
    }

    private static async Task<BrowserPortalFixture> SeedBrowserPortalAsync(QaApiFactory factory, Guid academy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
        var guardian = new Guardian { AcademyId = academy, FirstName = "PENTA", LastName = "Guardian" };
        var learner = new Student { AcademyId = academy, FirstName = "Mira", LastName = "Penta Learner" };
        var sibling = new Student { AcademyId = academy, FirstName = "Leo", LastName = "Penta Sibling" };
        var unlinked = new Student { AcademyId = academy, FirstName = "Hidden", LastName = "Penta Learner" };
        var foreignLearner = new Student { AcademyId = foreign, FirstName = "Foreign", LastName = "Penta Learner" };
        var batch = new Batch { AcademyId = academy, CourseId = Guid.NewGuid(), Name = "PENTA Browser Piano" };
        var siblingBatch = new Batch { AcademyId = academy, CourseId = Guid.NewGuid(), Name = "PENTA Browser Violin" };
        var slot = DateTime.UtcNow.Date.AddDays(10).AddHours(8);
        var visible = new Assignment { AcademyId = academy, BatchId = batch.Id, Title = "PENTA visible practice", IsPublished = true, DueAtUtc = slot };
        var submitted = new Assignment { AcademyId = academy, BatchId = batch.Id, Title = "PENTA submitted practice", IsPublished = true, DueAtUtc = slot };
        var invoice = new Invoice { AcademyId = academy, StudentId = learner.Id, InvoiceNumber = "PENTA-PORTAL-EXACT",
            TotalAmount = 1000m, AdjustedAmount = 200m, Currency = "USD", DueDate = DateOnly.FromDateTime(slot) };
        db.AddRange(guardian, learner, sibling, unlinked, foreignLearner, batch, siblingBatch, visible, submitted, invoice,
            new StudentGuardian { AcademyId = academy, StudentId = learner.Id, GuardianId = guardian.Id, CanAccessPortal = true },
            new StudentGuardian { AcademyId = academy, StudentId = sibling.Id, GuardianId = guardian.Id, CanAccessPortal = true, CanViewFinance = false },
            new StudentGuardian { AcademyId = academy, StudentId = foreignLearner.Id, GuardianId = guardian.Id, CanAccessPortal = true },
            new Enrollment { AcademyId = academy, StudentId = learner.Id, BatchId = batch.Id },
            new Enrollment { AcademyId = academy, StudentId = sibling.Id, BatchId = siblingBatch.Id },
            new ClassSession { AcademyId = academy, BatchId = batch.Id, StartUtc = slot, EndUtc = slot.AddHours(1), RoomName = "Private location" },
            new ClassSession { AcademyId = academy, BatchId = siblingBatch.Id, StartUtc = slot, EndUtc = slot.AddHours(1) },
            new Assignment { AcademyId = academy, BatchId = batch.Id, Title = "PENTA unpublished practice", IsPublished = false, DueAtUtc = slot },
            new Assignment { AcademyId = academy, BatchId = batch.Id, StudentId = sibling.Id, Title = "PENTA private sibling work", IsPublished = true, DueAtUtc = slot },
            new AssignmentSubmission { AcademyId = academy, StudentId = learner.Id, AssignmentId = submitted.Id },
            new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = "USD", Status = "Completed", Amount = 100m },
            new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = "USD", Status = "Reconciled", Amount = 250m },
            new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = "USD", Status = "Voided", Amount = 900m },
            new Payment { AcademyId = academy, InvoiceId = invoice.Id, Currency = "INR", Status = "Completed", Amount = 800m });
        await db.SaveChangesAsync();
        await CreateAccessActorAsync(factory, BrowserPortalStudent, "Student", academy, learner.Id);
        var actor = await CreateAccessActorAsync(factory, BrowserPortalGuardian, "Guardian", academy);
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await identity.Users.SingleAsync(x => x.Id == actor)).GuardianId = guardian.Id;
        await identity.SaveChangesAsync();
        return new(academy, guardian.Id, learner.Id, sibling.Id, unlinked.Id, foreignLearner.Id);
    }

    private static async Task VerifyBrowserPortalScopeAsync(QaApiFactory factory, BrowserPortalFixture f)
    {
        using var guardian = await PentaBrowserSignIn(factory, BrowserPortalGuardian);
        using var student = await PentaBrowserSignIn(factory, BrowserPortalStudent);
        using var admin = await PentaBrowserSignIn(factory, "qa-admin-a@example.invalid");
        using var guest = factory.CreateClient();
        const string root = "/api/portal/penta";
        await PentaBrowserStatus(guest.GetAsync(root + "/learners"), HttpStatusCode.Unauthorized, "anonymous PENTA portal");
        await PentaBrowserStatus(admin.GetAsync(root + "/learners"), HttpStatusCode.Forbidden, "admin PENTA portal");
        foreach (var learner in new[] { f.Unlinked, f.Foreign })
            await PentaBrowserStatus(guardian.GetAsync(root + $"/students/{learner:D}/next-class"), HttpStatusCode.Forbidden, "unlinked or foreign learner");
        await PentaBrowserStatus(student.GetAsync(root + $"/students/{f.Sibling:D}/next-class"), HttpStatusCode.Forbidden, "student cannot read sibling");
        await PentaBrowserStatus(guardian.GetAsync(root + $"/students/{f.Sibling:D}/outstanding-fees"), HttpStatusCode.Forbidden, "guardian finance subpermission");
        Console.WriteLine("PENTA PORTAL SQL SCOPE PASS: 6 denied reads.");
    }

    private static async Task VerifyBrowserPortalConversationAsync(QaApiFactory factory, BrowserPortalFixture f)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var link = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.AcademyId == f.Academy &&
            x.GuardianId == f.Guardian && x.StudentId == f.Learner);
        var sibling = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.AcademyId == f.Academy &&
            x.GuardianId == f.Guardian && x.StudentId == f.Sibling);
        PentaRequire(!link.CanAccessPortal && link.AccessRevokedAtUtc.HasValue && sibling.CanAccessPortal &&
            await db.AuditLogs.CountAsync(x => x.AcademyId == f.Academy && x.EntityId == f.Learner &&
                x.Action == "PentaPortalFeesRead") == 1,
            "The guardian revoke, sibling access or allowed finance audit differed from the browser result.");
        Console.WriteLine("PENTA PORTAL SQL BROWSER PASS: current revocation and sibling scope verified, one authorized learner fees audit.");
    }
}
