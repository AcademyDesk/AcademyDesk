using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Tests.Infrastructure;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifySessionPayrollRegressionAsync(QaApiFactory factory, HttpClient client,
        Guid academyId, Guid teacherId, string payrollPath)
    {
        using var create = await client.PostAsJsonAsync(payrollPath + "/profiles", new
        {
            workerType = "Teacher", teacherId, workerName = "Synthetic QA Payroll Teacher",
            paymentModel = "SessionBlock", amountPerCycle = 1000m, sessionsPerCycle = 12, effectiveFrom = "2026-09-01"
        });
        RequireFinanceStatus(create, HttpStatusCode.Created, "session-block payroll profile");
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var profileId = created.RootElement.GetProperty("id").GetGuid();
        var initial = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, "session-profile-created", "SessionBlock");
        if (initial.Payouts.Count != 0) throw new InvalidOperationException("Session-block profile not fresh.");
        var cases = new (string Name, decimal? Gross, int? Sessions, decimal Deduction, bool Accepted, decimal ExpectedGross, decimal ExpectedNet, int ExpectedSessions)[]
        {
            ("default-excess", null, null, 1000.01m, false, 1000m, 0m, 12),
            ("override-excess", 250m, 3, 250.01m, false, 250m, 0m, 3),
            ("override-cent", 250m, 3, 249.99m, true, 250m, 0.01m, 3),
            ("default-positive", null, null, 999m, true, 1000m, 1m, 12),
            ("zero-gross", 0m, 3, 0m, false, 0m, 0m, 3),
            ("zero-sessions", 250m, 0, 0m, false, 250m, 0m, 0),
            ("negative-deduction", 250m, 3, -1m, false, 250m, 0m, 3)
        };
        foreach (var test in cases)
        {
            var label = "QA-SESSION-" + test.Name;
            var before = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, label + "-before", "SessionBlock");
            await Task.Delay(1100); // Retain fixture collision protection for seconds/random payslip numbers.
            using var response = await client.PostAsJsonAsync(payrollPath + "/payouts", new
            { payrollProfileId = profileId, periodLabel = label, grossAmount = test.Gross, sessionsCovered = test.Sessions, deductions = test.Deduction });
            RequireFinanceStatus(response, test.Accepted ? HttpStatusCode.Created : HttpStatusCode.BadRequest, label);
            var after = await ReadPayrollSnapshotAsync(factory, academyId, teacherId, profileId, label + "-after", "SessionBlock");
            if (!test.Accepted)
            {
                if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(after))
                    throw new InvalidOperationException("Rejected session payroll changed financial snapshot.");
            }
            else
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var id = json.RootElement.GetProperty("id").GetGuid();
                var row = after.Payouts.Single(x => x.Id == id);
                if (before.Profile != after.Profile || after.Payouts.Count != before.Payouts.Count + 1 ||
                    !before.Payouts.All(x => after.Payouts.Contains(x)) || row.Period != label || row.Status != "Paid" ||
                    row.Gross != test.ExpectedGross || row.Deductions != test.Deduction || row.Net != test.ExpectedNet ||
                    row.Sessions != test.ExpectedSessions || row.Currency != "INR" || row.Method != "BankTransfer" ||
                    row.Reference is not null || string.IsNullOrWhiteSpace(row.Payslip) ||
                    json.RootElement.GetProperty("grossAmount").GetDecimal() != test.ExpectedGross ||
                    json.RootElement.GetProperty("deductions").GetDecimal() != test.Deduction ||
                    json.RootElement.GetProperty("netAmount").GetDecimal() != test.ExpectedNet ||
                    json.RootElement.GetProperty("sessionsCovered").GetInt32() != test.ExpectedSessions)
                    throw new InvalidOperationException("Session payroll response/fresh SQL mismatch.");
                using var list = await client.GetAsync(payrollPath + "/payouts");
                RequireFinanceStatus(list, HttpStatusCode.OK, "session payroll list");
                using var listed = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
                var item = listed.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id);
                if (item.GetProperty("payrollProfileId").GetGuid() != profileId ||
                    item.GetProperty("grossAmount").GetDecimal() != test.ExpectedGross ||
                    item.GetProperty("deductions").GetDecimal() != test.Deduction ||
                    item.GetProperty("netAmount").GetDecimal() != test.ExpectedNet ||
                    item.GetProperty("sessionsCovered").GetInt32() != test.ExpectedSessions)
                    throw new InvalidOperationException("Session payroll list mismatch.");
            }
            Console.WriteLine($"PAYROLL SESSION CASE {test.Name} PASS: HTTP={(int)response.StatusCode}; fresh profile/ledger and accepted response/list agree; rejection leaves captured financial snapshot unchanged.");
        }
        Console.WriteLine("PAYROLL REGRESSION PASS: monthly negative-net rejection and original eight boundaries; SessionBlock effective default/override gross, positive cents, zero gross/sessions and negative deductions; no negative payout persisted. Zero-net policy remains pending.");
    }
}
