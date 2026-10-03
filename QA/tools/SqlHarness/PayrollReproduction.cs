using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollBoundariesAsync(QaApiFactory factory, HttpClient client, bool enforceRegression = false)
    {
        Guid academyId;
        Guid teacherId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.AsNoTracking().Where(x => x.Name == "Synthetic Academy A")
                .Select(x => x.Id).SingleAsync();
            teacherId = await db.Teachers.AsNoTracking().Where(x => x.AcademyId == academyId && x.FirstName == "Synthetic")
                .Select(x => x.Id).SingleAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var payrollPath = $"/api/academies/{academyId}/payroll";
        using var profileResponse = await client.PostAsJsonAsync(payrollPath + "/profiles", new
        {
            workerType = "Teacher", teacherId, workerName = "Synthetic QA Payroll Teacher",
            paymentModel = "Monthly", monthlyAmount = 1000m, effectiveFrom = "2026-09-01"
        });
        RequireFinanceStatus(profileResponse, HttpStatusCode.Created, "monthly payroll profile create");
        using var profileJson = JsonDocument.Parse(await profileResponse.Content.ReadAsStringAsync());
        var profileId = profileJson.RootElement.GetProperty("id").GetGuid();
        var initial = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, "profile-created");
        if (initial.Payouts.Count != 0)
            throw new InvalidOperationException("The new payroll profile already had payouts.");

        // Cent edges match the model's decimal(18,2) scale. No rounding policy is invented.
        var deductions = new[] { 0m, 999m, 999.99m, 1000m, 1000.01m, 1001m, -0.01m, -1m };
        var failed = false;
        var negativePayouts = 0;
        var rejectedControls = 0;
        var zeroOutcome = "NOT OBSERVED";
        foreach (var deduction in deductions)
        {
            var label = "QA-DEDUCTION-" + deduction.ToString(CultureInfo.InvariantCulture);
            var before = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, label + "-before");
            // Avoid an unrelated collision in seconds-plus-random payslip numbering.
            await Task.Delay(1100);
            using var payout = await client.PostAsJsonAsync(payrollPath + "/payouts",
                new { payrollProfileId = profileId, periodLabel = label, deductions = deduction });
            if (payout.StatusCode is not (HttpStatusCode.Created or HttpStatusCode.BadRequest))
                throw new InvalidOperationException($"Payroll fixture deduction {deduction} returned {(int)payout.StatusCode}.");
            var after = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, label + "-after");
            var validPositiveNet = deduction >= 0m && deduction < 1000m;
            var invalidDeduction = deduction < 0m || deduction > 1000m;
            var disposition = deduction == 1000m ? "POLICY-PENDING" : "PASS";
            decimal? actualNet = null;
            Guid? payoutId = null;
            if (payout.StatusCode == HttpStatusCode.BadRequest)
            {
                if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after))
                    throw new InvalidOperationException("Rejected payroll request changed its financial snapshot.");
                if (validPositiveNet) throw new InvalidOperationException("Valid positive-net payroll control was rejected.");
                if (invalidDeduction) rejectedControls++;
                if (deduction == 1000m) zeroOutcome = "HTTP 400/no payout/unchanged financial snapshot";
            }
            else
            {
                using var response = JsonDocument.Parse(await payout.Content.ReadAsStringAsync());
                payoutId = response.RootElement.GetProperty("id").GetGuid();
                var row = after.Payouts.Single(x => x.Id == payoutId);
                actualNet = row.Net;
                var expectedNet = 1000m - deduction;
                if (after.Payouts.Count != before.Payouts.Count + 1 ||
                    !before.Payouts.All(x => after.Payouts.Contains(x)) || before.Profile != after.Profile ||
                    row.Period != label || row.Gross != 1000m || row.Deductions != deduction || row.Net != expectedNet ||
                    row.Status != "Paid" || row.Currency != "INR" || row.Sessions is not null ||
                    row.Method != "BankTransfer" || row.Reference is not null || string.IsNullOrWhiteSpace(row.Payslip) ||
                    response.RootElement.GetProperty("grossAmount").GetDecimal() != 1000m ||
                    response.RootElement.GetProperty("deductions").GetDecimal() != deduction ||
                    response.RootElement.GetProperty("netAmount").GetDecimal() != expectedNet)
                    throw new InvalidOperationException("Payroll creation response does not match the exact persisted fixture.");
                using var list = await client.GetAsync(payrollPath + "/payouts");
                RequireFinanceStatus(list, HttpStatusCode.OK, "payroll payout list");
                using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                var listedRow = listed.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == payoutId);
                if (listedRow.GetProperty("grossAmount").GetDecimal() != 1000m ||
                    listedRow.GetProperty("deductions").GetDecimal() != deduction ||
                    listedRow.GetProperty("netAmount").GetDecimal() != expectedNet ||
                    listedRow.GetProperty("payrollProfileId").GetGuid() != profileId)
                    throw new InvalidOperationException("Payroll list does not match the stored payout.");
                if (invalidDeduction) { failed = true; disposition = "FAIL"; }
                if (row.Net < 0m) negativePayouts++;
                if (deduction == 1000m) zeroOutcome = "HTTP 201/one Paid payout/net 0";
            }
            Console.WriteLine($"PAYROLL CASE deductions={deduction.ToString(CultureInfo.InvariantCulture)} {disposition}: " +
                $"HTTP={(int)payout.StatusCode}/SQL rows before={before.Payouts.Count}/after={after.Payouts.Count}/" +
                $"net={(actualNet?.ToString(CultureInfo.InvariantCulture) ?? "none")}/payout={(payoutId?.ToString() ?? "none")}; " +
                $"expected={(deduction == 1000m ? "zero-net policy pending" : invalidDeduction ? "reject without financial write" : "exact nonnegative payout")}; " +
                "fresh SQL and accepted payout list verified.");
        }
        Console.WriteLine($"PAYROLL-RULE-001 {(failed ? "FAIL" : "PASS")}: gross=1000; " +
            $"negative Paid payouts persisted={negativePayouts}; rejected invalid controls={rejectedControls}; " +
            $"zero-net POLICY-PENDING observed={zeroOutcome}; " +
            "deductions above gross must be rejected without a payout; no debt workflow assumed.");
        if (enforceRegression)
        {
            if (failed || negativePayouts != 0 || rejectedControls != 4)
                throw new InvalidOperationException("PAYROLL-RULE-001 negative-net repair regression failed.");
            await VerifySessionPayrollRegressionAsync(factory, client, academyId, teacherId, payrollPath);
        }
    }

    private static async Task<PayrollSnapshot> ReadPayrollSnapshotAsync(QaApiFactory factory,
        Guid academyId, Guid teacherId, Guid profileId, string stage, string model = "Monthly")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var profile = await db.PayrollProfiles.AsNoTracking().Where(x => x.Id == profileId)
            .Select(x => new PayrollProfileSnapshot(x.Id, x.AcademyId, x.TeacherId, x.WorkerType, x.WorkerName,
                x.PaymentModel, x.MonthlyAmount, x.AmountPerCycle, x.SessionsPerCycle, x.IsActive, x.EffectiveFrom)).SingleAsync();
        if (profile.AcademyId != academyId || profile.TeacherId != teacherId || profile.WorkerType != "Teacher" ||
            profile.WorkerName != "Synthetic QA Payroll Teacher" || profile.Model != model ||
            (model == "Monthly" ? profile.Monthly != 1000m || profile.CycleAmount is not null || profile.CycleSessions is not null
                : model != "SessionBlock" || profile.Monthly is not null || profile.CycleAmount != 1000m || profile.CycleSessions != 12) ||
            !profile.Active || profile.EffectiveFrom != new DateOnly(2026, 9, 1))
            throw new InvalidOperationException("Payroll profile SQL ownership/rate fixture mismatch.");
        var rows = await db.PayrollPayouts.AsNoTracking().Where(x => x.PayrollProfileId == profileId).OrderBy(x => x.Id)
            .Select(x => new PayrollLedger(x.Id, x.AcademyId, x.PayrollProfileId, x.PayslipNumber, x.PeriodLabel,
                x.GrossAmount, x.Deductions, x.NetAmount, x.Currency, x.Status, x.SessionsCovered,
                x.PaymentMethod, x.Reference, x.PaidAtUtc)).ToListAsync();
        if (rows.Any(x => x.AcademyId != academyId || x.ProfileId != profileId))
            throw new InvalidOperationException("Payroll ledger SQL ownership mismatch.");
        var snapshot = new PayrollSnapshot(profile, rows);
        Console.WriteLine($"PAYROLL SQL {stage}: {JsonSerializer.Serialize(snapshot)}");
        return snapshot;
    }

    private sealed record PayrollProfileSnapshot(Guid Id, Guid AcademyId, Guid? TeacherId, string WorkerType,
        string WorkerName, string Model, decimal? Monthly, decimal? CycleAmount, int? CycleSessions,
        bool Active, DateOnly EffectiveFrom);
    private sealed record PayrollLedger(Guid Id, Guid AcademyId, Guid ProfileId, string Payslip, string Period,
        decimal Gross, decimal Deductions, decimal Net, string Currency, string Status, int? Sessions,
        string Method, string? Reference, DateTime PaidAtUtc);
    private sealed record PayrollSnapshot(PayrollProfileSnapshot Profile, List<PayrollLedger> Payouts);
}
