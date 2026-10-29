using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifySpreadsheetExportsAsync(QaApiFactory factory, HttpClient client)
    {
        Guid academy, foreign, student;
        string originalModules;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var own = await db.Academies.SingleAsync(x => x.Name == "Synthetic Academy A");
            academy = own.Id; originalModules = own.EnabledModulesJson;
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            student = await db.Students.Where(x => x.AcademyId == academy).Select(x => x.Id).SingleAsync();
            own.EnabledModulesJson = "[\"Certificates\",\"Finance\",\"FinanceControls\",\"AccessGovernance\"]";
            db.AddRange(
                new Guardian { AcademyId = academy, FirstName = "=1+2", LastName = "CSV", Phone = "+910000000000" },
                new Guardian { AcademyId = foreign, FirstName = "FOREIGN-CSV-SENTINEL", LastName = "CSV" },
                new AuditLog { AcademyId = academy, Action = "@SUM(1)", EntityType = "CsvFixture", MetadataJson = "=1\",;\"=2" },
                new AuditLog { AcademyId = foreign, Action = "FOREIGN-CSV-SENTINEL", EntityType = "CsvFixture" });
            await db.SaveChangesAsync();
        }
        try
        {
            await CreateAccessActorAsync(factory, "qa-csv-student@example.invalid", "Student", academy, student);
            var admin = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
            var teacher = await LoginAsync(client, "qa-teacher-a@example.invalid", "Synthetic!39Ab");
            var learner = await LoginAsync(client, "qa-csv-student@example.invalid", "Synthetic!39Ab");
            var resources = new[] { "students", "guardians", "enrollments", "invoices", "payments" };
            var checks = 0;
            async Task<string> Read(string? bearer, string path, HttpStatusCode expected)
            {
                await Task.Delay(650); // Preserve the existing limiter; never accept429.
                client.DefaultRequestHeaders.Authorization = bearer is null ? null : new AuthenticationHeaderValue("Bearer", bearer);
                var before = await SpreadsheetSnapshotAsync(factory);
                using var response = await client.GetAsync(path);
                RequireFinanceStatus(response, expected, "CSV protected export");
                var body = await response.Content.ReadAsStringAsync();
                if (body.Contains("FOREIGN-CSV-SENTINEL", StringComparison.Ordinal))
                    throw new InvalidOperationException("Foreign CSV data leaked.");
                if (before != await SpreadsheetSnapshotAsync(factory))
                    throw new InvalidOperationException("CSV GET changed domain/audit state.");
                if (expected == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType != "text/csv")
                    throw new InvalidOperationException("Export MIME differs.");
                checks++;
                return body;
            }
            foreach (var resource in resources)
            {
                var body = await Read(admin, $"/api/academies/{academy}/exports/{resource}", HttpStatusCode.OK);
                var rows = SpreadsheetRows(body);
                if (rows.Count == 0 || rows.Any(row => row.Length != rows[0].Length))
                    throw new InvalidOperationException("CSV row shape differs.");
                if (resource == "guardians" && !rows.Any(row => row[0] == "\t=1+2" && row[3] == "\t+910000000000"))
                    throw new InvalidOperationException("Native SQL guardian text was not guarded.");
                foreach (var actor in new[] { teacher, learner })
                    await Read(actor, $"/api/academies/{academy}/exports/{resource}", HttpStatusCode.Forbidden);
                await Read(admin, $"/api/academies/{foreign}/exports/{resource}", HttpStatusCode.Forbidden);
                await Read(null, $"/api/academies/{academy}/exports/{resource}", HttpStatusCode.Unauthorized);
            }
            var auditPath = $"/api/academies/{academy}/audit-logs/export.csv";
            var auditRows = SpreadsheetRows(await Read(admin, auditPath, HttpStatusCode.OK));
            if (!auditRows.Any(row => row[1] == "\t@SUM(1)" && row[5] == "\t=1\",;\"=2"))
                throw new InvalidOperationException("Native SQL audit text was not guarded.");
            foreach (var actor in new[] { teacher, learner }) await Read(actor, auditPath, HttpStatusCode.Forbidden);
            await Read(admin, $"/api/academies/{foreign}/audit-logs/export.csv", HttpStatusCode.Forbidden);
            await Read(null, auditPath, HttpStatusCode.Unauthorized);
            var filtered = SpreadsheetRows(await Read(admin, auditPath + "?action=%40SUM%281%29&entityType=CsvFixture", HttpStatusCode.OK));
            if (filtered.Count != 2 || filtered[1][1] != "\t@SUM(1)")
                throw new InvalidOperationException("CSV audit filters changed.");
            await Read(admin, $"/api/academies/{academy}/exports/not-a-resource", HttpStatusCode.NotFound);
            Console.WriteLine($"ACCESS CSV PASS: {checks} actual Identity/HTTP/SQL requests; five registers+audit, guardian/audit formula guards, filters, Teacher/Student/anonymous/foreign denial, unchanged domain/audit snapshots, no429.");
        }
        finally
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            (await db.Academies.SingleAsync(x => x.Id == academy)).EnabledModulesJson = originalModules;
            await db.SaveChangesAsync();
        }
    }

    private static List<string[]> SpreadsheetRows(string csv)
    {
        using var reader = new TextFieldParser(new StringReader(csv)) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        reader.SetDelimiters(",");
        var rows = new List<string[]>();
        while (!reader.EndOfData) rows.Add(reader.ReadFields() ?? throw new InvalidOperationException("Missing CSV row"));
        return rows;
    }

    private static async Task<string> SpreadsheetSnapshotAsync(QaApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        return JsonSerializer.Serialize(new {
            Students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Enrollments = await db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Invoices = await db.Invoices.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Payments = await db.Payments.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
            Audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(),
        });
    }
}
