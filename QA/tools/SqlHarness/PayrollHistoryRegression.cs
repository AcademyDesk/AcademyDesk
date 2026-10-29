using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Baseline mode classifies known failures; it is NOT repaired acceptance.
    // Guarded mode requires all three historical identity/visibility assertions.
    private static async Task VerifyPayrollHistoryMigrationAsync(AcademyDeskDbContext db)
    {
        await db.GetService<IMigrator>().MigrateAsync("20261007172325_AddPentaMiniPrivateSessions");
        var id = Guid.NewGuid(); var academy = Guid.NewGuid(); var profile = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [PayrollPayouts] ([Id],[AcademyId],[PayrollProfileId],[PayslipNumber],[PeriodLabel],[GrossAmount],[Deductions],[NetAmount],[Currency],[Status],[PaymentMethod],[PaidAtUtc],[CreatedAtUtc]) VALUES ({id},{academy},{profile},{"QA-UPGRADE-LEGACY"},{"September"},{1000m},{200m},{800m},{"INR"},{"Paid"},{"BankTransfer"},{new DateTime(2026,9,30,12,0,0)},{new DateTime(2026,9,30,12,0,0)})");
        await db.Database.MigrateAsync();
        var row = await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id);
        if (row.AcademyId != academy || row.PayrollProfileId != profile || row.PayslipNumber != "QA-UPGRADE-LEGACY" || row.PeriodLabel != "September" ||
            row.GrossAmount != 1000m || row.Deductions != 200m || row.NetAmount != 800m || row.Currency != "INR" || row.Status != "Paid" || row.PaymentMethod != "BankTransfer" ||
            row.PaidAtUtc != new DateTime(2026,9,30,12,0,0) || row.CreatedAtUtc != row.PaidAtUtc || row.UpdatedAtUtc is not null || row.Reference is not null || row.SessionsCovered is not null ||
            row.WorkerNameSnapshot is not null || row.WorkerTypeSnapshot is not null || row.TeacherIdSnapshot is not null || row.StaffUserIdSnapshot is not null || row.IdentityRecordedAtUtc is not null ||
            db.Database.HasPendingModelChanges()) throw new InvalidOperationException("Payroll identity migration changed legacy row or invented identity.");
        if (Environment.GetEnvironmentVariable("QA_BATCH1") == "guarded")
        {
            var original = JsonSerializer.Serialize(row);
            var statement = new PayrollPayout { AcademyId = academy, PayrollProfileId = profile, PayslipNumber = "QA-MIGRATION-NOCASH", PeriodLabel = "Synthetic migration", GrossAmount = 1000m, Deductions = 1000m, NetAmount = 0m, Status = "NoCashDue", PaymentMethod = "NoCashDue", PaidAtUtc = null };
            db.Add(statement); await db.SaveChangesAsync();
            var blocked = false;
            try { await db.GetService<IMigrator>().MigrateAsync("20261012121216_PreservePayrollPayoutIdentity"); }
            catch (Microsoft.Data.SqlClient.SqlException error) when (error.Number == 51000) { blocked = true; }
            if (!blocked || !(await db.Database.GetAppliedMigrationsAsync()).Contains("20261012140937_PayrollNoCashDueStatements") ||
                (await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == statement.Id)).PaidAtUtc != null ||
                original != JsonSerializer.Serialize(await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id)))
                throw new InvalidOperationException("No-cash migration rollback guard failed or fabricated evidence.");
            await db.PayrollPayouts.Where(x => x.Id == statement.Id && x.AcademyId == academy).ExecuteDeleteAsync();
            await db.GetService<IMigrator>().MigrateAsync("20261012121216_PreservePayrollPayoutIdentity");
            await db.Database.MigrateAsync();
            if (original != JsonSerializer.Serialize(await db.PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == id)) || db.Database.HasPendingModelChanges())
                throw new InvalidOperationException("Compatible migration roundtrip changed the old payout.");
            Console.WriteLine("BATCH1 MIGRATION PASS: legacy row exact, null-paid statement downgrade51000/rollback/retained migration, compatible empty-statement down/up exact; no invented payment timestamp.");
        }
        await db.PayrollPayouts.Where(x => x.Id == id && x.AcademyId == academy).ExecuteDeleteAsync();
        Console.WriteLine("PAYROLLHISTORY MIGRATION PASS: pre-column orphan payout preserved exactly, all five identity fields remain NULL, current model matches snapshot; only owned migration fixture removed.");
    }

    private static async Task VerifyPayrollHistoryAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var baseline = Environment.GetEnvironmentVariable("QA_PAYROLL_HISTORY") == "baseline";
        var checks = 0;
        var failures = 0;
        Guid academy, foreign, teacher, actor;
        PayrollProfile sentinel;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            sentinel = new PayrollProfile { AcademyId = foreign, WorkerName = "Synthetic foreign sentinel", WorkerType = "Staff", MonthlyAmount = 2345m };
            db.PayrollProfiles.Add(sentinel);
            await db.SaveChangesAsync();
        }
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        using (var scope = factory.Services.CreateScope())
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        client.DefaultRequestHeaders.Authorization = null;
        var path = $"/api/academies/{academy}/payroll";
        var original = new SavePayrollProfileRequest("Teacher", teacher, null, "Synthetic Worker A", "Monthly", 1000m, null, null, new DateOnly(2026, 9, 1));

        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new
            {
                Profiles = (await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                Payouts = (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                Audits = (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                Invoices = (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                Payments = (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray()
            });
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string route, object? body, string? bearer)
        {
            await Task.Delay(650);
            using var request = new HttpRequestMessage(method, route);
            if (body is not null) request.Content = JsonContent.Create(body);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            var response = await client.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new InvalidOperationException("Payroll history may not pass with HTTP429.");
            return response;
        }
        void Pass(string label) { checks++; Console.WriteLine("PAYROLLHISTORY CONTROL PASS: " + label); }
        void Identity(bool guarded, bool reproduced, string label)
        {
            if (baseline)
            {
                if (!reproduced || guarded) throw new InvalidOperationException("Frozen payroll history baseline changed: " + label);
                failures++;
                Console.WriteLine("PAYROLLHISTORY ACCEPTANCE FAIL / REPRODUCED: " + label);
            }
            else
            {
                if (!guarded) throw new InvalidOperationException("Payroll history guard failed: " + label);
                Pass(label);
            }
        }
        void Equal(JsonElement a, JsonElement b, string label)
        {
            if (!JsonElement.DeepEquals(a, b)) throw new InvalidOperationException("Payroll history changed " + label);
        }
        void Mutation(string before, string after, string table, Guid id, bool insert, string action, string route)
        {
            using var a = JsonDocument.Parse(before); using var b = JsonDocument.Parse(after);
            foreach (var field in new[] { "Profiles", "Payouts", "Invoices", "Payments" })
            {
                var old = a.RootElement.GetProperty(field).EnumerateArray().ToArray();
                var now = b.RootElement.GetProperty(field).EnumerateArray().ToArray();
                if (now.Length != old.Length + (field == table && insert ? 1 : 0)) throw new InvalidOperationException("Unexpected payroll table count: " + field);
                foreach (var row in old)
                {
                    var key = row.GetProperty("Id").GetGuid();
                    if (field != table || key != id) Equal(row, now.Single(x => x.GetProperty("Id").GetGuid() == key), field + " old/sentinel row");
                }
                if (field == table && now.Count(x => x.GetProperty("Id").GetGuid() == id) != 1) throw new InvalidOperationException("Missing mutation target.");
            }
            var oldAudits = a.RootElement.GetProperty("Audits").EnumerateArray().ToArray();
            var audits = b.RootElement.GetProperty("Audits").EnumerateArray().ToArray();
            if (audits.Length != oldAudits.Length + 1) throw new InvalidOperationException("Expected exactly one payroll audit.");
            foreach (var old in oldAudits) Equal(old, audits.Single(x => x.GetProperty("Id").GetGuid() == old.GetProperty("Id").GetGuid()), "old audit");
            var audit = audits.Single(x => !oldAudits.Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid()));
            using var metadata = JsonDocument.Parse(audit.GetProperty("MetadataJson").GetString()!);
            if (audit.GetProperty("AcademyId").GetGuid() != academy || audit.GetProperty("ActorUserId").GetGuid() != actor ||
                audit.GetProperty("Action").GetString() != action || metadata.RootElement.GetProperty("Route").GetString() != route)
                throw new InvalidOperationException("Payroll success audit identity/action/route mismatch.");
        }
        async Task NoWrite(HttpMethod method, string route, object? body, string? bearer, HttpStatusCode status, string label)
        {
            var before = await Snapshot(); using var response = await Send(method, route, body, bearer);
            RequireFinanceStatus(response, status, label);
            using var a = JsonDocument.Parse(before); using var b = JsonDocument.Parse(await Snapshot());
            Equal(a.RootElement, b.RootElement, label + " full snapshot"); Pass(label);
        }
        var beforeCreate = await Snapshot();
        using var create = await Send(HttpMethod.Post, path + "/profiles", original, admin);
        RequireFinanceStatus(create, HttpStatusCode.Created, "history profile create");
        using var profileJson = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var profile = profileJson.RootElement.GetProperty("id").GetGuid();
        Mutation(beforeCreate, await Snapshot(), "Profiles", profile, true, "POST Payroll", path + "/profiles");
        Pass("profile creation, actor/tenant/route audit and foreign/finance preservation");
        var legacy = new List<PayrollPayoutSummary>();
        PayrollPayoutSummary? successor = null;
        Guid? foreignPayout = null;
        if (!baseline)
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            foreach (var mode in new[] { "current-profile", "orphan", "foreign-profile", "partial" })
            {
                var row = new PayrollPayout { AcademyId = academy, PayrollProfileId = mode == "orphan" ? Guid.NewGuid() : mode == "foreign-profile" ? sentinel.Id : profile,
                    PayslipNumber = "QA-LEGACY-" + mode, PeriodLabel = "Historical September", GrossAmount = 1000m, Deductions = 200m, NetAmount = 800m,
                    PaidAtUtc = new DateTime(2026,9,30,12,0,0,DateTimeKind.Utc) };
                if (mode == "partial") row.WorkerNameSnapshot = "Incomplete identity must not be trusted";
                db.PayrollPayouts.Add(row);
                legacy.Add(new(row.Id, row.PayrollProfileId, "Historical identity not recorded", "Unknown (legacy)", row.PayslipNumber,
                    row.PeriodLabel, null, 1000m, 200m, 800m, "INR", "Paid", "BankTransfer", null, row.PaidAtUtc));
            }
            var other = new PayrollPayout { AcademyId = foreign, PayrollProfileId = sentinel.Id, PayslipNumber = "QA-FOREIGN-HISTORY", PeriodLabel = "Foreign September", GrossAmount = 1234m, NetAmount = 1234m,
                WorkerNameSnapshot = sentinel.WorkerName, WorkerTypeSnapshot = "Staff", IdentityRecordedAtUtc = DateTime.UtcNow };
            db.PayrollPayouts.Add(other); foreignPayout = other.Id; await db.SaveChangesAsync();
            Pass("four local legacy/incomplete and positive foreign payout fixtures seeded only in owned SQL");
        }
        var payoutRequest = new CreatePayrollPayoutRequest(profile, "QA-HISTORY-SEPTEMBER", null, null, 200m, null, null, new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc));
        var beforePay = await Snapshot();
        using var paid = await Send(HttpMethod.Post, path + "/payouts", payoutRequest, admin);
        RequireFinanceStatus(paid, HttpStatusCode.Created, "history payout create");
        var initialPayout = await paid.Content.ReadFromJsonAsync<PayrollPayoutSummary>() ?? throw new InvalidOperationException("Missing payout.");
        Mutation(beforePay, await Snapshot(), "Payouts", initialPayout.Id, true, "POST Payroll", path + "/payouts");
        if (initialPayout.WorkerName != original.WorkerName || initialPayout.WorkerType != "Teacher" || initialPayout.PayrollProfileId != profile ||
            initialPayout.GrossAmount != 1000m || initialPayout.Deductions != 200m || initialPayout.NetAmount != 800m || initialPayout.Currency != "INR")
            throw new InvalidOperationException("Initial history payout incorrect.");
        using (var scope = factory.Services.CreateScope())
        {
            var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == initialPayout.Id);
            if (stored.AcademyId != academy || stored.PayrollProfileId != profile || stored.PayslipNumber != initialPayout.PayslipNumber ||
                stored.PeriodLabel != initialPayout.PeriodLabel || stored.SessionsCovered != initialPayout.SessionsCovered || stored.GrossAmount != initialPayout.GrossAmount ||
                stored.Deductions != initialPayout.Deductions || stored.NetAmount != initialPayout.NetAmount || stored.Currency != initialPayout.Currency ||
                stored.Status != initialPayout.Status || stored.PaymentMethod != initialPayout.PaymentMethod || stored.Reference != initialPayout.Reference || stored.PaidAtUtc != initialPayout.PaidAtUtc)
                throw new InvalidOperationException("Payout creation response/fresh SQL differs.");
            if (!baseline && (stored.WorkerNameSnapshot != original.WorkerName || stored.WorkerTypeSnapshot != original.WorkerType || stored.TeacherIdSnapshot != teacher || stored.StaffUserIdSnapshot is not null ||
                stored.IdentityRecordedAtUtc != initialPayout.IdentityRecordedAtUtc || initialPayout.IdentitySource != "PayoutSnapshot" || initialPayout.IdentityRecordedAtUtc is null))
                throw new InvalidOperationException("Persisted payout identity differs from locked source profile.");
        }
        Pass("payout creation identity/decimal values and exact success audit");
        async Task<PayrollPayoutSummary?> Read(string stage)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Get, path + "/payouts", null, admin);
            RequireFinanceStatus(response, HttpStatusCode.OK, stage);
            var list = await response.Content.ReadFromJsonAsync<PayrollPayoutSummary[]>() ?? throw new InvalidOperationException("Missing payout list.");
            if (baseline ? list.Any(x => x.PayrollProfileId == sentinel.Id) : list.Any(x => x.Id == foreignPayout)) throw new InvalidOperationException("Foreign sentinel leaked.");
            foreach (var expected in legacy)
                if (list.SingleOrDefault(x => x.Id == expected.Id) != expected) throw new InvalidOperationException("Legacy identity/visibility/money differs: " + stage);
            if (successor is not null && list.SingleOrDefault(x => x.Id == successor.Id) != successor)
                throw new InvalidOperationException("Later payout identity/amount changed: " + stage);
            using var a = JsonDocument.Parse(before); using var b = JsonDocument.Parse(await Snapshot()); Equal(a.RootElement, b.RootElement, stage + " read-only");
            var row = list.SingleOrDefault(x => x.Id == initialPayout.Id);
            if (row is not null && row with { WorkerName = initialPayout.WorkerName, WorkerType = initialPayout.WorkerType } != initialPayout)
                throw new InvalidOperationException("Historical money/period/reference/payslip/time changed: " + stage);
            Pass(stage + " real GET200, read-only full snapshot, foreign sentinel excluded and money/document equality");
            Console.WriteLine($"PAYROLLHISTORY OBSERVATION {stage}: visible={row is not null}; workerName={row?.WorkerName ?? "missing"}; workerType={row?.WorkerType ?? "missing"}; payslip={initialPayout.PayslipNumber}; net={initialPayout.NetAmount}");
            return row;
        }
        if (await Read("initial") != initialPayout) throw new InvalidOperationException("Initial GET differs from creation.");
        if (!baseline)
        {
            await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                await NoWrite(HttpMethod.Put, path + "/profiles/" + profile, original with { WorkerName = "Must roll back" }, admin, HttpStatusCode.InternalServerError, "same-action profile audit500 full rollback");
                await NoWrite(HttpMethod.Post, path + "/payouts", payoutRequest with { PeriodLabel = "Must roll back" }, admin, HttpStatusCode.InternalServerError, "same-action payout audit500 full rollback");
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }
            await NoWrite(HttpMethod.Get, path + "/profiles", null, admin, HttpStatusCode.OK, "audit restoration profile read");
            if (await Read("after audit restoration") != initialPayout) throw new InvalidOperationException("Audit fault altered recorded identity.");
        }
        async Task Update(SavePayrollProfileRequest request, string label)
        {
            var before = await Snapshot(); using var response = await Send(HttpMethod.Put, path + "/profiles/" + profile, request, admin);
            RequireFinanceStatus(response, HttpStatusCode.OK, label);
            var result = await response.Content.ReadFromJsonAsync<PayrollProfileSummary>() ?? throw new InvalidOperationException("Missing profile update.");
            if (result.WorkerName != request.WorkerName || result.WorkerType != request.WorkerType || result.TeacherId != request.TeacherId || result.StaffUserId != request.StaffUserId || result.IsActive != request.IsActive)
                throw new InvalidOperationException("Profile update response differs.");
            Mutation(before, await Snapshot(), "Profiles", profile, false, "PUT Payroll", path + "/profiles/" + profile);
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var stored = await db.PayrollProfiles.AsNoTracking().SingleAsync(x => x.Id == profile);
            if (stored.WorkerName != request.WorkerName || stored.WorkerType != request.WorkerType || stored.TeacherId != request.TeacherId || stored.StaffUserId != request.StaffUserId || stored.IsActive != request.IsActive ||
                stored.AcademyId != academy || stored.PaymentModel != request.PaymentModel || stored.MonthlyAmount != request.MonthlyAmount || stored.AmountPerCycle != request.AmountPerCycle ||
                stored.SessionsPerCycle != request.SessionsPerCycle || stored.EffectiveFrom != request.EffectiveFrom)
                throw new InvalidOperationException("Fresh SQL profile update differs.");
            Pass(label + " profile response/fresh SQL, historical ledger and exact audit preservation");
        }
        var renamed = original with { WorkerName = "Synthetic Worker B" };
        await Update(renamed, "rename"); var afterName = await Read("after rename");
        Identity(afterName?.WorkerName == original.WorkerName, afterName?.WorkerName == renamed.WorkerName, "retain payout-time name after profile rename");
        var relinked = renamed with { WorkerType = "Staff", TeacherId = null, StaffUserId = actor.ToString() };
        await Update(relinked, "type/link replacement"); var afterType = await Read("after type/link replacement");
        Identity(afterType?.WorkerType == original.WorkerType, afterType?.WorkerType == relinked.WorkerType, "retain payout-time type after profile/link replacement");
        if (!baseline)
        {
            var before = await Snapshot(); await Task.Delay(1100);
            using var response = await Send(HttpMethod.Post, path + "/payouts", payoutRequest with { PeriodLabel = "QA-HISTORY-OCTOBER", Deductions = 100m }, admin);
            RequireFinanceStatus(response, HttpStatusCode.Created, "post-fault later payout");
            successor = await response.Content.ReadFromJsonAsync<PayrollPayoutSummary>() ?? throw new InvalidOperationException("Missing later payout.");
            Mutation(before, await Snapshot(), "Payouts", successor.Id, true, "POST Payroll", path + "/payouts");
            using var scope = factory.Services.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == successor.Id);
            if (successor.WorkerName != relinked.WorkerName || successor.WorkerType != "Staff" || successor.GrossAmount != 1000m || successor.Deductions != 100m || successor.NetAmount != 900m ||
                successor.IdentitySource != "PayoutSnapshot" || successor.IdentityRecordedAtUtc is null || stored.IdentityRecordedAtUtc != successor.IdentityRecordedAtUtc ||
                stored.WorkerNameSnapshot != relinked.WorkerName || stored.WorkerTypeSnapshot != "Staff" || stored.TeacherIdSnapshot is not null || stored.StaffUserIdSnapshot != actor.ToString() ||
                stored.GrossAmount != 1000m || stored.Deductions != 100m || stored.NetAmount != 900m || stored.AcademyId != academy || stored.PayrollProfileId != profile ||
                stored.PayslipNumber != successor.PayslipNumber || stored.PeriodLabel != successor.PeriodLabel || stored.Currency != successor.Currency || stored.Status != successor.Status ||
                stored.PaymentMethod != successor.PaymentMethod || stored.Reference != successor.Reference || stored.PaidAtUtc != successor.PaidAtUtc || stored.SessionsCovered != successor.SessionsCovered)
                throw new InvalidOperationException("Later payout does not match edited profile/current exact values.");
            Pass("post-audit-fault payout recovery records current Staff identity, exact new row and audit, original Teacher history preserved");
            if (await Read("after later payout") != initialPayout) throw new InvalidOperationException("Later payout altered original identity.");
        }
        await Update(relinked with { IsActive = false }, "deactivate");
        if (await Read("after deactivation") != afterType) throw new InvalidOperationException("Deactivation unexpectedly changes visibility.");
        await NoWrite(HttpMethod.Post, path + "/payouts", payoutRequest, admin, HttpStatusCode.BadRequest, "inactive profile payout400");
        foreach (var denied in new[] { ("anonymous", (string?)null, HttpStatusCode.Unauthorized), ("foreign admin", (string?)foreignAdmin, HttpStatusCode.Forbidden), ("Teacher", (string?)teacherToken, HttpStatusCode.Forbidden) })
        {
            await NoWrite(HttpMethod.Get, path + "/payouts", null, denied.Item2, denied.Item3, denied.Item1 + " payout read");
            await NoWrite(HttpMethod.Put, path + "/profiles/" + profile, original, denied.Item2, denied.Item3, denied.Item1 + " profile edit");
            await NoWrite(HttpMethod.Post, path + "/payouts", payoutRequest, denied.Item2, denied.Item3, denied.Item1 + " payout create");
        }
        await NoWrite(HttpMethod.Get, $"/api/academies/{foreign}/payroll/payouts", null, admin, HttpStatusCode.Forbidden, "local admin foreign route");
        await NoWrite(HttpMethod.Put, path + "/profiles/" + Guid.NewGuid(), original, admin, HttpStatusCode.NotFound, "missing profile edit404");
        await NoWrite(HttpMethod.Post, path + "/payouts", payoutRequest with { PayrollProfileId = Guid.NewGuid() }, admin, HttpStatusCode.BadRequest, "missing profile payout400");
        // Test-only removal in this owned DB. There is no production DELETE route.
        var beforeRemove = await Snapshot();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.PayrollProfiles.Remove(await db.PayrollProfiles.SingleAsync(x => x.Id == profile && x.AcademyId == academy));
            await db.SaveChangesAsync();
        }
        using (var a = JsonDocument.Parse(beforeRemove)) using (var b = JsonDocument.Parse(await Snapshot()))
        {
            foreach (var field in new[] { "Payouts", "Audits", "Invoices", "Payments" }) Equal(a.RootElement.GetProperty(field), b.RootElement.GetProperty(field), "owned profile removal " + field);
            var old = a.RootElement.GetProperty("Profiles").EnumerateArray().Where(x => x.GetProperty("Id").GetGuid() != profile).ToArray();
            var now = b.RootElement.GetProperty("Profiles").EnumerateArray().ToArray();
            if (old.Length != now.Length) throw new InvalidOperationException("Removal changed extra profiles.");
            for (var i = 0; i < old.Length; i++) Equal(old[i], now[i], "owned removal sentinel profile");
        }
        Pass("owned missing-profile fixture removes only selected profile, not payout/audit/foreign/finance rows");
        var orphan = await Read("after owned profile removal");
        Identity(orphan?.WorkerName == original.WorkerName && orphan.WorkerType == original.WorkerType, orphan is null, "retain historical payout visibility after profile removal");
        Console.WriteLine($"PAYROLLHISTORY {(baseline ? "DIAGNOSTIC COMPLETE / NOT ACCEPTED" : "GUARDED PASS")}: controls={checks}; reproduced historical failures={failures}; no HTTP429; synthetic data only; browser/device/concurrency and wider migration/rollback release gates NOT RUN.");
        if (baseline && failures != 3) throw new InvalidOperationException("Expected three frozen historical failures.");
    }
}
