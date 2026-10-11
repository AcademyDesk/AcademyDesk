using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyStudentImportsAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest, bool baseline)
    {
        Guid academy, foreign, student;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            (await db.Students.SingleAsync(x => x.Id == student)).Email = " Existing@Example.Invalid ";
            (await db.Students.SingleAsync(x => x.AcademyId == foreign)).Email = "foreign-shared@example.invalid";
            await db.SaveChangesAsync();
        }
        await CreateAccessActorAsync(factory, "qa-import-student@example.invalid", "Student", academy, student);
        await CreateAccessActorAsync(factory, "qa-import-finance@example.invalid", "FinanceUser", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var foreignAdmin = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
        var portalStudent = await LoginAsync(client, "qa-import-student@example.invalid", "Synthetic!39Ab");
        var finance = await LoginAsync(client, "qa-import-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}/imports/students"; var count = 0;
        StudentImportRow Row(string? email = null) => new(" Import ", " Student ", null, email, null, null);
        async Task<string> Domain()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { Students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                Users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.StudentId, x.IsActive }).ToArrayAsync(),
                Roles = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToArrayAsync() });
        }
        async Task<string> Audit()
        {
            using var scope = factory.Services.CreateScope(); return JsonSerializer.Serialize(await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync());
        }
        async Task<HttpResponseMessage> Send(string path, StudentImportRequest request, string? token, bool paced = true)
        {
            if (paced) await Task.Delay(700); using var message = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(request) };
            if (token is not null) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(message);
        }
        void Pass(string label) { count++; Console.WriteLine("PASS: IMPORT " + label); }
        async Task CheckValidation(StudentImportRequest request, int[] expectedRows)
        {
            var before = await Domain(); using var response = await Send(root + "/validate", request, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "import validation");
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); var actual = json.RootElement.GetProperty("errors").EnumerateArray().Select(x => x.GetProperty("row").GetInt32()).ToArray();
            if (!actual.SequenceEqual(expectedRows) || json.RootElement.GetProperty("rowCount").GetInt32() != (request.Rows?.Count ?? 0) || before != await Domain()) throw new InvalidOperationException("Import row validation/persistence mismatch.");
            Pass("file validation exact rows/domain unchanged");
        }
        async Task Rejected(string path, StudentImportRequest request, string? token, HttpStatusCode status, string label)
        {
            var before = await Domain(); var audit = await Audit(); using var response = await Send(path, request, token); RequireFinanceStatus(response, status, label);
            if (before != await Domain() || audit != await Audit()) throw new InvalidOperationException("Rejected import changed domain/audit: " + label); Pass(label + " exact status/no-write");
        }
        async Task Created(StudentImportRequest request)
        {
            using var before = JsonDocument.Parse(await Domain()); using var oldAudit = JsonDocument.Parse(await Audit());
            using var response = await Send(root, request, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "valid import");
            using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync()); using var after = JsonDocument.Parse(await Domain()); using var audits = JsonDocument.Parse(await Audit());
            var oldRows = before.RootElement.GetProperty("Students").EnumerateArray().ToArray(); var rows = after.RootElement.GetProperty("Students").EnumerateArray().ToArray();
            var added = rows.Where(x => !oldRows.Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid())).ToArray();
            if (added.Length != request.Rows!.Count || result.RootElement.GetProperty("imported").GetInt32() != added.Length) throw new InvalidOperationException("Import count mismatch.");
            foreach (var old in oldRows) if (rows.Single(x => x.GetProperty("Id").GetGuid() == old.GetProperty("Id").GetGuid()).GetRawText() != old.GetRawText()) throw new InvalidOperationException("Import changed old/foreign student.");
            foreach (var field in new[] { "Invoices", "Payments", "Users", "Roles" }) if (before.RootElement.GetProperty(field).GetRawText() != after.RootElement.GetProperty(field).GetRawText()) throw new InvalidOperationException("Import changed unrelated " + field);
            string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            foreach (var input in request.Rows)
            {
                var row = added.FirstOrDefault(x => x.GetProperty("Email").GetString() == Clean(input.Email) && x.GetProperty("StudentNumber").GetString() == Clean(input.StudentNumber));
                if (row.ValueKind == JsonValueKind.Undefined || row.GetProperty("AcademyId").GetGuid() != academy || row.GetProperty("FirstName").GetString() != input.FirstName.Trim() || row.GetProperty("LastName").GetString() != input.LastName.Trim() || row.GetProperty("Phone").GetString() != Clean(input.Phone) ||
                    row.GetProperty("AdmissionDate").GetString() != (input.AdmissionDate ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToString("yyyy-MM-dd")) throw new InvalidOperationException("Import fields/date/optional null mismatch.");
            }
            var additions = audits.RootElement.EnumerateArray().Where(x => !oldAudit.RootElement.EnumerateArray().Any(old => old.GetProperty("Id").GetGuid() == x.GetProperty("Id").GetGuid())).ToArray();
            if (additions.Length != 1 || additions[0].GetProperty("AcademyId").GetGuid() != academy || additions[0].GetProperty("ActorUserId").ValueKind == JsonValueKind.Null || additions[0].GetProperty("Action").GetString() != "POST StudentImports") throw new InvalidOperationException("Import audit mismatch.");
            Pass("valid batch exact SQL fields/defaults/optional values/one audit");
        }
        var duplicate = new StudentImportRequest([Row(" DUPLICATE@example.invalid "), Row("unique@example.invalid"), Row("duplicate@EXAMPLE.invalid")]);
        if (baseline)
        {
            await CheckValidation(duplicate, []);
            using var response = await Send(root, duplicate, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "baseline duplicate import accepted");
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.Students.CountAsync(x => x.AcademyId == academy && x.Email != null && x.Email.Trim().ToLower() == "duplicate@example.invalid") != 2 || await db.Students.CountAsync(x => x.AcademyId == academy) != 4) throw new InvalidOperationException("Baseline duplicate SQL reproduction not observed.");
            Console.WriteLine("PASS: IMPORT BASELINE observation: validation200/no errors, import200, two normalized duplicate emails persisted; isolated SQL only."); return;
        }
        foreach (var pair in new[] { ("same@example.invalid", "same@example.invalid"), ("SAME@example.invalid", "same@example.invalid"), (" same@example.invalid ", "same@example.invalid"), (" Same@Example.Invalid ", "SAME@example.invalid") })
        {
            var request = new StudentImportRequest([Row(pair.Item1), Row("unique@example.invalid"), Row(pair.Item2)]); await CheckValidation(request, [1, 3]);
            await Rejected(root, request, admin, HttpStatusCode.BadRequest, "in-file normalized duplicate");
        }
        foreach (var blank in new string?[] { null, "", " " }) await Created(new([Row(blank) with { StudentNumber = blank, Phone = blank }, Row(blank)]));
        await Created(new([Row(" Valid@Example.Invalid ") with { StudentNumber = " IMPORT-001 ", Phone = " 123 ", AdmissionDate = new DateOnly(2024, 2, 29) }, Row("second@example.invalid")]));
        foreach (var email in new[] { "existing@example.invalid", " EXISTING@example.invalid " }) await Rejected(root, new([Row("new@example.invalid"), Row(email)]), admin, HttpStatusCode.Conflict, "existing normalized local email");
        await Created(new([Row("foreign-shared@example.invalid")]));
        var invalid = new[] { new StudentImportRequest(null), new StudentImportRequest([]), new StudentImportRequest([null!]), new StudentImportRequest([Row() with { FirstName = " " }]),
            new StudentImportRequest([Row("broken")]), new StudentImportRequest([Row() with { FirstName = new('x',121) }]), new StudentImportRequest([Row() with { LastName = new('x',121) }]),
            new StudentImportRequest([Row(new string('x',320) + "@")]), new StudentImportRequest([Row() with { StudentNumber = new('x',51) }]), new StudentImportRequest([Row() with { Phone = new('x',31) }]) };
        foreach (var request in invalid) { await CheckValidation(request, [request.Rows?.Count > 0 ? 1 : 0]); await Rejected(root, request, admin, HttpStatusCode.BadRequest, "invalid row before write"); }
        var valid = new StudentImportRequest([Row("authorized@example.invalid")]);
        foreach (var actor in new (string? Token, HttpStatusCode Status)[] { (null, HttpStatusCode.Unauthorized), (teacher, HttpStatusCode.Forbidden), (portalStudent, HttpStatusCode.Forbidden), (finance, HttpStatusCode.Forbidden), (foreignAdmin, HttpStatusCode.Forbidden) })
            foreach (var suffix in new[] { "", "/validate" }) await Rejected(root + suffix, valid, actor.Token, actor.Status, "import role/tenant denial");
        foreach (var suffix in new[] { "", "/validate" }) await Rejected($"/api/academies/{foreign}/imports/students" + suffix, valid, admin, HttpStatusCode.Forbidden, "foreign import route");
        await Rejected(root, new([Row("number-a@example.invalid") with { StudentNumber = "DUP-NUM" }, Row("number-b@example.invalid") with { StudentNumber = "DUP-NUM" }]), admin, HttpStatusCode.Conflict, "unique number constraint all-or-nothing");
        await SetAuditInsertDeniedAsync(manifest, true);
        try { await Rejected(root, new([Row("audit@example.invalid"), Row("audit-second@example.invalid")]), admin, HttpStatusCode.InternalServerError, "import audit rollback"); }
        finally { await SetAuditInsertDeniedAsync(manifest, false); }
        await Created(new([Row("recovered@example.invalid")]));
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var email = $"race-{attempt}@example.invalid"; var before = await Domain(); using var oldAudit = JsonDocument.Parse(await Audit());
            var responses = await Task.WhenAll(Send(root, new([Row(email)]), admin, false), Send(root, new([Row(" " + email.ToUpperInvariant() + " ")]), admin, false));
            try
            {
                if (!responses.Select(x => (int)x.StatusCode).Order().SequenceEqual(new[] { 200, 409 })) throw new InvalidOperationException("Concurrent imports must return exactly200/409, not429 or two successes.");
                using var prior = JsonDocument.Parse(before); using var after = JsonDocument.Parse(await Domain()); using var audits = JsonDocument.Parse(await Audit());
                var oldRows = prior.RootElement.GetProperty("Students").EnumerateArray().ToArray(); var rows = after.RootElement.GetProperty("Students").EnumerateArray().ToArray();
                if (rows.Length != oldRows.Length + 1 || rows.Count(x => x.GetProperty("AcademyId").GetGuid() == academy && string.Equals(x.GetProperty("Email").GetString()?.Trim(), email, StringComparison.OrdinalIgnoreCase)) != 1 || audits.RootElement.GetArrayLength() != oldAudit.RootElement.GetArrayLength() + 1) throw new InvalidOperationException("Concurrent import count/audit mismatch.");
                foreach (var old in oldRows) if (rows.Single(x => x.GetProperty("Id").GetGuid() == old.GetProperty("Id").GetGuid()).GetRawText() != old.GetRawText()) throw new InvalidOperationException("Concurrent import changed sentinel.");
                foreach (var field in new[] { "Invoices", "Payments", "Users", "Roles" }) if (prior.RootElement.GetProperty(field).GetRawText() != after.RootElement.GetProperty(field).GetRawText()) throw new InvalidOperationException("Concurrent import changed unrelated domain.");
                Pass("concurrent duplicate pair exactly200/409, one student and one audit");
            }
            finally { foreach (var response in responses) response.Dispose(); }
            await Task.Delay(1400);
        }
        using (var before = JsonDocument.Parse(await Domain()))
        {
            using var oldAudit = JsonDocument.Parse(await Audit()); var responses = await Task.WhenAll(Send(root, new([Row("distinct-a@example.invalid")]), admin, false), Send(root, new([Row("distinct-b@example.invalid")]), admin, false));
            try { foreach (var response in responses) RequireFinanceStatus(response, HttpStatusCode.OK, "distinct concurrent import"); using var after = JsonDocument.Parse(await Domain()); using var audits = JsonDocument.Parse(await Audit());
                if (after.RootElement.GetProperty("Students").GetArrayLength() != before.RootElement.GetProperty("Students").GetArrayLength() + 2 || audits.RootElement.GetArrayLength() != oldAudit.RootElement.GetArrayLength() + 2) throw new InvalidOperationException("Distinct concurrent imports lost records/audits."); Pass("distinct concurrent imports remain valid"); }
            finally { foreach (var response in responses) response.Dispose(); }
        }
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = false; await db.SaveChangesAsync(); }
        await Rejected(root, valid, admin, HttpStatusCode.Forbidden, "suspended academy import");
        using (var scope = factory.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); (await db.Academies.SingleAsync(x => x.Id == academy)).IsActive = true; await db.SaveChangesAsync(); }
        if (count != 57) throw new InvalidOperationException("Student import matrix incomplete: " + count);
        Console.WriteLine($"PASS: IMPORT all {count} SQL/Identity/HTTP cases; in-file/persisted/concurrent duplicates, optional values, tenant/role isolation and audit rollback; no429.");
    }
}
