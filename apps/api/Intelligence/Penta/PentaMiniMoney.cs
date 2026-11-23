using System.Globalization;
using AcademyDesk.Api.Infrastructure;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record MiniExactBalance(string Currency, string OutstandingExact);
public sealed record MiniExactMoneyRow(Guid SourceId, MiniExactBalance[] Balances);
public sealed record MiniExactMoney(string Version, MiniExactMoneyRow[] Rows);

// Presentation of the existing authorized decimal snapshot only. No ledger
// arithmetic, conversion, model interpretation or new financial authority.
public static class PentaMiniMoney
{
    public const string Version = "mini-exact-money.v1";
    public static MiniExactMoney? FromResult(OutstandingResult? result)
    {
        if (result?.Rows is null || result.Rows.Length > 10 ||
            result.Rows.Any(row => row is null || row.SourceId == Guid.Empty || row.Balances is null) ||
            result.Rows.Select(row => row.SourceId).Distinct().Count() != result.Rows.Length) return null;
        var rows = new List<MiniExactMoneyRow>();
        foreach (var row in result.Rows)
        {
            if (row.Balances.Length > 20) return null;
            var currencies = new HashSet<string>(StringComparer.Ordinal);
            var balances = new List<MiniExactBalance>();
            foreach (var balance in row.Balances)
            {
                if (balance is null || balance.Currency is not { Length: 3 } ||
                    !balance.Currency.All(c => c is >= 'A' and <= 'Z') || !currencies.Add(balance.Currency) ||
                    balance.Outstanding < 0 || decimal.Round(balance.Outstanding, 2) != balance.Outstanding) return null;
                balances.Add(new(balance.Currency, balance.Outstanding.ToString("0.00", CultureInfo.InvariantCulture)));
            }
            rows.Add(new(row.SourceId, balances.ToArray()));
        }
        return new(Version, rows.ToArray());
    }
}
