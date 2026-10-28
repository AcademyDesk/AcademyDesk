using System.Text;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class CsvExportSecurityTests
{
    [Fact]
    public void Ordinary_cells_keep_csv_escaping_nulls_unicode_and_typed_negative_amounts()
    {
        Assert.Equal("\"\",\"\",\"Synthetic, \"\"quoted\"\"\"",
            SpreadsheetCsv.Row(null, "", "Synthetic, \"quoted\""));
        Assert.Equal("\"line one\r\nline two\"", SpreadsheetCsv.Cell("line one\r\nline two"));
        Assert.Equal("\"नाम\"", SpreadsheetCsv.Cell("नाम"));
        Assert.Equal("\"'-42\"", SpreadsheetCsv.Cell("-42"));
        Assert.Equal("\"-42\"", SpreadsheetCsv.Cell(-42));
    }

    public static TheoryData<string> UntrustedCells => new()
    {
        "=1+1", "+SUM(1,2)", "-1+2", "@SUM(1,2)", "\t=1+1", "\r=1+1",
        "\n=1+1", "  =1+1", "\uFEFF=1+1", "\u0000=1+1", "\tordinary text",
        "＝1+1", "＋1+1", "－1+1", "＠SUM(1,2)"
    };

    [Theory, MemberData(nameof(UntrustedCells))]
    public async Task Student_export_neutralizes_formula_text_without_mutating_records(string value)
    {
        await using var db = Context();
        var academy = Guid.NewGuid();
        db.AddRange(new Student { AcademyId = academy, FirstName = value, LastName = "Synthetic" },
            new Student { AcademyId = Guid.NewGuid(), FirstName = "FOREIGN", LastName = "Tenant" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var file = Assert.IsType<FileContentResult>(await new AcademyExportsController(db).Export(academy, "students", default));
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains(Quoted("'" + value), csv);
        Assert.DoesNotContain("FOREIGN", csv);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("academydesk-students.csv", file.FileDownloadName);
        Assert.Equal(value, (await db.Students.AsNoTracking().SingleAsync(x => x.AcademyId == academy)).FirstName);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Theory, MemberData(nameof(UntrustedCells))]
    public async Task Audit_export_neutralizes_text_and_preserves_scope_and_filters(string value)
    {
        await using var db = Context();
        var academy = Guid.NewGuid();
        db.AddRange(new AuditLog { AcademyId = academy, Action = "Exported", EntityType = "Student", MetadataJson = value },
            new AuditLog { AcademyId = academy, Action = "Other", EntityType = "Student", MetadataJson = "FILTERED" },
            new AuditLog { AcademyId = Guid.NewGuid(), Action = "Exported", EntityType = "Student", MetadataJson = "FOREIGN" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var file = Assert.IsType<FileContentResult>(await new AuditLogsController(db).ExportCsv(academy, "Exported", "Student", null, null, default));
        var csv = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains(Quoted("'" + value), csv);
        Assert.DoesNotContain("FOREIGN", csv);
        Assert.DoesNotContain("FILTERED", csv);
        Assert.Equal("academydesk-audit-log.csv", file.FileDownloadName);
        Assert.Equal(value, (await db.AuditLogs.AsNoTracking().SingleAsync(x => x.AcademyId == academy && x.Action == "Exported")).MetadataJson);
        Assert.Empty(db.ChangeTracker.Entries());
    }

    private static string Quoted(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    private static AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
