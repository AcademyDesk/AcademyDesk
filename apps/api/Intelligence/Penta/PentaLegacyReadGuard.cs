using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Denial-only limit of the legacy learner/fees pilot. Does not extract
/// arguments or implement unsupported domains. The typed query route replaces
/// this pilot only after its explicit host/provider capability negotiation.</summary>
public static class PentaLegacyReadGuard
{
    public static bool Unsupported(string text) => Regex.IsMatch(text,
        @"\b(?:teachers?|faculty|teaching staff|batches|batch|attendance|scheduling|reschedule|payroll|salary|create|delete|enroll|export|send)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50));
}
