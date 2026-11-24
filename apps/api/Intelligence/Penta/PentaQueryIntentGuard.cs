using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Denial-only semantic assertions, never intent extraction/authority.
/// Unknown/mixed language is left to the planner; explicit conflicts fail closed.</summary>
public static class PentaQueryIntentGuard
{
    private static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(50);
    public static bool Compatible(string text, PentaQueryPlan? plan)
    {
        if (!PentaQueryContract.ValidPlan(plan)) return false;
        if (plan!.Kind is "clarify" or "unsupported") return true;
        var mentioned = new[] { ("teacher", @"\b(?:teachers?|faculty|teaching staff)\b"),
            ("student", @"\b(?:students?|learners?|pupils?)\b"), ("batch", @"\b(?:batch|batches)\b") }
            .Where(pair => Regex.IsMatch(text, pair.Item2, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound)).Select(x => x.Item1).ToArray();
        if (mentioned.Length == 1 && plan.Entity != mentioned[0]) return false;
        // A known unsupported operational request cannot be presented as a
        // successful read, even if a model returns a structurally valid plan.
        return !Regex.IsMatch(text, @"\b(?:create|delete|enroll|reschedule|payroll|salary|attendance|invoice|balances?|payments?|send|export|phone|email|address|password|compensation)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound);
    }
}
