using System.Globalization;
using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

internal static class PentaInvoiceFields
{
    internal static CreateInvoiceRequest? Read(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using var document = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = document.RootElement;
            if (root.GetProperty("tool_key").GetString() != "AcademyDesk.Invoice.Issue" ||
                root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Any(x => x.Name is not ("StudentId" or "FeePlanId" or "Amount" or "DueDate"))) return null;
            if (Reference("StudentId") is not { } student ||
                !DateOnly.TryParseExact(Field("DueDate", "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due))
                return null;

            var feePlan = Reference("FeePlanId");
            decimal? amount = null;
            if (Field("Amount", "decimal") is { } text)
            {
                if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed)) return null;
                amount = parsed;
            }
            return feePlan is null && amount is null ? null : new(student, feePlan, amount, due);

            string? Field(string name, string kind)
            {
                if (!fields.TryGetProperty(name, out var field)) return null;
                if (field.GetProperty("kind").GetString() != kind) throw new ArgumentException();
                return field.GetProperty("value").GetString();
            }
            Guid? Reference(string name)
            {
                var value = Field(name, "reference");
                if (value is null) return null;
                if (!Guid.TryParseExact(value, "D", out var id) || id == Guid.Empty || id.ToString("D") != value)
                    throw new ArgumentException();
                return id;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException) { return null; }
    }
}
