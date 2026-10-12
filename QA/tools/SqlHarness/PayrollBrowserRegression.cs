using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPayrollBrowserAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        Guid academy, teacher, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        client.DefaultRequestHeaders.Authorization = null;
        async Task<PayrollRaceState> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return new((await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray());
        }
        void Exact(object a, object b)
        {
            if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(a), JsonSerializer.SerializeToElement(b)))
                throw new InvalidOperationException("Linked payroll unexpected five-table row/field change.");
        }
        var initial = await State(); var ownedProfiles = new HashSet<Guid>(); var failures = new ConcurrentQueue<string>();
        var successful = 0; var denied = 0; var faults = 0; var reads = 0; var auditControls = 0;
        var nonce = Guid.NewGuid().ToString("N"); var root = $"/api/academies/{academy}/payroll";
        var builder = WebApplication.CreateSlimBuilder(); builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build(); using var pacing = new SemaphoreSlim(1, 1);
        // Retained native stages use this same production limiter; only QA pacing changes.
        Console.WriteLine("PAYROLL LINKED waiting one unchanged production limiter window.");
        await Task.Delay(TimeSpan.FromSeconds(60));
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") { context.Response.StatusCode = 400; return; }
            var route = context.Request.Path.ToString();
            var create = context.Request.Method == "POST" && route == root + "/profiles";
            var payout = context.Request.Method == "POST" && route == root + "/payouts";
            var edit = context.Request.Method == "PUT" && ownedProfiles.Any(id => route == root + "/profiles/" + id);
            var login = context.Request.Method == "POST" && route == "/api/auth/login";
            var control = context.Request.Method == "POST" && route == "/__qa/audit/" + nonce;
            if (context.Request.Method is not ("GET" or "OPTIONS") && !create && !payout && !edit && !login && !control) { context.Response.StatusCode = 405; return; }
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                await Task.Delay(650, context.RequestAborted);
                var before = await State(); var body = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
                if (control)
                {
                    var deniedInsert = JsonSerializer.Deserialize<bool>(body);
                    await SetAuditInsertDeniedAsync(manifest, deniedInsert); Exact(before, await State()); auditControls++;
                    context.Response.StatusCode = 204; return;
                }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (body.Length > 0) request.Content = new StringContent(body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && request.Content is not null) { request.Content.Headers.Remove(header.Key); request.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()); }
                }
                using var response = await client.SendAsync(request, context.RequestAborted); var bytes = await response.Content.ReadAsByteArrayAsync(context.RequestAborted);
                var after = await State();
                if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new InvalidOperationException("Linked payroll cannot pass with HTTP429.");
                if (create || payout || edit)
                {
                    if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.InternalServerError)
                    {
                        if (!payout) throw new InvalidOperationException("Unexpected failed profile write.");
                        Exact(before, after);
                        if (response.StatusCode == HttpStatusCode.BadRequest)
                        {
                            if (JsonDocument.Parse(bytes).RootElement.GetProperty("message").GetString() != "Deductions cannot exceed the gross payout amount.") throw new InvalidOperationException("Wrong payout rejection.");
                            denied++;
                        }
                        else faults++;
                    }
                    else
                    {
                        if (response.StatusCode != (edit ? HttpStatusCode.OK : HttpStatusCode.Created)) throw new InvalidOperationException("Unexpected linked payroll write status.");
                        using var result = JsonDocument.Parse(bytes); var id = result.RootElement.GetProperty("id").GetGuid();
                        var addedProfiles = after.Profiles.Where(x => before.Profiles.All(y => y.Id != x.Id)).ToArray();
                        var addedPayouts = after.Payouts.Where(x => before.Payouts.All(y => y.Id != x.Id)).ToArray();
                        var addedAudits = after.Audits.Where(x => before.Audits.All(y => y.Id != x.Id)).ToArray(); var audit = addedAudits.Single();
                        if (audit.AcademyId != academy || audit.ActorUserId != actor || audit.Action != (edit ? "PUT Payroll" : "POST Payroll") ||
                            JsonDocument.Parse(audit.MetadataJson!).RootElement.GetProperty("Route").GetString() != route) throw new InvalidOperationException("Linked payroll audit identity/action/route mismatch.");
                        var expectedProfiles = before.Profiles;
                        if (create || edit)
                        {
                            var submitted = JsonSerializer.Deserialize<SavePayrollProfileRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                            var stored = after.Profiles.Single(x => x.Id == id);
                            if (stored.AcademyId != academy || stored.WorkerName != submitted.WorkerName || stored.WorkerType != submitted.WorkerType || stored.TeacherId != submitted.TeacherId || stored.StaffUserId != submitted.StaffUserId ||
                                stored.PaymentModel != submitted.PaymentModel || stored.MonthlyAmount != submitted.MonthlyAmount || stored.AmountPerCycle != submitted.AmountPerCycle || stored.SessionsPerCycle != submitted.SessionsPerCycle || stored.EffectiveFrom != submitted.EffectiveFrom || stored.IsActive != submitted.IsActive)
                                throw new InvalidOperationException("Stored profile differs from request.");
                            if (create) { if (addedProfiles.Single().Id != id) throw new InvalidOperationException("Missing created profile."); ownedProfiles.Add(id); expectedProfiles = before.Profiles.Concat(addedProfiles).OrderBy(x => x.Id.ToString("N")).ToArray(); }
                            else
                            {
                                var previous = before.Profiles.Single(x => x.Id == id);
                                previous.WorkerType = submitted.WorkerType; previous.TeacherId = submitted.TeacherId; previous.StaffUserId = submitted.StaffUserId; previous.WorkerName = submitted.WorkerName;
                                previous.PaymentModel = submitted.PaymentModel; previous.MonthlyAmount = submitted.MonthlyAmount; previous.AmountPerCycle = submitted.AmountPerCycle; previous.SessionsPerCycle = submitted.SessionsPerCycle; previous.EffectiveFrom = submitted.EffectiveFrom!.Value; previous.IsActive = submitted.IsActive;
                                if (addedProfiles.Length != 0) throw new InvalidOperationException("Edit inserted profile.");
                            }
                            if (addedPayouts.Length != 0) throw new InvalidOperationException("Profile write inserted payout.");
                        }
                        else
                        {
                            var submitted = JsonSerializer.Deserialize<CreatePayrollPayoutRequest>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                            if (!ownedProfiles.Contains(submitted.PayrollProfileId)) throw new InvalidOperationException("Payout outside linked fixtures.");
                            var profile = before.Profiles.Single(x => x.Id == submitted.PayrollProfileId); var row = addedPayouts.Single();
                            var dto = JsonSerializer.Deserialize<PayrollPayoutSummary>(bytes, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                            if (row.Id != id || row.AcademyId != academy || row.PayrollProfileId != profile.Id || row.WorkerNameSnapshot != profile.WorkerName || row.WorkerTypeSnapshot != profile.WorkerType ||
                                row.TeacherIdSnapshot != profile.TeacherId || row.StaffUserIdSnapshot != profile.StaffUserId || row.IdentityRecordedAtUtc is null || row.GrossAmount != 1000m || row.Deductions != 200m || row.NetAmount != 800m ||
                                row.SessionsCovered != (profile.PaymentModel == "Monthly" ? null : 4) || row.PaymentMethod != "BankTransfer" || row.Reference is not null || row.Currency != "INR" || row.Status != "Paid" ||
                                dto.WorkerName != row.WorkerNameSnapshot || dto.WorkerType != row.WorkerTypeSnapshot || dto.NetAmount != row.NetAmount || dto.IdentitySource != "PayoutSnapshot" || dto.IdentityRecordedAtUtc != row.IdentityRecordedAtUtc || dto.PayslipNumber != row.PayslipNumber || dto.PaidAtUtc != row.PaidAtUtc)
                                throw new InvalidOperationException("Linked payout snapshot/financial/response mismatch.");
                            if (addedProfiles.Length != 0) throw new InvalidOperationException("Payout inserted profile.");
                        }
                        Exact(before with { Profiles = expectedProfiles, Payouts = before.Payouts.Concat(addedPayouts).OrderBy(x => x.Id.ToString("N")).ToArray(), Audits = before.Audits.Concat(addedAudits).OrderBy(x => x.Id.ToString("N")).ToArray() }, after);
                        successful++;
                    }
                }
                else { Exact(before, after); reads++; }
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers)) if (!header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) && !header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) context.Response.Headers[header.Key] = header.Value.ToArray();
                Console.WriteLine($"PAYROLL LINKED HTTP PASS: {context.Request.Method} {route} {(int)response.StatusCode}, full five-table snapshot verified.");
                await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
            }
            catch (Exception error) { failures.Enqueue(error.Message); throw; }
            finally { pacing.Release(); }
        });
        await bridge.StartAsync();
        var start = new ProcessStartInfo("node") { WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var argument in new[] { Path.GetFullPath("QA/tools/payroll-live.cjs"), bridge.Urls.Single(), academy.ToString(), teacher.ToString(), actor.ToString(), nonce }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Payroll linked browser failed to start.");
        var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
        try { using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8)); await process.WaitForExitAsync(timeout.Token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        finally { await bridge.StopAsync(); await SetAuditInsertDeniedAsync(manifest, false); }
        Console.Write(await output); var errorText = await errors;
        if (process.ExitCode != 0 || !failures.IsEmpty || successful != 20 || denied != 4 || faults != 1 || auditControls != 2 || ownedProfiles.Count != 4 || reads == 0)
            throw new InvalidOperationException($"Payroll linked failed ({successful}/{denied}/{faults}/{auditControls}): " + errorText + string.Join("; ", failures));
        var final = await State();
        foreach (var id in ownedProfiles)
            if (final.Profiles.Single(x => x.Id == id).IsActive || final.Payouts.Count(x => x.PayrollProfileId == id) != 2) throw new InvalidOperationException("Linked final profile/payout count differs.");
        Exact(initial.Payouts, final.Payouts.Where(x => !ownedProfiles.Contains(x.PayrollProfileId)).ToArray());
        Console.WriteLine($"PAYROLL LINKED PASS: four browser journeys, 20 successful writes/four400/one audit500 full rollback, two audit controls, {reads} unchanged read snapshots; four profiles/eight payouts/exact audits/no429. Physical devices NOT RUN.");
    }
}
