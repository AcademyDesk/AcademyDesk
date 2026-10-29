using System.Buffers.Binary;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

internal static partial class SqlHarnessEntryPoint
{
    // Real native endpoints/current options; new disposable accounts only.
    // No authenticator key, token, password, cookie or recovery code is logged.
    private static async Task VerifyIdentityChallengesAsync(QaApiFactory factory, HttpClient client)
    {
        // The retained private-media +36session checks share the real120/minute
        // global window. Start this distinct slice after that normal window;
        // never disable the limiter, forge a new IP or retry a rejected write.
        Console.WriteLine("IDENTITYCHALLENGE pacing: waiting61seconds for the existing native rate window; limiter unchanged.");
        await Task.Delay(TimeSpan.FromSeconds(61));
        const string password = "Synthetic!Challenge748";
        const string mfaEmail = "qa-native-mfa@example.invalid";
        const string lockEmail = "qa-native-lockout@example.invalid";
        client.DefaultRequestHeaders.Authorization = null;
        using var challengeClient = factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = false });
        var challengeCookieName = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.TwoFactorUserIdScheme).Cookie.Name!;
        var cases = 0;
        var requests = 0;
        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("IDENTITYCHALLENGE failed: " + label);
            cases++; Console.WriteLine("IDENTITYCHALLENGE PASS " + label);
        }
        async Task<HttpResponseMessage> Send(string path, object body, int expected, string label, string? bearer = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            requests++;
            var response = await challengeClient.SendAsync(request);
            if ((int)response.StatusCode != expected)
            {
                var actual = (int)response.StatusCode; response.Dispose();
                throw new InvalidOperationException($"IDENTITYCHALLENGE {label}: expected {expected}, actual {actual}; body withheld.");
            }
            if (expected >= 400)
            {
                // Identity may issue a partial MFA challenge cookie on 401.
                // It must not issue a completed application authentication cookie.
                var cookies = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.ToArray() : [];
                Check(cookies.All(cookie => expected == 401 && path == "/api/auth/login" &&
                    cookie.StartsWith(challengeCookieName + "=",StringComparison.Ordinal)),label + "; no completed-session cookie");
                if (cookies.Length > 0)
                {
                    using var probe = new HttpRequestMessage(HttpMethod.Get,"/api/auth/session");
                    probe.Headers.TryAddWithoutValidation("Cookie",string.Join("; ",cookies.Select(cookie=>cookie.Split(';')[0])));
                    requests++;using var probeResponse=await challengeClient.SendAsync(probe);
                    Check((int)probeResponse.StatusCode==401,label + "; partial challenge cookie alone cannot authenticate");
                }
            }
            Check(true, label + " HTTP " + expected);
            return response;
        }
        async Task<(string access, string refresh)> Pair(HttpResponseMessage response)
        {
            using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var access = body.RootElement.GetProperty("accessToken").GetString();
            var refresh = body.RootElement.GetProperty("refreshToken").GetString();
            if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh))
                throw new InvalidOperationException("Native challenge did not return a valid credential pair; values withheld.");
            return (access, refresh);
        }
        async Task Session(string access, int expected, string label)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/session");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access); requests++;
            using var response = await challengeClient.SendAsync(request);
            Check((int)response.StatusCode == expected, label + " HTTP " + expected);
        }
        async Task<string> Originals()
        {
            using var scope = factory.Services.CreateScope();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var users = await identity.Users.AsNoTracking().Where(user => user.Email != mfaEmail && user.Email != lockEmail).OrderBy(user => user.Id).ToArrayAsync();
            var ids = users.Select(user => user.Id).ToArray();
            var roles = await identity.UserRoles.AsNoTracking().Where(row => ids.Contains(row.UserId)).OrderBy(row => row.UserId).ThenBy(row => row.RoleId).ToArrayAsync();
            var tokens = await identity.UserTokens.AsNoTracking().Where(row => ids.Contains(row.UserId)).OrderBy(row => row.UserId).ThenBy(row => row.LoginProvider).ThenBy(row => row.Name).ToArrayAsync();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { users, roles, tokens,
                students = await db.Students.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(),
                invoices = await db.Invoices.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(),
                payments = await db.Payments.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync() });
        }
        var original = await Originals();
        int failedLimit;
        Guid academy;
        using (var scope = factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value.Lockout;
            failedLimit = options.MaxFailedAccessAttempts;
            if (failedLimit is < 2 or > 10 || options.DefaultLockoutTimeSpan <= TimeSpan.Zero)
                throw new InvalidOperationException("Current lockout policy is outside this bounded test; no policy override is allowed.");
            academy = await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Academies.Where(row => row.Name == "Synthetic Academy A").Select(row => row.Id).SingleAsync();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var email in new[] { mfaEmail, lockEmail })
            {
                var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, IsActive = true, AcademyId = academy, DisplayName = "Synthetic native challenge", LockoutEnabled = true };
                if (!(await users.CreateAsync(user, password)).Succeeded || !(await users.AddToRoleAsync(user,"AcademyAdmin")).Succeeded || !(await users.SetLockoutEnabledAsync(user,true)).Succeeded)
                    throw new InvalidOperationException("Disposable native challenge fixture creation failed; details withheld.");
            }
        }
        using var firstLogin = await Send("/api/auth/login",new { email=mfaEmail,password },200,"MFA fixture initial native login");
        var first = await Pair(firstLogin);
        using var anonymousSetup = await Send("/api/auth/manage/2fa",new { },401,"anonymous MFA setup denied");
        using var setup = await Send("/api/auth/manage/2fa",new { },200,"authenticated native MFA setup",first.access);
        using var setupBody = await JsonDocument.ParseAsync(await setup.Content.ReadAsStreamAsync());
        var sharedKey = setupBody.RootElement.GetProperty("sharedKey").GetString()!;
        Check(!setupBody.RootElement.GetProperty("isTwoFactorEnabled").GetBoolean(),"setup alone does not enable MFA");
        // Native first-time key creation rotates Identity's security stamp.
        // Reauthenticate rather than weakening the host's stale-session check.
        await Session(first.access,401,"authenticator key creation revokes previous access");
        using var setupLogin = await Send("/api/auth/login",new { email=mfaEmail,password },200,"native login after authenticator key creation");
        var setupPair = await Pair(setupLogin);
        using var missingProof = await Send("/api/auth/manage/2fa",new { enable=true },400,"MFA enable without proof denied",setupPair.access);
        using var invalidProof = await Send("/api/auth/manage/2fa",new { enable=true,twoFactorCode="invalid" },400,"MFA enable invalid proof denied",setupPair.access);
        using var enable = await Send("/api/auth/manage/2fa",new { enable=true,twoFactorCode=ChallengeTotp(sharedKey,DateTimeOffset.UtcNow) },200,"native MFA enable with current authenticator proof",setupPair.access);
        using var enableBody = await JsonDocument.ParseAsync(await enable.Content.ReadAsStreamAsync());
        Check(enableBody.RootElement.GetProperty("isTwoFactorEnabled").GetBoolean(),"native response confirms MFA enabled");
        var recovery = enableBody.RootElement.GetProperty("recoveryCodes").EnumerateArray().Select(row=>row.GetString()!).ToArray();
        Check(recovery.Length==10 && recovery.Distinct().Count()==10,"ten distinct native recovery codes generated; values withheld");
        using (var scope=factory.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user=(await users.FindByEmailAsync(mfaEmail))!;
            Check(user.TwoFactorEnabled && await users.GetAuthenticatorKeyAsync(user)==sharedKey && await users.CountRecoveryCodesAsync(user)==10,"fresh SQL/store confirms MFA key/flag/recovery count");
        }
        using var missingOtp = await Send("/api/auth/login",new { email=mfaEmail,password },401,"password alone cannot obtain MFA token");
        using var invalidOtp = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorCode="invalid" },401,"invalid authenticator proof denied");
        using var otpLogin = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorCode=ChallengeTotp(sharedKey,DateTimeOffset.UtcNow) },200,"valid authenticator native login");
        var mfaPair=await Pair(otpLogin);await Session(mfaPair.access,200,"MFA-issued access works");
        using var renewal = await Send("/api/auth/refresh",new { refreshToken=mfaPair.refresh },200,"MFA-issued native refresh");
        await Session((await Pair(renewal)).access,200,"MFA-renewed access works");
        using var recoveryLogin = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorRecoveryCode=recovery[0] },200,"native recovery code login");
        await Session((await Pair(recoveryLogin)).access,200,"recovery-issued access works");
        using var replay = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorRecoveryCode=recovery[0] },401,"consumed recovery code replay denied");
        using (var scope=factory.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByEmailAsync(mfaEmail))!;
            Check(await users.CountRecoveryCodesAsync(user)==9,"fresh store shows exactly one recovery code consumed");
            user.IsActive=false; if(!(await users.UpdateAsync(user)).Succeeded)throw new InvalidOperationException("MFA disposable disable failed.");
        }
        using var inactiveOtp = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorCode=ChallengeTotp(sharedKey,DateTimeOffset.UtcNow) },401,"inactive MFA account cannot obtain tokens");
        using var inactiveRecovery = await Send("/api/auth/login",new { email=mfaEmail,password,twoFactorRecoveryCode=recovery[1] },401,"inactive account cannot use unused recovery code");
        using var inactiveRefresh = await Send("/api/auth/refresh",new { refreshToken=mfaPair.refresh },401,"inactive MFA refresh denied");
        await Session(mfaPair.access,403,"inactive MFA existing access denied");
        using (var scope=factory.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Check(await users.CountRecoveryCodesAsync((await users.FindByEmailAsync(mfaEmail))!)==9,"inactive denial does not consume an unused recovery code");
        }
        using var lockLogin=await Send("/api/auth/login",new { email=lockEmail,password },200,"lockout fixture native login");
        var lockPair=await Pair(lockLogin);
        for(var attempt=1;attempt<=failedLimit;attempt++)
        {
            using var wrong=await Send("/api/auth/login",new { email=lockEmail,password="Synthetic!Incorrect748" },401,"wrong password attempt "+attempt);
            using var scope=factory.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByEmailAsync(lockEmail))!;
            Check(attempt<failedLimit ? user.AccessFailedCount==attempt && !await users.IsLockedOutAsync(user) : user.AccessFailedCount==0 && await users.IsLockedOutAsync(user),"fresh SQL failure/lockout state after attempt "+attempt);
        }
        using var locked=await Send("/api/auth/login",new { email=lockEmail,password },401,"correct password cannot bypass current lockout");
        // Existing-token revocation on lockout is a separate policy question. Do
        // not invent stamp rotation or assert universal session revocation here.
        await Session(lockPair.access,200,"retained policy observation: password lockout alone does not revoke issued access");
        using (var scope=factory.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var user=(await users.FindByEmailAsync(lockEmail))!;
            // Advance ONLY the owned row's lockout end; no host clock or options override.
            if(!(await users.SetLockoutEndDateAsync(user,DateTimeOffset.UtcNow.AddMinutes(-1))).Succeeded)throw new InvalidOperationException("Disposable lockout-expiry setup failed.");
        }
        using var recovered=await Send("/api/auth/login",new { email=lockEmail,password },200,"correct native login after fixture lockout expiry");
        await Session((await Pair(recovered)).access,200,"post-expiry native access works");
        Check(original==await Originals(),"original identity/membership/token and selected domain/audit rows unchanged");
        Console.WriteLine("IDENTITYCHALLENGE SUMMARY "+JsonSerializer.Serialize(new { cases,requests,failedLimit,nativeIdentity=true,sql=true,policyChanged=false,physicalDevice=false,productionAccepted=false }));
    }

    // QA-only RFC6238 authenticator code from the native disposable shared key.
    private static string ChallengeTotp(string base32,DateTimeOffset now)
    {
        const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes=new List<byte>();var bits=0;var buffer=0;
        foreach(var character in base32.TrimEnd('=').ToUpperInvariant())
        {
            var digit=alphabet.IndexOf(character);if(digit<0)throw new InvalidOperationException("Invalid disposable authenticator key; value withheld.");
            buffer=(buffer<<5)|digit;bits+=5;if(bits>=8){bits-=8;bytes.Add((byte)(buffer>>bits));}
        }
        Span<byte> counter=stackalloc byte[8];BinaryPrimitives.WriteInt64BigEndian(counter,now.ToUnixTimeSeconds()/30);
        var digest=HMACSHA1.HashData(bytes.ToArray(),counter);var offset=digest[^1]&15;
        var value=BinaryPrimitives.ReadInt32BigEndian(digest.AsSpan(offset,4))&0x7fffffff;
        return (value%1_000_000).ToString("D6",CultureInfo.InvariantCulture);
    }
}
