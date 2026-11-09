using AcademyDesk.Api.Data;
using AcademyDesk.Api.ReadModels;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/fee-reminders")]
public sealed class FeeRemindersController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<FeeReminderHistory>>> History(Guid academyId, CancellationToken token) =>
        Ok(await dbContext.Notifications.AsNoTracking()
            .Where(x => x.AcademyId == academyId && x.Title == "Fee payment reminder")
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new FeeReminderHistory(x.Id, x.Title, x.Channel, x.Status, x.SentAtUtc))
            .ToListAsync(token));

    [HttpPost]
    public async Task<ActionResult<FeeReminderResult>> Queue(Guid academyId, QueueFeeReminderRequest request, CancellationToken token)
    {
        // This queue has no external-provider consent/connection workflow.
        if (request.Channel is not null && !string.Equals(request.Channel.Trim(), "InApp", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Fee reminders support in-app notifications only." });
        var invoices = dbContext.Invoices.Where(x => x.AcademyId == academyId && x.Status.ToLower() != "cancelled");
        if (request.InvoiceId.HasValue) invoices = invoices.Where(x => x.Id == request.InvoiceId.Value);
        else invoices = invoices.Where(x => x.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow));
        var invoiceList = await invoices.ToListAsync(token);
        if (invoiceList.Count == 0) return Ok(new FeeReminderResult(0, "No matching invoices need a reminder."));
        var ids = invoiceList.Select(x => x.Id).ToArray();
        var paid = await InvoiceReceiptReadModel.PaidAsync(dbContext, academyId, ids, token);
        var queued = 0;
        foreach (var invoice in invoiceList)
        {
            var balance = invoice.TotalAmount - invoice.AdjustedAmount - paid.GetValueOrDefault((invoice.Id, InvoiceReceiptReadModel.CurrencyKey(invoice.Currency)));
            if (balance <= 0) continue;
            dbContext.Notifications.Add(new Notification { AcademyId = academyId, RecipientId = invoice.StudentId, RecipientType = "Student", Title = "Fee payment reminder", Message = $"Your outstanding balance is {invoice.Currency} {balance:0.00}. Invoice: {invoice.InvoiceNumber}. Due date: {invoice.DueDate:yyyy-MM-dd}.", Channel = "InApp" });
            queued++;
        }
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "FeeRemindersQueued", EntityType = "Invoice", MetadataJson = $"{{\"count\":{queued}}}" });
        await dbContext.SaveChangesAsync(token);
        return Ok(new FeeReminderResult(queued, $"Queued {queued} fee reminder(s)."));
    }
}
public sealed record QueueFeeReminderRequest(Guid? InvoiceId, string? Channel);
public sealed record FeeReminderResult(int QueuedCount, string Message);
public sealed record FeeReminderHistory(Guid Id, string Title, string Channel, string Status, DateTime? SentAtUtc);
