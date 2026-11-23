using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Intelligence.Penta;

namespace AcademyDesk.Api.Tests;

public sealed class PentaMiniMoneyTests
{
    private static readonly Guid Student = Guid.NewGuid();
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static OutstandingResult Result(params LearnerBalance[] balances) => new(1, false,
        [new(Student, "Synthetic learner", ["Piano"], balances, $"/student-management?studentId={Student:D}")], DateTime.UtcNow);
    private static MiniReceipt Receipt(OutstandingResult? result, string kind = "RESULT") => new(Guid.NewGuid(), Guid.NewGuid(), 1,
        kind, "Verified snapshot.", "executor", result, new MiniState(null, result?.Rows.Select(r => r.SourceId.ToString("D")).ToArray() ?? [], [], null));

    [Theory]
    [InlineData("0", "0.00")]
    [InlineData("0.01", "0.01")]
    [InlineData("450.0100", "450.01")]
    [InlineData("90071992547409.91", "90071992547409.91")]
    [InlineData("9999999999999999.99", "9999999999999999.99")]
    [InlineData("9999999999999999999999.99", "9999999999999999999999.99")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335.00")]
    public void Exact_strings_preserve_the_stored_decimal_value(string input, string expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        var source = Result(new LearnerBalance("USD", value)); var receipt = Receipt(source);
        var projected = receipt.ExactMoney!;
        Assert.Equal(PentaMiniMoney.Version, projected.Version);
        Assert.Equal(Student, projected.Rows.Single().SourceId);
        Assert.Equal(expected, projected.Rows.Single().Balances.Single().OutstandingExact);
        Assert.Same(source, receipt.Result); Assert.Equal(value, source.Rows.Single().Balances.Single().Outstanding);
        Assert.Equal("0.1", receipt.Protocol);
    }
    [Theory]
    [InlineData("fr-FR")][InlineData("hi-IN")][InlineData("ar-SA")]
    public void Culture_does_not_change_currency_or_decimal_syntax(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal("1234.50", Receipt(Result(new LearnerBalance("INR", 1234.50m))).ExactMoney!.Rows[0].Balances[0].OutstandingExact);
        } finally { CultureInfo.CurrentCulture = before; }
    }
    [Theory]
    [InlineData("-0.01")][InlineData("0.001")][InlineData("10.005")]
    public void Unsupported_values_are_not_rounded_into_an_exact_claim(string value)
    {
        var original = decimal.Parse(value, CultureInfo.InvariantCulture); var result = Result(new LearnerBalance("USD", original));
        Assert.Null(PentaMiniMoney.FromResult(result)); Assert.Equal(original, result.Rows[0].Balances[0].Outstanding);
    }
    [Theory]
    [InlineData("")][InlineData("usd")][InlineData("US")][InlineData("USD/INR")][InlineData("US$")]
    public void Invalid_currency_has_no_exact_money_projection(string currency) =>
        Assert.Null(PentaMiniMoney.FromResult(Result(new LearnerBalance(currency, 1m))));
    [Fact]
    public void Duplicate_currencies_invalid_row_IDs_and_duplicate_rows_are_refused()
    {
        Assert.Null(PentaMiniMoney.FromResult(Result(new LearnerBalance("USD", 1m), new LearnerBalance("USD", 2m))));
        var source = Result(new LearnerBalance("USD", 1m));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = [source.Rows[0] with { SourceId = Guid.Empty }] }));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = [source.Rows[0], source.Rows[0]] }));
    }
    [Fact]
    public void Missing_invalid_and_oversized_containers_cannot_throw_or_claim_exactness()
    {
        Assert.Null(PentaMiniMoney.FromResult(null));
        var source = Result(new LearnerBalance("USD", 1m));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = null! }));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = [null!] }));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = [source.Rows[0] with { Balances = null! }] }));
        Assert.Null(PentaMiniMoney.FromResult(Result((LearnerBalance)null!)));
        Assert.Null(PentaMiniMoney.FromResult(source with { Rows = Enumerable.Range(0, 11).Select(_ => source.Rows[0] with { SourceId = Guid.NewGuid() }).ToArray() }));
    }
    [Fact]
    public void Currencies_are_kept_separate_without_conversion_or_totals()
    {
        var p = PentaMiniMoney.FromResult(Result(new LearnerBalance("USD", 0.01m), new LearnerBalance("INR", 100000.99m)))!;
        Assert.Equal(new[] { new MiniExactBalance("USD", "0.01"), new MiniExactBalance("INR", "100000.99") }, p.Rows[0].Balances);
    }
    [Fact]
    public void Empty_result_and_empty_balance_groups_are_explicit()
    {
        Assert.Empty(PentaMiniMoney.FromResult(new(0, false, [], DateTime.UtcNow))!.Rows);
        Assert.Empty(PentaMiniMoney.FromResult(Result())!.Rows.Single().Balances);
    }
    [Fact]
    public void Additive_projection_retains_the_original_numeric_wire_contract()
    {
        var receipt = Receipt(Result(new LearnerBalance("USD", 9999999999999999.99m)));
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(receipt, Web));
        var root = doc.RootElement;
        var numeric = root.GetProperty("result").GetProperty("rows")[0].GetProperty("balances")[0].GetProperty("outstanding");
        Assert.Equal(JsonValueKind.Number, numeric.ValueKind); Assert.Equal(9999999999999999.99m, numeric.GetDecimal());
        Assert.Equal("mini-exact-money.v1", root.GetProperty("exactMoney").GetProperty("version").GetString());
        Assert.Equal("9999999999999999.99", root.GetProperty("exactMoney").GetProperty("rows")[0].GetProperty("balances")[0].GetProperty("outstandingExact").GetString());
    }
    [Fact]
    public void Legacy_receipt_decimals_recover_exactly_without_rewriting_the_original()
    {
        var receipt = Receipt(Result(new LearnerBalance("USD", 9999999999999999.99m)));
        var legacy = JsonNode.Parse(JsonSerializer.Serialize(receipt, Web))!; legacy.AsObject().Remove("exactMoney");
        var original = legacy.ToJsonString(); var restored = JsonSerializer.Deserialize<MiniReceipt>(original, Web)!;
        Assert.DoesNotContain("exactMoney", original);
        Assert.Equal("9999999999999999.99", restored.ExactMoney!.Rows[0].Balances[0].OutstandingExact);
        Assert.True(PentaMiniRecovery.ValidReceipt(restored, receipt.ConversationId, receipt.RequestId, 0));
    }
    [Fact]
    public void Incoming_projection_cannot_override_the_authoritative_decimal_snapshot()
    {
        var json = JsonNode.Parse(JsonSerializer.Serialize(Receipt(Result(new LearnerBalance("USD", 450.01m))), Web))!;
        json["exactMoney"]!["version"] = "untrusted";
        json["exactMoney"]!["rows"]![0]!["balances"]![0]!["outstandingExact"] = "0.01";
        var restored = JsonSerializer.Deserialize<MiniReceipt>(json.ToJsonString(), Web)!;
        Assert.Equal("mini-exact-money.v1", restored.ExactMoney!.Version);
        Assert.Equal("450.01", restored.ExactMoney.Rows[0].Balances[0].OutstandingExact);
    }
    [Fact]
    public void Optional_presentation_cannot_push_an_existing_read_over_its_receipt_bound()
    {
        var currencies = new[] { "USD", "INR", "GBP", "EUR", "AUD", "CAD", "JPY", "CNY", "SGD", "CHF", "NZD", "SEK", "NOK", "DKK", "AED", "SAR", "ZAR", "BRL", "KRW", "HKD" };
        var rows = Enumerable.Range(0, 10).Select(_ => new OutstandingLearner(Guid.NewGuid(), new string('n', 500),
            Enumerable.Repeat(new string('s', 150), 5).ToArray(), currencies.Select(c => new LearnerBalance(c, decimal.MaxValue)).ToArray(), "")).ToArray();
        rows = rows.Select(row => row with { SourcePath = $"/student-management?studentId={row.SourceId:D}" }).ToArray();
        var source = new OutstandingResult(10, false, rows, DateTime.UtcNow); var receipt = Receipt(source);
        var actual = JsonSerializer.Serialize(receipt);
        Assert.True(actual.Length <= MiniReceipt.MaximumSerializedCharacters);
        Assert.Null(receipt.ExactMoney);
        var hypothetical = JsonNode.Parse(actual)!;
        hypothetical["ExactMoney"] = JsonSerializer.SerializeToNode(PentaMiniMoney.FromResult(source));
        Assert.True(hypothetical.ToJsonString().Length > MiniReceipt.MaximumSerializedCharacters);
        Assert.Same(source, receipt.Result);
    }
    [Theory]
    [InlineData("ERROR")][InlineData("REFUSAL")][InlineData("UNSUPPORTED")]
    public void Nonresult_responses_do_not_add_financial_claims(string kind)
    {
        var r = Receipt(Result(new LearnerBalance("USD", 1m)), kind); Assert.Null(r.ExactMoney);
        Assert.DoesNotContain("exactMoney", JsonSerializer.Serialize(r, Web));
    }
}
