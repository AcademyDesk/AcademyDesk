using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/leads")]
public sealed class LeadsController(AcademyDeskDbContext dbContext) : ControllerBase
{
    private static readonly string[] Stages = ["New", "Contacted", "TrialBooked", "FollowUp", "Won", "Lost", "Converted"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LeadSummary>>> List(Guid academyId, string? stage, CancellationToken cancellationToken)
    {
        var query = dbContext.Leads.AsNoTracking().Where(x => x.AcademyId == academyId);
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(x => x.Stage == stage);
        return Ok(await query.OrderBy(x => x.FollowUpAtUtc ?? DateTime.MaxValue).ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new LeadSummary(x.Id, x.FullName, x.Email, x.Phone, x.DateOfBirth, x.ParentName, x.ProgramInterest, x.Source, x.Stage, x.BranchId, x.AssignedTeacherId, x.FollowUpAtUtc, x.Notes, x.ConvertedStudentId))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<LeadSummary>> Create(Guid academyId, CreateLeadRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return BadRequest(new { message = "Lead name is required." });
        var lead = new Lead { AcademyId = academyId, FullName = request.FullName.Trim(), Email = request.Email?.Trim(), Phone = request.Phone?.Trim(), DateOfBirth = request.DateOfBirth, ParentName = request.ParentName?.Trim(), ProgramInterest = request.ProgramInterest?.Trim(), Source = string.IsNullOrWhiteSpace(request.Source) ? "WalkIn" : request.Source.Trim(), FollowUpAtUtc = request.FollowUpAtUtc, Notes = request.Notes?.Trim() };
        dbContext.Leads.Add(lead);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/leads/{lead.Id}", ToSummary(lead));
    }

    [HttpPatch("{leadId:guid}/stage")]
    public async Task<ActionResult<LeadSummary>> UpdateStage(Guid academyId, Guid leadId, UpdateLeadStageRequest request, CancellationToken cancellationToken)
    {
        if (!Stages.Contains(request.Stage, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid lead stage." });
        var lead = await dbContext.Leads.SingleOrDefaultAsync(x => x.Id == leadId && x.AcademyId == academyId, cancellationToken);
        if (lead is null) return NotFound();
        if (lead.ConvertedStudentId.HasValue) return Conflict(new { message = "A converted lead cannot be moved back through the pipeline." });
        lead.Stage = Stages.Single(x => x.Equals(request.Stage, StringComparison.OrdinalIgnoreCase));
        lead.FollowUpAtUtc = request.FollowUpAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToSummary(lead));
    }
    [HttpPatch("{leadId:guid}/notes")]
    public async Task<ActionResult<LeadSummary>> UpdateNotes(Guid academyId, Guid leadId, UpdateLeadNotesRequest request, CancellationToken token)
    { var lead=await dbContext.Leads.SingleOrDefaultAsync(x=>x.Id==leadId&&x.AcademyId==academyId,token); if(lead is null)return NotFound(); lead.Notes=string.IsNullOrWhiteSpace(request.Notes)?null:request.Notes.Trim(); await dbContext.SaveChangesAsync(token); return Ok(ToSummary(lead)); }

    [HttpPost("{leadId:guid}/convert")]
    public async Task<ActionResult<LeadConversionSummary>> Convert(Guid academyId, Guid leadId, ConvertLeadRequest request, CancellationToken cancellationToken)
    {
        var lead = await dbContext.Leads.SingleOrDefaultAsync(x => x.Id == leadId && x.AcademyId == academyId, cancellationToken);
        if (lead is null) return NotFound();
        if (lead.ConvertedStudentId.HasValue) return Conflict(new { message = "This lead has already been converted." });
        var parts = lead.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstName = string.IsNullOrWhiteSpace(request.FirstName) ? parts.FirstOrDefault() ?? "Student" : request.FirstName.Trim();
        var lastName = string.IsNullOrWhiteSpace(request.LastName) ? string.Join(' ', parts.Skip(1)).Trim() : request.LastName.Trim();
        if (string.IsNullOrWhiteSpace(lastName)) lastName = "Student";
        var student = new Student { AcademyId = academyId, FirstName = firstName, LastName = lastName, Email = lead.Email, Phone = lead.Phone, DateOfBirth = lead.DateOfBirth };
        dbContext.Students.Add(student);
        lead.ConvertedStudentId = student.Id;
        lead.Stage = "Converted";
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "LeadConverted", EntityType = "Lead", EntityId = lead.Id, MetadataJson = $"{{\"studentId\":\"{student.Id}\"}}" });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new LeadConversionSummary(lead.Id, student.Id, student.FirstName, student.LastName));
    }

    private static LeadSummary ToSummary(Lead lead) => new(lead.Id, lead.FullName, lead.Email, lead.Phone, lead.DateOfBirth, lead.ParentName, lead.ProgramInterest, lead.Source, lead.Stage, lead.BranchId, lead.AssignedTeacherId, lead.FollowUpAtUtc, lead.Notes, lead.ConvertedStudentId);
}

public sealed record CreateLeadRequest(string FullName, string? Email, string? Phone, DateOnly? DateOfBirth, string? ParentName, string? ProgramInterest, string? Source, DateTime? FollowUpAtUtc, string? Notes);
public sealed record UpdateLeadStageRequest(string Stage, DateTime? FollowUpAtUtc);
public sealed record UpdateLeadNotesRequest(string? Notes);
public sealed record ConvertLeadRequest(string? FirstName, string? LastName);
public sealed record LeadSummary(Guid Id, string FullName, string? Email, string? Phone, DateOnly? DateOfBirth, string? ParentName, string? ProgramInterest, string Source, string Stage, Guid? BranchId, Guid? AssignedTeacherId, DateTime? FollowUpAtUtc, string? Notes, Guid? ConvertedStudentId);
public sealed record LeadConversionSummary(Guid LeadId, Guid StudentId, string FirstName, string LastName);
