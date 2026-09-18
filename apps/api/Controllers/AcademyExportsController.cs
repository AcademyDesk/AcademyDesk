using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/exports")]
public sealed class AcademyExportsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpGet("{resource}")]
    public async Task<ActionResult> Export(Guid academyId, string resource, CancellationToken token)
    {
        string csv = resource.ToLowerInvariant() switch
        {
            "students" => "StudentNumber,FirstName,LastName,Email,Phone,Active\n" + string.Join('\n', await db.Students.Where(x => x.AcademyId == academyId).OrderBy(x => x.LastName).Select(x => Csv(x.StudentNumber,x.FirstName,x.LastName,x.Email,x.Phone,x.IsActive)).ToListAsync(token)),
            "guardians" => "FirstName,LastName,Email,Phone,Active\n" + string.Join('\n', await db.Guardians.Where(x => x.AcademyId == academyId).OrderBy(x => x.LastName).Select(x => Csv(x.FirstName,x.LastName,x.Email,x.Phone,x.IsActive)).ToListAsync(token)),
            "enrollments" => "StudentId,BatchId,StartDate,EndDate,Status\n" + string.Join('\n', await db.Enrollments.Where(x => x.AcademyId == academyId).OrderByDescending(x => x.StartDate).Select(x => Csv(x.StudentId,x.BatchId,x.StartDate,x.EndDate,x.Status)).ToListAsync(token)),
            "invoices" => "InvoiceNumber,GrossAmount,AdjustedAmount,NetAmount,DueDate,Status\n" + string.Join('\n', await db.Invoices.Where(x => x.AcademyId == academyId).OrderByDescending(x => x.IssuedDate).Select(x => Csv(x.InvoiceNumber,x.TotalAmount,x.AdjustedAmount,x.TotalAmount-x.AdjustedAmount,x.DueDate,x.Status)).ToListAsync(token)),
            "payments" => "InvoiceId,Amount,Method,Status,Reference,ReconciliationReference,ReconciledAt\n" + string.Join('\n', await db.Payments.Where(x => x.AcademyId == academyId).OrderByDescending(x => x.PaidAtUtc).Select(x => Csv(x.InvoiceId,x.Amount,x.Method,x.Status,x.Reference,x.ReconciliationReference,x.ReconciledAtUtc)).ToListAsync(token)),
            _ => string.Empty
        };
        if (string.IsNullOrEmpty(csv)) return NotFound();
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"academydesk-{resource}.csv");
    }
    private static string Csv(params object?[] values) => string.Join(',', values.Select(value => $"\"{(value?.ToString() ?? string.Empty).Replace("\"", "\"\"")}\""));
}
