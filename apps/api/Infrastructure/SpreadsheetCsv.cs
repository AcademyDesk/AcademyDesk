namespace AcademyDesk.Api.Infrastructure;

/// <summary>Quotes CSV cells and treats formula-like untrusted text as spreadsheet text.</summary>
public static class SpreadsheetCsv
{
    public static string Row(params object?[] values) => string.Join(',', values.Select(Cell));

    public static string Cell(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        // Typed amounts remain numeric. Strings (including phone numbers and references)
        // must never become formulas, even after spreadsheet whitespace normalization.
        if (value is string && NeedsTextPrefix(text)) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private static bool NeedsTextPrefix(string text)
    {
        var index = 0;
        var leadingControl = false;
        while (index < text.Length && (char.IsWhiteSpace(text[index]) || char.IsControl(text[index]) || text[index] == '\uFEFF'))
        {
            leadingControl |= char.IsControl(text[index]);
            index++;
        }
        return leadingControl || (index < text.Length && "=+-@＝＋－＠".Contains(text[index]));
    }
}
