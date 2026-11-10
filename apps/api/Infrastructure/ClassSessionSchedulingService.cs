using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

internal sealed record ClassSessionPlanOutcome(int Status, ClassSessionSummary? Plan = null, string? Error = null);

public sealed class ClassSessionSchedulingService(AcademyDeskDbContext db)
{
    // Manual and protected host writers take the same lock before resolving
    // assignments or checking overlaps. The caller holds it through save and audit.
    internal async Task LockAsync(Guid academyId, CancellationToken token)
    {
        if (db.Database.IsSqlServer())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT [Id] FROM [Academies] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={academyId}", token);
    }

    internal async Task<ClassSessionPlanOutcome> PlanCreateAsync(Guid academyId, CreateClassSessionRequest request, CancellationToken token)
    {
        var mode = string.IsNullOrWhiteSpace(request.DeliveryMode) ? "InPerson" : request.DeliveryMode.Trim();
        var room = request.RoomName?.Trim();
        if (RequiresLink(mode) && string.IsNullOrWhiteSpace(room))
            return new(400, Error: "A meeting link is required for online and hybrid classes.");

        var batch = await db.Batches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.BatchId && x.AcademyId == academyId, token);
        if (batch is null) return new(400, Error: "The selected batch does not belong to this academy.");
        if (request.EndUtc <= request.StartUtc) return new(400, Error: "End time must be after start time.");

        var teacher = request.TeacherId ?? batch.TeacherId;
        var branch = request.BranchId ?? batch.BranchId;
        if (teacher.HasValue && !await db.Teachers.AnyAsync(x => x.Id == teacher && x.AcademyId == academyId, token))
            return new(400, Error: "The selected teacher does not belong to this academy.");
        if (branch.HasValue && !await db.Branches.AnyAsync(x => x.Id == branch && x.AcademyId == academyId, token))
            return new(400, Error: "The selected branch does not belong to this academy.");

        return await CheckAsync(academyId, new(Guid.Empty, batch.Id, teacher, branch, request.StartUtc,
            request.EndUtc, mode, room, "Scheduled"), token);
    }

    internal async Task<ClassSessionPlanOutcome> PlanUpdateAsync(Guid academyId, Guid sessionId, UpdateClassSessionRequest request, CancellationToken token)
    {
        var mode = request.DeliveryMode?.Trim() ?? "InPerson";
        var room = request.RoomName?.Trim();
        if (RequiresLink(mode) && string.IsNullOrWhiteSpace(room))
            return new(400, Error: "A meeting link is required for online and hybrid classes.");
        var session = await db.ClassSessions.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == sessionId, token);
        if (session is null) return new(404);
        if (request.EndUtc <= request.StartUtc) return new(400, Error: "End time must be after the start time.");
        if (!new[] { "Scheduled", "Completed", "Cancelled", "NoShow" }.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return new(400, Error: "Invalid session status.");
        var plan = new ClassSessionSummary(session.Id, session.BatchId, session.TeacherId, session.BranchId,
            request.StartUtc, request.EndUtc, mode, room, request.Status.Trim());

        // Status-only changes can finish a historical session even if existing
        // data has a clash. Moving or reopening a session must check the slot.
        if (string.Equals(plan.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(session.Status, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
            plan.StartUtc == session.StartUtc && plan.EndUtc == session.EndUtc && plan.RoomName == session.RoomName)
            return new(200, plan);
        return await CheckAsync(academyId, plan, token);
    }

    internal async Task<ClassSessionSummary> SaveAsync(Guid academyId, ClassSessionSummary plan, CancellationToken token)
    {
        var creating = plan.Id == Guid.Empty;
        var row = creating
            ? new ClassSession { AcademyId = academyId, BatchId = plan.BatchId, TeacherId = plan.TeacherId, BranchId = plan.BranchId }
            : await db.ClassSessions.SingleAsync(x => x.AcademyId == academyId && x.Id == plan.Id, token);
        row.StartUtc = plan.StartUtc;
        row.EndUtc = plan.EndUtc;
        row.DeliveryMode = plan.DeliveryMode;
        row.RoomName = plan.RoomName;
        row.Status = plan.Status;
        if (creating) db.ClassSessions.Add(row);
        await db.SaveChangesAsync(token);
        return plan with { Id = row.Id };
    }

    private async Task<ClassSessionPlanOutcome> CheckAsync(Guid academyId, ClassSessionSummary plan, CancellationToken token)
    {
        var overlaps = db.ClassSessions.AsNoTracking().Where(x => x.AcademyId == academyId && x.Id != plan.Id &&
            x.Status != "Cancelled" && x.StartUtc < plan.EndUtc && x.EndUtc > plan.StartUtc);
        if (plan.TeacherId.HasValue && await overlaps.AnyAsync(x => x.TeacherId == plan.TeacherId, token))
            return new(409, Error: "Teacher clash: this teacher already has a session during the selected time.");
        if (!string.IsNullOrWhiteSpace(plan.RoomName) && await overlaps.AnyAsync(x => x.BranchId == plan.BranchId && x.RoomName == plan.RoomName, token))
            return new(409, Error: "Room clash: this room is already scheduled during the selected time.");
        if (await overlaps.AnyAsync(x => x.BatchId == plan.BatchId && x.StartUtc == plan.StartUtc, token))
            return new(409, Error: "This batch already has a session starting at the selected time.");
        return new(200, plan);
    }

    private static bool RequiresLink(string mode) => string.Equals(mode, "Online", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mode, "Hybrid", StringComparison.OrdinalIgnoreCase);
}
