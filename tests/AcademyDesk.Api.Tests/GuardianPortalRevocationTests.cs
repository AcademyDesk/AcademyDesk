using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class GuardianPortalRevocationTests
{
    public static IEnumerable<object[]> AuthorityCases()
    {
        foreach (var age in new[] { "minor", "adult", "birthday18", "unknown" })
            foreach (var access in new[] { false, true })
                foreach (var flags in new[] { false, true }) yield return [age, access, flags];
    }

    [Theory, MemberData(nameof(AuthorityCases))]
    public async Task Explicit_authority_is_independent_of_age_and_subpermissions(string age, bool access, bool flags)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = Guid.NewGuid(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = age,
            DateOfBirth = age switch { "minor" => today.AddYears(-10), "adult" => today.AddYears(-25), "birthday18" => today.AddYears(-18), _ => null } };
        var guardian = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" };
        var link = new StudentGuardian { AcademyId = academy, StudentId = student.Id, GuardianId = guardian.Id, CanAccessPortal = true, Relationship = "Parent", IsPrimary = true, AccessGrantedAtUtc = DateTime.UtcNow.AddDays(-1) };
        var other = new StudentGuardian { AcademyId = academy, StudentId = Guid.NewGuid(), GuardianId = guardian.Id, CanAccessPortal = true, AccessGrantedAtUtc = DateTime.UtcNow.AddDays(-1) };
        db.AddRange(student, guardian, link, other); await db.SaveChangesAsync();
        var otherGranted = other.AccessGrantedAtUtc;
        var start = DateTime.UtcNow;
        var result = await new StudentGuardiansController(db).SetPortalAccess(academy, student.Id, guardian.Id, new(access, flags, flags, flags, flags), default);
        var response = Assert.IsType<StudentGuardianSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
        db.ChangeTracker.Clear(); var saved = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.Id == link.Id);
        Assert.Equal(access, saved.CanAccessPortal); Assert.Equal(access && flags, saved.CanViewAcademicProgress);
        Assert.Equal(access && flags, saved.CanViewFinance); Assert.Equal(access && flags, saved.CanViewDocuments); Assert.Equal(access && flags, saved.CanManageLeave);
        Assert.Equal(access, response.CanAccessPortal); Assert.Equal(access && flags, response.CanViewAcademicProgress);
        Assert.Equal(access && flags, response.CanViewFinance); Assert.Equal(access && flags, response.CanViewDocuments); Assert.Equal(access && flags, response.CanManageLeave);
        Assert.Equal(saved.AccessGrantedAtUtc, response.AccessGrantedAtUtc); Assert.Equal(saved.AccessRevokedAtUtc, response.AccessRevokedAtUtc);
        Assert.Equal("Parent", saved.Relationship); Assert.True(saved.IsPrimary); Assert.Equal(academy, saved.AcademyId);
        Assert.Equal(student.Id, saved.StudentId); Assert.Equal(guardian.Id, saved.GuardianId);
        if (access) { Assert.Null(saved.AccessRevokedAtUtc); Assert.InRange(saved.AccessGrantedAtUtc!.Value, start, DateTime.UtcNow); }
        else { Assert.Null(saved.AccessGrantedAtUtc); Assert.InRange(saved.AccessRevokedAtUtc!.Value, start, DateTime.UtcNow); }
        var untouched = await db.StudentGuardians.AsNoTracking().SingleAsync(x => x.Id == other.Id);
        Assert.True(untouched.CanAccessPortal); Assert.Equal(otherGranted, untouched.AccessGrantedAtUtc); Assert.Null(untouched.AccessRevokedAtUtc);
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Initial_link_minor_default_is_not_changed_by_revocation_repair(bool minor)
    {
        await using var db = new AcademyDeskDbContext(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var academy = Guid.NewGuid();
        var student = new Student { AcademyId = academy, FirstName = "Synthetic", LastName = "Learner", DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(minor ? -10 : -25) };
        var guardian = new Guardian { AcademyId = academy, FirstName = "Synthetic", LastName = "Guardian" };
        db.AddRange(student, guardian); await db.SaveChangesAsync();
        var result = await new StudentGuardiansController(db).Link(academy, student.Id, new(guardian.Id, "Parent", true), default);
        var response = Assert.IsType<StudentGuardianSummary>(Assert.IsType<CreatedResult>(result.Result).Value);
        Assert.Equal(minor, response.CanAccessPortal);
        Assert.Equal(minor, (await db.StudentGuardians.AsNoTracking().SingleAsync()).CanAccessPortal);
    }
}
