using System.Globalization;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

internal sealed record InvoiceIssuancePlan(Guid StudentId, string StudentName, string? StudentNumber, bool StudentActive,
    Guid? FeePlanId, string? FeePlanName, decimal? FeePlanAmount, decimal? RequestedAmount,
    decimal Amount, string Currency, DateOnly IssuedDate, DateOnly DueDate)
{
    public string AmountExact => Amount.ToString("0.00", CultureInfo.InvariantCulture);
}

internal static class InvoiceIssuance
{
    internal static async Task<(InvoiceIssuancePlan? Plan, string? Error)> ReviewAsync(AcademyDeskDbContext db,
        Guid academy, CreateInvoiceRequest request, DateTime utcNow, CancellationToken token)
    {
        var student = await db.Students.AsNoTracking().Where(x => x.Id == request.StudentId && x.AcademyId == academy)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.StudentNumber, x.IsActive }).SingleOrDefaultAsync(token);
        if (student is null) return (null, "The student does not belong to this academy.");

        FeePlan? plan = null;
        if (request.FeePlanId is { } planId)
        {
            plan = await db.FeePlans.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == planId && x.AcademyId == academy && x.IsActive, token);
            if (plan is null) return (null, "The fee plan does not belong to this academy.");
        }
        var amount = request.Amount ?? plan?.Amount ?? 0;
        if (amount <= 0) return (null, "A positive invoice amount is required.");
        if (amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount)
            return (null, "Invoice amount must fit within 16 whole digits and two decimal places.");

        return (new(student.Id, (student.FirstName + " " + student.LastName).Trim(), student.StudentNumber, student.IsActive,
            plan?.Id, plan?.Name, plan?.Amount, request.Amount, amount, plan?.Currency ?? "INR",
            DateOnly.FromDateTime(utcNow), request.DueDate ?? DateOnly.FromDateTime(utcNow.AddDays(7))), null);
    }

    internal static Invoice Create(AcademyDeskDbContext db, Guid academy, InvoiceIssuancePlan plan, DateTime utcNow)
    {
        var number = FormattableString.Invariant($"INV-{utcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}");
        var invoice = new Invoice { AcademyId = academy, InvoiceNumber = number, StudentId = plan.StudentId,
            FeePlanId = plan.FeePlanId, TotalAmount = plan.Amount, Currency = plan.Currency, IssuedDate = plan.IssuedDate,
            DueDate = plan.DueDate };
        db.Invoices.Add(invoice);
        return invoice;
    }
}
