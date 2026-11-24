using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Denial-only semantic assertions, never intent extraction/authority.
/// Unknown/mixed language is left to the planner; explicit conflicts fail closed.</summary>
public static class PentaQueryIntentGuard
{
    private static readonly TimeSpan Bound = TimeSpan.FromMilliseconds(50);
    public static bool Compatible(string text, PentaQueryPlan? plan, PentaQueryContext? context = null)
    {
        if (!PentaQueryContract.ValidPlan(plan)) return false;
        if (plan!.Kind is "clarify" or "unsupported") return true;
        var mentioned = new[] { ("teacher", @"\b(?:teachers?|faculty|teaching staff)\b"),
            ("student", @"\b(?:students?|learners?|pupils?)\b"), ("batch", @"\b(?:batch|batches)\b"),
            ("session", @"\b(?:classes|class|sessions?|calendar|schedule)\b") }
            .Where(pair => Regex.IsMatch(text, pair.Item2, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound)).Select(x => x.Item1).ToArray();
        var financial = Regex.IsMatch(text, @"\b(?:fees?|balances?|dues?|owe|owing|outstanding)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound);
        var target = plan.Entity == "student-fees" ? "student" : plan.Entity;
        // Teacher/batch names can qualify a calendar search; those directory
        // tools must not replace a request that explicitly asks for classes.
        if (mentioned.Contains("session") && target != "session") return false;
        // This calendar slice has batch/teacher predicates, not enrollment joins.
        if (target == "session" && mentioned.Contains("student")) return false;
        if (mentioned.Length == 1 && target != mentioned[0]) return false;
        if (financial && plan.Entity != "student-fees") return false;
        // Do not disclose balances on a generic directory query. An explicit
        // fee question or a current financial follow-up must justify that view.
        if (plan.Entity == "student-fees" && !financial && mentioned.Length > 0) return false;
        if (plan.Entity == "student-fees" && !financial && context?.Entity != "student-fees") return false;
        if (Regex.IsMatch(text, @"\b(?:cancel|book|schedule|update|edit|change|add|assign)\s+(?:(?:a|an|the|new|this|that)\s+){0,3}(?:class(?:es)?|sessions?|lessons?|batch(?:es)?|students?|teachers?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound)) return false;
        // A known unsupported operational request cannot be presented as a
        // successful read, even if a model returns a structurally valid plan.
        return !Regex.IsMatch(text, @"\b(?:create|delete|enroll|reschedule|payroll|salary|attendance|invoice|payments?|send|export|phone|email|address|password|compensation)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Bound);
    }
}
