using System.Net;
using System.Text.RegularExpressions;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AcademyDesk.Api.Tests;

public sealed class HolidayRouteTests
{
    [Fact]
    public void Equivalent_controller_method_routes_have_one_matching_endpoint()
    {
        var manifest = QaRunManifest.Create();
        try
        {
            using var factory = new QaApiFactory(manifest); _ = factory.Server;
            var entries = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
                .Where(x => x.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
                .SelectMany(x => (x.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
                    .Select(method => new { Key = method + " " + x.Order + " " + Regex.Replace(x.RoutePattern.RawText ?? "", @"\{[^}:?]+", "{parameter").ToLowerInvariant(), Name = x.DisplayName }));
            var collisions = entries.GroupBy(x => x.Key).Where(x => x.Count() > 1).Select(x => x.Key + ": " + string.Join(" / ", x.Select(y => y.Name))).ToArray();
            Assert.Empty(collisions);
        }
        finally { Cleanup(manifest); }
    }

    [Fact]
    public async Task Anonymous_holiday_delete_reaches_authorization_without_ambiguous_routing_or_SQL()
    {
        var manifest = QaRunManifest.Create();
        try
        {
            using var factory = new QaApiFactory(manifest);
            using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
            using var response = await client.DeleteAsync($"/api/academies/{Guid.NewGuid()}/holidays/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally { Cleanup(manifest); }
    }

    [Fact]
    public async Task Own_delete_removes_only_target_and_repeat_is_not_found()
    {
        await using var db = CreateDb(); var academy = Guid.NewGuid();
        var own = new AcademyHoliday { AcademyId = academy, Name = "Synthetic own", HolidayDate = new(2026, 10, 28) };
        var other = new AcademyHoliday { AcademyId = Guid.NewGuid(), Name = "Synthetic foreign", HolidayDate = new(2026, 10, 28) };
        db.AddRange(own, other); await db.SaveChangesAsync(); var controller = new HolidaysController(db);
        Assert.IsType<NoContentResult>(await controller.Delete(academy, own.Id, default));
        Assert.Equal(other.Id, (await db.AcademyHolidays.SingleAsync()).Id);
        Assert.IsType<NotFoundResult>(await controller.Delete(academy, own.Id, default));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Foreign_or_missing_delete_preserves_rows(bool foreign)
    {
        await using var db = CreateDb(); var item = new AcademyHoliday { AcademyId = Guid.NewGuid(), Name = "Synthetic", HolidayDate = new(2026, 10, 28) };
        db.Add(item); await db.SaveChangesAsync();
        Assert.IsType<NotFoundResult>(await new HolidaysController(db).Delete(foreign ? Guid.NewGuid() : item.AcademyId, foreign ? item.Id : Guid.NewGuid(), default));
        Assert.Equal(item.Id, (await db.AcademyHolidays.SingleAsync()).Id);
    }

    private static AcademyDeskDbContext CreateDb() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseInMemoryDatabase("qa-holiday-route-" + Guid.NewGuid()).Options);
    private static void Cleanup(QaRunManifest manifest)
    {
        if (Directory.Exists(manifest.Root)) QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, true); return true; });
    }
}
