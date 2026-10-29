using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollPrecisionAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, teacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
        }
        var token = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}/payroll";
        const decimal maximum = 9999999999999999.99m;
        var rejected = 0; var accepted = 0;
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new {
                Profiles = (await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                Payouts = (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                Audits = (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                Invoices = (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray(),
                Payments = (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id).ToArray()
            });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string route, object? body = null)
        {
            await Task.Delay(1100); // Retain production rate limits and payslip collision protection.
            using var request = new HttpRequestMessage(method, route);
            if (body is not null) request.Content = JsonContent.Create(body);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new InvalidOperationException("Payroll precision cannot pass with HTTP429.");
            return response;
        }
        async Task Reject(HttpMethod method, string route, object body, string label)
        {
            var before = await State();
            using var response = await Send(method, route, body);
            RequireFinanceStatus(response, HttpStatusCode.BadRequest, label);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.GetProperty("message").GetString() != "Payroll amounts must fit within 16 whole digits and two decimal places; no rounding is applied." || before != await State())
                throw new InvalidOperationException("Payroll precision rejection changed five-table state or returned incorrect guidance.");
            rejected++;
        }
        foreach (var model in new[] { "Monthly", "SessionBlock" })
        {
            var valid = new SavePayrollProfileRequest("Teacher", teacher, null, "Synthetic precision " + model, model,
                model == "Monthly" ? maximum : null, model == "SessionBlock" ? maximum : null,
                model == "SessionBlock" ? 4 : null, new DateOnly(2026, 10, 1));
            using var create = await Send(HttpMethod.Post, root + "/profiles", valid);
            RequireFinanceStatus(create, HttpStatusCode.Created, "precision maximum profile create");
            var profile = await create.Content.ReadFromJsonAsync<PayrollProfileSummary>() ?? throw new InvalidOperationException("Missing precision profile.");
            if ((model == "Monthly" ? profile.MonthlyAmountExact : profile.AmountPerCycleExact) != "9999999999999999.99")
                throw new InvalidOperationException("Maximum profile create lost cents.");
            accepted++;
            using var update = await Send(HttpMethod.Put, root + "/profiles/" + profile.Id, valid with { WorkerName = valid.WorkerName + " Edited" });
            RequireFinanceStatus(update, HttpStatusCode.OK, "precision maximum profile update");
            using var list = await Send(HttpMethod.Get, root + "/profiles");
            RequireFinanceStatus(list, HttpStatusCode.OK, "precision profile readback");
            using var profiles = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            var listed = profiles.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == profile.Id);
            var amountField = model == "Monthly" ? "monthlyAmount" : "amountPerCycle";
            if (listed.GetProperty(amountField).GetDecimal() != maximum || listed.GetProperty(amountField + "Exact").GetString() != "9999999999999999.99")
                throw new InvalidOperationException("Maximum profile readback lost cents.");
            using (var scope = factory.Services.CreateScope())
            {
                var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
                if ((model == "Monthly" ? row.MonthlyAmount : row.AmountPerCycle) != maximum || row.WorkerName != valid.WorkerName + " Edited")
                    throw new InvalidOperationException("Maximum profile fresh SQL differs.");
            }
            accepted++;
            foreach (var invalid in new[] { 1000.001m, 10000000000000000m })
            {
                var bad = valid with { MonthlyAmount = model == "Monthly" ? invalid : null, AmountPerCycle = model == "SessionBlock" ? invalid : null };
                await Reject(HttpMethod.Post, root + "/profiles", bad, model + " invalid create");
                await Reject(HttpMethod.Put, root + "/profiles/" + profile.Id, bad, model + " invalid update");
            }
            var payout = new CreatePayrollPayoutRequest(profile.Id, "Synthetic precision " + model, 4, null, maximum - 0.01m, "Cash", null, null);
            await Reject(HttpMethod.Post, root + "/payouts", payout with { Deductions = 0.001m }, model + " sub-cent deduction");
            if (model == "SessionBlock")
            {
                await Reject(HttpMethod.Post, root + "/payouts", payout with { GrossAmount = 1000.001m, Deductions = 0 }, "sub-cent gross");
                await Reject(HttpMethod.Post, root + "/payouts", payout with { GrossAmount = 10000000000000000m, Deductions = 0 }, "overflow gross");
            }
            using var pay = await Send(HttpMethod.Post, root + "/payouts", payout);
            RequireFinanceStatus(pay, HttpStatusCode.Created, "maximum payout exact one cent");
            using var paid = JsonDocument.Parse(await pay.Content.ReadAsStringAsync());
            var id = paid.RootElement.GetProperty("id").GetGuid();
            void Check(JsonElement row)
            {
                if (row.GetProperty("grossAmountExact").GetString() != "9999999999999999.99" ||
                    row.GetProperty("deductionsExact").GetString() != "9999999999999999.98" ||
                    row.GetProperty("netAmountExact").GetString() != "0.01" || row.GetProperty("netAmount").GetDecimal() != 0.01m)
                    throw new InvalidOperationException("Maximum payout response/readback lost exact cents.");
            }
            Check(paid.RootElement);
            using var read = await Send(HttpMethod.Get, root + "/payouts");
            RequireFinanceStatus(read, HttpStatusCode.OK, "precision payout readback");
            using var payouts = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
            Check(payouts.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id));
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var row = await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id);
                if (row.GrossAmount != maximum || row.Deductions != maximum - 0.01m || row.NetAmount != 0.01m || row.Status != "Paid" || row.PayrollProfileId != profile.Id)
                    throw new InvalidOperationException("Maximum payout fresh SQL differs.");
            }
            accepted++;
        }
        if (rejected != 12 || accepted != 6) throw new InvalidOperationException("Incomplete precision acceptance.");
        Console.WriteLine("PAYROLL PRECISION PASS:12 exact-message HTTP400/full five-table no-write rejects;6 maximum create/update/payout successes; both models exact SQL/profile/payout wire/readback/one-cent net; no429.");
    }
}
