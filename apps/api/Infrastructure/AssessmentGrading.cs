using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Infrastructure;

public static class AssessmentGrading
{
    public sealed record Resolution(string? Grade, bool IsManual, string? Error = null);

    public static async Task<Resolution> ResolveAsync(AcademyDeskDbContext db, Assessment assessment,
        decimal score, string? grade, bool? isManual, CancellationToken token)
    {
        if (assessment.MaxScore <= 0 || score < 0 || score > assessment.MaxScore)
            return new(null, false, "Score must be within a positive assessment range.");
        if (decimal.Round(score, 2) != score)
            return new(null, false, "Score must have at most two decimal places.");
        var value = string.IsNullOrWhiteSpace(grade) ? null : grade.Trim();
        // Older clients retain their explicit-grade contract; new clients send a mode.
        var manual = isManual ?? value is not null;
        if (manual)
            return value is null || value.Length > 30
                ? new(null, true, "Enter a manual grade of 1 to 30 characters.")
                : new(value, true);
        if (!assessment.GradingSchemeId.HasValue) return new(null, false);
        var scheme = await db.GradingSchemes.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == assessment.GradingSchemeId && x.AcademyId == assessment.AcademyId, token);
        if (scheme is null || scheme.PassingPercent < 0 || scheme.PassingPercent > 100)
            return new(null, false, "This assessment's grading scheme is unavailable. Contact your academy administrator.");
        return new(score * 100m / assessment.MaxScore >= scheme.PassingPercent ? "Pass" : "Fail", false);
    }
}
