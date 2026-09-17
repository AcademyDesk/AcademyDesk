using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/fee-reminders")]
public sealed class FeeRemindersController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FeeReminderResult>> Queue(Guid academyId, QueueFeeReminderRequest request, CancellationToken token)
    {
        var invoices = dbContext.Invoices.Where(x => x.AcademyId == academyId);
        if (request.InvoiceId.HasValue) invoices = invoices.Where(x => x.Id == request.InvoiceId.Value);
        else invoices = invoices.Where(x => x.DueDate <= DateOnly.FromDateTime(DateTime.UtcNow));
        var invoiceList = await invoices.ToListAsync(token);
        if (invoiceList.Count == 0) return Ok(new FeeReminderResult(0, "No matching invoices need a reminder."));
        var ids = invoiceList.Select(x => x.Id).ToArray();
        var paid = await dbContext.Payments.Where(x => ids.Contains(x.InvoiceId) && x.Status == "Completed").GroupBy(x => x.InvoiceId).Select(x => new { InvoiceId = x.Key, Total = x.Sum(p => p.Amount) }).ToDictionaryAsync(x => x.InvoiceId, x => x.Total, token);
        var queued = 0;
        foreach (var invoice in invoiceList)
        {
            var balance = invoice.TotalAmount - paid.GetValueOrDefault(invoice.Id);
            if (balance <= 0) continue;
            dbContext.Notifications.Add(new Notification { AcademyId = academyId, RecipientId = invoice.StudentId, RecipientType = "Student", Title = "Fee payment reminder", Message = $"Your outstanding balance is {invoice.Currency} {balance:0.00}. Invoice: {invoice.InvoiceNumber}. Due date: {invoice.DueDate:yyyy-MM-dd}.", Channel = request.Channel ?? "InApp" });
            queued++;
        }
        dbContext.AuditLogs.Add(new AuditLog { AcademyId = academyId, Action = "FeeRemindersQueued", EntityType = "Invoice", MetadataJson = $"{{\"count\":{queued}}}" });
        await dbContext.SaveChangesAsync(token);
        return Ok(new FeeReminderResult(queued, $"Queued {queued} fee reminder(s)."));
    }
}
public sealed record QueueFeeReminderRequest(Guid? InvoiceId, string? Channel);
public sealed record FeeReminderResult(int QueuedCount, string Message);
