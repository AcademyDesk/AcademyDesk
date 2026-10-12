using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private sealed record PayrollRaceState(PayrollProfile[] Profiles, PayrollPayout[] Payouts,
        AuditLog[] Audits, Invoice[] Invoices, Payment[] Payments);

    private static async Task VerifyPayrollHistoryConcurrencyAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        if (Environment.GetEnvironmentVariable("QA_PAYROLL_HISTORY") != "guarded")
            throw new InvalidOperationException("Payroll concurrency requires retained guarded history.");
        Guid academy, teacher, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        var bearer = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}/payroll";
        async Task<PayrollRaceState> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return new((await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray());
        }
        void Exact(PayrollRaceState expected, PayrollRaceState actual)
        {
            using var a = JsonDocument.Parse(JsonSerializer.Serialize(expected)); using var b = JsonDocument.Parse(JsonSerializer.Serialize(actual));
            if (!JsonElement.DeepEquals(a.RootElement, b.RootElement)) throw new InvalidOperationException("Payroll race changed unexpected row/field in five-table snapshot.");
        }
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object body)
        {
            await Task.Delay(650); // Unchanged production limiter, no retry or bypass.
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            var result = await client.SendAsync(request);
            if (result.StatusCode == HttpStatusCode.TooManyRequests) { result.Dispose(); throw new InvalidOperationException("Payroll race cannot pass with HTTP429."); }
            return result;
        }
        async Task<PayrollProfile> Seed(string mode)
        {
            var profile = new PayrollProfile { AcademyId = academy, WorkerType = "Teacher", TeacherId = teacher, WorkerName = "Synthetic Race Original",
                PaymentModel = mode == "session" ? "SessionBlock" : "Monthly", MonthlyAmount = mode == "session" ? null : 1000m,
                AmountPerCycle = mode == "session" ? 1000m : null, SessionsPerCycle = mode == "session" ? 12 : null, EffectiveFrom = new DateOnly(2026,9,1) };
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            db.PayrollProfiles.Add(profile);
            db.PayrollPayouts.Add(new PayrollPayout { AcademyId = academy, PayrollProfileId = profile.Id, WorkerNameSnapshot = profile.WorkerName,
                WorkerTypeSnapshot = profile.WorkerType, TeacherIdSnapshot = teacher, IdentityRecordedAtUtc = new DateTime(2026,9,30,12,0,0),
                PayslipNumber = "QA-RACE-OLD-" + profile.Id.ToString("N"), PeriodLabel = "Historical September", GrossAmount = 1000m, Deductions = 200m, NetAmount = 800m });
            await db.SaveChangesAsync(); return profile;
        }
        var outcomes = new Dictionary<string, HashSet<string>>(); var pairs = 0; var faultPairs = 0; var recoveryPairs = 0;
        async Task Pair(string mode, bool editLaunchedFirst, bool fault, bool recovery)
        {
            var profile = await Seed(mode); var before = await State(); var started = DateTime.UtcNow;
            var edited = new SavePayrollProfileRequest(mode == "deactivate" ? "Teacher" : "Staff", mode == "deactivate" ? teacher : null,
                mode == "deactivate" ? null : actor.ToString(), mode == "deactivate" ? profile.WorkerName : "Synthetic Race Replacement",
                profile.PaymentModel, mode == "session" ? null : mode == "deactivate" ? 1000m : 2000m,
                mode == "session" ? 2500m : null, mode == "session" ? 5 : null, profile.EffectiveFrom, mode != "deactivate");
            var pay = new CreatePayrollPayoutRequest(profile.Id, "QA-RACE-OCTOBER", null, null, 200m, "BankTransfer", "QA-PROFILE-RACE",
                new DateTime(2026,10,1,12,0,0,DateTimeKind.Utc));
            var editPath = root + "/profiles/" + profile.Id;
            async Task<(HttpResponseMessage Edit, HttpResponseMessage Pay)> Overlap()
            {
                // Reuse the marker-validated owned SQL coordinator, never a live connection.
                await using var blocker = await OpenInvoiceIntegrityAdminAsync(manifest);
                await using var transaction = (SqlTransaction)await blocker.BeginTransactionAsync();
                await using (var hold = new SqlCommand("SELECT [Id] FROM [PayrollProfiles] WITH (UPDLOCK,HOLDLOCK) WHERE [AcademyId]=@academy AND [Id]=@profile", blocker, transaction))
                {
                    hold.Parameters.AddWithValue("@academy", academy); hold.Parameters.AddWithValue("@profile", profile.Id);
                    if ((Guid)(await hold.ExecuteScalarAsync())! != profile.Id) throw new InvalidOperationException("Missing owned payroll blocker row.");
                }
                Task<HttpResponseMessage>? first = null, second = null; Exception? coordinationFailure = null;
                try
                {
                    first = editLaunchedFirst ? Send(HttpMethod.Put, editPath, edited) : Send(HttpMethod.Post, root + "/payouts", pay);
                    await RequirePayrollRaceWaitersAsync(manifest, blocker.ServerProcessId, 1);
                    second = editLaunchedFirst ? Send(HttpMethod.Post, root + "/payouts", pay) : Send(HttpMethod.Put, editPath, edited);
                    await RequirePayrollRaceWaitersAsync(manifest, blocker.ServerProcessId, 2);
                }
                catch (Exception failure) { coordinationFailure = failure; }
                finally { await transaction.RollbackAsync(); }
                var tasks = new[] { first, second }.Where(x => x is not null).Select(x => x!).ToArray();
                var responses = await Task.WhenAll(tasks);
                if (coordinationFailure is not null || responses.Length != 2)
                {
                    foreach (var response in responses) response.Dispose();
                    throw new InvalidOperationException("Payroll SQL overlap not accepted; launched responses drained.", coordinationFailure);
                }
                return editLaunchedFirst ? (responses[0], responses[1]) : (responses[1], responses[0]);
            }
            if (fault) await SetAuditInsertDeniedAsync(manifest, true);
            (HttpResponseMessage Edit, HttpResponseMessage Pay) results;
            try { results = await Overlap(); }
            finally { if (fault) await SetAuditInsertDeniedAsync(manifest, false); }
            using var editResponse = results.Edit; using var payResponse = results.Pay;
            var after = await State(); var finished = DateTime.UtcNow;
            if (fault)
            {
                RequireFinanceStatus(editResponse, HttpStatusCode.InternalServerError, "overlapped profile audit rollback");
                RequireFinanceStatus(payResponse, HttpStatusCode.InternalServerError, "overlapped payout audit rollback");
                Exact(before, after); faultPairs++;
                Console.WriteLine($"PAYROLLRACE FAULT PASS mode={mode} statuses=500/500 SQLwaiters=2 fullRollback=true no429=true");
                // Same profile and same operations must recover after the injected fault.
                results = await Overlap(); using var recoveredEdit = results.Edit; using var recoveredPay = results.Pay;
                await Validate(recoveredEdit, recoveredPay, before, await State(), DateTime.UtcNow, recovered: true);
            }
            else await Validate(editResponse, payResponse, before, after, finished, recovered: recovery);

            async Task Validate(HttpResponseMessage edit, HttpResponseMessage payout, PayrollRaceState old, PayrollRaceState now, DateTime ended, bool recovered)
            {
                RequireFinanceStatus(edit, HttpStatusCode.OK, "overlapped profile update");
                var summary = await edit.Content.ReadFromJsonAsync<PayrollProfileSummary>() ?? throw new InvalidOperationException("Missing edit summary.");
                var expected = JsonSerializer.Deserialize<PayrollRaceState>(JsonSerializer.Serialize(old))!;
                var target = expected.Profiles.Single(x => x.Id == profile.Id);
                target.WorkerName = edited.WorkerName; target.WorkerType = edited.WorkerType; target.TeacherId = edited.TeacherId; target.StaffUserId = edited.StaffUserId;
                target.PaymentModel = edited.PaymentModel; target.MonthlyAmount = edited.MonthlyAmount; target.AmountPerCycle = edited.AmountPerCycle;
                target.SessionsPerCycle = edited.SessionsPerCycle; target.EffectiveFrom = edited.EffectiveFrom!.Value; target.IsActive = edited.IsActive;
                if (summary != new PayrollProfileSummary(target.Id, target.WorkerType, target.TeacherId, target.StaffUserId, target.WorkerName,
                    target.PaymentModel, target.MonthlyAmount, target.AmountPerCycle, target.SessionsPerCycle, target.EffectiveFrom, target.IsActive))
                    throw new InvalidOperationException("Race edit response differs from exact requested profile.");
                var auditExpected = new List<(string Action, string Route)> { ("PUT Payroll", editPath) }; string outcome;
                if (payout.StatusCode == HttpStatusCode.BadRequest)
                {
                    if (mode != "deactivate" || (await payout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString() != "Choose an active payroll profile.")
                        throw new InvalidOperationException("Unexpected payroll race rejection/message.");
                    outcome = "edit-before-pay";
                }
                else
                {
                    RequireFinanceStatus(payout, HttpStatusCode.Created, "overlapped payout");
                    var dto = await payout.Content.ReadFromJsonAsync<PayrollPayoutSummary>() ?? throw new InvalidOperationException("Missing payout summary.");
                    var added = now.Payouts.Where(x => old.Payouts.All(y => x.Id != y.Id)).Single();
                    var source = added.WorkerNameSnapshot == profile.WorkerName ? profile : target;
                    if (mode == "deactivate" && source != profile) throw new InvalidOperationException("Inactive profile paid after deactivation.");
                    outcome = source == profile ? "pay-before-edit" : "edit-before-pay";
                    var gross = mode == "session" ? source.AmountPerCycle!.Value : source.MonthlyAmount!.Value;
                    if (added.Id != dto.Id || added.AcademyId != academy || added.PayrollProfileId != profile.Id ||
                        added.WorkerNameSnapshot != source.WorkerName || added.WorkerTypeSnapshot != source.WorkerType || added.TeacherIdSnapshot != source.TeacherId || added.StaffUserIdSnapshot != source.StaffUserId ||
                        added.GrossAmount != gross || added.Deductions != 200m || added.NetAmount != gross-200m || added.Currency != "INR" || added.Status != "Paid" ||
                        added.SessionsCovered != (mode == "session" ? source.SessionsPerCycle : null) || added.PeriodLabel != pay.PeriodLabel || added.Reference != pay.Reference ||
                        added.PaymentMethod != pay.PaymentMethod || added.PaidAtUtc != pay.PaidAtUtc || string.IsNullOrEmpty(added.PayslipNumber) ||
                        added.IdentityRecordedAtUtc is null || added.IdentityRecordedAtUtc < started || added.IdentityRecordedAtUtc > ended || added.CreatedAtUtc < started || added.CreatedAtUtc > ended || added.UpdatedAtUtc is not null ||
                        dto != new PayrollPayoutSummary(added.Id, profile.Id, source.WorkerName, source.WorkerType, added.PayslipNumber, added.PeriodLabel, added.SessionsCovered,
                            gross,200m,gross-200m,added.Currency,added.Status,added.PaymentMethod,added.Reference,added.PaidAtUtc,"PayoutSnapshot",added.IdentityRecordedAtUtc))
                        throw new InvalidOperationException("Payout mixed identity/pay versions or response/fresh SQL differs.");
                    expected = expected with { Payouts = expected.Payouts.Append(added).OrderBy(x => x.Id.ToString("N")).ToArray() };
                    auditExpected.Add(("POST Payroll", root + "/payouts"));
                }
                var audits = now.Audits.Where(x => old.Audits.All(y => x.Id != y.Id)).ToArray();
                if (audits.Length != auditExpected.Count) throw new InvalidOperationException("Race success audit count differs.");
                foreach (var audit in audits)
                {
                    using var metadata = JsonDocument.Parse(audit.MetadataJson!);
                    var index = auditExpected.FindIndex(x => x.Action == audit.Action && x.Route == metadata.RootElement.GetProperty("Route").GetString());
                    if (index < 0 || audit.AcademyId != academy || audit.ActorUserId != actor || audit.EntityType != "Payroll" || audit.EntityId is not null ||
                        audit.OccurredAtUtc < started || audit.OccurredAtUtc > ended) throw new InvalidOperationException("Race audit actor/tenant/action/route/time differs.");
                    auditExpected.RemoveAt(index);
                }
                expected = expected with { Audits = expected.Audits.Concat(audits).OrderBy(x => x.Id.ToString("N")).ToArray() };
                Exact(expected, now); // All old payout/audit/foreign/finance rows and every non-target field retained.
                var beforeRead = await State();
                using var readRequest = new HttpRequestMessage(HttpMethod.Get, root + "/payouts");
                readRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                await Task.Delay(650); using var read = await client.SendAsync(readRequest); RequireFinanceStatus(read,HttpStatusCode.OK,"race history read");
                var rows = await read.Content.ReadFromJsonAsync<PayrollPayoutSummary[]>() ?? throw new InvalidOperationException("Missing race register.");
                foreach (var row in now.Payouts.Where(x => x.AcademyId == academy && x.PayrollProfileId == profile.Id))
                {
                    var actual = rows.Single(x => x.Id == row.Id);
                    if (actual != new PayrollPayoutSummary(row.Id,row.PayrollProfileId,row.WorkerNameSnapshot!,row.WorkerTypeSnapshot!,row.PayslipNumber,row.PeriodLabel,
                        row.SessionsCovered,row.GrossAmount,row.Deductions,row.NetAmount,row.Currency,row.Status,row.PaymentMethod,row.Reference,row.PaidAtUtc,"PayoutSnapshot",row.IdentityRecordedAtUtc))
                        throw new InvalidOperationException("Race register/history identity or money differs.");
                }
                if (rows.Any(x => !now.Payouts.Any(p => p.Id == x.Id && p.AcademyId == academy)) || rows.Length != now.Payouts.Count(x => x.AcademyId == academy))
                    throw new InvalidOperationException("Race register leaked foreign or omitted own payout.");
                Exact(beforeRead, await State());
                if (!outcomes.TryGetValue(mode,out var observed)) outcomes[mode] = observed = [];
                observed.Add(outcome); if (recovered) recoveryPairs++; else pairs++;
                Console.WriteLine($"PAYROLLRACE PAIR PASS mode={mode} launchedFirst={(editLaunchedFirst ? "edit" : "pay")} observed={outcome} statuses={(int)edit.StatusCode}/{(int)payout.StatusCode} SQLwaiters=2 exactRowsAuditForeignRegister=true recovery={recovered} no429=true");
            }
        }
        foreach (var mode in new[] { "monthly", "session", "deactivate" })
        {
            for (var attempt=0;attempt<3;attempt++) foreach (var first in new[] { true, false }) await Pair(mode,first,fault:false,recovery:false);
            await Pair(mode,editLaunchedFirst:true,fault:true,recovery:false);
            if (!outcomes[mode].SetEquals(new[] { "edit-before-pay", "pay-before-edit" })) throw new InvalidOperationException("Both actual serialization outcomes not observed: " + mode);
        }
        if (pairs!=18 || faultPairs!=3 || recoveryPairs!=3) throw new InvalidOperationException("Payroll overlap acceptance count incomplete.");
        Console.WriteLine("PAYROLLRACE GUARDED PASS:18 normal +3 audit500 rollback +3 same-profile recovered SQL-overlapped pairs; both actual orders in all three modes; exact five-table snapshots/audits/register/no429. No app change; direct/platform/browser/device/critical/release remain OPEN.");
    }

    private static async Task RequirePayrollRaceWaitersAsync(QaRunManifest manifest, int blocker, int expected)
    {
        await using var monitor = await OpenInvoiceIntegrityAdminAsync(manifest);
        for(var poll=0;poll<150;poll++)
        {
            await using var command = new SqlCommand("WITH chain AS (SELECT @blocker AS session_id UNION ALL SELECT CAST(r.session_id AS int) FROM sys.dm_exec_requests r INNER JOIN chain c ON r.blocking_session_id=c.session_id WHERE r.database_id=DB_ID()) SELECT COUNT(DISTINCT r.session_id) FROM chain c INNER JOIN sys.dm_exec_requests r ON r.session_id=c.session_id CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t WHERE r.session_id<>@blocker AND r.database_id=DB_ID() AND r.wait_type LIKE 'LCK_M_%' AND t.text LIKE '%PayrollProfiles%' AND t.text LIKE '%UPDLOCK%' AND t.text LIKE '%HOLDLOCK%' OPTION(MAXRECURSION 30)",monitor);
            command.Parameters.AddWithValue("@blocker",blocker);
            if (Convert.ToInt32(await command.ExecuteScalarAsync())==expected)
            {
                if(expected==2)Console.WriteLine($"PAYROLLRACE overlapProof chainWaiters=2 root={blocker}");
                return;
            }
            await Task.Delay(100);
        }
        throw new InvalidOperationException($"Expected{expected} real payroll-lock HTTP waiters not observed in owned SQL within15s; no overlap acceptance.");
    }
}
