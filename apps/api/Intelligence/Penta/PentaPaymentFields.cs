using System.Globalization;
using System.Text.Json;
using AcademyDesk.Api.Controllers;

namespace AcademyDesk.Api.Intelligence.Penta;

internal static class PentaPaymentFields
{
    internal const string Tool = "AcademyDesk.Payment.Record";

    internal static RecordPaymentRequest? Read(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using var document = JsonDocument.Parse(PentaOperationalPayload.Canonicalize(payload));
            var root = document.RootElement;
            if (root.GetProperty("tool_key").GetString() != Tool || root.GetProperty("tool_revision").GetInt64() != 1) return null;
            var fields = root.GetProperty("fields");
            if (fields.EnumerateObject().Count() != 3) return null;
            var invoice = Field("InvoiceId", "reference");
            var amount = Field("Amount", "decimal");
            var method = Field("Method", "text");
            if (!Guid.TryParseExact(invoice, "D", out var id) || id == Guid.Empty || id.ToString("D") != invoice ||
                !decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value) ||
                method is not ("Cash" or "Offline" or "UPI" or "BankTransfer" or "Card" or "Cheque")) return null;
            return new(id, value, method, null);

            string? Field(string name, string kind) =>
                fields.TryGetProperty(name, out var field) && field.GetProperty("kind").GetString() == kind
                    ? field.GetProperty("value").GetString() : null;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException) { return null; }
    }
}
