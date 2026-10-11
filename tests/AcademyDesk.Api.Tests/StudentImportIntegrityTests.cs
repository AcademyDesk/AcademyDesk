using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class StudentImportIntegrityTests
{
    private static AcademyDeskDbContext Db() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static StudentImportRow Row(string? email = null) => new(" First ", " Last ", null, email, null, null);
    private static JsonElement Json(object? value) => JsonSerializer.SerializeToElement(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Theory]
    [InlineData("same@example.invalid", "same@example.invalid")]
    [InlineData("SAME@example.invalid", "same@example.invalid")]
    [InlineData(" same@example.invalid ", "same@example.invalid")]
    [InlineData(" Same@Example.Invalid ", "SAME@example.invalid")]
    public async Task Duplicate_email_reports_both_rows_and_import_writes_nothing(string first, string second)
    {
        using var db = Db(); var academy = Guid.NewGuid(); db.Add(new Academy { Id = academy, Name = "Fixture" }); await db.SaveChangesAsync();
        var request = new StudentImportRequest([Row(first), Row("unique@example.invalid"), Row(second)]);
        var validation = Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(new StudentImportsController(db).Validate(academy, request)).Value);
        Assert.Equal(3, validation.RowCount); Assert.Equal(new[] { 1, 3 }, Json(validation.Errors).EnumerateArray().Select(x => x.GetProperty("row").GetInt32()).ToArray());
        foreach (var error in Json(validation.Errors).EnumerateArray()) Assert.Contains("repeated", error.GetProperty("message").GetString());
        Assert.IsType<BadRequestObjectResult>(await new StudentImportsController(db).Import(academy, request, default));
        Assert.Empty(await db.Students.ToArrayAsync()); Assert.Empty(await db.AuditLogs.ToArrayAsync());
    }

    [Theory][InlineData(null)][InlineData("")][InlineData("   ")]
    public async Task Optional_blank_values_remain_allowed_and_persist_null(string? blank)
    {
        using var db = Db(); var academy = Guid.NewGuid(); db.Add(new Academy { Id = academy, Name = "Fixture" }); await db.SaveChangesAsync();
        var request = new StudentImportRequest([Row(blank) with { StudentNumber = blank, Phone = blank }, Row(blank)]);
        Assert.Empty(Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(new StudentImportsController(db).Validate(academy, request)).Value).Errors);
        Assert.IsType<OkObjectResult>(await new StudentImportsController(db).Import(academy, request, default));
        var rows = await db.Students.AsNoTracking().ToArrayAsync(); Assert.Equal(2, rows.Length);
        foreach (var row in rows) { Assert.Null(row.Email); Assert.Null(row.StudentNumber); Assert.Null(row.Phone); Assert.Equal("First", row.FirstName); Assert.Equal("Last", row.LastName); }
    }

    [Fact]
    public async Task Existing_local_email_conflicts_after_case_and_whitespace_normalization()
    {
        using var db = Db(); var academy = Guid.NewGuid(); db.Add(new Academy { Id = academy, Name = "Fixture" }); db.Add(new Student { AcademyId = academy, FirstName = "Old", LastName = "Student", Email = " Existing@Example.Invalid " }); await db.SaveChangesAsync();
        var controller = new StudentImportsController(db);
        Assert.IsType<ConflictObjectResult>(await controller.Import(academy, new([Row("existing@example.invalid"), Row("new@example.invalid")]), default));
        Assert.Equal(1, await db.Students.CountAsync());
    }

    [Fact]
    public async Task Foreign_email_does_not_block_valid_import_and_optional_date_is_preserved()
    {
        using var db = Db(); var academy = Guid.NewGuid(); var foreign = new Student { AcademyId = Guid.NewGuid(), FirstName = "Foreign", LastName = "Sentinel", Email = "shared@example.invalid" };
        db.Add(new Academy { Id = academy, Name = "Fixture" }); db.Add(foreign); await db.SaveChangesAsync(); var before = Json(foreign).GetRawText(); var date = new DateOnly(2024, 2, 29);
        var result = Assert.IsType<OkObjectResult>(await new StudentImportsController(db).Import(academy, new([Row(" shared@example.invalid ") with { AdmissionDate = date }]), default));
        Assert.Equal(1, Json(result.Value).GetProperty("imported").GetInt32()); Assert.Equal(before, Json(await db.Students.AsNoTracking().SingleAsync(x => x.Id == foreign.Id)).GetRawText());
        var added = await db.Students.SingleAsync(x => x.AcademyId == academy); Assert.Equal(date, added.AdmissionDate); Assert.Equal("shared@example.invalid", added.Email);
    }

    [Theory][InlineData("first")][InlineData("last")][InlineData("email")][InlineData("number")][InlineData("phone")][InlineData("null-row")]
    public async Task Invalid_row_is_reported_before_any_write(string field)
    {
        using var db = Db(); var row = Row(); row = field switch { "first" => row with { FirstName = new('x',121) }, "last" => row with { LastName = new('x',121) },
            "email" => row with { Email = new string('x',321) + "@" }, "number" => row with { StudentNumber = new('x',51) }, "phone" => row with { Phone = new('x',31) }, _ => null! };
        var controller = new StudentImportsController(db); var request = new StudentImportRequest([Row("valid@example.invalid"), row]);
        var validation = Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(controller.Validate(Guid.NewGuid(), request)).Value); Assert.NotEmpty(validation.Errors);
        Assert.All(Json(validation.Errors).EnumerateArray(), x => Assert.Equal(2, x.GetProperty("row").GetInt32()));
        Assert.IsType<BadRequestObjectResult>(await controller.Import(Guid.NewGuid(), request, default)); Assert.Empty(await db.Students.ToArrayAsync());
    }

    [Theory][InlineData(true)][InlineData(false)]
    public async Task Missing_rows_are_a_validation_error(bool isNull)
    {
        using var db = Db(); var controller = new StudentImportsController(db); var request = new StudentImportRequest(isNull ? null : []);
        var validation = Assert.IsType<StudentImportValidation>(Assert.IsType<OkObjectResult>(controller.Validate(Guid.NewGuid(), request)).Value);
        Assert.Equal(0, validation.RowCount); Assert.Single(validation.Errors); Assert.IsType<BadRequestObjectResult>(await controller.Import(Guid.NewGuid(), request, default));
    }
}
