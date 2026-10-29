using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
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
    private static async Task VerifyPayrollDocumentBrowserAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, teacher, actor;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            teacher = await db.Teachers.Where(x => x.AcademyId == academy && x.FirstName == "Synthetic").Select(x => x.Id).SingleAsync();
            actor = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
        }
        async Task<PayrollRaceState> State()
        {
            using var scope = factory.Services.CreateScope();var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return new((await db.PayrollProfiles.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.PayrollPayouts.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.AuditLogs.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Invoices.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray(),
                (await db.Payments.AsNoTracking().ToArrayAsync()).OrderBy(x => x.Id.ToString("N")).ToArray());
        }
        void Exact(object a, object b)
        {
            if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(a), JsonSerializer.SerializeToElement(b)))
                throw new InvalidOperationException("Payroll document linked five-table mismatch.");
        }
        var initial = await State();var profiles = new HashSet<Guid>();var failures = new ConcurrentQueue<string>();
        var writes = 0;var reads = 0;var denied = 0;var teacherReads = 0;
        var root = $"/api/academies/{academy}/payroll";
        var builder = WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build();using var pacing = new SemaphoreSlim(1,1);
        Console.WriteLine("PAYROLL DOCUMENT waiting unchanged production limiter window before linked precision/Teacher proof.");
        await Task.Delay(TimeSpan.FromSeconds(60));client.DefaultRequestHeaders.Authorization = null;
        bridge.Run(async context =>
        {
            if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1") {context.Response.StatusCode=400;return;}
            var route=context.Request.Path.ToString();var create=context.Request.Method=="POST"&&route==root+"/profiles";
            var pay=context.Request.Method=="POST"&&route==root+"/payouts";var login=context.Request.Method=="POST"&&route=="/api/auth/login";
            if(context.Request.Method is not ("GET" or "OPTIONS")&&!create&&!pay&&!login){context.Response.StatusCode=405;return;}
            await pacing.WaitAsync(context.RequestAborted);
            try
            {
                await Task.Delay(650,context.RequestAborted);var before=await State();
                var body=await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted);
                using var request=new HttpRequestMessage(new HttpMethod(context.Request.Method),context.Request.Path+context.Request.QueryString);
                if(body.Length>0)request.Content=new StringContent(body);
                foreach(var header in context.Request.Headers)
                {
                    if(header.Key.Equals("Host",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Connection",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase)||header.Key.Equals("Content-Length",StringComparison.OrdinalIgnoreCase))continue;
                    if(!request.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray())&&request.Content is not null){request.Content.Headers.Remove(header.Key);request.Content.Headers.TryAddWithoutValidation(header.Key,header.Value.ToArray());}
                }
                using var response=await client.SendAsync(request,context.RequestAborted);var bytes=await response.Content.ReadAsByteArrayAsync(context.RequestAborted);var after=await State();
                if(response.StatusCode==HttpStatusCode.TooManyRequests)throw new InvalidOperationException("Payroll document cannot pass with429.");
                if(create||pay)
                {
                    RequireFinanceStatus(response,HttpStatusCode.Created,"linked boundary write");
                    var addedProfiles=after.Profiles.Where(x=>before.Profiles.All(y=>y.Id!=x.Id)).ToArray();
                    var addedPayouts=after.Payouts.Where(x=>before.Payouts.All(y=>y.Id!=x.Id)).ToArray();
                    var addedAudits=after.Audits.Where(x=>before.Audits.All(y=>y.Id!=x.Id)).ToArray();var audit=addedAudits.Single();
                    if(audit.AcademyId!=academy||audit.ActorUserId!=actor||audit.Action!="POST Payroll"||JsonDocument.Parse(audit.MetadataJson!).RootElement.GetProperty("Route").GetString()!=route)throw new InvalidOperationException("Boundary actor/tenant/route audit mismatch.");
                    using var wire=JsonDocument.Parse(bytes);var id=wire.RootElement.GetProperty("id").GetGuid();
                    if(create)
                    {
                        var submitted=JsonSerializer.Deserialize<SavePayrollProfileRequest>(body,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;var row=addedProfiles.Single();
                        var amount=submitted.PaymentModel=="Monthly"?submitted.MonthlyAmount:submitted.AmountPerCycle;
                        if(row.Id!=id||row.AcademyId!=academy||row.TeacherId!=teacher||row.StaffUserId is not null||row.WorkerType!="Teacher"||row.WorkerName!=submitted.WorkerName||row.PaymentModel!=submitted.PaymentModel||row.MonthlyAmount!=submitted.MonthlyAmount||row.AmountPerCycle!=submitted.AmountPerCycle||row.SessionsPerCycle!=submitted.SessionsPerCycle||row.EffectiveFrom!=submitted.EffectiveFrom||!row.IsActive||amount is not (0.01m or 9999999999999999.99m)||addedPayouts.Length!=0)throw new InvalidOperationException("Boundary profile SQL/request differs.");
                        var field=submitted.PaymentModel=="Monthly"?"monthlyAmountExact":"amountPerCycleExact";
                        if(wire.RootElement.GetProperty(field).GetString()!=amount.Value.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture))throw new InvalidOperationException("Boundary profile wire lost cents.");
                        profiles.Add(row.Id);
                    }
                    else
                    {
                        var submitted=JsonSerializer.Deserialize<CreatePayrollPayoutRequest>(body,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
                        if(!profiles.Contains(submitted.PayrollProfileId))throw new InvalidOperationException("Boundary payout outside owned profiles.");
                        var profile=before.Profiles.Single(x=>x.Id==submitted.PayrollProfileId);var row=addedPayouts.Single();
                        var gross=profile.PaymentModel=="Monthly"?profile.MonthlyAmount:submitted.GrossAmount;
                        if(row.Id!=id||row.AcademyId!=academy||row.PayrollProfileId!=profile.Id||row.TeacherIdSnapshot!=teacher||row.WorkerNameSnapshot!=profile.WorkerName||row.WorkerTypeSnapshot!="Teacher"||row.StaffUserIdSnapshot is not null||row.IdentityRecordedAtUtc is null||row.PeriodLabel!=submitted.PeriodLabel||row.GrossAmount!=gross||row.Deductions!=submitted.Deductions||row.NetAmount!=0.01m||row.GrossAmount-row.Deductions!=row.NetAmount||row.SessionsCovered!=(profile.PaymentModel=="Monthly"?null:4)||row.PaymentMethod!="BankTransfer"||row.Reference is not null||row.Currency!="INR"||row.Status!="Paid"||addedProfiles.Length!=0||wire.RootElement.GetProperty("grossAmountExact").GetString()!=gross!.Value.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)||wire.RootElement.GetProperty("deductionsExact").GetString()!=submitted.Deductions.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)||wire.RootElement.GetProperty("netAmountExact").GetString()!="0.01")throw new InvalidOperationException("Boundary payout SQL/wire/one-cent net differs.");
                    }
                    Exact(before with{Profiles=before.Profiles.Concat(addedProfiles).OrderBy(x=>x.Id.ToString("N")).ToArray(),Payouts=before.Payouts.Concat(addedPayouts).OrderBy(x=>x.Id.ToString("N")).ToArray(),Audits=before.Audits.Concat(addedAudits).OrderBy(x=>x.Id.ToString("N")).ToArray()},after);writes++;
                }
                else
                {
                    Exact(before,after);reads++;
                    if(response.StatusCode==HttpStatusCode.Forbidden)denied++;
                    if(route=="/api/teacher/payments")
                    {
                        RequireFinanceStatus(response,HttpStatusCode.OK,"Teacher linked payments");
                        using var json=JsonDocument.Parse(bytes);var rows=json.RootElement.GetProperty("payslips").EnumerateArray().ToArray();
                        var expected=before.Payouts.Where(x=>x.AcademyId==academy&&x.TeacherIdSnapshot==teacher&&x.WorkerTypeSnapshot=="Teacher"&&x.WorkerNameSnapshot is not null&&x.IdentityRecordedAtUtc is not null);
                        if(int.TryParse(context.Request.Query["year"],out var year)&&int.TryParse(context.Request.Query["month"],out var month))expected=expected.Where(x=>(x.Status=="NoCashDue"?x.CreatedAtUtc:x.PaidAtUtc)?.Year==year&&(x.Status=="NoCashDue"?x.CreatedAtUtc:x.PaidAtUtc)?.Month==month);
                        expected=expected.OrderByDescending(x=>x.CreatedAtUtc).Take(context.Request.Query.ContainsKey("year")?100:12);
                        var expectedIds=expected.Select(x=>x.Id).Order().ToArray();var actualIds=rows.Select(x=>x.GetProperty("id").GetGuid()).Order().ToArray();
                        if(!expectedIds.SequenceEqual(actualIds))throw new InvalidOperationException($"Teacher response disclosed/omitted protected history: query={context.Request.QueryString};expected={string.Join(',',expectedIds)};actual={string.Join(',',actualIds)}.");
                        foreach(var item in rows)
                        {
                            var saved=before.Payouts.Single(x=>x.Id==item.GetProperty("id").GetGuid());
                            if(item.GetProperty("workerName").GetString()!=saved.WorkerNameSnapshot||item.GetProperty("grossAmountExact").GetString()!=saved.GrossAmount.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)||item.GetProperty("deductionsExact").GetString()!=saved.Deductions.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)||item.GetProperty("netAmountExact").GetString()!=saved.NetAmount.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)||item.GetProperty("sessionsCovered").ToString()!=(saved.SessionsCovered?.ToString()??"")||!item.GetProperty("identityRecordedAtUtc").GetString()!.EndsWith("Z"))throw new InvalidOperationException("Teacher response differs from exact historical SQL fields.");
                        }
                        teacherReads++;
                    }
                }
                context.Response.StatusCode=(int)response.StatusCode;
                foreach(var header in response.Headers.Concat(response.Content.Headers))if(!header.Key.Equals("Transfer-Encoding",StringComparison.OrdinalIgnoreCase)&&!header.Key.Equals("Connection",StringComparison.OrdinalIgnoreCase))context.Response.Headers[header.Key]=header.Value.ToArray();
                await context.Response.Body.WriteAsync(bytes,context.RequestAborted);
            }
            catch(Exception error){failures.Enqueue(error.Message);throw;}
            finally{pacing.Release();}
        });
        await bridge.StartAsync();var start=new ProcessStartInfo("node"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        foreach(var argument in new[]{Path.GetFullPath("QA/tools/payroll-document-live.cjs"),bridge.Urls.Single(),academy.ToString(),teacher.ToString()})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)??throw new InvalidOperationException("Payroll document browser failed to start.");var output=process.StandardOutput.ReadToEndAsync();var errors=process.StandardError.ReadToEndAsync();
        try{using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(8));await process.WaitForExitAsync(timeout.Token);}catch{if(!process.HasExited)process.Kill(entireProcessTree:true);throw;}finally{await bridge.StopAsync();}
        Console.Write(await output);var errorText=await errors;
        if(process.ExitCode!=0||!failures.IsEmpty||writes!=16||profiles.Count!=8||teacherReads<12||denied!=4)throw new InvalidOperationException($"Payroll document linked failed writes={writes}/profiles={profiles.Count}/Teacher={teacherReads}/denied={denied}: "+errorText+string.Join("; ",failures));
        var final=await State();if(final.Payouts.Count(x=>profiles.Contains(x.PayrollProfileId))!=8)throw new InvalidOperationException("Boundary payout count differs.");
        Exact(initial.Profiles,final.Profiles.Where(x=>!profiles.Contains(x.Id)).ToArray());Exact(initial.Payouts,final.Payouts.Where(x=>!profiles.Contains(x.PayrollProfileId)).ToArray());
        Console.WriteLine($"PAYROLL DOCUMENT LINKED PASS:8 minimum/maximum browser journeys/16 exact writes/8 profiles/8 one-cent payouts/exact actor audits;4 Teacher journeys/at least12 verified own-history reads/four finance403;{reads} full five-table unchanged read/login/denial snapshots/no429. Physical devices NOT RUN.");
    }
}
