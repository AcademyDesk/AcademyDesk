using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyExportSecurityAsync(QaApiFactory factory, HttpClient client)
    {
        string[] values = ["=1+1", "+SUM(1,2)", "-1+2", "@SUM(1,2)", "\t=1+1", "\r=1+1", "\n=1+1", "  =1+1", "\uFEFF=1+1", "\0=1+1", "\tordinary text", "＝1+1", "＋1+1", "－1+1", "＠SUM(1,2)"];
        Guid academy, foreign;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            var a = await db.Academies.SingleAsync(x => x.Id == academy);
            var modules = JsonSerializer.Deserialize<List<string>>(a.EnabledModulesJson ?? "[]") ?? [];
            if (!modules.Contains("AccessGovernance")) modules.Add("AccessGovernance");
            a.EnabledModulesJson = JsonSerializer.Serialize(modules);
            var course = new ProgramCourse { AcademyId = academy, Name = "Synthetic export course" };
            var batch = new Batch { AcademyId = academy, CourseId = course.Id, Name = "Synthetic export class" };
            db.AddRange(course, batch);
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i]; var label = "CSV-" + i.ToString("D2", CultureInfo.InvariantCulture);
                var student = new Student { AcademyId = academy, FirstName = value, LastName = label, StudentNumber = label };
                var guardian = new Guardian { AcademyId = academy, FirstName = value, LastName = label };
                var invoice = new Invoice { AcademyId = academy, StudentId = student.Id, InvoiceNumber = value + "-" + i, TotalAmount = 42m, AdjustedAmount = 1m };
                var payment = new Payment { AcademyId = academy, InvoiceId = invoice.Id, Amount = 1m, Reference = value, ReconciliationReference = value };
                var enrollment = new Enrollment { AcademyId = academy, StudentId = student.Id, BatchId = batch.Id, Status = value };
                var audit = new AuditLog { AcademyId = academy, Action = "QA-CSV", EntityType = "CsvFixture", MetadataJson = value };
                db.AddRange(student, guardian, invoice, payment, enrollment, audit);
            }
            db.AddRange(new Student { AcademyId = foreign, FirstName = "FOREIGN-CSV-SENTINEL", LastName = "Synthetic" },
                new AuditLog { AcademyId = foreign, Action = "QA-CSV", EntityType = "CsvFixture", MetadataJson = "FOREIGN-CSV-SENTINEL" },
                new AuditLog { AcademyId = academy, Action = "OTHER-CSV", EntityType = "CsvFixture", MetadataJson = "FILTERED-CSV-SENTINEL" });
            await db.SaveChangesAsync();
        }
        await CreateAccessActorAsync(factory, "qa-export-teacher@example.invalid", "Teacher", academy);
        await CreateAccessActorAsync(factory, "qa-export-finance@example.invalid", "FinanceUser", academy);
        var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        var other = await LoginAsync(client, "qa-admin-b@example.invalid", "Synthetic!39Ab");
        var teacher = await LoginAsync(client, "qa-export-teacher@example.invalid", "Synthetic!39Ab");
        var finance = await LoginAsync(client, "qa-export-finance@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var root = $"/api/academies/{academy}"; var count = 0;
        async Task<string> State()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        async Task<HttpResponseMessage> Get(string path, string? token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, root + path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
        var before = await State();
        foreach (var resource in new[] { "students", "guardians", "enrollments", "invoices", "payments", "audit" })
        {
            var path = resource == "audit" ? "/audit-logs/export.csv?action=QA-CSV&entityType=CsvFixture" : "/exports/" + resource;
            using var response = await Get(path, admin); RequireFinanceStatus(response, HttpStatusCode.OK, "CSV " + resource);
            if (response.Content.Headers.ContentType?.MediaType != "text/csv" || response.Content.Headers.ContentDisposition?.DispositionType != "attachment")
                throw new InvalidOperationException("CSV response type/disposition changed: " + resource);
            var csv = new UTF8Encoding(false, true).GetString(await response.Content.ReadAsByteArrayAsync());
            if (csv.Contains("FOREIGN-CSV-SENTINEL", StringComparison.Ordinal) || csv.Contains("FILTERED-CSV-SENTINEL", StringComparison.Ordinal))
                throw new InvalidOperationException("CSV tenant/filter boundary failed: " + resource);
            using var parser = new TextFieldParser(new StringReader(csv)) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
            parser.SetDelimiters(","); var rows = new List<string[]>();
            while (!parser.EndOfData) rows.Add(parser.ReadFields() ?? throw new InvalidOperationException("Missing CSV row."));
            var data = rows.Skip(1).ToList();
            for (var i = 0; i < values.Length; i++)
            {
                var value = "'" + values[i];
                var present = resource switch
                {
                    "students" => data.Any(x => x.Length == 6 && x[1] == value && x[2] == "CSV-" + i.ToString("D2", CultureInfo.InvariantCulture)),
                    "guardians" => data.Any(x => x.Length == 5 && x[0] == value && x[1] == "CSV-" + i.ToString("D2", CultureInfo.InvariantCulture)),
                    "enrollments" => data.Any(x => x.Length == 5 && x[4] == value),
                    "invoices" => data.Any(x => x.Length == 6 && x[0] == value + "-" + i && decimal.TryParse(x[1], CultureInfo.InvariantCulture, out var gross) && gross == 42m && decimal.TryParse(x[3], CultureInfo.InvariantCulture, out var net) && net == 41m),
                    "payments" => data.Any(x => x.Length == 7 && x[4] == value && x[5] == value),
                    "audit" => data.Any(x => x.Length == 6 && x[1] == "QA-CSV" && x[2] == "CsvFixture" && x[5] == value),
                    _ => false
                };
                if (!present) throw new InvalidOperationException("CSV protected field/shape/numeric control missing: " + resource + " case=" + i);
            }
            if (before != await State()) throw new InvalidOperationException("Read-only export mutated stored domain/audit rows.");
            count++; Console.WriteLine($"EXPORTSECURITY CASE {resource} PASS:15 exact protected cells, tenant/filter/headers, numeric shape and unchanged fresh SQL.");
            foreach (var (label, token, expected) in new (string, string?, HttpStatusCode)[] { ("anonymous", null, HttpStatusCode.Unauthorized), ("foreign", other, HttpStatusCode.Forbidden), ("teacher", teacher, HttpStatusCode.Forbidden), ("finance", finance, resource == "audit" ? HttpStatusCode.Forbidden : HttpStatusCode.OK) })
            {
                using var denied = await Get(path, token); RequireFinanceStatus(denied, expected, "CSV " + resource + " " + label);
                if (before != await State()) throw new InvalidOperationException("Scoped export access changed stored rows.");
                count++;
            }
        }
        using (var response = await Get("/exports/unknown", admin)) RequireFinanceStatus(response, HttpStatusCode.NotFound, "unknown CSV resource");
        if (before != await State()) throw new InvalidOperationException("Unknown export changed stored rows.");
        count++;
        if (count != 31) throw new InvalidOperationException("Export security case inventory changed: " + count);
        Console.WriteLine($"EXPORTSECURITY REGRESSION PASS:{count} real Identity/HTTP/fresh SQL cases;6 export paths x15 dangerous-text values, exact parser cells/typed amounts, headers, filters, tenant/role and unchanged records. Browser downloads and supported stable spreadsheet consumers remain separate.");
    }
}
