using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Tests;

public sealed class AcademicYearClosureTests
{
    [Theory]
    [InlineData(false, false, 0, 365, " Term ", 200)]
    [InlineData(false, true, 0, 365, "Term", 200)]
    [InlineData(false, false, 0, 0, "First day", 200)]
    [InlineData(false, false, 365, 365, "Last day", 200)]
    [InlineData(true, false, 0, 365, "Term", 409)]
    [InlineData(true, true, 0, 365, "Term", 409)]
    [InlineData(false, true, 0, 365, " ", 400)]
    [InlineData(false, true, 10, 9, "Term", 400)]
    [InlineData(false, true, -1, 365, "Term", 400)]
    [InlineData(false, true, 0, 366, "Term", 400)]
    public async Task Term_creation_obeys_parent_state_and_boundaries(bool closed, bool current, int start, int end, string name, int expected)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var year = Year(Guid.NewGuid(), closed, current); var sibling = Year(year.AcademyId);
        var foreign = Year(Guid.NewGuid()); db.AddRange(year, sibling, foreign); await db.SaveChangesAsync();
        var before = await State(db);
        var result = await new AcademicPeriodsController(db).CreateTerm(year.AcademyId,
            new(year.Id, name, year.StartDate.AddDays(start), year.StartDate.AddDays(end)), CancellationToken.None);
        Assert.Equal(expected, Status(result.Result!));
        await using var fresh = new AcademyDeskDbContext(options); var after = await State(fresh);
        Assert.Equal(before.Years, after.Years);
        if (expected != 200)
        {
            Assert.Equal(before, after);
            if (closed) Assert.Contains("closed", JsonSerializer.Serialize(Assert.IsType<ConflictObjectResult>(result.Result).Value), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            var saved = await fresh.AcademicTerms.SingleAsync();
            Assert.Equal(year.AcademyId, saved.AcademyId); Assert.Equal(year.Id, saved.AcademicYearId);
            Assert.Equal(name.Trim(), saved.Name); Assert.Equal(year.StartDate.AddDays(start), saved.StartDate);
            Assert.Equal(year.StartDate.AddDays(end), saved.EndDate); Assert.False(saved.IsClosed);
            var response = Assert.IsType<AcademicTermSummary>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(new(saved.Id, saved.AcademicYearId, saved.Name, saved.StartDate, saved.EndDate, false), response);
        }
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("foreign-year")]
    [InlineData("foreign-route")]
    public async Task Wrong_parent_is_rejected_without_writes(string mode)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var year = Year(Guid.NewGuid()); var foreign = Year(Guid.NewGuid()); db.AddRange(year, foreign); await db.SaveChangesAsync();
        var before = await State(db);
        var result = await new AcademicPeriodsController(db).CreateTerm(mode == "foreign-route" ? foreign.AcademyId : year.AcademyId,
            new(mode == "missing" ? Guid.NewGuid() : mode == "foreign-year" ? foreign.Id : year.Id,"Term",year.StartDate,year.EndDate),CancellationToken.None);
        Assert.Equal(400, Status(result.Result!)); await using var fresh = new AcademyDeskDbContext(options); Assert.Equal(before, await State(fresh));
    }

    [Theory]
    [InlineData("empty",200)]
    [InlineData("all-closed",200)]
    [InlineData("open-term",409)]
    [InlineData("already-closed",200)]
    [InlineData("missing",404)]
    [InlineData("foreign",404)]
    public async Task Closing_year_preserves_existing_rules_and_child_rows(string mode,int expected)
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var year = Year(Guid.NewGuid(),mode == "already-closed",mode != "already-closed"); var sibling = Year(year.AcademyId); db.AddRange(year,sibling);
        if (mode is "all-closed" or "open-term") db.AcademicTerms.Add(new AcademicTerm { AcademyId=year.AcademyId,AcademicYearId=year.Id,Name="Existing term",StartDate=year.StartDate,EndDate=year.EndDate,IsClosed=mode == "all-closed" });
        await db.SaveChangesAsync(); var before = await State(db);
        var result = await new AcademicPeriodsController(db).CloseYear(mode == "foreign" ? Guid.NewGuid() : year.AcademyId,
            mode == "missing" ? Guid.NewGuid() : year.Id,CancellationToken.None);
        Assert.Equal(expected,Status(result)); await using var fresh = new AcademyDeskDbContext(options); var after = await State(fresh);
        Assert.Equal(before.Terms,after.Terms);
        if (expected != 200) Assert.Equal(before,after);
        else
        {
            year.IsClosed=true;year.IsCurrent=false;
            Assert.Equal(JsonSerializer.Serialize(new[] {year,sibling}.OrderBy(x=>x.Id)),after.Years);
        }
    }

    [Fact]
    public async Task Request_prepared_before_year_closure_cannot_create_an_open_term()
    {
        var options = Options(); await using var db = new AcademyDeskDbContext(options);
        var year=Year(Guid.NewGuid(),current:true); db.Add(year); await db.SaveChangesAsync();
        var request = new AcademicTermRequest(year.Id,"Stale form",year.StartDate,year.EndDate);
        Assert.Equal(200,Status(await new AcademicPeriodsController(db).CloseYear(year.AcademyId,year.Id,CancellationToken.None)));
        await using var fresh = new AcademyDeskDbContext(options);var before=await State(fresh);
        var result=await new AcademicPeriodsController(fresh).CreateTerm(year.AcademyId,request,CancellationToken.None);
        Assert.Equal(409,Status(result.Result!));await using var verify=new AcademyDeskDbContext(options);Assert.Equal(before,await State(verify));
    }

    [Theory]
    [InlineData(nameof(AcademicPeriodsController.CreateTerm))]
    [InlineData(nameof(AcademicPeriodsController.CloseYear))]
    public void Coupled_year_mutations_use_domain_audit_boundary(string method)
    {
        var action=new ControllerActionDescriptor {MethodInfo=typeof(AcademicPeriodsController).GetMethod(method)!};
        Assert.True(AcademyAccessFilter.UsesAtomicBoundary(nameof(AcademicPeriodsController),action));Assert.False(AcademyAccessFilter.IncludesIdentity(action));
        Assert.Null(Attribute.GetCustomAttribute(typeof(AcademicPeriodsController).GetMethod(nameof(AcademicPeriodsController.CreateYear))!,typeof(AtomicAcademyMutationAttribute)));
        Assert.Null(Attribute.GetCustomAttribute(typeof(AcademicPeriodsController).GetMethod(nameof(AcademicPeriodsController.CloseTerm))!,typeof(AtomicAcademyMutationAttribute)));
    }

    private static AcademicYear Year(Guid academy,bool closed=false,bool current=false)=>new(){AcademyId=academy,Name="Synthetic "+Guid.NewGuid(),StartDate=new(2026,1,1),EndDate=new(2027,1,1),IsClosed=closed,IsCurrent=current};
    private static DbContextOptions<AcademyDeskDbContext> Options()=>new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("year-closure-"+Guid.NewGuid()).Options;
    private static int Status(ActionResult result)=>result switch {StatusCodeResult r=>r.StatusCode,ObjectResult r=>r.StatusCode??200,_=>throw new InvalidOperationException(result.GetType().Name)};
    private static async Task<(string Years,string Terms)> State(AcademyDeskDbContext db)=>(JsonSerializer.Serialize(await db.AcademicYears.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()),JsonSerializer.Serialize(await db.AcademicTerms.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()));
}
