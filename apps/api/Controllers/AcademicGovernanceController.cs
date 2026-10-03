using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/academic-governance")]
public sealed class AcademicGovernanceController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("grading-schemes")]
    public async Task<ActionResult> Schemes(Guid academyId, CancellationToken t) =>
        Ok(await db.GradingSchemes.AsNoTracking().Where(x => x.AcademyId == academyId).ToListAsync(t));

    [HttpPost("grading-schemes")]
    public async Task<ActionResult> AddScheme(Guid academyId, GradingSchemeRequest r, CancellationToken t)
    {
        if (string.IsNullOrWhiteSpace(r.Name) || r.PassingPercent < 0 || r.PassingPercent > 100) return BadRequest();
        try { System.Text.Json.JsonDocument.Parse(r.BandsJson ?? "[]"); }
        catch { return BadRequest(new { message = "Grade bands must be valid JSON." }); }
        db.GradingSchemes.Add(new GradingScheme { AcademyId = academyId, Name = r.Name.Trim(), PassingPercent = r.PassingPercent, BandsJson = r.BandsJson ?? "[]" });
        await db.SaveChangesAsync(t);
        return Ok();
    }

    [HttpGet("prerequisites")]
    public async Task<ActionResult> Prerequisites(Guid academyId, CancellationToken t) =>
        Ok(await db.CoursePrerequisites.AsNoTracking().Where(x => x.AcademyId == academyId).ToListAsync(t));

    [HttpPost("prerequisites")]
    [AtomicAcademyMutation]
    public async Task<ActionResult> AddPrerequisite(Guid academyId, PrerequisiteRequest r, CancellationToken t)
    {
        if (r.CourseId == r.RequiredCourseId) return BadRequest();

        // Normal academy actors reuse the save/audit transaction. Platform-owner
        // bypass/direct SQL calls still need their own graph write boundary.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(t) : null;
        if (db.Database.IsSqlServer() && !await LockGraphAsync(academyId, t))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Course prerequisites are being updated. Please try again." });

        if (!await db.Courses.AnyAsync(x => x.AcademyId == academyId && x.Id == r.CourseId, t) ||
            !await db.Courses.AnyAsync(x => x.AcademyId == academyId && x.Id == r.RequiredCourseId, t)) return BadRequest();

        var edges = await db.CoursePrerequisites.AsNoTracking().Where(x => x.AcademyId == academyId)
            .Select(x => new { x.CourseId, x.RequiredCourseId }).ToListAsync(t);
        if (edges.Any(x => x.CourseId == r.CourseId && x.RequiredCourseId == r.RequiredCourseId))
            return Conflict(new { message = "This prerequisite is already configured." });

        // Adding course -> required is unsafe if required already reaches course.
        // Iteration + visited nodes also terminate on pre-existing corrupt cycles.
        var graph = edges.ToLookup(x => x.CourseId, x => x.RequiredCourseId);
        var pending = new Stack<Guid>(); var visited = new HashSet<Guid>();
        pending.Push(r.RequiredCourseId);
        while (pending.TryPop(out var node))
        {
            t.ThrowIfCancellationRequested();
            if (node == r.CourseId) return BadRequest(new { message = "This prerequisite would create a circular course dependency." });
            if (!visited.Add(node)) continue;
            foreach (var next in graph[node]) pending.Push(next);
        }

        db.CoursePrerequisites.Add(new CoursePrerequisite { AcademyId = academyId, CourseId = r.CourseId, RequiredCourseId = r.RequiredCourseId });
        await db.SaveChangesAsync(t);
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(t);
        return Ok();
    }

    private async Task<bool> LockGraphAsync(Guid academyId, CancellationToken t)
    {
        // Transaction-owned SQL application lock serializes this academy's graph
        // across API replicas; released only with the edge + audit commit/rollback.
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; SELECT @result;";
        var resource = command.CreateParameter(); resource.ParameterName = "@resource";
        resource.Value = $"AcademyDesk:CoursePrerequisites:{academyId:D}"; command.Parameters.Add(resource);
        return (int)(await command.ExecuteScalarAsync(t))! >= 0;
    }
}

public sealed record GradingSchemeRequest(string Name, decimal PassingPercent, string? BandsJson);
public sealed record PrerequisiteRequest(Guid CourseId, Guid RequiredCourseId);
