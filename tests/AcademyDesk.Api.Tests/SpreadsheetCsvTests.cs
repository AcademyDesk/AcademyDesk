using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class SpreadsheetCsvTests
{
    [Theory]
    [InlineData("=1+2")]
    [InlineData("+1+2")]
    [InlineData("-1+2")]
    [InlineData("@SUM(1)")]
    [InlineData("\tordinary")]
    [InlineData("\r=1")]
    [InlineData("\n=1")]
    [InlineData("  =1")]
    [InlineData("\u00a0\u200b=1")]
    [InlineData("\ufeff=1")]
    [InlineData("\U000E0001=1")]
    [InlineData("'=1")]
    [InlineData("＝1")]
    [InlineData("＋1")]
    [InlineData("－1")]
    [InlineData("＠SUM(1)")]
    [InlineData("=1\",;\"=2")]
    public void Dangerous_text_is_guarded_inside_one_quoted_cell(string value)
    {
        Assert.Equal("\"\t" + value.Replace("\"", "\"\"") + "\"", SpreadsheetCsv.Cell(value));
    }

    [Theory]
    [InlineData("ordinary")]
    [InlineData("  ordinary")]
    [InlineData("O'Neil")]
    [InlineData("Piano, strings; drums")]
    [InlineData("A \"quoted\" label")]
    [InlineData("line one\r\nline two")]
    [InlineData("")]
    [InlineData("ಕನ್ನಡ नाम")]
    public void Ordinary_text_preserves_data_and_csv_escaping(string value)
    {
        Assert.Equal("\"" + value.Replace("\"", "\"\"") + "\"", SpreadsheetCsv.Cell(value));
    }

    [Fact]
    public void Typed_numbers_booleans_and_null_are_not_untrusted_formula_text()
    {
        Assert.Equal("\"-12\"", SpreadsheetCsv.Cell(-12));
        Assert.Equal("\"0\"", SpreadsheetCsv.Cell(0));
        Assert.Equal("\"False\"", SpreadsheetCsv.Cell(false));
        Assert.Equal("\"\"", SpreadsheetCsv.Cell(null));
        Assert.Equal("\"\t-12\"", SpreadsheetCsv.Cell("-12"));
        Assert.Equal("\"\t=1\",\"a,b\",\"-12\"", SpreadsheetCsv.Row("=1", "a,b", -12));
    }

    [Fact]
    public async Task Academy_and_audit_exports_guard_owned_text_without_writes_or_foreign_rows()
    {
        var options = new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var academy = Guid.NewGuid();
        await using var db = new AcademyDeskDbContext(options);
        var student = new Student { AcademyId = academy, FirstName = "=1+2", LastName = "Ordinary", StudentNumber = "CSV-1", Phone = "+910000000000" };
        var audit = new AuditLog { AcademyId = academy, Action = "@SUM(1)", EntityType = "Student", MetadataJson = "=1\",;\"=2" };
        db.AddRange(student, audit,
            new Student { AcademyId = Guid.NewGuid(), FirstName = "FOREIGN-SENTINEL", LastName = "Other" },
            new AuditLog { AcademyId = Guid.NewGuid(), Action = "FOREIGN-SENTINEL", EntityType = "Other" });
        await db.SaveChangesAsync();
        async Task<string> Snapshot()
        {
            await using var read = new AcademyDeskDbContext(options);
            return JsonSerializer.Serialize(new { students = await read.Students.OrderBy(x => x.Id).ToListAsync(),
                audit = await read.AuditLogs.OrderBy(x => x.Id).ToListAsync() });
        }
        var before = await Snapshot();
        var exported = Assert.IsType<FileContentResult>(await new AcademyExportsController(db).Export(academy, "students", default));
        var text = Encoding.UTF8.GetString(exported.FileContents);
        Assert.StartsWith("StudentNumber,FirstName,LastName,Email,Phone,Active\n", text);
        Assert.Contains("\"\t=1+2\"", text);
        Assert.Contains("\"\t+910000000000\"", text);
        Assert.DoesNotContain("FOREIGN-SENTINEL", text);
        var auditExport = Assert.IsType<FileContentResult>(await new AuditLogsController(db).ExportCsv(academy, null, null, null, null, default));
        var auditText = Encoding.UTF8.GetString(auditExport.FileContents);
        Assert.Contains("\"\t@SUM(1)\"", auditText);
        Assert.Contains("\"\t=1\"\",;\"\"=2\"", auditText);
        Assert.DoesNotContain("FOREIGN-SENTINEL", auditText);
        Assert.Equal("text/csv", exported.ContentType);
        Assert.Equal("academydesk-students.csv", exported.FileDownloadName);
        Assert.Equal("academydesk-audit-log.csv", auditExport.FileDownloadName);
        Assert.IsType<NotFoundResult>(await new AcademyExportsController(db).Export(academy, "not-a-resource", default));
        Assert.Equal(before, await Snapshot());
    }
}
