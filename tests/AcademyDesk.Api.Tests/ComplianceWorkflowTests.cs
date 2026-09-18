using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class ComplianceWorkflowTests
{
    [Fact]
    public async Task Withdrawing_consent_retains_the_record_and_timestamp()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var consent = new ConsentRecord { AcademyId = academyId, StudentId = Guid.NewGuid(), ConsentType = "Media", Granted = true };
        db.ConsentRecords.Add(consent); await db.SaveChangesAsync();

        var result = await new ComplianceController(db).WithdrawConsent(academyId, consent.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var stored = await db.ConsentRecords.SingleAsync(item => item.Id == consent.Id);
        Assert.False(stored.Granted); Assert.NotNull(stored.WithdrawnAtUtc);
    }

    [Fact]
    public async Task Document_review_rejects_invalid_state()
    {
        await using var db = CreateDb();
        var academyId = Guid.NewGuid();
        var document = new PersonDocument { AcademyId = academyId, StudentId = Guid.NewGuid(), DocumentType = "ID proof", FileName = "id.pdf" };
        db.PersonDocuments.Add(document); await db.SaveChangesAsync();

        var result = await new ComplianceController(db).ReviewDocument(academyId, document.Id, new DocumentReviewRequest("Unknown", null), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase($"compliance-tests-{Guid.NewGuid()}").Options);
}
