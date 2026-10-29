namespace AcademyDesk.Api.Infrastructure;

/// <summary>
/// CSV for human spreadsheet viewing, not a lossless interchange format.
/// Dangerous untrusted text receives a tab inside its quoted cell. Original
/// database values are never changed. Spreadsheet application acceptance is
/// independent; no CSV escape is universal across every import/save workflow.
/// </summary>
public static class SpreadsheetCsv
{
    public static string Row(params object?[] values) => string.Join(',', values.Select(Cell));

    public static string Cell(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (value is string && NeedsTextGuard(text)) text = "\t" + text;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static bool NeedsTextGuard(string text)
    {
        foreach (var character in text.EnumerateRunes())
        {
            // Leading control characters are not ordinary display text. Also
            // look beyond whitespace/format marks and text-prefix apostrophes.
            if (System.Text.Rune.IsControl(character)) return true;
            if (System.Text.Rune.IsWhiteSpace(character) || character.Value == '\'' ||
                System.Text.Rune.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.Format)
                continue;
            return character.Value is '=' or '+' or '-' or '@' or '＝' or '＋' or '－' or '＠';
        }
        return false;
    }
}
