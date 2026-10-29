using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Meetings;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyMeetingOAuthBrowserAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy; string connection;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); connection = db.Database.GetConnectionString()!;
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        }
        var target = new SqlConnectionStringBuilder(connection);
        if (target.DataSource != manifest.SqlServer || target.InitialCatalog != manifest.Database || target.UserID != manifest.RuntimeLogin)
            throw new InvalidOperationException("OAuth browser proof requires exact owned runtime target");
        var id = await CreateAccessActorAsync(factory, "qa-oauth-browser@example.invalid", "AcademyAdmin", academy);
        var actor = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "synthetic-oauth-browser"));
        const string api = "https://api.academy.invalid", workspace = "https://app.academy.invalid";
        var providers = new[] { "GoogleWorkspace", "Microsoft365", "Zoom" };
        var registry = MeetingOAuthRegistrationRegistry.Create(api, workspace, providers.Select(p =>
            new MeetingOAuthRegistrationInput(p, 1, "synthetic-client", api + MeetingOAuthRegistrationRegistry.CallbackPath(p), workspace + "/communication-settings")).ToArray());
        var clock = new OperationClock(); var keys = factory.Services.GetRequiredService<IDataProtectionProvider>();
        AcademyDeskDbContext Open() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection).Options);
        async Task<MeetingOAuthBridgeOutcome> Run(Func<MeetingOAuthBrowserBridge, Task<MeetingOAuthBridgeOutcome>> work)
        {
            using var scope = factory.Services.CreateScope(); await using var db = Open(); var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            return await work(new(new(db, users, new(db, users, clock), keys, clock), registry, keys, clock));
        }
        DefaultHttpContext Context(string provider, string state, string cookie)
        {
            var context = new DefaultHttpContext { User = actor }; context.Request.Scheme = "https"; context.Request.Host = new("api.academy.invalid");
            context.Request.Method = "GET"; context.Request.Path = MeetingOAuthRegistrationRegistry.CallbackPath(provider);
            context.Request.QueryString = new("?state=" + state + "&code=synthetic-code"); context.Request.Headers.Cookie = cookie; return context;
        }
        var checks = new List<string>();
        void Check(string label, bool passed) { if (!passed) throw new InvalidOperationException("MEETING OAUTH BROWSER " + label); checks.Add(label); Console.WriteLine("MEETING OAUTH BROWSER CASE " + label + " PASS"); }
        foreach (var provider in providers)
        {
            int version; await using (var db = Open()) version = await db.MeetingProviderBindings.Where(x => x.AcademyId == academy && x.Provider == provider).Select(x => x.Version).SingleAsync();
            var begin = Context(provider, "", ""); begin.Request.Method = "POST"; begin.Request.Headers.Origin = workspace;
            var prepared = await Run(b => b.BeginAsync(begin, academy, provider, version));
            var header = SetCookieHeaderValue.Parse(begin.Response.Headers.SetCookie.ToString()); var cookie = header.ToString().Split(';')[0];
            Check(provider + "-prepare-secure-cookie", prepared.StatusCode == 201 && header.Secure && header.HttpOnly && !header.Domain.HasValue && header.Path == "/" && header.SameSite == Microsoft.Net.Http.Headers.SameSiteMode.Lax);
            var state = prepared.Material!.State;
            var anonymous = Context(provider, state, cookie); anonymous.User = new(new ClaimsIdentity());
            Check(provider + "-anonymous-not-reauthenticated", (await Run(b => b.AcceptAuthenticatedCallbackAsync(anonymous, academy, provider))).StatusCode == 401);
            Check(provider + "-missing-cookie", (await Run(b => b.AcceptAuthenticatedCallbackAsync(Context(provider, state, ""), academy, provider))).StatusCode == 409);
            var duplicate = Context(provider, state, cookie); duplicate.Request.QueryString = new(duplicate.Request.QueryString.Value + "&state=" + state);
            Check(provider + "-duplicate-state", (await Run(b => b.AcceptAuthenticatedCallbackAsync(duplicate, academy, provider))).StatusCode == 400);
            var accepted = await Run(b => b.AcceptAuthenticatedCallbackAsync(Context(provider, state, cookie), academy, provider));
            Check(provider + "-authenticated-claim-only", accepted.StatusCode == 200 && accepted.Code == "OAuthCallbackClaimedNotConnected" && accepted.Material!.Verifier is not null && accepted.AuthorizationCode == "synthetic-code");
            Check(provider + "-replay-denied", (await Run(b => b.AcceptAuthenticatedCallbackAsync(Context(provider, state, cookie), academy, provider))).StatusCode == 409);
            await using var current = Open();
            Check(provider + "-durable-two-audits-no-connect", await current.MeetingOAuthAttempts.CountAsync(x => x.AcademyId == academy && x.ActorUserId == id && x.Provider == provider && x.Status == "Claimed") == 1 &&
                await current.AuditLogs.CountAsync(x => x.ActorUserId == id && current.MeetingOAuthAttempts.Any(a => a.Id == x.EntityId && a.Provider == provider)) == 2 &&
                (await current.MeetingProviderBindings.SingleAsync(x => x.AcademyId == academy && x.Provider == provider)).Status == "VerificationRequired");
        }
        if (checks.Count != 21) throw new InvalidOperationException("OAuth browser case count mismatch");
        var evidence = Path.Combine("QA", "EVIDENCE", "meeting-oauth-browser-sql-" + manifest.RunId.ToString("N")); Directory.CreateDirectory(evidence);
        var sources = new[] { "apps/api/Infrastructure/Meetings/MeetingOAuthRegistrationRegistry.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthBrowserBridge.cs", "apps/api/Infrastructure/Meetings/MeetingOAuthAttemptStore.cs", "QA/tools/SqlHarness/MeetingOAuthBrowserRegression.cs", "QA/tools/SqlHarness/MeetingReadinessRegression.cs" }
            .ToDictionary(p => p, p => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        await File.WriteAllTextAsync(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(new { checks, sources, scope = "Internal bridge with synthetic HttpContext/cookies and real owned SQL/Identity, NOT routed OAuth HTTP, browser reauthentication, provider exchange or live connection" }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("MEETING OAUTH BROWSER PASS:21internal bridge/SQL cases; no public OAuth or provider calls.");
    }
}
