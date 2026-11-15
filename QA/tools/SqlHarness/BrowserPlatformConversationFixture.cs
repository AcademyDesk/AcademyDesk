using System.Net;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private const string PentaBrowserPlatformEmail = "qa-platform-conversation@example.invalid";
    private sealed record BrowserPlatformFixture(Guid Owner, Guid Academy, Guid Profile, Guid OpenCase);

    private static async Task<BrowserPlatformFixture> SeedBrowserPlatformConversationAsync(QaApiFactory factory, Guid academy)
    {
        var owner = await CreateAccessActorAsync(factory, PentaBrowserPlatformEmail, "PlatformOwner", academy);
        var mislabeled = await CreateAccessActorAsync(factory, "qa-platform-flag-denied@example.invalid", "PlatformOwner", academy);
        PentaRequire(mislabeled != owner, "Fixture actors must be distinct.");
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var database = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var account = await identity.Users.SingleAsync(x => x.Id == owner);
        account.IsPlatformOwner = true;
        account.AcademyId = null;
        var profile = new TenantOnboardingProfile { AcademyId = academy, Status = "InProgress",
            PrimaryContactEmail = "private-portal-probe@example.invalid" };
        var support = new PlatformSupportCase { AcademyId = academy, Subject = "Private support probe", Priority = "High", Status = "Open" };
        database.AddRange(profile, support,
            new PlatformSupportCase { AcademyId = academy, Subject = "Normal probe", Priority = "Normal", Status = "Open" },
            new PlatformSupportCase { AcademyId = academy, Subject = "Resolved probe", Priority = "Urgent", Status = "Resolved" });
        await database.SaveChangesAsync();
        await identity.SaveChangesAsync();
        return new(owner, academy, profile.Id, support.Id);
    }

    private static async Task VerifyBrowserPlatformOptionsAsync(QaApiFactory factory)
    {
        const string root = "/api/platform/penta";
        using var anonymous = factory.CreateClient();
        using var admin = await PentaBrowserSignIn(factory, "qa-admin-a@example.invalid");
        using var mislabeled = await PentaBrowserSignIn(factory, "qa-platform-flag-denied@example.invalid");
        using var owner = await PentaBrowserSignIn(factory, PentaBrowserPlatformEmail);
        await PentaBrowserStatus(anonymous.GetAsync(root + "/readiness"), HttpStatusCode.Unauthorized, "anonymous platform readiness");
        await PentaBrowserStatus(admin.GetAsync(root + "/health"), HttpStatusCode.Forbidden, "academy admin platform denial");
        await PentaBrowserStatus(mislabeled.GetAsync(root + "/support"), HttpStatusCode.Forbidden, "platform role label without owner flag");
        await PentaBrowserStatus(owner.GetAsync(root + "/private"), HttpStatusCode.NotFound, "unsupported platform read");
        Console.WriteLine("PENTA PLATFORM SQL SCOPE PASS: 4 status checks.");
    }

    private static async Task VerifyBrowserPlatformConversationAsync(QaApiFactory factory, BrowserPlatformFixture fixture)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
        var audits = await db.PlatformAuditEntries.AsNoTracking()
            .Where(x => x.ActorUserId == fixture.Owner && x.EntityType == "Platform").Select(x => x.Action).ToArrayAsync();
        PentaRequire(audits.Order().SequenceEqual(new[] { "PentaPlatformHealthRead", "PentaPlatformOnboardingRead", "PentaPlatformSupportRead" }) &&
            await db.TenantOnboardingProfiles.AnyAsync(x => x.Id == fixture.Profile && x.AcademyId == fixture.Academy && x.Status == "InProgress") &&
            await db.PlatformSupportCases.AnyAsync(x => x.Id == fixture.OpenCase && x.AcademyId == fixture.Academy && x.Status == "Open"),
            "Platform conversation affected tenant data or did not attribute the expected audits.");
        Console.WriteLine("PENTA PLATFORM SQL BROWSER PASS: three attributed audits and unchanged fixtures.");
    }
}
