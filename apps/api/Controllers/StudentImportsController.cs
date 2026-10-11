using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;
[ApiController]
[Route("api/academies/{academyId:guid}/imports/students")]
public sealed class StudentImportsController(AcademyDeskDbContext db) : ControllerBase
{
    [HttpPost("validate")]
    public ActionResult Validate(Guid academyId, StudentImportRequest request) => Ok(ValidateRows(request.Rows));
    [HttpPost]
    [AtomicAcademyMutation(IncludePlatformOwner = true)]
    public async Task<ActionResult> Import(Guid academyId, StudentImportRequest request, CancellationToken token)
    {
        var result = ValidateRows(request.Rows); if (result.Errors.Count > 0) return BadRequest(result);
        // Serialize imports for this academy before reading existing identities.
        // The host boundary retains the lock through the batch and success audit.
        await using var ownedTransaction = db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;
        var academyQuery = db.Database.IsSqlServer()
            ? db.Academies.FromSqlInterpolated($"SELECT * FROM [Academies] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {academyId}")
            : db.Academies.Where(x => x.Id == academyId);
        if (await academyQuery.AsNoTracking().SingleOrDefaultAsync(token) is null) return NotFound();
        var rows = request.Rows ?? []; var emails = rows.Where(x => Clean(x.Email) is not null).Select(x => Clean(x.Email)!.ToLowerInvariant()).Distinct().ToList();
        var existing = await db.Students.Where(x => x.AcademyId == academyId && x.Email != null && emails.Contains(x.Email.Trim().ToLower())).Select(x => x.Email!.Trim().ToLower()).Distinct().ToListAsync(token);
        if (existing.Count > 0) return Conflict(new { message = "Some email addresses already exist in this academy.", emails = existing });
        db.Students.AddRange(rows.Select(row => new Student { AcademyId = academyId, FirstName = row.FirstName.Trim(), LastName = row.LastName.Trim(), StudentNumber = Clean(row.StudentNumber), Email = Clean(row.Email), Phone = Clean(row.Phone), AdmissionDate = row.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow) }));
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Conflict(new { message = "Some student identifiers already exist or are repeated in this file. No students were imported." });
        }
        if (ownedTransaction is not null) await ownedTransaction.CommitAsync(token);
        return Ok(new { imported = rows.Count });
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static StudentImportValidation ValidateRows(IReadOnlyList<StudentImportRow>? rows)
    {
        var errors = new List<object>(); if (rows is null || rows.Count == 0) errors.Add(new { row = 0, message = "Provide at least one student row." });
        var emails = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        if (rows is not null) for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index]; var number = index + 1;
            if (row is null) { errors.Add(new { row = number, message = "Provide a student row." }); continue; }
            if (string.IsNullOrWhiteSpace(row.FirstName) || string.IsNullOrWhiteSpace(row.LastName)) errors.Add(new { row = number, message = "First name and last name are required." });
            if (Clean(row.FirstName)?.Length > 120 || Clean(row.LastName)?.Length > 120 || Clean(row.Email)?.Length > 320 || Clean(row.StudentNumber)?.Length > 50 || Clean(row.Phone)?.Length > 30)
                errors.Add(new { row = number, message = "A student field exceeds its supported length (names120, email320, number50, phone30)." });
            var email = Clean(row.Email);
            if (email is null) continue;
            if (!email.Contains('@')) errors.Add(new { row = number, message = "Email address is invalid." });
            if (!emails.TryGetValue(email, out var occurrences)) emails[email] = occurrences = [];
            occurrences.Add(number);
        }
        foreach (var occurrences in emails.Values.Where(x => x.Count > 1))
            foreach (var number in occurrences) errors.Add(new { row = number, message = $"Email address is repeated in this file (rows {string.Join(", ", occurrences)})." });
        return new StudentImportValidation(rows?.Count ?? 0, errors);
    }
}
public sealed record StudentImportRequest(IReadOnlyList<StudentImportRow>? Rows);
public sealed record StudentImportRow(string FirstName, string LastName, string? StudentNumber, string? Email, string? Phone, DateOnly? AdmissionDate);
public sealed record StudentImportValidation(int RowCount, IReadOnlyList<object> Errors);
