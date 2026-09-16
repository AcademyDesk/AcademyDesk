using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/fee-plans")]
public sealed class FeePlansController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeePlanSummary>>> List(Guid academyId, CancellationToken cancellationToken) => Ok(await dbContext.FeePlans.AsNoTracking().Where(x => x.AcademyId == academyId && x.IsActive).OrderBy(x => x.Name).Select(x => new FeePlanSummary(x.Id, x.Name, x.Amount, x.Currency, x.Frequency, x.IsActive)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<FeePlanSummary>> Create(Guid academyId, CreateFeePlanRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Amount <= 0) return BadRequest(new { message = "Name and a positive amount are required." });
        var plan = new FeePlan { AcademyId = academyId, Name = request.Name.Trim(), Amount = request.Amount, Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim().ToUpperInvariant(), Frequency = string.IsNullOrWhiteSpace(request.Frequency) ? "Monthly" : request.Frequency.Trim() };
        dbContext.FeePlans.Add(plan); await dbContext.SaveChangesAsync(cancellationToken);
        return Created($"/api/academies/{academyId}/fee-plans/{plan.Id}", new FeePlanSummary(plan.Id, plan.Name, plan.Amount, plan.Currency, plan.Frequency, plan.IsActive));
    }
}

public sealed record CreateFeePlanRequest(string Name, decimal Amount, string? Currency, string? Frequency);
public sealed record FeePlanSummary(Guid Id, string Name, decimal Amount, string Currency, string Frequency, bool IsActive);
