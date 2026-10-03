using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class InvoiceStudentOptionTests
{
    [Fact]
    public async Task Lookup_is_scoped_ordered_nullable_and_retains_inactive_billing_identities()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = Guid.NewGuid();
        var zed = new Student { AcademyId = academy, FirstName = "Zed", LastName = "Alpha", IsActive = false };
        var amy = new Student { AcademyId = academy, FirstName = "Amy", LastName = "Alpha", Email = "qa@example.invalid", Phone = "QA" };
        db.Students.AddRange(zed, amy, new Student { AcademyId = Guid.NewGuid(), FirstName = "Foreign", LastName = "Synthetic" });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var result = await new InvoicesController(db).StudentOptions(academy, default);
        var rows = Assert.IsType<List<InvoiceStudentOption>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(new[] { amy.Id, zed.Id }, rows.Select(x => x.Id));
        Assert.Equal("qa@example.invalid", rows[0].Email); Assert.Equal("QA", rows[0].Phone);
        Assert.Null(rows[1].Email); Assert.Null(rows[1].Phone);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(3, await db.Students.CountAsync()); Assert.Equal(0, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task Empty_academy_lookup_returns_empty_not_foreign_students()
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Students.Add(new Student { AcademyId = Guid.NewGuid(), FirstName = "Foreign", LastName = "Synthetic" }); await db.SaveChangesAsync();
        var result = await new InvoicesController(db).StudentOptions(Guid.NewGuid(), default);
        Assert.Empty(Assert.IsType<List<InvoiceStudentOption>>(Assert.IsType<OkObjectResult>(result.Result).Value));
    }

    [Fact]
    public void Contract_exposes_only_billing_fields_and_uses_existing_finance_gate()
    {
        Assert.Equal(new[] { "Email", "FirstName", "Id", "LastName", "Phone" }, typeof(InvoiceStudentOption).GetProperties().Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        Assert.Equal(new[] { "finance.manage" }, AcademyDesk.Api.Security.PermissionCatalog.RequiredFor(nameof(InvoicesController)));
        Assert.Equal(new[] { "students.manage" }, AcademyDesk.Api.Security.PermissionCatalog.RequiredFor("StudentsController"));
        Assert.DoesNotContain("students.manage", AcademyDesk.Api.Security.PermissionCatalog.ForSystemRole("FinanceUser"));
    }
}
