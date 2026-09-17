using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/platform")]
public sealed class PlatformControlController(AcademyDeskDbContext db, UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult> Overview(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var academies = db.Academies.AsNoTracking();
        var invoices = db.PlatformBillingInvoices.AsNoTracking();
        return Ok(new
        {
            Academies = await academies.CountAsync(token),
            ActiveAcademies = await academies.CountAsync(x => x.IsActive, token),
            ActiveStudents = await db.Students.CountAsync(x => x.IsActive, token),
            OpenSupportCases = await db.PlatformSupportCases.CountAsync(x => x.Status != "Resolved" && x.Status != "Closed", token),
            OutstandingBilling = await invoices.Where(x => x.Status == "Issued" || x.Status == "Overdue").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
            Plans = await academies.GroupBy(x => x.SubscriptionPlan).Select(x => new { Plan = x.Key, Count = x.Count() }).ToListAsync(token),
            RecentAudit = await db.PlatformAuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(8).Select(x => new { x.Id, x.Action, x.EntityType, x.ActorName, x.OccurredAtUtc }).ToListAsync(token)
        });
    }

    [HttpGet("settings")]
    public async Task<ActionResult<PlatformSettings>> GetSettings(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        return Ok(await GetSettingsRecord(token));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<PlatformSettings>> SaveSettings(UpdatePlatformSettingsRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.PlatformName) || request.DefaultTrialDays < 1 || request.DataRetentionDays < 30) return BadRequest(new { message = "Platform name, a positive trial period, and at least 30 days retention are required." });
        var settings = await GetSettingsRecord(token);
        settings.PlatformName = request.PlatformName.Trim(); settings.SupportEmail = request.SupportEmail?.Trim(); settings.DefaultCurrency = string.IsNullOrWhiteSpace(request.DefaultCurrency) ? "INR" : request.DefaultCurrency.Trim().ToUpperInvariant(); settings.DefaultTrialDays = request.DefaultTrialDays; settings.DataRetentionDays = request.DataRetentionDays; settings.MaintenanceMode = request.MaintenanceMode; settings.StatusMessage = request.StatusMessage?.Trim(); settings.UpdatedAtUtc = DateTime.UtcNow;
        await Audit("Platform settings updated", "PlatformSettings", settings.Id, new { settings.MaintenanceMode }, token); await db.SaveChangesAsync(token); return Ok(settings);
    }

    [HttpGet("admins")]
    public async Task<ActionResult> Admins(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var admins = await users.GetUsersInRoleAsync("AcademyAdmin");
        var academyNames = await db.Academies.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, token);
        return Ok(admins.OrderBy(x => x.DisplayName).Select(x => new { x.Id, x.DisplayName, x.UserName, x.Email, x.IsActive, x.AcademyId, AcademyName = x.AcademyId.HasValue && academyNames.TryGetValue(x.AcademyId.Value, out var name) ? name : null }));
    }

    [HttpPatch("admins/{userId:guid}/active")]
    public async Task<ActionResult> SetAdminActive(Guid userId, SetPlatformAdminActiveRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null || !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound();
        admin.IsActive = request.IsActive; var result = await users.UpdateAsync(admin); if (!result.Succeeded) return BadRequest(new { message = string.Join(" ", result.Errors.Select(x => x.Description)) });
        await Audit(request.IsActive ? "Academy admin activated" : "Academy admin deactivated", "ApplicationUser", admin.Id, new { admin.AcademyId, admin.DisplayName }, token); await db.SaveChangesAsync(token); return Ok(new { admin.Id, admin.IsActive });
    }

    [HttpPost("admins/{userId:guid}/reset-password")]
    public async Task<ActionResult> ResetAdminPassword(Guid userId, ResetPlatformAdminPasswordRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8) return BadRequest(new { message = "Use a password of at least eight characters." });
        var admin = await users.FindByIdAsync(userId.ToString()); if (admin is null || !await users.IsInRoleAsync(admin, "AcademyAdmin")) return NotFound();
        var reset = await users.ResetPasswordAsync(admin, await users.GeneratePasswordResetTokenAsync(admin), request.NewPassword); if (!reset.Succeeded) return BadRequest(new { message = string.Join(" ", reset.Errors.Select(x => x.Description)) });
        await Audit("Academy admin password reset", "ApplicationUser", admin.Id, new { admin.AcademyId, admin.DisplayName }, token); await db.SaveChangesAsync(token); return Ok(new { message = "Password reset." });
    }

    [HttpGet("support-cases")]
    public async Task<ActionResult> ListSupportCases(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        return Ok(await (from item in db.PlatformSupportCases.AsNoTracking() join academy in db.Academies.AsNoTracking() on item.AcademyId equals academy.Id orderby item.CreatedAtUtc descending select new { item.Id, item.AcademyId, AcademyName = academy.Name, item.Subject, item.Priority, item.Status, item.Description, item.CreatedAtUtc, item.ResolvedAtUtc }).ToListAsync(token));
    }

    [HttpPost("support-cases")]
    public async Task<ActionResult> CreateSupportCase(CreatePlatformSupportCaseRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Subject) || !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "A valid academy and subject are required." });
        var item = new PlatformSupportCase { AcademyId = request.AcademyId, Subject = request.Subject.Trim(), Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(), Description = request.Description?.Trim() }; db.PlatformSupportCases.Add(item); await Audit("Support case created", "PlatformSupportCase", item.Id, new { item.AcademyId, item.Subject }, token); await db.SaveChangesAsync(token); return Created($"/api/platform/support-cases/{item.Id}", item);
    }

    [HttpPatch("support-cases/{caseId:guid}")]
    public async Task<ActionResult> UpdateSupportCase(Guid caseId, UpdatePlatformSupportCaseRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var item = await db.PlatformSupportCases.SingleOrDefaultAsync(x => x.Id == caseId, token); if (item is null) return NotFound();
        item.Status = request.Status.Trim(); item.Priority = request.Priority.Trim(); item.ResolvedAtUtc = item.Status is "Resolved" or "Closed" ? DateTime.UtcNow : null; item.UpdatedAtUtc = DateTime.UtcNow; await Audit("Support case updated", "PlatformSupportCase", item.Id, new { item.Status, item.Priority }, token); await db.SaveChangesAsync(token); return Ok(item);
    }

    [HttpGet("billing-invoices")]
    public async Task<ActionResult> ListInvoices(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        return Ok(await (from invoice in db.PlatformBillingInvoices.AsNoTracking() join academy in db.Academies.AsNoTracking() on invoice.AcademyId equals academy.Id orderby invoice.DueDate descending select new { invoice.Id, invoice.AcademyId, AcademyName = academy.Name, invoice.InvoiceNumber, invoice.Amount, invoice.Currency, invoice.Status, invoice.PeriodStart, invoice.PeriodEnd, invoice.DueDate, invoice.PaidAtUtc }).ToListAsync(token));
    }

    [HttpPost("billing-invoices")]
    public async Task<ActionResult> CreateInvoice(CreatePlatformBillingInvoiceRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (request.Amount < 0 || string.IsNullOrWhiteSpace(request.InvoiceNumber) || !await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return BadRequest(new { message = "Academy, invoice number, and non-negative amount are required." });
        var item = new PlatformBillingInvoice { AcademyId = request.AcademyId, InvoiceNumber = request.InvoiceNumber.Trim(), Amount = request.Amount, Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(), Status = "Draft", PeriodStart = request.PeriodStart, PeriodEnd = request.PeriodEnd, DueDate = request.DueDate }; db.PlatformBillingInvoices.Add(item); await Audit("Platform invoice created", "PlatformBillingInvoice", item.Id, new { item.AcademyId, item.InvoiceNumber, item.Amount }, token); await db.SaveChangesAsync(token); return Created($"/api/platform/billing-invoices/{item.Id}", item);
    }

    [HttpPatch("billing-invoices/{invoiceId:guid}/status")]
    public async Task<ActionResult> UpdateInvoiceStatus(Guid invoiceId, UpdatePlatformInvoiceStatusRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var invoice = await db.PlatformBillingInvoices.SingleOrDefaultAsync(x => x.Id == invoiceId, token); if (invoice is null) return NotFound(); invoice.Status = request.Status.Trim(); invoice.PaidAtUtc = invoice.Status == "Paid" ? DateTime.UtcNow : null; invoice.UpdatedAtUtc = DateTime.UtcNow; await Audit("Platform invoice status updated", "PlatformBillingInvoice", invoice.Id, new { invoice.Status }, token); await db.SaveChangesAsync(token); return Ok(invoice);
    }

    [HttpGet("audit")]
    public async Task<ActionResult> AuditLog(CancellationToken token) { if (!await IsPlatformOwner()) return Forbid(); return Ok(await db.PlatformAuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(250).ToListAsync(token)); }

    [HttpGet("health")]
    public async Task<ActionResult> Health(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid(); var settings = await GetSettingsRecord(token); return Ok(new { Api = "Operational", Database = await db.Database.CanConnectAsync(token) ? "Operational" : "Unavailable", BackgroundJobs = "Not configured", CommunicationProviders = "Not configured", MaintenanceMode = settings.MaintenanceMode, StatusMessage = settings.StatusMessage });
    }

    private async Task<PlatformSettings> GetSettingsRecord(CancellationToken token) { var settings = await db.PlatformSettings.SingleOrDefaultAsync(token); if (settings is not null) return settings; settings = new PlatformSettings(); db.PlatformSettings.Add(settings); await db.SaveChangesAsync(token); return settings; }
    private async Task<bool> IsPlatformOwner() { var user = await users.GetUserAsync(User); return user?.IsPlatformOwner == true; }
    private async Task Audit(string action, string entityType, Guid? entityId, object metadata, CancellationToken token) { var user = await users.GetUserAsync(User); db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user?.Id, ActorName = user?.DisplayName ?? "System", Action = action, EntityType = entityType, EntityId = entityId, MetadataJson = JsonSerializer.Serialize(metadata) }); await Task.CompletedTask; }
}

public sealed record UpdatePlatformSettingsRequest(string PlatformName, string? SupportEmail, string? DefaultCurrency, int DefaultTrialDays, int DataRetentionDays, bool MaintenanceMode, string? StatusMessage);
public sealed record SetPlatformAdminActiveRequest(bool IsActive);
public sealed record ResetPlatformAdminPasswordRequest(string NewPassword);
public sealed record CreatePlatformSupportCaseRequest(Guid AcademyId, string Subject, string? Priority, string? Description);
public sealed record UpdatePlatformSupportCaseRequest(string Status, string Priority);
public sealed record CreatePlatformBillingInvoiceRequest(Guid AcademyId, string InvoiceNumber, decimal Amount, string? Currency, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate);
public sealed record UpdatePlatformInvoiceStatusRequest(string Status);
