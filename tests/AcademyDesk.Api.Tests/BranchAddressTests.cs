using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class BranchAddressTests
{
    private static AcademyDeskDbContext Store() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Theory]
    [InlineData(true, "edit")]
    [InlineData(true, "deactivate")]
    [InlineData(true, "reactivate")]
    [InlineData(false, "edit")]
    [InlineData(false, "deactivate")]
    [InlineData(false, "reactivate")]
    public async Task List_and_update_round_trip_preservation_fields(bool populated, string action)
    {
        using var db = Store();
        var academy = new Academy { Name = "Synthetic" };
        var branch = new Branch { AcademyId = academy.Id, Name = "Synthetic", City = "City", State = "State",
            AddressLine1 = populated ? "12 Synthetic street" : null, PostalCode = populated ? "560001" : null,
            IsActive = action != "reactivate" };
        var foreign = new Branch { AcademyId = Guid.NewGuid(), Name = "Foreign", AddressLine1 = "Foreign marker" };
        db.AddRange(academy, branch, foreign); await db.SaveChangesAsync();
        var controller = new BranchesController(db);
        var list = Assert.IsAssignableFrom<IReadOnlyList<BranchSummary>>(Assert.IsType<OkObjectResult>((await controller.List(academy.Id, default)).Result).Value);
        var projection = JsonSerializer.SerializeToElement(Assert.Single(list));
        Assert.Equal(branch.AddressLine1, projection.GetProperty("AddressLine1").GetString());
        Assert.Equal(branch.PostalCode, projection.GetProperty("PostalCode").GetString());
        var created = branch.CreatedAtUtc;
        var result = await controller.Update(academy.Id, branch.Id, new UpdateBranchRequest(
            action == "edit" ? "Renamed" : branch.Name, projection.GetProperty("AddressLine1").GetString(),
            branch.City, branch.State, projection.GetProperty("PostalCode").GetString(), action == "edit" ? branch.IsActive : !branch.IsActive), default);
        var updated = JsonSerializer.SerializeToElement(Assert.IsType<BranchSummary>(Assert.IsType<OkObjectResult>(result.Result).Value));
        db.ChangeTracker.Clear();
        var stored = await db.Branches.SingleAsync(x => x.Id == branch.Id);
        Assert.Equal(branch.AddressLine1, stored.AddressLine1); Assert.Equal(branch.PostalCode, stored.PostalCode);
        Assert.Equal(branch.AddressLine1, updated.GetProperty("AddressLine1").GetString());
        Assert.Equal(branch.PostalCode, updated.GetProperty("PostalCode").GetString());
        Assert.Equal(created, stored.CreatedAtUtc); Assert.Null(stored.UpdatedAtUtc);
        Assert.Equal("Foreign marker", (await db.Branches.SingleAsync(x => x.Id == foreign.Id)).AddressLine1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_returns_trimmed_or_null_address_fields(bool populated)
    {
        using var db = Store(); var academy = new Academy { Name = "Synthetic" }; db.Add(academy); await db.SaveChangesAsync();
        var response = await new BranchesController(db).Create(academy.Id,
            new CreateBranchRequest(" Synthetic ", populated ? " Street " : null, " City ", " State ", populated ? " 560001 " : null), default);
        var created = Assert.IsType<BranchSummary>(Assert.IsType<CreatedAtActionResult>(response.Result).Value);
        var json = JsonSerializer.SerializeToElement(created);
        Assert.Equal(populated ? "Street" : null, json.GetProperty("AddressLine1").GetString());
        Assert.Equal(populated ? "560001" : null, json.GetProperty("PostalCode").GetString());
        Assert.Equal("Synthetic", created.Name);
    }

    [Fact]
    public async Task Explicit_null_clear_retains_existing_replacement_semantics()
    {
        using var db = Store(); var branch = new Branch { AcademyId = Guid.NewGuid(), Name = "Synthetic", AddressLine1 = "Street", PostalCode = "560001" };
        db.Add(branch); await db.SaveChangesAsync();
        Assert.IsType<OkObjectResult>((await new BranchesController(db).Update(branch.AcademyId, branch.Id,
            new UpdateBranchRequest(branch.Name, null, null, null, null, true), default)).Result);
        db.ChangeTracker.Clear(); var stored = await db.Branches.SingleAsync();
        Assert.Null(stored.AddressLine1); Assert.Null(stored.PostalCode);
    }
}
