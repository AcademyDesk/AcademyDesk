using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.ReadModels;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

public sealed partial class PortalController
{
    private const int PentaPortalLimit = 10;
    private sealed record PentaLearner(Guid Id, string Name, bool Academic, bool Finance);
    private sealed record PentaClass(string BatchName, DateTime StartUtc, DateTime EndUtc, string DeliveryMode);
    private sealed record PentaWork(string Title, DateTime DueAtUtc);
    private sealed record PentaFee(string InvoiceNumber, string Currency, string Balance, DateOnly DueDate);

    [HttpGet("penta/learners"), Authorize(AuthenticationSchemes = "Identity.Bearer")]
    public Task<ActionResult> PentaLearners([FromServices] IdentityDbContext identity, [FromServices] IConfiguration config,
        CancellationToken token) => InPentaPortalAsync(identity, config, token, async (user, academy, guardian) =>
        {
            var students = db.Students.AsNoTracking().Where(x => x.AcademyId == academy && x.IsActive);
            PentaLearner[] learners;
            if (guardian)
                learners = await (from link in db.StudentGuardians.AsNoTracking()
                    join student in students on link.StudentId equals student.Id
                    where link.AcademyId == academy && link.GuardianId == user.GuardianId && link.CanAccessPortal && link.AccessRevokedAtUtc == null
                    orderby student.LastName, student.FirstName, student.Id
                    select new PentaLearner(student.Id, student.FirstName + " " + student.LastName,
                        link.CanViewAcademicProgress, link.CanViewFinance)).Take(PentaPortalLimit + 1).ToArrayAsync(token);
            else
                learners = await students.Where(x => x.Id == user.StudentId)
                    .Select(x => new PentaLearner(x.Id, x.FirstName + " " + x.LastName, true, true)).ToArrayAsync(token);

            AuditPentaPortal(user, academy, academy, "PentaPortalLearnersRead", "Academy");
            return Ok(new { protocol = "portal-reads.v1", academyId = academy, learners = learners.Take(PentaPortalLimit),
                hasMore = learners.Length > PentaPortalLimit });
        });

    [HttpGet("penta/students/{studentId:guid}/{kind}"), Authorize(AuthenticationSchemes = "Identity.Bearer")]
    public Task<ActionResult> PentaStudentRead(Guid studentId, string kind, [FromServices] IdentityDbContext identity,
        [FromServices] IConfiguration config, [FromServices] TimeProvider clock, CancellationToken token) =>
        InPentaPortalAsync(identity, config, token, async (user, academy, guardian) =>
        {
            if (kind is not ("next-class" or "due-work" or "outstanding-fees")) return NotFound();
            var link = guardian ? await ParentAccess(user, studentId, token) : null;
            if ((!guardian && user.StudentId != studentId) || (guardian && link is null) ||
                !await IsActiveStudent(academy, studentId, token)) return Forbid();
            if (link is not null && (kind == "outstanding-fees" ? !link.CanViewFinance : !link.CanViewAcademicProgress)) return Forbid();

            var now = clock.GetUtcNow().UtcDateTime;
            var scopedBatchIds = db.Enrollments.AsNoTracking().Where(x => x.AcademyId == academy &&
                x.StudentId == studentId && x.Status == "Active").Select(x => x.BatchId);
            Array rows = kind switch
            {
                "next-class" => await ReadPentaClassesAsync(academy, scopedBatchIds, now, token),
                "due-work" => await ReadPentaWorkAsync(academy, studentId, scopedBatchIds, now, token),
                _ => await ReadPentaFeesAsync(academy, studentId, token)
            };
            AuditPentaPortal(user, academy, studentId, kind switch
            {
                "next-class" => "PentaPortalNextClassRead", "due-work" => "PentaPortalDueWorkRead", _ => "PentaPortalFeesRead"
            }, "Student");
            return Ok(new { academyId = academy, studentId, kind, asOfUtc = now, rows, sourcePath = "/portal" });
        });

    private async Task<PentaClass[]> ReadPentaClassesAsync(Guid academy, IQueryable<Guid> batches, DateTime now, CancellationToken token)
    {
        var rows = await (from session in db.ClassSessions.AsNoTracking()
            join batch in db.Batches.AsNoTracking() on session.BatchId equals batch.Id
            where session.AcademyId == academy && batch.AcademyId == academy && session.Status == "Scheduled" &&
                session.StartUtc >= now && batches.Contains(batch.Id)
            orderby session.StartUtc, session.Id
            select new PentaClass(batch.Name, session.StartUtc, session.EndUtc, session.DeliveryMode))
            .Take(1).ToArrayAsync(token);
        return rows.Select(x => x with { StartUtc = UtcPentaPortal(x.StartUtc), EndUtc = UtcPentaPortal(x.EndUtc) }).ToArray();
    }

    private async Task<PentaWork[]> ReadPentaWorkAsync(Guid academy, Guid learner, IQueryable<Guid> batches, DateTime now, CancellationToken token)
    {
        var rows = await db.Assignments.AsNoTracking().Where(x => x.AcademyId == academy && x.IsPublished &&
            x.DueAtUtc >= now && (x.StudentId == null || x.StudentId == learner) && batches.Contains(x.BatchId) &&
            db.Batches.Any(batch => batch.Id == x.BatchId && batch.AcademyId == academy) &&
            !db.AssignmentSubmissions.Any(submission => submission.AcademyId == academy && submission.StudentId == learner &&
                submission.AssignmentId == x.Id))
            .OrderBy(x => x.DueAtUtc).ThenBy(x => x.Id).Take(PentaPortalLimit)
            .Select(x => new PentaWork(x.Title, x.DueAtUtc!.Value)).ToArrayAsync(token);
        return rows.Select(x => x with { DueAtUtc = UtcPentaPortal(x.DueAtUtc) }).ToArray();
    }

    private async Task<PentaFee[]> ReadPentaFeesAsync(Guid academy, Guid learner, CancellationToken token)
    {
        var rows = await InvoiceReadModel.WithCollected(db, db.Invoices.AsNoTracking().Where(x =>
            x.AcademyId == academy && x.StudentId == learner && x.Status != "Cancelled"))
            .Where(x => x.TotalAmount - x.AdjustedAmount - x.PaidAmount > 0)
            .OrderBy(x => x.DueDate).ThenBy(x => x.Id).Take(PentaPortalLimit).ToArrayAsync(token);
        return rows.Select(x => new PentaFee(x.InvoiceNumber, InvoiceReceiptReadModel.CurrencyKey(x.Currency),
            (x.TotalAmount - x.AdjustedAmount - x.PaidAmount).ToString("0.00", CultureInfo.InvariantCulture), x.DueDate)).ToArray();
    }

    private async Task<ActionResult> InPentaPortalAsync(IdentityDbContext identity, IConfiguration config, CancellationToken token,
        Func<ApplicationUser, Guid, bool, Task<ActionResult>> read)
    {
        Response.Headers.CacheControl = "no-store";
        if (!environment.IsEnvironment("Testing") || !config.GetValue<bool>("Penta:Enabled") ||
            !config.GetValue<bool>("Penta:PortalReadsEnabled")) return NotFound();
        try
        {
            var actorId = users.GetUserId(User);
            var stamp = User.FindFirstValue(users.Options.ClaimsIdentity.SecurityStampClaimType);
            if (!Guid.TryParse(actorId, out var id) || string.IsNullOrEmpty(stamp)) return Forbid();
            var connection = AcademyIdentityTransaction.Validate(db, identity);
            identity.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
            await using var linked = await AcademyIdentityTransaction.EnlistAsync(db, identity, transaction, connection, token);
            var user = await identity.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token);
            if (user is not { IsActive: true, IsPlatformOwner: false, AcademyId: { } academy } || user.SecurityStamp != stamp ||
                !await db.Academies.AnyAsync(x => x.Id == academy && x.IsActive, token)) return Forbid();

            var guardian = user.StudentId is null && user.GuardianId.HasValue && await users.IsInRoleAsync(user, "Guardian") &&
                await db.Guardians.AnyAsync(x => x.Id == user.GuardianId && x.AcademyId == academy && x.IsActive, token);
            var learner = user.GuardianId is null && user.StudentId is { } student && await users.IsInRoleAsync(user, "Student") &&
                await IsActiveStudent(academy, student, token);
            if (!guardian && !learner) return Forbid();

            var result = await read(user, academy, guardian);
            if (result is OkObjectResult)
            {
                await db.SaveChangesAsync(token);
                await transaction.CommitAsync(token);
            }
            return result;
        }
        catch (Exception error) when (error is DbException or DbUpdateException or InvalidOperationException)
        {
            return StatusCode(503, new { message = "PENTA could not verify this portal read. Try the Student Portal." });
        }
    }

    private void AuditPentaPortal(ApplicationUser user, Guid academy, Guid entity, string action, string entityType) =>
        db.AuditLogs.Add(new AuditLog { AcademyId = academy, ActorUserId = user.Id, EntityId = entity,
            EntityType = entityType, Action = action, OccurredAtUtc = DateTime.UtcNow });

    private static DateTime UtcPentaPortal(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
