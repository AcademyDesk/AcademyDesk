using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static partial class SqlHarnessEntryPoint
{
    // Real loopback API for one authenticated certificate issue. The browser signs in itself.
    private static async Task ServeBrowserCertificateAsync(QaRunManifest manifest)
    {
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BROWSER_API_PORT"), out var port) || port is < 1024 or > 65535 ||
            !Uri.TryCreate(Environment.GetEnvironmentVariable("QA_BROWSER_ORIGIN"), UriKind.Absolute, out var origin) ||
            origin.Scheme != "http" || origin.Host != "127.0.0.1" || origin.Port is < 1024 or > 65535 || origin.Port == port ||
            origin.AbsolutePath != "/" || origin.Query != "" || origin.Fragment != "" || origin.UserInfo != "")
            throw new InvalidOperationException("Exact distinct loopback browser API port and origin required.");
        try
        {
            using var factory = new QaApiFactory(manifest, new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = origin.GetLeftPart(UriPartial.Authority) });
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
            if (!factory.PreflightPassed || (await client.GetAsync("/health")).StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Backend preflight failed.");
            await VerifyTenantIsolationAsync(factory, client);
            client.DefaultRequestHeaders.Authorization = null;
            Guid academyId, studentId, batchId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
                var academy = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
                academy.CertificateSignatoryName = "Portal Signatory";
                academy.CertificateAccentColor = "#0F6CBD";
                var course = await db.Courses.FirstAsync(x => x.AcademyId == academy.Id);
                var teacher = await db.Teachers.FirstAsync(x => x.AcademyId == academy.Id);
                var student = new Student { AcademyId = academy.Id, FirstName = "Portal", LastName = "Learner" };
                var batch = new Batch { AcademyId = academy.Id, CourseId = course.Id, TeacherId = teacher.Id, Name = "Portal programme" };
                db.AddRange(student, batch);
                db.Add(new Enrollment { AcademyId = academy.Id, StudentId = student.Id, BatchId = batch.Id, Status = "Active", StartDate = new DateOnly(2030, 1, 1) });
                await db.SaveChangesAsync();
                academyId = academy.Id; studentId = student.Id; batchId = batch.Id;
            }
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
            await using var bridge = builder.Build();
            bridge.Run(async context =>
            {
                if (context.Connection.RemoteIpAddress is null || !IPAddress.IsLoopback(context.Connection.RemoteIpAddress) || context.Request.Host.Host != "127.0.0.1" || context.Request.Host.Port != port)
                {
                    context.Response.StatusCode = 400;
                    return;
                }
                using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
                if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")) request.Content = new StreamContent(context.Request.Body);
                foreach (var header in context.Request.Headers)
                {
                    if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                context.Response.StatusCode = (int)response.StatusCode;
                foreach (var header in response.Headers.Concat(response.Content.Headers))
                {
                    if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase)) continue;
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
                Console.WriteLine($"CERTPORTAL HTTP {context.Request.Method} {context.Request.Path} {(int)response.StatusCode}");
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
            await bridge.StartAsync();
            var stop = Path.Combine(manifest.Root, "stop-browser");
            Console.WriteLine($"CERTPORTAL READY academy={academyId:N} student={studentId:N} batch={batchId:N} api={port} origin={origin.Port} stop={stop}");
            var deadline = DateTime.UtcNow.AddMinutes(30);
            while (!File.Exists(stop) && DateTime.UtcNow < deadline) await Task.Delay(1000);
            await bridge.StopAsync();
            using var finalScope = factory.Services.CreateScope();
            var finalDb = finalScope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var issued = await finalDb.Certificates.AsNoTracking().Where(x => x.AcademyId == academyId && x.StudentId == studentId).ToListAsync();
            var audits = await finalDb.AuditLogs.AsNoTracking().Where(x => x.AcademyId == academyId && x.EntityType == "Certificate").OrderBy(x => x.OccurredAtUtc).ToListAsync();
            Console.WriteLine("CERTPORTAL SQL " + JsonSerializer.Serialize(new
            {
                count = issued.Count,
                certificates = issued.Select(x => new { x.Id, x.CertificateNumber, x.VerificationCode, x.Title, x.Status, x.StudentId, x.BatchId, x.IssuedDate, x.Notes, x.TemplateKey }),
                audits = audits.Select(x => new { x.Action, x.EntityId, x.ActorUserId })
            }));
        }
        catch (Exception exception)
        {
            Console.WriteLine("CERTPORTAL FAIL " + exception.ToString().ReplaceLineEndings(" | "));
            throw;
        }
        finally
        {
            if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
    }
}
