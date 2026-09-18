using System.Text.Json;
using AcademyDesk.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Controllers;

[ApiController]
[Route("api/academies/{academyId:guid}/teachers/{teacherId:guid}/compensation")]
public sealed class TeacherCompensationController(AcademyDeskDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TeacherCompensationSummary>> Get(Guid academyId, Guid teacherId, CancellationToken token)
    {
        var teacher = await dbContext.Teachers.AsNoTracking().SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == teacherId, token);
        return teacher is null ? NotFound() : Ok(Read(teacherId, teacher.CompensationJson));
    }

    [HttpPut]
    public async Task<ActionResult<TeacherCompensationSummary>> Save(Guid academyId, Guid teacherId, UpdateTeacherCompensationRequest request, CancellationToken token)
    {
        var teacher = await dbContext.Teachers.SingleOrDefaultAsync(x => x.AcademyId == academyId && x.Id == teacherId, token);
        if (teacher is null) return NotFound();
        if (request.Model is not ("Monthly" or "Hourly")) return BadRequest(new { message = "Choose monthly salary or hourly rates." });
        if (request.Model == "Monthly" && request.MonthlySalary is null) return BadRequest(new { message = "Enter the monthly salary." });
        if (request.Model == "Hourly" && request.StandardHourlyRate is null) return BadRequest(new { message = "Enter the standard hourly rate." });
        if (new[] { request.MonthlySalary, request.StandardHourlyRate, request.BeginnerHourlyRate, request.IntermediateHourlyRate, request.AdvancedHourlyRate }.Any(value => value is < 0)) return BadRequest(new { message = "Payment rates cannot be negative." });

        var summary = request.Model == "Monthly"
            ? new TeacherCompensationSummary(teacherId, "Monthly", request.MonthlySalary, null, null, null, null, request.EffectiveFrom)
            : new TeacherCompensationSummary(teacherId, "Hourly", null, request.StandardHourlyRate, request.BeginnerHourlyRate, request.IntermediateHourlyRate, request.AdvancedHourlyRate, request.EffectiveFrom);
        teacher.CompensationJson = JsonSerializer.Serialize(summary);
        await dbContext.SaveChangesAsync(token);
        return Ok(summary);
    }

    private static TeacherCompensationSummary Read(Guid teacherId, string? compensationJson)
    {
        if (string.IsNullOrWhiteSpace(compensationJson)) return new TeacherCompensationSummary(teacherId, "", null, null, null, null, null, null);
        try
        {
            using var document = JsonDocument.Parse(compensationJson);
            var root = document.RootElement;
            return new TeacherCompensationSummary(teacherId, Text(root, "model"), Number(root, "baseMonthlySalary", "monthlySalary"), Number(root, "baseHourlyRate", "standardHourlyRate"), Number(root, "beginnerHourlyRate"), Number(root, "intermediateHourlyRate"), Number(root, "advancedHourlyRate"), Date(root, "effectiveFrom"));
        }
        catch (JsonException)
        {
            return new TeacherCompensationSummary(teacherId, "", null, null, null, null, null, null);
        }
    }

    private static JsonElement? Property(JsonElement root, params string[] names)
    {
        foreach (var property in root.EnumerateObject())
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))) return property.Value;
        return null;
    }
    private static string Text(JsonElement root, string name) => Property(root, name)?.ToString() ?? "";
    private static decimal? Number(JsonElement root, params string[] names) { var value = Property(root, names); return value is null ? null : value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetDecimal(out var number) ? number : decimal.TryParse(value.Value.ToString(), out var parsed) ? parsed : null; }
    private static DateOnly? Date(JsonElement root, string name) { var value = Property(root, name); return value is null ? null : DateOnly.TryParse(value.Value.ToString(), out var parsed) ? parsed : null; }
}

public sealed record TeacherCompensationSummary(Guid TeacherId, string Model, decimal? MonthlySalary, decimal? StandardHourlyRate, decimal? BeginnerHourlyRate, decimal? IntermediateHourlyRate, decimal? AdvancedHourlyRate, DateOnly? EffectiveFrom);
public sealed record UpdateTeacherCompensationRequest(string Model, decimal? MonthlySalary, decimal? StandardHourlyRate, decimal? BeginnerHourlyRate, decimal? IntermediateHourlyRate, decimal? AdvancedHourlyRate, DateOnly? EffectiveFrom);
