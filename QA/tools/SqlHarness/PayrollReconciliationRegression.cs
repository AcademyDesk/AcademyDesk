using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollReconciliationAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, teacher, staff; Guid teacherUser;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacherUser = await identity.Users.Where(x => x.UserName == "qa-teacher-a@example.invalid").Select(x => x.Id).SingleAsync();
            teacher = (await identity.Users.Where(x => x.Id == teacherUser).Select(x => x.TeacherId).SingleAsync())!.Value;
            staff = await identity.Users.Where(x => x.AcademyId == academy && x.TeacherId == null && !x.IsPlatformOwner).Select(x => x.Id).FirstAsync();
        }
        // Authenticate before capturing financial/audit snapshots.
        var adminToken = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var path = $"/api/academies/{academy}/payroll";
        var checks = 0; var statuses = new List<int[]>();
        void Pass(string label) { checks++; Console.WriteLine($"PAYROLLRECONCILE CASE {label} PASS."); }
        async Task<Guid> Profile(string model = "Monthly")
        {
            using var reply = await client.PostAsJsonAsync(path + "/profiles", new { workerType = "Teacher", teacherId = teacher,
                workerName = "Synthetic reconciled worker", paymentModel = model, monthlyAmount = model == "Monthly" ? (decimal?)1000 : null,
                amountPerCycle = model == "SessionBlock" ? (decimal?)1000 : null, sessionsPerCycle = model == "SessionBlock" ? (int?)4 : null });
            RequireFinanceStatus(reply, HttpStatusCode.Created, "reconciled valid profile");
            using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync()); return json.RootElement.GetProperty("id").GetGuid();
        }
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { Profiles = await db.PayrollProfiles.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Payouts = await db.PayrollPayouts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task Refuse(string label, Func<Task<HttpResponseMessage>> action, HttpStatusCode expected = HttpStatusCode.BadRequest)
        {
            var before = await State(); using var reply = await action(); RequireFinanceStatus(reply, expected, label);
            if (before != await State()) throw new InvalidOperationException("Rejected payroll request changed persisted state: " + label);
            Pass(label);
        }
        var first = await Profile(); var alternate = await Profile(); var period = "QA-reconcile-" + manifest.RunId.ToString("N");
        Guid paidId;
        using (var reply = await client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = first, periodLabel = period, deductions = 0.01m }))
        {
            RequireFinanceStatus(reply, HttpStatusCode.Created, "reconciled payout");
            using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync()); var body = json.RootElement; paidId = body.GetProperty("id").GetGuid();
            if (body.GetProperty("netAmountExact").GetString() != "999.99" || body.GetProperty("identitySource").GetString() != "PayoutSnapshot")
                throw new InvalidOperationException("Recorded payout lacks exact amount/identity provenance.");
            using var scope = factory.Services.CreateScope(); var row = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().PayrollPayouts.AsNoTracking().SingleAsync(x => x.Id == paidId);
            if (row.TeacherIdSnapshot != teacher || row.IdentityRecordedAtUtc is null || row.NetAmount != 999.99m) throw new InvalidOperationException("Payout snapshot not persisted.");
        }
        Pass("positive payout records exact amount and worker provenance");
        await Refuse("alternate profile cannot repeat regular period", () => client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = alternate, periodLabel = "  " + period.ToUpperInvariant() + "  ", deductions = 0m }), HttpStatusCode.Conflict);
        foreach (var model in new[] { "Monthly", "SessionBlock" })
        {
            var profile = model == "Monthly" ? first : await Profile(model);
            await Refuse(model + " zero-net rejected", () => client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = profile, periodLabel = "Rejected zero", deductions = 1000m }));
        }
        await Refuse("profile decimal rounding rejected", () => client.PostAsJsonAsync(path + "/profiles", new { workerType = "Teacher", teacherId = teacher, workerName = "Rejected rounding", paymentModel = "Monthly", monthlyAmount = 1000.001m }));
        await Refuse("profile update decimal rounding rejected", () => client.PutAsJsonAsync(path + "/profiles/" + first, new { workerType = "Teacher", teacherId = teacher, workerName = "Rejected rounding", paymentModel = "Monthly", monthlyAmount = 1000.001m }));
        await Refuse("payout decimal rounding rejected", () => client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = first, periodLabel = "Rejected rounding", deductions = 0.001m }));
        await Refuse("teacher identity cannot be staff worker", () => client.PostAsJsonAsync(path + "/profiles", new { workerType = "Staff", staffUserId = teacherUser.ToString(), workerName = "Invalid staff", paymentModel = "Monthly", monthlyAmount = 1000m }));
        await Refuse("profile cannot change to teacher identity labelled staff", () => client.PutAsJsonAsync(path + "/profiles/" + alternate, new { workerType = "Staff", staffUserId = teacherUser.ToString(), workerName = "Invalid staff", paymentModel = "Monthly", monthlyAmount = 1000m }));
        Guid invalidProfile;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var invalid = new PayrollProfile { AcademyId = academy, WorkerType = "Staff", StaffUserId = teacherUser.ToString(), WorkerName = "Legacy invalid worker", MonthlyAmount = 1000m };
            invalidProfile = invalid.Id; db.Add(invalid); await db.SaveChangesAsync();
        }
        await Refuse("legacy invalid worker cannot be paid", () => client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = invalidProfile, periodLabel = "Rejected worker", deductions = 0m }));
        Guid historicalId, unverifiedId;
        var legacyPeriod = "QA-historical-" + manifest.RunId.ToString("N");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var historical = new PayrollPayout { AcademyId = academy, PayrollProfileId = first, PayslipNumber = "QA-OLD-" + Guid.NewGuid().ToString("N"),
                PeriodLabel = legacyPeriod, GrossAmount = 1000m, Deductions = 1000m, NetAmount = 0m, Status = "NoCashDue", PaymentMethod = "NoCashDue", PaidAtUtc = null,
                WorkerNameSnapshot = "Historical worker", WorkerTypeSnapshot = "Teacher", TeacherIdSnapshot = teacher, IdentityRecordedAtUtc = DateTime.UtcNow };
            var unverified = new PayrollPayout { AcademyId = academy, PayrollProfileId = first, PayslipNumber = "QA-UNKNOWN-" + Guid.NewGuid().ToString("N"),
                PeriodLabel = "Unverified", WorkerNameSnapshot = "Backfilled current profile name", WorkerTypeSnapshot = "Teacher", GrossAmount = 100m, NetAmount = 100m };
            historicalId = historical.Id; unverifiedId = unverified.Id; db.AddRange(historical, unverified); await db.SaveChangesAsync();
        }
        using (var listed = await client.GetAsync(path + "/payouts"))
        {
            RequireFinanceStatus(listed, HttpStatusCode.OK, "legacy register"); using var json = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
            var historical = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == historicalId);
            if (historical.GetProperty("paidAtUtc").ValueKind != JsonValueKind.Null || historical.GetProperty("settlementKind").GetString() != "NoCashDue") throw new InvalidOperationException("Historical zero-net row fabricated a payment.");
            Pass("historical zero-net remains visible without fabricated timestamp");
            var unknown = json.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetGuid() == unverifiedId);
            if (unknown.GetProperty("workerName").GetString() != "Unavailable worker" || unknown.GetProperty("identitySource").GetString() != "LegacyUnverified") throw new InvalidOperationException("Backfilled name was promoted to verified identity.");
            Pass("legacy name backfill is not verified payout provenance");
        }
        await Refuse("historical no-cash period cannot be paid again", () => client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = alternate, periodLabel = legacyPeriod, deductions = 0m }), HttpStatusCode.Conflict);
        async Task TeacherHistory(string label)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/teacher/payments"); request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", teacherToken);
            using var response = await client.SendAsync(request); RequireFinanceStatus(response, HttpStatusCode.OK, label);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var rows = json.RootElement.GetProperty("payslips").EnumerateArray().ToArray();
            if (!rows.Any(x => x.GetProperty("id").GetGuid() == paidId) || rows.Any(x => x.GetProperty("id").GetGuid() == unverifiedId)) throw new InvalidOperationException("Teacher history lost its own payout or disclosed unverified identity.");
            Pass(label);
        }
        await TeacherHistory("teacher sees only recorded own-worker history");
        using (var reassigned = await client.PutAsJsonAsync(path + "/profiles/" + first, new { workerType = "Staff", staffUserId = staff.ToString(), workerName = "Reassigned worker", paymentModel = "Monthly", monthlyAmount = 2000m }))
            RequireFinanceStatus(reassigned, HttpStatusCode.OK, "legitimate worker reassignment");
        await TeacherHistory("profile reassignment does not grant or remove historical payroll ownership");
        foreach (var action in new[] { "payout", "profile-update" })
        {
            var before = await State(); await SetAuditInsertDeniedAsync(manifest, true);
            try
            {
                using var response = action == "payout"
                    ? await client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = alternate, periodLabel = "Audit fault", deductions = 1m })
                    : await client.PutAsJsonAsync(path + "/profiles/" + alternate, new { workerType = "Teacher", teacherId = teacher, workerName = "Must roll back", paymentModel = "Monthly", monthlyAmount = 2000m });
                RequireFinanceStatus(response, HttpStatusCode.InternalServerError, "payroll required audit fault");
                if (before != await State()) throw new InvalidOperationException("Payroll mutation survived required audit failure.");
            }
            finally { await SetAuditInsertDeniedAsync(manifest, false); }
            Pass(action + " rolls back on mandatory audit failure");
        }
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var aProfile = await Profile(); var bProfile = await Profile(); var label = $"QA-race-{manifest.RunId:N}-{attempt}";
            await using var blocker = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!));
            await blocker.OpenAsync(); await using var transaction = (SqlTransaction)await blocker.BeginTransactionAsync();
            await using (var hold = new SqlCommand("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=@key,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=1000; SELECT @r;", blocker, transaction))
            {
                hold.Parameters.AddWithValue("@key", $"AcademyDesk:RegularPayroll:{academy:D}");
                if (await hold.ExecuteScalarAsync() is not int acquired || acquired < 0) throw new InvalidOperationException("Synthetic payroll lock not acquired.");
            }
            string lockResource;
            await using (var held = new SqlCommand("SELECT resource_description FROM sys.dm_tran_locks WHERE resource_database_id=DB_ID() AND resource_type='APPLICATION' AND request_session_id=@@SPID AND request_status='GRANT'", blocker, transaction))
                lockResource = (string)(await held.ExecuteScalarAsync() ?? throw new InvalidOperationException("Held payroll application lock missing."));
            var left = client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = aProfile, periodLabel = label, deductions = 0m });
            var right = client.PostAsJsonAsync(path + "/payouts", new { payrollProfileId = bProfile, periodLabel = " " + label.ToUpperInvariant() + " ", deductions = 0m });
            var observed = false; var maximumWaiters = 0;
            try
            {
                await using var probe = new SqlConnection(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!)); await probe.OpenAsync();
                for (var poll = 0; poll < 60; poll++)
                {
                    // Observe the exact held APPLICATION resource, not an implementation-
                    // dependent inner system-procedure sql_handle/text during its wait.
                    await using var command = new SqlCommand("SELECT COUNT(DISTINCT l.request_session_id) FROM sys.dm_tran_locks l JOIN sys.dm_exec_sessions s ON l.request_session_id=s.session_id WHERE l.resource_database_id=DB_ID() AND l.resource_type='APPLICATION' AND l.request_status='WAIT' AND l.resource_description=@resource AND s.login_name=@login", probe);
                    command.Parameters.AddWithValue("@login", manifest.RuntimeLogin);
                    command.Parameters.AddWithValue("@resource", lockResource);
                    maximumWaiters = Math.Max(maximumWaiters, (int)(await command.ExecuteScalarAsync())!);
                    if (maximumWaiters >= 2) { observed = true; break; }
                    await Task.Delay(100);
                }
            }
            finally { await transaction.CommitAsync(); }
            await Task.WhenAll(left, right); using var l = await left; using var r = await right;
            var pair = new[] { (int)l.StatusCode, (int)r.StatusCode }.Order().ToArray();
            if (!observed || !pair.SequenceEqual(new[] { 201, 409 })) throw new InvalidOperationException($"Payroll race refused:observed={observed};waiters={maximumWaiters};statuses={string.Join(',', pair)}; requires two exact-resource waiters and201/409.");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var rows = await db.PayrollPayouts.AsNoTracking().Where(x => x.PayrollProfileId == aProfile || x.PayrollProfileId == bProfile).ToArrayAsync();
            if (rows.Length != 1 || rows[0].NetAmount != 1000m || rows[0].TeacherIdSnapshot != teacher) throw new InvalidOperationException("Concurrent payroll stored duplicate or incorrect payout.");
            statuses.Add(pair); Pass("two observed SQL lock waiters; alternate-profile race " + attempt + " one201/one409 and one payout");
        }
        var beforeDowngrade = await State();
        await using (var admin = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseSqlServer(Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!)).Options))
        {
            var denied = false;
            try { await admin.GetService<IMigrator>().MigrateAsync("20261028173216_SnapshotPayrollWorkerIdentity"); }
            catch (SqlException error) when (error.Number == 51000) { denied = true; }
            if (!denied || !(await admin.Database.GetAppliedMigrationsAsync()).Any(x => x.EndsWith("_ReconcilePayrollProvenance"))) throw new InvalidOperationException("Unsafe provenance downgrade was not blocked atomically.");
        }
        if (beforeDowngrade != await State()) throw new InvalidOperationException("Blocked downgrade changed payout history.");
        Pass("downgrade refuses legacy null timestamp before removing provenance");
        if (checks != 23) throw new InvalidOperationException("Payroll reconciliation check inventory changed: " + checks);
        var directory = Path.GetFullPath(Path.Combine("QA", "EVIDENCE", "payroll-reconciliation-sql-" + manifest.RunId.ToString("N"))); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(new { passed = true, checks, observedWaiterRaces = 5, statuses,
            zeroNetRejected = true, legacyRowsPreserved = true, auditRollbackCases = 2, modelInference = false, productionApproved = false }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PAYROLLRECONCILE PASS:23 real SQL/HTTP checks, five deterministic alternate-profile races, two mandatory audit rollbacks and guarded downgrade.");
    }
}
