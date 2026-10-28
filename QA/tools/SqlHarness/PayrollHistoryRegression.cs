using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollHistoryMigrationAsync(AcademyDeskDbContext db)
    {
        // Called only after Program validates/creates a fresh, run-owned DB.
        await db.GetService<IMigrator>().MigrateAsync("20261001152214_AddNotificationReadReceipts");
        var profile = new PayrollProfile { AcademyId = Guid.NewGuid(), WorkerName = "Legacy current identity", WorkerType = "Staff" };
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [PayrollProfiles] ([Id],[AcademyId],[WorkerType],[WorkerName],[PaymentModel],[EffectiveFrom],[IsActive],[CreatedAtUtc]) VALUES ({profile.Id},{profile.AcademyId},{profile.WorkerType},{profile.WorkerName},{profile.PaymentModel},{profile.EffectiveFrom},{profile.IsActive},{profile.CreatedAtUtc})");
        var rows = new[] {
            new PayrollPayout { AcademyId = profile.AcademyId, PayrollProfileId = profile.Id, PayslipNumber = "QA-LEGACY-OWN", PeriodLabel = "Legacy period", GrossAmount = 1000m, Deductions = 100m, NetAmount = 900m, Reference = "Preserve this reference", SessionsCovered = 3 },
            new PayrollPayout { AcademyId = Guid.NewGuid(), PayrollProfileId = profile.Id, PayslipNumber = "QA-LEGACY-FOREIGN", PeriodLabel = "Foreign link", GrossAmount = 20m, NetAmount = 20m },
            new PayrollPayout { AcademyId = profile.AcademyId, PayrollProfileId = Guid.NewGuid(), PayslipNumber = "QA-LEGACY-ORPHAN", PeriodLabel = "Missing link", GrossAmount = 30m, NetAmount = 30m }
        };
        foreach (var p in rows)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [PayrollPayouts] ([Id],[AcademyId],[PayrollProfileId],[PayslipNumber],[PeriodLabel],[SessionsCovered],[GrossAmount],[Deductions],[NetAmount],[Currency],[Status],[PaymentMethod],[Reference],[PaidAtUtc],[CreatedAtUtc]) VALUES ({p.Id},{p.AcademyId},{p.PayrollProfileId},{p.PayslipNumber},{p.PeriodLabel},{p.SessionsCovered},{p.GrossAmount},{p.Deductions},{p.NetAmount},{p.Currency},{p.Status},{p.PaymentMethod},{p.Reference},{p.PaidAtUtc},{p.CreatedAtUtc})");
        }
        await db.Database.MigrateAsync();
        rows[0].WorkerNameSnapshot = profile.WorkerName; rows[0].WorkerTypeSnapshot = profile.WorkerType;
        foreach (var expected in rows)
        {
            var actual = await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == expected.Id);
            if (!SamePayrollEntity(expected, actual))
                throw new InvalidOperationException("Payroll identity migration changed legacy payout or copied a foreign identity.");
            await db.PayrollPayouts.Where(x => x.Id == expected.Id).ExecuteDeleteAsync();
        }
        if (!SamePayrollEntity(profile, await db.PayrollProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id)))
            throw new InvalidOperationException("Payroll identity migration changed the legacy profile.");
        await db.PayrollProfiles.Where(x => x.Id == profile.Id).ExecuteDeleteAsync();
        Console.WriteLine("PAYROLL HISTORY MIGRATION PASS:owned pre-column upgrade preserves all three legacy payouts and profile; same-academy identity backfilled; foreign/orphan identities stay unknown. Earlier names cannot be recovered.");
    }

    private static async Task VerifyPayrollHistoryAsync(QaApiFactory factory, HttpClient client, Guid academy, Guid teacher, string path)
    {
        string staffUserId;
        using (var scope = factory.Services.CreateScope())
            staffUserId = (await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.AsNoTracking()
                .Where(x => x.AcademyId == academy && x.TeacherId == null && x.StudentId == null && x.GuardianId == null && !x.IsPlatformOwner)
                .Select(x => x.Id).FirstAsync()).ToString();
        using var create = await client.PostAsJsonAsync(path + "/profiles", new { workerType = "Teacher", teacherId = teacher, workerName = "Paid identity", paymentModel = "Monthly", monthlyAmount = 1000m });
        RequireFinanceStatus(create, HttpStatusCode.Created, "history profile create");
        using var profileJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync()); var profileId = profileJson.RootElement.GetProperty("id").GetGuid();
        Guid foreignTeacher;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var other = await db.Academies.Where(x => x.Id != academy).Select(x => x.Id).FirstAsync();
            var row = new Teacher { AcademyId = other, FirstName = "Foreign payroll", LastName = "Teacher" }; foreignTeacher = row.Id;
            db.Add(row); await db.SaveChangesAsync();
        }
        async Task<string> ProfileState()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payouts = await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        foreach (var invalidTeacher in new[] { foreignTeacher, Guid.NewGuid() })
        {
            var state = await ProfileState();
            using var update = await client.PutAsJsonAsync(path + "/profiles/" + profileId, new { workerType = "Teacher", teacherId = invalidTeacher, workerName = "Rejected change", paymentModel = "Monthly", monthlyAmount = 2000m });
            RequireFinanceStatus(update, HttpStatusCode.BadRequest, "foreign/missing payroll teacher");
            if (state != await ProfileState()) throw new InvalidOperationException("Rejected payroll teacher link changed profile/payout/audit rows.");
            Console.WriteLine("PAYROLL HISTORY CASE invalid-teacher-link PASS:HTTP400; fresh profiles/payouts/audits unchanged.");
        }
        using var pay = await client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = profileId, periodLabel = "History period", deductions = 100m });
        RequireFinanceStatus(pay, HttpStatusCode.Created, "history payout create");
        var original = await pay.Content.ReadAsStringAsync(); using var payoutJson = JsonDocument.Parse(original); var id = payoutJson.RootElement.GetProperty("id").GetGuid();
        if (payoutJson.RootElement.GetProperty("workerName").GetString() != "Paid identity" || payoutJson.RootElement.GetProperty("workerType").GetString() != "Teacher")
            throw new InvalidOperationException("Created payout identity is incorrect.");
        PayrollPayout before;
        using (var scope = factory.Services.CreateScope()) before = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id);
        if (before.WorkerNameSnapshot != "Paid identity" || before.WorkerTypeSnapshot != "Teacher") throw new InvalidOperationException("Payout identity was not persisted.");
        foreach (var mode in new[] { "rename", "reassign", "deactivate", "remove" })
        {
            if (mode == "remove")
            {
                using var scope = factory.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollProfiles.Where(x => x.Id == profileId).ExecuteDeleteAsync();
            }
            else
            {
                using var update = await client.PutAsJsonAsync(path + "/profiles/" + profileId, new { workerType = mode == "rename" ? "Teacher" : "Staff", teacherId = mode == "rename" ? (Guid?)teacher : null, staffUserId = mode == "rename" ? null : staffUserId, workerName = "Current identity", paymentModel = "Monthly", monthlyAmount = 2000m, isActive = mode != "deactivate" });
                RequireFinanceStatus(update, HttpStatusCode.OK, "history " + mode);
            }
            using var listed = await client.GetAsync(path + "/payouts"); RequireFinanceStatus(listed, HttpStatusCode.OK, "history list " + mode);
            using var json = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
            if (!SameHistory(json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == id), payoutJson.RootElement))
                throw new InvalidOperationException("Historical payout response changed after " + mode);
            using var fresh = factory.Services.CreateScope(); var db = fresh.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (JsonSerializer.Serialize(before) != JsonSerializer.Serialize(await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id)))
                throw new InvalidOperationException("Historical payout SQL changed after " + mode);
            Console.WriteLine($"PAYROLL HISTORY CASE {mode} PASS:HTTP payout identity and full fresh SQL row retained.");
        }
    }

    private static bool SameHistory(JsonElement actual, JsonElement expected) =>
        actual.EnumerateObject().Count() == expected.EnumerateObject().Count() &&
        expected.EnumerateObject().All(p => actual.TryGetProperty(p.Name, out var value) &&
            (p.Name == "paidAtUtc" ? value.GetDateTime().Ticks == p.Value.GetDateTime().Ticks :
                value.ValueKind == JsonValueKind.Number ? value.GetDecimal() == p.Value.GetDecimal() : value.GetRawText() == p.Value.GetRawText()));

    // SQL datetime2 omits Kind and decimal(18,2) fixes scale. Compare scalar
    // values across the upgrade rather than their incidental JSON formatting.
    private static bool SamePayrollEntity<T>(T expected, T actual) => typeof(T).GetProperties()
        .All(p => Equals(p.GetValue(expected), p.GetValue(actual)));
}
