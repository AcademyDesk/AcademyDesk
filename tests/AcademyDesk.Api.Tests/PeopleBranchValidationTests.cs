using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class PeopleBranchValidationTests
{
    [Theory]
    [InlineData("students", "foreign")]
    [InlineData("students", "missing")]
    [InlineData("students", "empty")]
    [InlineData("teachers", "foreign")]
    [InlineData("teachers", "missing")]
    [InlineData("teachers", "empty")]
    public async Task Invalid_branch_is_rejected_before_person_mutation_or_account_store_use(string kind, string invalid)
    {
        using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = Guid.NewGuid();
        var own = new Branch { AcademyId = academy, Name = "Synthetic own branch" };
        var foreign = new Branch { AcademyId = Guid.NewGuid(), Name = "Synthetic foreign branch" };
        db.AddRange(own, foreign);
        Guid person;
        if (kind == "students")
        {
            var row = new Student { AcademyId = academy, BranchId = own.Id, FirstName = "Original", LastName = "Synthetic", Phone = "old" };
            db.Students.Add(row); person = row.Id;
        }
        else
        {
            var row = new Teacher { AcademyId = academy, BranchId = own.Id, FirstName = "Original", LastName = "Synthetic", Phone = "old", Specialties = "old" };
            db.Teachers.Add(row); person = row.Id;
        }
        await db.SaveChangesAsync();
        var before = await SnapshotAsync(db, kind);
        var branch = invalid == "foreign" ? foreign.Id : invalid == "empty" ? Guid.Empty : Guid.NewGuid();
        // Deliberately no account store: the guard must return before reaching it.
        // This narrow unit test is not a substitute for real Identity/SQL tests.
        IActionResult? result = kind == "students"
            ? (await new StudentsController(db, null!).Update(academy, person, new UpdateStudentRequest("Changed", "Synthetic", "qa@example.invalid", "new", branch, false), default)).Result
            : (await new TeachersController(db, null!).Update(academy, person, new UpdateTeacherRequest("Changed", "Synthetic", "qa@example.invalid", "new", "new", branch, false), default)).Result;
        var rejection = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("The selected branch does not belong to this academy.", JsonSerializer.SerializeToElement(rejection.Value).GetProperty("message").GetString());
        db.ChangeTracker.Clear();
        Assert.Equal(before, await SnapshotAsync(db, kind));
        Assert.Equal(0, await db.AuditLogs.CountAsync());
    }

    private static async Task<string> SnapshotAsync(AcademyDeskDbContext db, string kind) => kind == "students"
        ? JsonSerializer.Serialize(await db.Students.AsNoTracking().ToListAsync())
        : JsonSerializer.Serialize(await db.Teachers.AsNoTracking().ToListAsync());
}
