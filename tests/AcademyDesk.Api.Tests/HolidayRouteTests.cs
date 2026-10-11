using System.Net;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

// Actual MVC discovery/routing with disposable InMemory rows. Not SQL or
// production authentication/filter proof: those are in HolidayRouteRegression.
public sealed class HolidayRouteTests
{
    private static IHost Server(AcademyDeskDbContext db) => new HostBuilder().ConfigureWebHost(web => web.UseTestServer()
        .ConfigureServices(services => {
            services.AddSingleton(db);
            services.AddControllers().AddApplicationPart(typeof(HolidaysController).Assembly);
        })
        .Configure(app => {
            app.Use(async (context, next) => {
                try { await next(context); }
                catch (Exception error) when (error.GetType().FullName == "Microsoft.AspNetCore.Routing.Matching.AmbiguousMatchException") {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Synthetic test: ambiguous routing");
                }
            });
            app.UseRouting(); app.UseEndpoints(endpoints => endpoints.MapControllers());
        })).Start();

    private static AcademyDeskDbContext Database() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Mvc_discovers_exactly_one_canonical_holiday_delete()
    {
        using var db = Database(); using var server = Server(db);
        var actions = server.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>().Where(x => x.ControllerTypeInfo.Name is "HolidaysController" or "HolidayDeleteController" &&
                x.ActionConstraints?.OfType<HttpMethodActionConstraint>().Any(c => c.HttpMethods.Contains("DELETE")) == true).ToArray();
        var action = Assert.Single(actions); Assert.Equal(typeof(HolidaysController), action.ControllerTypeInfo.AsType());
    }

    [Fact]
    public async Task Http_delete_removes_only_the_matching_row_and_repeat_is_404()
    {
        using var db = Database(); var academy = Guid.NewGuid();
        var local = new AcademyHoliday { AcademyId = academy, Name = "Synthetic local", HolidayDate = new(2026, 10, 11) };
        var foreign = new AcademyHoliday { AcademyId = Guid.NewGuid(), Name = "Foreign sentinel", HolidayDate = new(2026, 10, 12) };
        db.AddRange(local, foreign); await db.SaveChangesAsync(); using var server = Server(db); using var client = server.GetTestClient();
        using var response = await client.DeleteAsync($"/api/academies/{academy}/holidays/{local.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode); db.ChangeTracker.Clear();
        Assert.Equal(foreign.Id, (await db.AcademyHolidays.SingleAsync()).Id);
        using var repeated = await client.DeleteAsync($"/api/academies/{academy}/holidays/{local.Id}");
        Assert.Equal(HttpStatusCode.NotFound, repeated.StatusCode); Assert.Equal(1, await db.AcademyHolidays.CountAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Http_foreign_or_missing_record_does_not_remove_rows(bool missing)
    {
        using var db = Database(); var foreign = new AcademyHoliday { AcademyId = Guid.NewGuid(), Name = "Foreign sentinel", HolidayDate = new(2026, 10, 12) };
        db.Add(foreign); await db.SaveChangesAsync(); using var server = Server(db); using var client = server.GetTestClient();
        using var response = await client.DeleteAsync($"/api/academies/{Guid.NewGuid()}/holidays/{(missing ? Guid.NewGuid() : foreign.Id)}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); db.ChangeTracker.Clear(); Assert.Equal(foreign.Id, (await db.AcademyHolidays.SingleAsync()).Id);
    }
}
