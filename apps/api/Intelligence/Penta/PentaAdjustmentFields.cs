using System.Globalization;
using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

internal static class PentaAdjustmentFields
{
    internal static CreateFinanceAdjustmentRequest? Read(string payload)
    {
        if (payload.Length > 4096) return null;
        try
        {
            using var document = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = document.RootElement;
            if (root.GetProperty("tool_key").GetString() != "AcademyDesk.InvoiceAdjustment.Propose" ||
                root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Count() != 4) return null;
            var id = Field("InvoiceId", "reference");
            var type = Field("Type", "text");
            var amount = Field("Amount", "decimal");
            var reason = Field("Reason", "text");
            if (!Guid.TryParseExact(id, "D", out var invoice) || invoice == Guid.Empty || invoice.ToString("D") != id ||
                !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
                type is not { Length: > 0 and <= 30 } || reason is not { Length: > 0 and <= 500 } ||
                type.Any(char.IsControl) || reason.Any(char.IsControl)) return null;
            return new(invoice, type, value, reason);

            string? Field(string name, string kind) =>
                fields.TryGetProperty(name, out var field) && field.GetProperty("kind").GetString() == kind
                    ? field.GetProperty("value").GetString() : null;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException) { return null; }
    }
}
