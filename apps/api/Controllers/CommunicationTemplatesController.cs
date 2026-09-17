using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/academies/{academyId:guid}/communication-templates")]
public sealed class CommunicationTemplatesController(
    AcademyDeskDbContext dbContext,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    private static readonly string[] Channels = ["Email", "WhatsApp"];
    private static readonly string[] Categories = ["Utility", "Marketing", "Authentication", "Transactional"];
    private static readonly string[] Statuses = ["Draft", "Approved", "Disabled"];

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CommunicationTemplateSummary>>> List(Guid academyId, CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();
        return Ok(await dbContext.CommunicationTemplates.AsNoTracking().Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Channel).ThenBy(x => x.Name)
            .Select(x => new CommunicationTemplateSummary(x.Id, x.Channel, x.Name, x.TemplateKey, x.Category, x.TemplateGroup, x.Status, x.Language, x.ProviderTemplateName, x.Subject, x.Body, x.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CommunicationTemplateSummary>> Create(Guid academyId, SaveCommunicationTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();
        if (!TryValidate(request, out var error)) return BadRequest(new { message = error });
        var template = new CommunicationTemplate { AcademyId = academyId, Channel = Canonical(Channels, request.Channel)!, Name = request.Name.Trim(), TemplateKey = request.TemplateKey.Trim().ToLowerInvariant(), Category = Canonical(Categories, request.Category)!, TemplateGroup = Clean(request.TemplateGroup) ?? "General", Status = Canonical(Statuses, request.Status)!, Language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim(), ProviderTemplateName = Clean(request.ProviderTemplateName), Subject = Clean(request.Subject), Body = request.Body.Trim(), IsActive = request.IsActive };
        dbContext.CommunicationTemplates.Add(template);
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "CommunicationTemplateCreated", EntityType = "CommunicationTemplate", EntityId = template.Id, MetadataJson = JsonSerializer.Serialize(new { template.Channel, template.TemplateKey, template.Status }) });
        await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/communication-templates/{template.Id}", ToSummary(template));
    }

    [HttpGet("starter-templates")]
    public async Task<ActionResult<IReadOnlyList<StarterTemplateDefinition>>> StarterTemplateCatalogue(Guid academyId)
    {
        if (!await IsOwner(academyId)) return Forbid();
        return Ok(StarterTemplates.Select(x => new StarterTemplateDefinition(x.Id, x.Channel, x.TemplateGroup, x.Name, x.TemplateKey, x.Category, x.Subject, x.Body)).ToList());
    }

    [HttpPost("starter-templates")]
    public async Task<ActionResult<StarterTemplateResult>> AddStarterTemplates(Guid academyId, AddStarterTemplatesRequest request, CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();
        if (request.TemplateIds is null || request.TemplateIds.Count == 0) return BadRequest(new { message = "Select at least one starter template." });
        var selectedIds = request.TemplateIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedTemplates = StarterTemplates.Where(x => selectedIds.Contains(x.Id)).ToList();
        if (selectedTemplates.Count == 0) return BadRequest(new { message = "The selected templates are not valid." });

        var existingKeys = await dbContext.CommunicationTemplates.AsNoTracking()
            .Where(x => x.AcademyId == academyId)
            .Select(x => new { x.Channel, x.TemplateKey })
            .ToListAsync(cancellationToken);
        var existing = existingKeys.Select(x => $"{x.Channel}|{x.TemplateKey}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions = selectedTemplates
            .Where(x => !existing.Contains($"{x.Channel}|{x.TemplateKey}"))
            .Select(x => new CommunicationTemplate
            {
                AcademyId = academyId,
                Channel = x.Channel,
                Name = x.Name,
                TemplateKey = x.TemplateKey,
                Category = x.Category,
                TemplateGroup = x.TemplateGroup,
                Status = "Draft",
                Language = "en",
                Subject = x.Subject,
                Body = x.Body
            })
            .ToList();
        if (additions.Count > 0)
        {
            dbContext.CommunicationTemplates.AddRange(additions);
            dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "StarterCommunicationTemplatesAdded", EntityType = "CommunicationTemplate", MetadataJson = JsonSerializer.Serialize(new { count = additions.Count }) });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return Ok(new StarterTemplateResult(additions.Count, additions.Count == 0 ? "All starter templates already exist." : $"Added {additions.Count} starter template(s). Review each one before use."));
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<CommunicationTemplateSummary>> Update(Guid academyId, Guid templateId, SaveCommunicationTemplateRequest request, CancellationToken cancellationToken)
    {
        if (!await IsOwner(academyId)) return Forbid();
        if (!TryValidate(request, out var error)) return BadRequest(new { message = error });
        var template = await dbContext.CommunicationTemplates.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == templateId, cancellationToken);
        if (template is null) return NotFound();
        template.Channel = Canonical(Channels, request.Channel)!; template.Name = request.Name.Trim(); template.TemplateKey = request.TemplateKey.Trim().ToLowerInvariant(); template.Category = Canonical(Categories, request.Category)!; template.TemplateGroup = Clean(request.TemplateGroup) ?? "General"; template.Status = Canonical(Statuses, request.Status)!; template.Language = string.IsNullOrWhiteSpace(request.Language) ? "en" : request.Language.Trim(); template.ProviderTemplateName = Clean(request.ProviderTemplateName); template.Subject = Clean(request.Subject); template.Body = request.Body.Trim(); template.IsActive = request.IsActive; template.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToSummary(template));
    }

    private static bool TryValidate(SaveCommunicationTemplateRequest request, out string error)
    {
        if (Canonical(Channels, request.Channel) is null) { error = "Channel must be Email or WhatsApp."; return false; }
        if (Canonical(Categories, request.Category) is null) { error = "Choose a valid template category."; return false; }
        if (Canonical(Statuses, request.Status) is null) { error = "Choose a valid template status."; return false; }
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TemplateKey) || string.IsNullOrWhiteSpace(request.Body)) { error = "Name, template key, and message body are required."; return false; }
        error = string.Empty; return true;
    }
    private async Task<bool> IsOwner(Guid academyId) { var user = await userManager.GetUserAsync(User); return user?.AcademyId == academyId && await userManager.IsInRoleAsync(user, "Owner"); }
    private static string? Canonical(IEnumerable<string> values, string? value) => values.SingleOrDefault(x => x.Equals(value, StringComparison.OrdinalIgnoreCase));
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static CommunicationTemplateSummary ToSummary(CommunicationTemplate x) => new(x.Id, x.Channel, x.Name, x.TemplateKey, x.Category, x.TemplateGroup, x.Status, x.Language, x.ProviderTemplateName, x.Subject, x.Body, x.IsActive);

    private static readonly StarterTemplate[] StarterTemplates =
    [
        new("whatsapp-fee-due", "WhatsApp", "Finance", "Fee due reminder", "fee_due_reminder", "Utility", null, "Hello {{guardian_name}}, this is a reminder that {{student_name}}'s {{fee_month}} fee of {{currency}} {{amount_due}} is due on {{due_date}}. Invoice: {{invoice_number}}. Please contact {{academy_name}} if you need help."),
        new("whatsapp-payment-receipt", "WhatsApp", "Finance", "Payment receipt", "payment_receipt", "Utility", null, "Hello {{guardian_name}}, we received {{currency}} {{payment_amount}} for {{student_name}}'s {{fee_month}} fee. Receipt: {{receipt_number}}. Thank you, {{academy_name}}."),
        new("whatsapp-class-reminder", "WhatsApp", "Classes", "Class reminder", "class_reminder", "Utility", null, "Reminder: {{student_name}} has {{course_name}} on {{class_day}}, {{class_date}} at {{class_time}} with {{teacher_name}}. Venue: {{venue}}."),
        new("whatsapp-class-change", "WhatsApp", "Classes", "Class cancelled or rescheduled", "class_change", "Utility", null, "Update from {{academy_name}}: {{student_name}}'s {{course_name}} class on {{class_date}} at {{class_time}} is {{change_type}}. {{change_details}}"),
        new("whatsapp-holiday", "WhatsApp", "Academy updates", "Holiday notice", "holiday_notice", "Utility", null, "{{academy_name}} will be closed for {{holiday_name}} on {{holiday_date}} ({{holiday_day}}). Classes resume on {{resume_date}}. {{additional_note}}"),
        new("whatsapp-absence", "WhatsApp", "Academic", "Attendance absence alert", "attendance_absence", "Utility", null, "Hello {{guardian_name}}, {{student_name}} was marked absent for {{course_name}} on {{class_date}}. Please reply if this needs correction."),
        new("whatsapp-assessment", "WhatsApp", "Academic", "Assessment reminder", "assessment_reminder", "Utility", null, "Reminder: {{student_name}} has {{assessment_name}} for {{course_name}} on {{assessment_date}} at {{assessment_time}}. {{preparation_note}}"),
        new("whatsapp-event", "WhatsApp", "Academy updates", "Event reminder", "event_reminder", "Utility", null, "{{academy_name}} reminder: {{event_name}} is on {{event_date}} at {{event_time}}. Venue: {{venue}}. {{event_note}}"),
        new("email-fee-due", "Email", "Finance", "Fee due reminder", "fee_due_reminder", "Transactional", "Fee reminder for {{student_name}} – {{fee_month}}", "Dear {{guardian_name}},\n\nThis is a reminder that {{student_name}}'s fee for {{fee_month}} is {{currency}} {{amount_due}}, due on {{due_date}}.\n\nInvoice number: {{invoice_number}}\nPayment link/details: {{payment_details}}\n\nPlease contact us if you need any help.\n\nRegards,\n{{academy_name}}"),
        new("email-payment-receipt", "Email", "Finance", "Payment receipt", "payment_receipt", "Transactional", "Payment received – {{receipt_number}}", "Dear {{guardian_name}},\n\nWe have received {{currency}} {{payment_amount}} for {{student_name}}'s {{fee_month}} fee.\n\nReceipt number: {{receipt_number}}\nPayment date: {{payment_date}}\n\nThank you.\n\nRegards,\n{{academy_name}}"),
        new("email-enrollment", "Email", "Admissions", "Enrollment welcome", "enrollment_welcome", "Transactional", "Welcome to {{academy_name}}, {{student_name}}", "Dear {{guardian_name}},\n\nWelcome to {{academy_name}}. {{student_name}} is enrolled in {{course_name}}.\n\nBatch: {{batch_name}}\nFirst class: {{first_class_date}} at {{first_class_time}}\nTeacher: {{teacher_name}}\nVenue: {{venue}}\n\nWe are delighted to have you with us.\n\nRegards,\n{{academy_name}}"),
        new("email-class-reminder", "Email", "Classes", "Class reminder", "class_reminder", "Transactional", "Class reminder – {{course_name}} on {{class_date}}", "Dear {{guardian_name}},\n\nThis is a reminder that {{student_name}} has {{course_name}} on {{class_day}}, {{class_date}} at {{class_time}}.\n\nTeacher: {{teacher_name}}\nVenue: {{venue}}\n\nRegards,\n{{academy_name}}"),
        new("email-holiday", "Email", "Academy updates", "Holiday notice", "holiday_notice", "Transactional", "Holiday notice: {{holiday_name}}", "Dear {{guardian_name}},\n\n{{academy_name}} will remain closed for {{holiday_name}} on {{holiday_date}} ({{holiday_day}}).\n\nClasses resume on {{resume_date}}.\n{{additional_note}}\n\nRegards,\n{{academy_name}}"),
        new("email-absence", "Email", "Academic", "Attendance absence alert", "attendance_absence", "Transactional", "Attendance update for {{student_name}}", "Dear {{guardian_name}},\n\n{{student_name}} was marked absent for {{course_name}} on {{class_date}}.\n\nIf this is incorrect, please contact the academy so we can review it.\n\nRegards,\n{{academy_name}}"),
        new("email-assessment", "Email", "Academic", "Assessment reminder", "assessment_reminder", "Transactional", "Assessment reminder – {{assessment_name}}", "Dear {{guardian_name}},\n\n{{student_name}} has {{assessment_name}} for {{course_name}} on {{assessment_date}} at {{assessment_time}}.\n\n{{preparation_note}}\n\nRegards,\n{{academy_name}}"),
        new("email-event", "Email", "Academy updates", "Event invitation", "event_reminder", "Transactional", "{{event_name}} – {{event_date}}", "Dear {{guardian_name}},\n\nYou are invited to {{event_name}} on {{event_date}} at {{event_time}}.\n\nVenue: {{venue}}\n{{event_note}}\n\nRegards,\n{{academy_name}}")
    ];
}

public sealed record SaveCommunicationTemplateRequest(string Channel, string Name, string TemplateKey, string Category, string? TemplateGroup, string Status, string? Language, string? ProviderTemplateName, string? Subject, string Body, bool IsActive);
public sealed record CommunicationTemplateSummary(Guid Id, string Channel, string Name, string TemplateKey, string Category, string TemplateGroup, string Status, string Language, string? ProviderTemplateName, string? Subject, string Body, bool IsActive);
public sealed record StarterTemplateResult(int AddedCount, string Message);
public sealed record AddStarterTemplatesRequest(IReadOnlyList<string> TemplateIds);
public sealed record StarterTemplateDefinition(string Id, string Channel, string TemplateGroup, string Name, string TemplateKey, string Category, string? Subject, string Body);
internal sealed record StarterTemplate(string Id, string Channel, string TemplateGroup, string Name, string TemplateKey, string Category, string? Subject, string Body);
