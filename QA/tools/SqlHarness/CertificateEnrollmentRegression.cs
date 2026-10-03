using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyCertificateEnrollmentAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, actor; Student student, other, inactive, foreignStudent; Batch batch, foreignBatch;
        var examples = new List<(string Label, Student Student, Batch Batch, bool Allowed)>();
        using (var scope = factory.Services.CreateScope()) {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = new() { AcademyId = academy, FirstName = "Certificate", LastName = "Learner" };
            other = new() { AcademyId = academy, FirstName = "Certificate", LastName = "Other" };
            inactive = new() { AcademyId = academy, FirstName = "Certificate", LastName = "Inactive", IsActive = false };
            foreignStudent = new() { AcademyId = foreign, FirstName = "Certificate", LastName = "Foreign" };
            var course = await db.Courses.FirstAsync(x => x.AcademyId == academy);
            var teacher = await db.Teachers.FirstAsync(x => x.AcademyId == academy);
            batch = new() { AcademyId = academy, CourseId = course.Id, TeacherId = teacher.Id, Name = "Certificate unrelated" };
            foreignBatch = new() { AcademyId = foreign, CourseId = course.Id, TeacherId = teacher.Id, Name = "Certificate foreign" };
            db.AddRange(student, other, inactive, foreignStudent, batch, foreignBatch);
            foreach (var status in new[] { "Active", "Completed", "active", "aCtIvE", "completed", "CoMpLeTeD", "Waitlisted", "Paused", "Withdrawn", "Cancelled", "Transferred", "Unknown", "" }) {
                var current = new Batch { AcademyId = academy, CourseId = course.Id, TeacherId = teacher.Id, Name = "Certificate status " + examples.Count + " " + status };
                db.AddRange(current, new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = current.Id, Status = status, StartDate = new(2030, 1, 1), EndDate = new(2030, 2, 1) });
                examples.Add(("status-" + (status == "" ? "empty" : status), student, current, new[] { "active", "completed" }.Contains(status.ToLowerInvariant())));
            }
            foreach (var isInactiveStudent in new[] { false, true }) foreach (var status in new[] { "Active", "Completed" }) {
                var current = new Batch { AcademyId = academy, CourseId = course.Id, TeacherId = teacher.Id, Name = "Certificate inactive " + isInactiveStudent + status, IsActive = false };
                var learner = isInactiveStudent ? inactive : student;
                db.AddRange(current, new Enrollment { AcademyId = academy, StudentId = learner.Id, BatchId = current.Id, Status = status, StartDate = new(1999, 1, 1), EndDate = new(1999, 2, 1) });
                examples.Add(("inactive-batch-" + isInactiveStudent + "-" + status, learner, current, true));
            }
            // Non-composite relation IDs deliberately probe the enrollment AcademyId predicate.
            db.Add(new Enrollment { AcademyId = foreign, StudentId = student.Id, BatchId = batch.Id, Status = "Active" });
            db.Add(new Enrollment { AcademyId = academy, StudentId = other.Id, BatchId = batch.Id, Status = "Active" });
            foreach (var reverse in new[] { false, true }) {
                var current = new Batch { AcademyId = academy, CourseId = course.Id, TeacherId = teacher.Id, Name = "Certificate mixed " + reverse };
                db.Add(current);
                foreach (var status in reverse ? new[] { "Completed", "Withdrawn" } : new[] { "Withdrawn", "Completed" }) db.Add(new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = current.Id, Status = status });
                examples.Add(("mixed-rows-" + reverse, student, current, true));
            }
            db.AddRange(new Certificate { AcademyId = academy, StudentId = student.Id, BatchId = batch.Id, CertificateNumber = "QA-PRIOR", VerificationCode = "QA-PRIOR", Title = "Prior preserved", Status = "Revoked", Notes = "Prior mismatched legacy record", IssuedDate = new(1998, 1, 1) },
                new Certificate { AcademyId = foreign, StudentId = foreignStudent.Id, BatchId = foreignBatch.Id, CertificateNumber = "QA-FOREIGN", VerificationCode = "QA-FOREIGN", Title = "Foreign preserved" });
            await db.SaveChangesAsync();
            actor = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync("qa-admin-a@example.invalid"))!.Id;
        }
        await CreateAccessActorAsync(factory, "qa-certificate-manager@example.invalid", "Manager", academy);
        await CreateAccessActorAsync(factory, "qa-certificate-student@example.invalid", "Student", academy, student.Id);
        await CreateAccessActorAsync(factory, "qa-certificate-guardian@example.invalid", "Guardian", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacherToken = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var manager = await LoginAsync(client, "qa-certificate-manager@example.invalid", "Synthetic!39Ab");
        var studentToken = await LoginAsync(client, "qa-certificate-student@example.invalid", "Synthetic!39Ab");
        var guardianToken = await LoginAsync(client, "qa-certificate-guardian@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var route = $"/api/academies/{academy}/certificates"; var count = 0; var writes = 0;
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        void Pass(string label) { count++; Console.WriteLine("CERTIFICATE CASE " + label + " PASS."); }
        async Task<JsonElement> Snapshot() {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.SerializeToElement(new { certificates = await db.Certificates.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), batches = await db.Batches.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), courses = await db.Courses.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync() });
        }
        static string Digest(JsonElement value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.GetRawText())));
        async Task<(string Body, JsonElement Before, JsonElement After, DateTime Start, DateTime End, string? Location)> Send(string label, HttpMethod method, string path, object? body, HttpStatusCode expected, string? auth, string? raw = null) {
            // Preserve the application's real limiter; avoid number collisions in successful same-second writes.
            await Task.Delay(expected == HttpStatusCode.Created ? 1100 : 650);
            var before = await Snapshot(); var start = DateTime.UtcNow;
            using var request = new HttpRequestMessage(method, path);
            if (auth is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth);
            if (raw is not null) request.Content = new StringContent(raw, System.Text.Encoding.UTF8, "application/json"); else if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await client.SendAsync(request); var end = DateTime.UtcNow; var text = await response.Content.ReadAsStringAsync(); var after = await Snapshot();
            Console.WriteLine("CERTIFICATE EVIDENCE " + JsonSerializer.Serialize(new { label, method = method.Method, path, request = raw ?? (body is null ? null : JsonSerializer.Serialize(body, json)), status = (int)response.StatusCode, response = (int)response.StatusCode < 500 ? text : "Server failure body omitted", beforeDigest = Digest(before), afterDigest = Digest(after), location = response.Headers.Location?.ToString() }));
            RequireFinanceStatus(response, expected, label);
            if (method == HttpMethod.Get || (int)expected >= 400) if (!JsonElement.DeepEquals(before, after)) throw new InvalidOperationException("Read/denial changed SQL/Identity/audits: " + label);
            return (text, before, after, start, end, response.Headers.Location?.ToString());
        }
        Dictionary<string, object?> Body(Guid learner, Guid? selectedBatch) => new() { ["studentId"] = learner, ["batchId"] = selectedBatch, ["title"] = "  Synthetic achievement  ", ["templateKey"] = null, ["designKey"] = null, ["artworkX"] = null, ["artworkY"] = null, ["artworkSize"] = null, ["issuedDate"] = null, ["notes"] = null };
        static CertificateSummary Summary(Certificate x) => new(x.Id, x.CertificateNumber, x.StudentId, x.BatchId, x.Title, x.TemplateKey, x.DesignKey, x.ArtworkX, x.ArtworkY, x.ArtworkSize, x.VerificationCode, x.IssuedDate, x.Status, x.Notes);
        async Task Issue(string label, Dictionary<string, object?> body) {
            var spoof = Guid.NewGuid(); body["id"] = spoof; body["academyId"] = foreign; body["status"] = "Revoked"; body["createdAtUtc"] = "2001-01-01T00:00:00Z"; body["verificationCode"] = "FORGED"; body["certificateNumber"] = "FORGED";
            var result = await Send(label, HttpMethod.Post, route, body, HttpStatusCode.Created, admin);
            var returned = JsonSerializer.Deserialize<CertificateSummary>(result.Body, json)!;
            using var scope = factory.Services.CreateScope(); var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Certificates.AsNoTracking().SingleAsync(x => x.Id == returned.Id);
            if (returned != Summary(stored) || result.Location != route + "/" + stored.Id) throw new InvalidOperationException("Complete summary/Location/fresh SQL mismatch");
            var notes = (string?)body.GetValueOrDefault("notes"); notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 2000)];
            var expected = new Certificate { Id = stored.Id, AcademyId = academy, StudentId = (Guid)body["studentId"]!, BatchId = (Guid?)body.GetValueOrDefault("batchId"), Title = ((string)body["title"]!).Trim(), CertificateNumber = stored.CertificateNumber, VerificationCode = stored.VerificationCode, IssuedDate = (DateOnly?)body.GetValueOrDefault("issuedDate") ?? DateOnly.FromDateTime(result.Start), TemplateKey = ((string?)body.GetValueOrDefault("templateKey"))?.ToLowerInvariant() ?? "music-recital", Notes = notes, CreatedAtUtc = stored.CreatedAtUtc };
            foreach (var property in typeof(Certificate).GetProperties()) if (!Equals(property.GetValue(stored), property.GetValue(expected))) throw new InvalidOperationException("Stored certificate differs: " + property.Name);
            if (stored.Id == spoof || stored.CreatedAtUtc < result.Start || stored.CreatedAtUtc > result.End || !System.Text.RegularExpressions.Regex.IsMatch(stored.CertificateNumber, @"^CERT-\d{14}-\d{3}$") || !System.Text.RegularExpressions.Regex.IsMatch(stored.VerificationCode, "^[0-9A-F]{16}$")) throw new InvalidOperationException("Server-generated values differ");
            foreach (var property in result.Before.EnumerateObject()) {
                var after = result.After.GetProperty(property.Name);
                if (property.Name is not ("certificates" or "audits")) { if (!JsonElement.DeepEquals(property.Value, after)) throw new InvalidOperationException("Source changed: " + property.Name); continue; }
                var old = property.Value.EnumerateArray().ToArray(); var current = after.EnumerateArray().ToArray();
                if (current.Length != old.Length + (property.Name == "certificates" ? 1 : 2) || old.Any(x => !current.Any(y => JsonElement.DeepEquals(x, y)))) throw new InvalidOperationException("Prior/foreign rows or counts changed: " + property.Name);
            }
            var previous = result.Before.GetProperty("audits").EnumerateArray().Select(x => x.GetProperty("Id").GetGuid()).ToHashSet();
            var audits = result.After.GetProperty("audits").EnumerateArray().Where(x => !previous.Contains(x.GetProperty("Id").GetGuid())).Select(x => x.Deserialize<AuditLog>()!).ToArray();
            var domain = audits.Single(x => x.Action == "CertificateIssued"); var action = audits.Single(x => x.Action == "POST Certificates");
            if (domain.AcademyId != academy || domain.EntityType != "Certificate" || domain.EntityId != stored.Id || domain.ActorUserId is not null || action.AcademyId != academy || action.ActorUserId != actor || action.EntityType != "Certificates" || action.EntityId is not null || JsonDocument.Parse(action.MetadataJson!).RootElement.GetProperty("Route").GetString() != route || audits.Any(x => x.CreatedAtUtc < result.Start || x.CreatedAtUtc > result.End || x.OccurredAtUtc < result.Start || x.OccurredAtUtc > result.End)) throw new InvalidOperationException("Domain/actor/route audit differs");
            writes++; Pass(label);
        }
        foreach (var example in examples) {
            var body = Body(example.Student.Id, example.Batch.Id);
            if (example.Allowed) await Issue(example.Label, body);
            else { var result = await Send(example.Label, HttpMethod.Post, route, body, HttpStatusCode.BadRequest, admin); if (JsonDocument.Parse(result.Body).RootElement.GetProperty("message").GetString() != "Select a class or batch where this student has an Active or Completed enrollment.") throw new InvalidOperationException("Eligibility feedback differs"); Pass(example.Label); }
        }
        foreach (var example in new[] { ("unrelated-local-and-foreign-enrollment", student.Id, (Guid?)batch.Id), ("other-learner-no-matching-class", other.Id, (Guid?)examples[0].Batch.Id), ("missing-student", Guid.NewGuid(), (Guid?)batch.Id), ("foreign-student", foreignStudent.Id, (Guid?)batch.Id), ("missing-batch", student.Id, (Guid?)Guid.NewGuid()), ("foreign-batch", student.Id, (Guid?)foreignBatch.Id), ("empty-student", Guid.Empty, (Guid?)null), ("empty-batch", student.Id, (Guid?)Guid.Empty) }) {
            await Send(example.Item1, HttpMethod.Post, route, Body(example.Item2, example.Item3), HttpStatusCode.BadRequest, admin); Pass(example.Item1);
        }
        foreach (var optional in new[] { "null", "omitted", "blank", "filled", "long" }) {
            var body = Body(inactive.Id, null);
            if (optional == "omitted") foreach (var key in new[] { "batchId", "templateKey", "designKey", "artworkX", "artworkY", "artworkSize", "issuedDate", "notes" }) body.Remove(key);
            if (optional == "blank") body["notes"] = "  ";
            if (optional is "filled" or "long") { body["notes"] = optional == "long" ? new string('x', 2001) : "  Preserved note  "; body["issuedDate"] = new DateOnly(2020, 2, 29); body["templateKey"] = "SCHOOL-MERIT"; body["designKey"] = "NONE"; body["artworkX"] = 100; body["artworkY"] = 0; body["artworkSize"] = 180; }
            await Issue("optional-independent-" + optional, body);
        }
        foreach (var invalid in new[] { ("title", (object?)" "), ("templateKey", (object?)"unknown"), ("designKey", (object?)"portrait"), ("artworkX", (object?)-1), ("artworkY", (object?)101), ("artworkSize", (object?)35) }) {
            var body = Body(student.Id, null); body[invalid.Item1] = invalid.Item2; await Send("invalid-" + invalid.Item1, HttpMethod.Post, route, body, HttpStatusCode.BadRequest, admin); Pass("invalid-" + invalid.Item1);
        }
        foreach (var raw in new[] { "null", "{", "{}", "{\"studentId\":\"not-a-guid\",\"title\":\"Synthetic\"}", $"{{\"studentId\":\"{student.Id}\",\"title\":\"Synthetic\",\"issuedDate\":\"2026-02-30\"}}" }) {
            await Send("binding-" + count, HttpMethod.Post, route, null, HttpStatusCode.BadRequest, admin, raw); Pass("binding-" + count);
        }
        foreach (var denied in new[] { ("anonymous", (string?)null, HttpStatusCode.Unauthorized), ("foreign-admin", (string?)foreignAdmin, HttpStatusCode.Forbidden), ("teacher", (string?)teacherToken, HttpStatusCode.Forbidden), ("manager", (string?)manager, HttpStatusCode.Forbidden), ("student", (string?)studentToken, HttpStatusCode.Forbidden), ("guardian", (string?)guardianToken, HttpStatusCode.Forbidden) }) foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post }) {
            var label = "access-" + denied.Item1 + "-" + method; await Send(label, method, route, method == HttpMethod.Post ? Body(student.Id, null) : null, denied.Item3, denied.Item2); Pass(label);
        }
        foreach (var mode in new[] { "module-disabled", "academy-inactive" }) {
            using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = mode == "module-disabled" ? "[\"Core\"]" : "[\"Core\",\"Certificates\"]"; row.IsActive = mode != "academy-inactive"; await db.SaveChangesAsync(); }
            foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post }) { var label = mode + "-" + method; await Send(label, method, route, method == HttpMethod.Post ? Body(student.Id, null) : null, HttpStatusCode.Forbidden, admin); Pass(label); }
        }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var row = await db.Academies.SingleAsync(x => x.Id == academy); row.EnabledModulesJson = "[\"Core\",\"Certificates\"]"; row.IsActive = true; await db.SaveChangesAsync(); }
        foreach (var own in new[] { true, false }) {
            var id = own ? academy : foreign; var label = own ? "complete-scoped-register-readback" : "foreign-scoped-register-readback";
            var result = await Send(label, HttpMethod.Get, $"/api/academies/{id}/certificates", null, HttpStatusCode.OK, own ? admin : foreignAdmin);
            var returned = JsonSerializer.Deserialize<List<CertificateSummary>>(result.Body, json)!;
            using var scope = factory.Services.CreateScope(); var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Certificates.AsNoTracking().Where(x => x.AcademyId == id).ToListAsync();
            if (returned.Count != stored.Count || returned.Count != (own ? writes + 1 : 1) || !returned.Select(x => x.IssuedDate).SequenceEqual(returned.Select(x => x.IssuedDate).OrderDescending()) || returned.Any(x => x != Summary(stored.Single(y => y.Id == x.Id)))) throw new InvalidOperationException("Scoped full register/order differs"); Pass(label);
        }
        var picker = await Send("scoped-enrollment-picker-input", HttpMethod.Get, $"/api/academies/{academy}/enrollments", null, HttpStatusCode.OK, admin);
        var enrollments = JsonSerializer.Deserialize<List<EnrollmentSummary>>(picker.Body, json)!;
        using (var scope = factory.Services.CreateScope()) { var stored = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Enrollments.AsNoTracking().Where(x => x.AcademyId == academy).ToListAsync(); if (stored.Count != enrollments.Count || enrollments.Any(x => x != stored.Where(y => y.Id == x.Id).Select(y => new EnrollmentSummary(y.Id, y.StudentId, y.BatchId, y.StartDate, y.EndDate, y.Status)).Single())) throw new InvalidOperationException("Enrollment picker inputs differ"); }
        Pass("scoped-enrollment-picker-input");
        Console.WriteLine($"CERTIFICATE REGRESSION PASS: {count} cases; {writes} exact committed issuances; {count - writes} no-write reads/denials; fresh SQL field/source/Identity/prior/foreign preservation and domain+actor audits. Browser/races/faults/legacy remediation/preview-print remain OPEN.");
    }
}
