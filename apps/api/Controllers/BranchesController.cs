using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/branches")]
public sealed class BranchesController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BranchSummary>>> List(
        Guid academyId,
        CancellationToken cancellationToken)
    {
        var academyExists = await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken);
        if (!academyExists)
        {
            return NotFound();
        }

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(x => x.AcademyId == academyId)
            .OrderBy(x => x.Name)
            .Select(x => new BranchSummary(x.Id, x.Name, x.City, x.State, x.PostalCode, x.IsActive, x.AddressLine1))
            .ToListAsync(cancellationToken);

        return Ok(branches);
    }

    [HttpPost]
    public async Task<ActionResult<BranchSummary>> Create(
        Guid academyId,
        CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Academies.AnyAsync(x => x.Id == academyId, cancellationToken))
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Branch name is required." });
        }

        var branch = new Branch
        {
            AcademyId = academyId,
            Name = request.Name.Trim(),
            AddressLine1 = request.AddressLine1?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim()
        };

        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new BranchSummary(
            branch.Id,
            branch.Name,
            branch.City,
            branch.State,
            branch.PostalCode,
            branch.IsActive,
            branch.AddressLine1);

        return CreatedAtAction(nameof(List), new { academyId }, response);
    }
    [HttpPut("{branchId:guid}")]
    public async Task<ActionResult<BranchSummary>> Update(Guid academyId,Guid branchId,UpdateBranchRequest request,CancellationToken token){var x=await dbContext.Branches.SingleOrDefaultAsync(b=>b.Id==branchId&&b.AcademyId==academyId,token);if(x is null)return NotFound();if(string.IsNullOrWhiteSpace(request.Name))return BadRequest();x.Name=request.Name.Trim();x.AddressLine1=request.AddressLine1?.Trim();x.City=request.City?.Trim();x.State=request.State?.Trim();x.PostalCode=request.PostalCode?.Trim();x.IsActive=request.IsActive;await dbContext.SaveChangesAsync(token);return Ok(new BranchSummary(x.Id,x.Name,x.City,x.State,x.PostalCode,x.IsActive,x.AddressLine1));}
}

public sealed record CreateBranchRequest(
    string Name,
    string? AddressLine1,
    string? City,
    string? State,
    string? PostalCode);

public sealed record BranchSummary(
    Guid Id,
    string Name,
    string? City,
    string? State,
    string? PostalCode,
    bool IsActive,
    string? AddressLine1 = null);
public sealed record UpdateBranchRequest(string Name,string? AddressLine1,string? City,string? State,string? PostalCode,bool IsActive);
