using System.Globalization;

namespace AcademyDesk.Api.Intelligence.Penta;

/// <summary>Validation/presentation only. All balances originate in the shared
/// deterministic ledger read; no model or browser arithmetic/conversion.</summary>
public static class PentaQueryMoney
{
    public static bool Valid(MiniExactBalance[]? balances) => balances is { Length: <= 20 } &&
        balances.All(value => value is not null && value.Currency is { Length: 3 } &&
            value.Currency.All(c => c is >= 'A' and <= 'Z') && ValidAmount(value.OutstandingExact)) &&
        balances.Select(x => x.Currency).Distinct(StringComparer.Ordinal).Count() == balances.Length;
    public static bool ValidAmount(string? value) => value is { Length: > 0 and <= 32 } &&
        decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) &&
        amount >= 0 && amount.ToString("0.00", CultureInfo.InvariantCulture) == value;
}
