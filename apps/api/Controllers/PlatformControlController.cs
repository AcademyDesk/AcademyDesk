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
            TotalBilled = await invoices.Where(x => x.Status != "Draft" && x.Status != "Void").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
            CollectedBilling = await invoices.Where(x => x.Status == "Paid").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
            OutstandingBilling = await invoices.Where(x => x.Status == "Issued" || x.Status == "Overdue").SumAsync(x => (decimal?)x.Amount, token) ?? 0,
            OverdueInvoices = await invoices.CountAsync(x => x.Status == "Overdue", token),
            Plans = await academies.GroupBy(x => x.SubscriptionPlan).Select(x => new { Plan = x.Key, Count = x.Count() }).ToListAsync(token),
            RecentAudit = await db.PlatformAuditEntries.AsNoTracking().OrderByDescending(x => x.OccurredAtUtc).Take(8).Select(x => new { x.Id, x.Action, x.EntityType, x.ActorName, x.OccurredAtUtc }).ToListAsync(token)
        });
    }

    [HttpPost("announcements")]
    public async Task<ActionResult> CreateAnnouncement(CreatePlatformAnnouncementRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message) || request.DisplayHours is < 1 or > 168) return BadRequest(new { message = "Select an academy, message, and duration from 1 to 168 hours." });
        if (!await db.Academies.AnyAsync(x => x.Id == request.AcademyId, token)) return NotFound();
        var notification = new Notification { AcademyId = request.AcademyId, RecipientType = "Academy", Title = request.Title.Trim(), Message = request.Message.Trim(), Channel = "InApp", Status = "Sent", SentAtUtc = DateTime.UtcNow, VariablesJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["important"] = "true", ["expiresAtUtc"] = DateTime.UtcNow.AddHours(request.DisplayHours).ToString("O"), ["audiences"] = "Admin" }) };
        db.Notifications.Add(notification); await Audit("Academy admin announcement published", "Notification", notification.Id, new { request.AcademyId, Audience = "Admin", request.DisplayHours }, token); await db.SaveChangesAsync(token); return Ok(new { notification.Id });
    }

    [HttpGet("announcements")]
    public async Task<ActionResult> ListAnnouncements(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var rows = await db.Notifications.AsNoTracking()
            .Where(x => x.RecipientType == "Academy" && x.RecipientId == null)
            .Join(db.Academies.AsNoTracking(), notification => notification.AcademyId, academy => academy.Id, (notification, academy) => new { notification, academy.Name })
            .OrderByDescending(x => x.notification.CreatedAtUtc)
            .Take(100)
            .ToListAsync(token);
        return Ok(rows.Where(x => HasAdminAudience(x.notification.VariablesJson)).Select(x => new
        {
            x.notification.Id,
            x.notification.AcademyId,
            AcademyName = x.Name,
            x.notification.Title,
            x.notification.Message,
            x.notification.CreatedAtUtc,
            ExpiresAtUtc = ReadAnnouncementExpiry(x.notification.VariablesJson)
        }));
    }

    [HttpGet("academies/{academyId:guid}/onboarding")]
    public async Task<ActionResult> GetTenantOnboarding(Guid academyId, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound();
        return Ok(await db.TenantOnboardingProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId, token));
    }

    [HttpPut("academies/{academyId:guid}/onboarding")]
    public async Task<ActionResult> SaveTenantOnboarding(Guid academyId, TenantOnboardingRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (!await db.Academies.AnyAsync(x => x.Id == academyId, token)) return NotFound();
        var profile = await db.TenantOnboardingProfiles.SingleOrDefaultAsync(x => x.AcademyId == academyId, token);
        if (profile is null) { profile = new TenantOnboardingProfile { AcademyId = academyId }; db.TenantOnboardingProfiles.Add(profile); }
        profile.Status = string.IsNullOrWhiteSpace(request.Status) ? "InProgress" : request.Status.Trim();
        profile.CurrentSection = string.IsNullOrWhiteSpace(request.CurrentSection) ? "Personal" : request.CurrentSection.Trim();
        profile.PrimaryContactName = request.PrimaryContactName?.Trim(); profile.PrimaryContactRole = request.PrimaryContactRole?.Trim(); profile.PrimaryContactEmail = request.PrimaryContactEmail?.Trim(); profile.PrimaryContactPhone = request.PrimaryContactPhone?.Trim(); profile.Country = request.Country?.Trim(); profile.State = request.State?.Trim(); profile.City = request.City?.Trim(); profile.PostalCode = request.PostalCode?.Trim(); profile.AddressLine1 = request.AddressLine1?.Trim(); profile.AddressLine2 = request.AddressLine2?.Trim();
        profile.BusinessType = request.BusinessType?.Trim(); profile.OperatingSince = request.OperatingSince?.Trim(); profile.Website = request.Website?.Trim(); profile.BranchSummary = request.BranchSummary?.Trim();
        profile.FinanceModel = request.FinanceModel?.Trim(); profile.BillingFrequency = request.BillingFrequency?.Trim(); profile.PaymentCollectionMethods = request.PaymentCollectionMethods?.Trim(); profile.TeacherPaymentModels = request.TeacherPaymentModels?.Trim();
        profile.TeacherCount = request.TeacherCount; profile.StudentCount = request.StudentCount; profile.SubjectCount = request.SubjectCount; profile.SubjectTypes = request.SubjectTypes?.Trim(); profile.DeliveryModes = request.DeliveryModes?.Trim(); profile.ClassRatios = request.ClassRatios?.Trim(); profile.BatchAndClassSetup = request.BatchAndClassSetup?.Trim(); profile.OperationalNotes = request.OperationalNotes?.Trim(); profile.DocumentsJson = string.IsNullOrWhiteSpace(request.DocumentsJson) ? "[]" : request.DocumentsJson; profile.UpdatedAtUtc = DateTime.UtcNow;
        await Audit("Tenant onboarding profile saved", "TenantOnboardingProfile", profile.Id, new { academyId, profile.Status, profile.CurrentSection }, token); await db.SaveChangesAsync(token); return Ok(profile);
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

    [HttpGet("activity-logs")]
    public async Task<ActionResult> ActivityLogs(string? scope, DateTime? fromUtc, DateTime? toUtc, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        var includePlatform = !string.Equals(scope, "AcademyAdmin", StringComparison.OrdinalIgnoreCase);
        var includeAdmin = !string.Equals(scope, "PlatformOwner", StringComparison.OrdinalIgnoreCase);
        var platformQuery = db.PlatformAuditEntries.AsNoTracking().AsQueryable();
        var adminQuery = db.AuditLogs.AsNoTracking().AsQueryable();
        if (fromUtc.HasValue) { platformQuery = platformQuery.Where(x => x.OccurredAtUtc >= fromUtc); adminQuery = adminQuery.Where(x => x.OccurredAtUtc >= fromUtc); }
        if (toUtc.HasValue) { platformQuery = platformQuery.Where(x => x.OccurredAtUtc < toUtc); adminQuery = adminQuery.Where(x => x.OccurredAtUtc < toUtc); }
        var platform = includePlatform ? await platformQuery.OrderByDescending(x => x.OccurredAtUtc).Take(500).ToListAsync(token) : [];
        var admin = includeAdmin ? await adminQuery.OrderByDescending(x => x.OccurredAtUtc).Take(500).ToListAsync(token) : [];
        var actorIds = admin.Where(x => x.ActorUserId.HasValue).Select(x => x.ActorUserId!.Value).Distinct().ToList();
        var actors = actorIds.Count == 0 ? new Dictionary<Guid, string>() : await users.Users.Where(x => actorIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.DisplayName ?? x.UserName ?? "Academy admin", token);
        var academyIds = admin.Select(x => x.AcademyId).Distinct().ToList();
        var academyNames = await db.Academies.Where(x => academyIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, token);
        var rows = platform.Select(x => new { Id = $"platform:{x.Id}", Scope = "Platform owner", x.ActorName, AcademyName = (string?)null, x.Action, x.EntityType, x.OccurredAtUtc })
            .Concat(admin.Select(x => new { Id = $"admin:{x.Id}", Scope = "Academy admin", ActorName = x.ActorUserId.HasValue && actors.TryGetValue(x.ActorUserId.Value, out var name) ? name : "Academy admin", AcademyName = academyNames.TryGetValue(x.AcademyId, out var academyName) ? academyName : null, x.Action, x.EntityType, x.OccurredAtUtc }))
            .OrderByDescending(x => x.OccurredAtUtc).Take(500);
        return Ok(rows);
    }

    [HttpDelete("activity-logs")]
    public async Task<ActionResult> DeleteActivityLogs(DeleteActivityLogsRequest request, CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid();
        if (request.ToUtc <= request.FromUtc) return BadRequest(new { message = "Choose a valid start and end date." });
        var includePlatform = !string.Equals(request.Scope, "AcademyAdmin", StringComparison.OrdinalIgnoreCase);
        var includeAdmin = !string.Equals(request.Scope, "PlatformOwner", StringComparison.OrdinalIgnoreCase);
        var platform = includePlatform ? await db.PlatformAuditEntries.Where(x => x.OccurredAtUtc >= request.FromUtc && x.OccurredAtUtc < request.ToUtc).ToListAsync(token) : [];
        var admin = includeAdmin ? await db.AuditLogs.Where(x => x.OccurredAtUtc >= request.FromUtc && x.OccurredAtUtc < request.ToUtc).ToListAsync(token) : [];
        db.PlatformAuditEntries.RemoveRange(platform); db.AuditLogs.RemoveRange(admin);
        await Audit("Activity logs deleted", "ActivityLog", null, new { request.Scope, request.FromUtc, request.ToUtc, PlatformLogs = platform.Count, AdminLogs = admin.Count }, token);
        await db.SaveChangesAsync(token);
        return Ok(new { deleted = platform.Count + admin.Count });
    }

    [HttpGet("health")]
    public async Task<ActionResult> Health(CancellationToken token)
    {
        if (!await IsPlatformOwner()) return Forbid(); var settings = await GetSettingsRecord(token); return Ok(new { Api = "Operational", Database = await db.Database.CanConnectAsync(token) ? "Operational" : "Unavailable", BackgroundJobs = "Not configured", CommunicationProviders = "Not configured", MaintenanceMode = settings.MaintenanceMode, StatusMessage = settings.StatusMessage });
    }

    private async Task<PlatformSettings> GetSettingsRecord(CancellationToken token)
    {
        // Platform settings are singleton data. Older local runs could create more
        // than one row, so retain the most recently updated record and clean up the
        // stale copies instead of making the Settings screen unavailable.
        var records = await db.PlatformSettings
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(token);
        if (records.Count > 0)
        {
            if (records.Count > 1)
            {
                db.PlatformSettings.RemoveRange(records.Skip(1));
                await db.SaveChangesAsync(token);
            }
            return records[0];
        }

        var settings = new PlatformSettings();
        db.PlatformSettings.Add(settings);
        await db.SaveChangesAsync(token);
        return settings;
    }
    private async Task<bool> IsPlatformOwner() { var user = await users.GetUserAsync(User); return user?.IsPlatformOwner == true; }
    private static bool HasAdminAudience(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson)) return false;
        try { using var document = JsonDocument.Parse(variablesJson); return document.RootElement.TryGetProperty("audiences", out var audience) && audience.GetString()?.Contains("Admin", StringComparison.OrdinalIgnoreCase) == true; }
        catch (JsonException) { return false; }
    }
    private static string? ReadAnnouncementExpiry(string? variablesJson)
    {
        if (string.IsNullOrWhiteSpace(variablesJson)) return null;
        try { using var document = JsonDocument.Parse(variablesJson); return document.RootElement.TryGetProperty("expiresAtUtc", out var expiry) ? expiry.GetString() : null; }
        catch (JsonException) { return null; }
    }
    private async Task Audit(string action, string entityType, Guid? entityId, object metadata, CancellationToken token) { var user = await users.GetUserAsync(User); db.PlatformAuditEntries.Add(new PlatformAuditEntry { ActorUserId = user?.Id, ActorName = user?.DisplayName ?? "System", Action = action, EntityType = entityType, EntityId = entityId, MetadataJson = JsonSerializer.Serialize(metadata) }); await Task.CompletedTask; }
}

public sealed record UpdatePlatformSettingsRequest(string PlatformName, string? SupportEmail, string? DefaultCurrency, int DefaultTrialDays, int DataRetentionDays, bool MaintenanceMode, string? StatusMessage);
public sealed record DeleteActivityLogsRequest(DateTime FromUtc, DateTime ToUtc, string? Scope);
public sealed record CreatePlatformAnnouncementRequest(Guid AcademyId, string Title, string Message, int DisplayHours);
public sealed record TenantOnboardingRequest(string? Status, string? CurrentSection, string? PrimaryContactName, string? PrimaryContactRole, string? PrimaryContactEmail, string? PrimaryContactPhone, string? Country, string? State, string? City, string? PostalCode, string? AddressLine1, string? AddressLine2, string? BusinessType, string? OperatingSince, string? Website, string? BranchSummary, string? FinanceModel, string? BillingFrequency, string? PaymentCollectionMethods, string? TeacherPaymentModels, int? TeacherCount, int? StudentCount, int? SubjectCount, string? SubjectTypes, string? DeliveryModes, string? ClassRatios, string? BatchAndClassSetup, string? OperationalNotes, string? DocumentsJson);
public sealed record SetPlatformAdminActiveRequest(bool IsActive);
public sealed record ResetPlatformAdminPasswordRequest(string NewPassword);
public sealed record CreatePlatformSupportCaseRequest(Guid AcademyId, string Subject, string? Priority, string? Description);
public sealed record UpdatePlatformSupportCaseRequest(string Status, string Priority);
public sealed record CreatePlatformBillingInvoiceRequest(Guid AcademyId, string InvoiceNumber, decimal Amount, string? Currency, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly DueDate);
public sealed record UpdatePlatformInvoiceStatusRequest(string Status);
