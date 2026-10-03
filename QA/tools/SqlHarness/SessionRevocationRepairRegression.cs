using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    // Strict repair regression: any original policy gap fails the run.
    private static async Task VerifySessionRevocationRepairAsync(QaApiFactory factory, HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var observations = new List<object>();
        var requests = 0;
        var gaps = 0;
        const string oldPassword = "Synthetic!39Ab";
        const string newPassword = "Synthetic!Changed48Cd";
        const string admin = "qa-admin-a@example.invalid";
        const string teacher = "qa-teacher-a@example.invalid";

        void Observe(string id, int actual, int[] expected, bool gate = false)
        {
            var accepted = expected.Contains(actual);
            if (!accepted) throw new InvalidOperationException($"Invalid diagnostic control {id}: {actual}.");
            if (!accepted) gaps++;
            var entry = new { id, actual, expected, accepted, policyGate = gate };
            observations.Add(entry);
            Console.WriteLine("SESSIONREVOCATION CASE " + JsonSerializer.Serialize(entry));
        }

        async Task<HttpResponseMessage> Send(string method, string path, string? bearer = null, object? body = null)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            if (body is not null) request.Content = JsonContent.Create(body);
            requests++;
            return await client.SendAsync(request);
        }

        static async Task<(string access, string refresh)> Tokens(HttpResponseMessage response)
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var access = doc.RootElement.GetProperty("accessToken").GetString();
            var refresh = doc.RootElement.GetProperty("refreshToken").GetString();
            if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh))
                throw new InvalidOperationException("Native token pair missing; secrets omitted.");
            return (access, refresh);
        }

        async Task<(string access, string refresh)> Login(string email, string password, string id)
        {
            using var response = await Send("POST", "/api/auth/login", body: new { email, password });
            Observe(id, (int)response.StatusCode, [200]);
            return await Tokens(response);
        }

        async Task Session(string id, string bearer, int[] expected, bool gate = false)
        {
            using var response = await Send("GET", "/api/auth/session", bearer);
            Observe(id, (int)response.StatusCode, expected, gate);
        }

        async Task<string> Stamp(string email)
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == email).Select(x => x.SecurityStamp!).SingleAsync();
        }

        async Task SetActive(bool active)
        {
            // Only the disposable fixture row; equivalent IsActive persistence, not a UI-action acceptance.
            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(admin) ?? throw new InvalidOperationException("Fixture missing.");
            user.IsActive = active;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Fixture active toggle failed.");
            using var verify = factory.Services.CreateScope();
            if (await verify.ServiceProvider.GetRequiredService<IdentityDbContext>().Users
                .Where(x => x.Email == admin).Select(x => x.IsActive).SingleAsync() != active)
                throw new InvalidOperationException("Active fixture persistence failed.");
        }

        Guid academyId;
        string[] linksBefore;
        int usersBefore, rolesBefore, auditsBefore;
        async Task<string[]> Links()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var rows = await db.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
            var roles = await db.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync();
            return rows.Select(x => $"{x.Id}|{x.AcademyId}|{x.TeacherId}|{x.StudentId}|{x.GuardianId}|{x.IsPlatformOwner}|{x.IsActive}|{x.Email}|{x.DisplayName}")
                .Concat(roles.Select(x => $"{x.UserId}|{x.RoleId}")).ToArray();
        }
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            academyId = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            usersBefore = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Users.CountAsync();
            rolesBefore = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().UserRoles.CountAsync();
            auditsBefore = await db.AuditLogs.CountAsync();
        }
        linksBefore = await Links();

        var adminPair = await Login(admin, oldPassword, "active-admin-login");
        var teacherPair = await Login(teacher, oldPassword, "active-teacher-login");
        var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
        if (options.BearerTokenExpiration != TimeSpan.FromHours(8) || options.RefreshTokenExpiration != TimeSpan.FromDays(30))
            throw new InvalidOperationException("Production token lifetime contract changed.");
        var expiredAccess = options.BearerTokenProtector.Unprotect(adminPair.access)!;
        var expiredRefresh = options.RefreshTokenProtector.Unprotect(adminPair.refresh)!;
        // QA-only expired copies of native signed tickets, with original claims.
        expiredAccess.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        expiredRefresh.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(-1);
        await Session("expired-access", options.BearerTokenProtector.Protect(expiredAccess), [401]);
        using (var expired = await Send("POST", "/api/auth/refresh", body: new { refreshToken = options.RefreshTokenProtector.Protect(expiredRefresh) }))
            Observe("expired-refresh", (int)expired.StatusCode, [401]);
        using (var malformed = await Send("POST", "/api/auth/refresh", body: new { refreshToken = "synthetic-invalid-credential" }))
            Observe("malformed-refresh", (int)malformed.StatusCode, [401]);
        using (var teacherAdmin = await Send("GET", $"/api/academies/{academyId}/students", teacherPair.access))
            Observe("teacher-admin-route-denied", (int)teacherAdmin.StatusCode, [403]);

        // Cookies use the same account/stamp policy; keep the main client bearer-only.
        using var cookieClient = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        requests++;
        using var cookieLogin = await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = teacher, password = oldPassword });
        Observe("active-cookie-login", (int)cookieLogin.StatusCode, [200]);
        var cookieHeader = string.Join("; ", cookieLogin.Headers.GetValues("Set-Cookie").Select(x => x.Split(';')[0]));
        cookieClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        requests++;
        using (var cookieSession = await cookieClient.GetAsync("/api/auth/session"))
            Observe("active-cookie-access", (int)cookieSession.StatusCode, [200]);
        await Session("active-admin-session", adminPair.access, [200]);
        await Session("active-teacher-session", teacherPair.access, [200]);
        using (var activeRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = adminPair.refresh }))
        {
            Observe("active-admin-refresh", (int)activeRefresh.StatusCode, [200]);
            await Session("active-renewed-session", (await Tokens(activeRefresh)).access, [200]);
        }

        var adminStamp = await Stamp(admin);
        await SetActive(false);
        requests++;
        using (var disabledCookieLogin = await cookieClient.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email = admin, password = oldPassword }))
        {
            Observe("disabled-cookie-login", (int)disabledCookieLogin.StatusCode, [401]);
            if (disabledCookieLogin.Headers.Contains("Set-Cookie")) throw new InvalidOperationException("Disabled login issued a cookie.");
        }
        if (await Stamp(admin) != adminStamp) throw new InvalidOperationException("Unexpected fixture stamp change.");
        await Session("disabled-original-access", adminPair.access, [401, 403], true);
        using (var deniedLogin = await Send("POST", "/api/auth/login", body: new { email = admin, password = oldPassword }))
        {
            Observe("disabled-login", (int)deniedLogin.StatusCode, [401, 403], true);
            if (deniedLogin.IsSuccessStatusCode)
                await Session("disabled-login-issued-access", (await Tokens(deniedLogin)).access, [401, 403], true);
        }
        using (var disabledRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = adminPair.refresh }))
        {
            Observe("disabled-refresh", (int)disabledRefresh.StatusCode, [401, 403], true);
            if (disabledRefresh.IsSuccessStatusCode)
                await Session("disabled-refresh-issued-access", (await Tokens(disabledRefresh)).access, [401, 403], true);
        }
        await Session("unrelated-teacher-still-active", teacherPair.access, [200]);
        await SetActive(true);
        // Observation only: no policy decision that pre-disable credentials should revive.
        await Session("reactivated-original-access-observation", adminPair.access, [200]);

        var teacherStamp = await Stamp(teacher);
        using (var wrong = await Send("POST", "/api/auth/session/change-password", teacherPair.access,
                   new { currentPassword = "Synthetic!WrongOnly", newPassword }))
            Observe("wrong-password-change", (int)wrong.StatusCode, [400]);
        if (await Stamp(teacher) != teacherStamp) throw new InvalidOperationException("Rejected password change changed stamp.");
        await Session("wrong-change-original-access", teacherPair.access, [200]);
        using (var wrongRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = teacherPair.refresh }))
            Observe("wrong-change-original-refresh", (int)wrongRefresh.StatusCode, [200]);

        using (var changed = await Send("POST", "/api/auth/session/change-password", teacherPair.access,
                   new { currentPassword = oldPassword, newPassword }))
        {
            Observe("password-change", (int)changed.StatusCode, [200]);
            using var doc = JsonDocument.Parse(await changed.Content.ReadAsStringAsync());
            if (doc.RootElement.GetProperty("message").GetString() != "Password changed.")
                throw new InvalidOperationException("Password success notice missing.");
        }
        if (await Stamp(teacher) == teacherStamp) throw new InvalidOperationException("Successful password change did not update stamp.");
        Console.WriteLine("SESSIONREVOCATION SQL stamps: rejected-change unchanged; successful native password change rotated; active fixture toggle unchanged.");
        await Session("password-change-old-access", teacherPair.access, [401, 403], true);
        requests++;
        using (var staleCookie = await cookieClient.GetAsync("/api/auth/session"))
            Observe("password-change-old-cookie", (int)staleCookie.StatusCode, [401]);
        using (var oldRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = teacherPair.refresh }))
            Observe("password-change-old-refresh", (int)oldRefresh.StatusCode, [401, 403], true);
        using (var oldLogin = await Send("POST", "/api/auth/login", body: new { email = teacher, password = oldPassword }))
            Observe("password-change-old-login", (int)oldLogin.StatusCode, [401]);
        var teacherNew = await Login(teacher, newPassword, "password-change-new-login");
        await Session("password-change-new-access", teacherNew.access, [200]);
        using (var newRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = teacherNew.refresh }))
            Observe("password-change-new-refresh", (int)newRefresh.StatusCode, [200]);
        await Session("unrelated-admin-still-active", adminPair.access, [200]);

        const string removedEmail = "qa-revocation-deleted@example.invalid";
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var removed = new ApplicationUser { UserName = removedEmail, Email = removedEmail, EmailConfirmed = true,
                DisplayName = "Synthetic Revocation Deleted", AcademyId = academyId, IsActive = true };
            if (!(await manager.CreateAsync(removed, oldPassword)).Succeeded || !(await manager.AddToRoleAsync(removed, "AcademyAdmin")).Succeeded)
                throw new InvalidOperationException("Deleted-identity synthetic fixture creation failed.");
        }
        var removedPair = await Login(removedEmail, oldPassword, "deleted-fixture-active-login");
        await Session("deleted-fixture-active-access", removedPair.access, [200]);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var removed = await manager.FindByEmailAsync(removedEmail) ?? throw new InvalidOperationException("Delete fixture missing.");
            if (!(await manager.DeleteAsync(removed)).Succeeded) throw new InvalidOperationException("Fixture deletion failed.");
        }
        await Session("deleted-old-session", removedPair.access, [401, 403], true);
        using (var deletedRefresh = await Send("POST", "/api/auth/refresh", body: new { refreshToken = removedPair.refresh }))
            Observe("deleted-old-refresh", (int)deletedRefresh.StatusCode, [401, 403], true);
        using (var deletedLogin = await Send("POST", "/api/auth/login", body: new { email = removedEmail, password = oldPassword }))
            Observe("deleted-old-login", (int)deletedLogin.StatusCode, [401]);
        // This endpoint relies on authentication alone; a session-controller 401 is not global revocation.
        using (var deletedResource = await Send("GET", "/api/academies", removedPair.access))
            Observe("deleted-old-protected-academies", (int)deletedResource.StatusCode, [401, 403], true);

        using (var scope = factory.Services.CreateScope())
        {
            var id = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await id.Users.CountAsync() != usersBefore || await id.UserRoles.CountAsync() != rolesBefore ||
                await db.AuditLogs.CountAsync() != auditsBefore || await id.Users.AnyAsync(x => x.Email == removedEmail) ||
                !(await Links()).SequenceEqual(linksBefore))
                throw new InvalidOperationException("Fresh SQL associations/membership/count preservation failed.");
        }
        Console.WriteLine("SESSIONREVOCATION SQL PASS fresh contexts: original identity/count/role/academy/typed links/active flags preserved; synthetic deleted row absent; audit count unchanged; password/stamp intentionally changed only for Teacher fixture.");
        Console.WriteLine("SESSIONREVOCATION SUMMARY " + JsonSerializer.Serialize(new { requests, cases = observations.Count, policyGaps = gaps,
            diagnosticComplete = true, acceptance = gaps == 0 ? "BOUNDED PASS" : "FAIL / OPEN", productChanged = true }));
    }
}
