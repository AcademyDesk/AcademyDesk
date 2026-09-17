using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;
[ApiController]
[Route("api/academies/{academyId:guid}/imports/students")]
public sealed class StudentImportsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpPost("validate")]
    public ActionResult Validate(Guid academyId, StudentImportRequest request) => Ok(ValidateRows(request.Rows));
    [HttpPost]
    public async Task<ActionResult> Import(Guid academyId, StudentImportRequest request, CancellationToken token)
    {
        var result = ValidateRows(request.Rows); if (result.Errors.Count > 0) return BadRequest(result);
        var rows = request.Rows ?? []; var emails = rows.Where(x => !string.IsNullOrWhiteSpace(x.Email)).Select(x => x.Email!.Trim().ToLower()).ToList();
        var existing = await db.Students.Where(x => x.AcademyId == academyId && x.Email != null && emails.Contains(x.Email.ToLower())).Select(x => x.Email!.ToLower()).ToListAsync(token);
        if (existing.Count > 0) return Conflict(new { message = "Some email addresses already exist in this academy.", emails = existing });
        db.Students.AddRange(rows.Select(row => new Student { AcademyId = academyId, FirstName = row.FirstName.Trim(), LastName = row.LastName.Trim(), StudentNumber = row.StudentNumber?.Trim(), Email = row.Email?.Trim(), Phone = row.Phone?.Trim(), AdmissionDate = row.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow) })); await db.SaveChangesAsync(token);
        return Ok(new { imported = rows.Count });
    }
    private static StudentImportValidation ValidateRows(IReadOnlyList<StudentImportRow>? rows)
    {
        var errors = new List<object>(); if (rows is null || rows.Count == 0) errors.Add(new { row = 0, message = "Provide at least one student row." });
        if (rows is not null) for (var index = 0; index < rows.Count; index++) { var row = rows[index]; if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName)) errors.Add(new { row = index + 1, message = "First name and last name are required." }); if (!string.IsNullOrWhiteSpace(row.Email) && !row.Email.Contains('@')) errors.Add(new { row = index + 1, message = "Email address is invalid." }); }
        return new StudentImportValidation(rows?.Count ?? 0, errors);
    }
}
public sealed record StudentImportRequest(IReadOnlyList<StudentImportRow>? Rows);
public sealed record StudentImportRow(string FirstName, string LastName, string? StudentNumber, string? Email, string? Phone, DateOnly? AdmissionDate);
public sealed record StudentImportValidation(int RowCount, IReadOnlyList<object> Errors);
