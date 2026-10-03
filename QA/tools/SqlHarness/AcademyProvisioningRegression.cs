using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyAcademyProvisioningAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        var usersByActor = new Dictionary<string, Guid>(); var tokens = new Dictionary<string, string>();
        var modes = new[] { "academy-sql", "update-result", "update-sql", "assignment-result", "assignment-sql" };
        foreach (var actor in new[] { "first", "sparse", "full", "already-owner", "teacher-role", "guard", "inactive", "race" }.Concat(modes))
        {
            var email = "qa-academy-" + actor + "@example.invalid";
            using (var scope = factory.Services.CreateScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser { UserName = email, Email = email, DisplayName = "Synthetic original " + actor, PhoneNumber = "1234567890", ProfileImageUrl = "/synthetic.png", EmailConfirmed = true };
                if (!(await users.CreateAsync(user, "Synthetic!39Ab")).Succeeded) throw new InvalidOperationException("Self-service actor fixture failed");
                usersByActor[actor] = user.Id;
                if (actor == "teacher-role" && !(await users.AddToRoleAsync(user, "Teacher")).Succeeded) throw new InvalidOperationException("Existing Teacher fixture failed");
            }
            tokens[actor] = await LoginAsync(client, email, "Synthetic!39Ab");
        }
        tokens["assigned-admin"] = await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab");
        client.DefaultRequestHeaders.Authorization = null;
        var fault = new AcademyProvisioningFault();
        using var faultFactory = new QaApiFactory(manifest, isolatedIdentityInterceptor: fault);
        using var injected = faultFactory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            if (!faultFactory.PreflightPassed) throw new InvalidOperationException("Refused self-service injection before owned SQL preflight");
            services.ConfigureDbContext<AcademyDeskDbContext>(options => options.AddInterceptors(fault));
            services.AddScoped<IUserValidator<ApplicationUser>>(sp => new AcademyProvisioningUserValidator(fault, sp.GetRequiredService<IdentityDbContext>(), sp.GetRequiredService<AcademyDeskDbContext>()));
            services.AddSingleton<IRoleValidator<ApplicationRole>>(new AcademyProvisioningRoleValidator(fault));
        }));
        using var http = injected.CreateClient(new() { AllowAutoRedirect = false }); int count = 0;
        void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(); var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { academies = await db.Academies.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), teachers = await db.Teachers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), guardians = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), platformAudits = await db.PlatformAuditEntries.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), roles = await identity.Roles.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), memberships = await identity.UserRoles.AsNoTracking().OrderBy(x => x.UserId).ThenBy(x => x.RoleId).ToListAsync(), grants = await identity.AccessGrants.AsNoTracking().OrderBy(x => x.Id).ToListAsync(), finance = await AccessFinancialSnapshotAsync(factory) });
        }
        Dictionary<string, object?> Body(string label) => new() { ["name"] = "Synthetic self-service " + label };
        (Guid Academy, int Roles, int Memberships) Success(string label, string before, string after, Dictionary<string, object?> body, Guid userId, string response)
        {
            using var oldDoc = JsonDocument.Parse(before); using var savedDoc = JsonDocument.Parse(after); using var outputDoc = JsonDocument.Parse(response); var old = oldDoc.RootElement; var saved = savedDoc.RootElement; var output = outputDoc.RootElement;
            var id = output.GetProperty("id").GetGuid(); var roleDelta = saved.GetProperty("roles").GetArrayLength() - old.GetProperty("roles").GetArrayLength(); var membershipDelta = saved.GetProperty("memberships").GetArrayLength() - old.GetProperty("memberships").GetArrayLength();
            Check(roleDelta is 0 or 1 && membershipDelta is 0 or 1, label + ": role/membership delta mismatch");
            foreach (var field in old.EnumerateObject())
            {
                if (!new[] { "academies", "users", "roles", "memberships" }.Contains(field.Name)) Check(field.Value.GetRawText() == saved.GetProperty(field.Name).GetRawText(), label + ": unrelated captured state changed: " + field.Name);
                else
                {
                    string Key(JsonElement row) => field.Name == "memberships" ? row.GetProperty("UserId").GetString() + ":" + row.GetProperty("RoleId").GetString() : row.GetProperty("Id").GetString()!;
                    var original = field.Value.EnumerateArray().ToDictionary(Key); var now = saved.GetProperty(field.Name).EnumerateArray().ToDictionary(Key);
                    var delta = field.Name switch { "academies" => 1, "roles" => roleDelta, "memberships" => membershipDelta, _ => 0 };
                    Check(now.Count == original.Count + delta, label + ": captured row count mismatch: " + field.Name);
                    foreach (var row in original)
                    {
                        Check(now.ContainsKey(row.Key), label + ": row removed: " + field.Name);
                        if (field.Name == "users" && row.Key == userId.ToString())
                        {
                            foreach (var column in row.Value.EnumerateObject()) if (!new[] { "AcademyId", "DisplayName", "ConcurrencyStamp" }.Contains(column.Name)) Check(column.Value.GetRawText() == now[row.Key].GetProperty(column.Name).GetRawText(), label + ": unrelated creator field changed: " + column.Name);
                        }
                        else Check(row.Value.GetRawText() == now[row.Key].GetRawText(), label + ": original row changed: " + field.Name);
                    }
                }
            }
            var academy = saved.GetProperty("academies").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == id); var user = saved.GetProperty("users").EnumerateArray().Single(x => x.GetProperty("Id").GetGuid() == userId);
            var name = body["name"]!.ToString()!.Trim(); var legal = body.GetValueOrDefault("legalName")?.ToString(); var country = body.GetValueOrDefault("countryCode")?.ToString();
            Check(academy.GetProperty("Name").GetString() == name && academy.GetProperty("LegalName").GetString() == (string.IsNullOrWhiteSpace(legal) ? null : legal.Trim()) && academy.GetProperty("CountryCode").GetString() == (string.IsNullOrWhiteSpace(country) ? "IN" : country.Trim().ToUpperInvariant()) && academy.GetProperty("TimeZone").GetString() == "Asia/Kolkata", label + ": optional/default normalization mismatch");
            Check(academy.GetProperty("IsActive").GetBoolean() && academy.GetProperty("SubscriptionPlan").GetString() == "Trial" && academy.GetProperty("SubscriptionStatus").GetString() == "Trial" && academy.GetProperty("SubscriptionEndsAtUtc").ValueKind == JsonValueKind.Null && academy.GetProperty("StudentLimit").GetInt32() == 100 && academy.GetProperty("StaffLimit").GetInt32() == 10 && academy.GetProperty("EnabledModulesJson").GetString() == "[\"Core\"]", label + ": preserved self-service defaults changed");
            Check(user.GetProperty("AcademyId").GetGuid() == id && user.GetProperty("DisplayName").GetString() == name, label + ": creator association mismatch");
            foreach (var field in output.EnumerateObject()) { var column = char.ToUpperInvariant(field.Name[0]) + field.Name[1..]; var persisted = academy.GetProperty(column); Check(field.Value.ValueKind == JsonValueKind.String ? field.Value.GetString() == persisted.GetString() : field.Value.GetRawText() == persisted.GetRawText(), label + ": response/readback mismatch: " + field.Name); }
            var ownerRole = saved.GetProperty("roles").EnumerateArray().Single(x => x.GetProperty("Name").GetString() == "Owner").GetProperty("Id").GetGuid();
            Check(saved.GetProperty("memberships").EnumerateArray().Count(x => x.GetProperty("UserId").GetGuid() == userId && x.GetProperty("RoleId").GetGuid() == ownerRole) == 1, label + ": Owner membership missing/duplicate");
            return (id, roleDelta, membershipDelta);
        }
        async Task<(HttpStatusCode Status, string Text)> Send(string? actor, Dictionary<string, object?> body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/academies"); if (actor is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]); request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await http.SendAsync(request); return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        async Task Request(string label, string? actor, Dictionary<string, object?> body, HttpStatusCode expected, string mode = "none")
        {
            await Task.Delay(250); var before = await Snapshot(); using (var owned = await LinkedOwnedSqlAsync(manifest)) { }
            fault.Arm(mode, actor is null ? "" : "qa-academy-" + actor + "@example.invalid", body.GetValueOrDefault("name")?.ToString()?.Trim() ?? "");
            (HttpStatusCode Status, string Text) result;
            try { result = await Send(actor, body); } finally { fault.Disarm(); }
            var after = await Snapshot(); Check(result.Status == expected, label + ": unexpected status " + (int)result.Status); Check(fault.Faults == (mode == "none" ? 0 : 1), label + ": requested fault did not execute exactly once");
            if (mode.EndsWith("-result")) Check(result.Text.Contains("No changes were saved."), label + ": failure rollback message missing");
            if (mode.EndsWith("-sql")) Check(JsonDocument.Parse(result.Text).RootElement.GetProperty("message").GetString() == "An unexpected server error occurred.", label + ": exception details leaked");
            if (mode == "update-result") Check(fault.AcademyPersisted && fault.OriginalUserUnassigned, label + ": update failure stage not verified");
            if (mode == "assignment-result") Check(fault.AcademyPersisted && fault.UserAssociationPersisted && fault.PendingMembership, label + ": assignment failure stage not verified");
            Guid? newAcademy = null; int roleDelta = 0, membershipDelta = 0;
            if (expected == HttpStatusCode.Created) { var created = Success(label, before, after, body, usersByActor[actor!], result.Text); newAcademy = created.Academy; roleDelta = created.Roles; membershipDelta = created.Memberships; }
            else Check(before == after, label + ": rejected request left captured academy/Identity/finance changes");
            Console.WriteLine("ACADEMYPROVISION EVIDENCE " + JsonSerializer.Serialize(new { label, actor, mode, status = (int)result.Status, response = result.Text, newAcademy, roleDelta, membershipDelta, fault.Faults, fault.AcademyPersisted, fault.OriginalUserUnassigned, fault.UserAssociationPersisted, fault.PendingMembership, beforeDigest = Hash(before), afterDigest = Hash(after), bodyDigest = Hash(result.Text), exactSnapshotVerified = true }));
            count++; Console.WriteLine("ACADEMYPROVISION CASE " + label + " PASS.");
        }
        foreach (var mode in new[] { "role-result", "role-sql", "academy-sql", "update-result", "update-sql", "assignment-result", "assignment-sql" }) await Request("missing-" + mode, "first", Body("first"), mode.EndsWith("-sql") || mode == "role-result" ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode);
        await Request("missing-role-retry", "first", Body("first"), HttpStatusCode.Created);
        foreach (var mode in modes) { await Request("existing-" + mode, mode, Body(mode), mode.EndsWith("-sql") ? HttpStatusCode.InternalServerError : HttpStatusCode.BadRequest, mode); await Request("existing-" + mode + "-retry", mode, Body(mode), HttpStatusCode.Created); }
        var sparse = Body("sparse"); sparse["legalName"] = null; sparse["countryCode"] = null; sparse["timeZone"] = null; await Request("optional-null", "sparse", sparse, HttpStatusCode.Created);
        var full = new Dictionary<string, object?> { ["name"] = "  Synthetic populated  ", ["legalName"] = "  Synthetic legal  ", ["countryCode"] = " us ", ["timeZone"] = "Europe/London" }; await Request("optional-populated", "full", full, HttpStatusCode.Created);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); if (!(await users.AddToRoleAsync((await users.FindByIdAsync(usersByActor["already-owner"].ToString()))!, "Owner")).Succeeded) throw new InvalidOperationException("Preexisting Owner fixture failed"); }
        await Request("already-owner-membership", "already-owner", Body("already-owner"), HttpStatusCode.Created); await Request("existing-teacher-role", "teacher-role", Body("teacher-role"), HttpStatusCode.Created);
        await Request("assigned-replay", "first", Body("cannot-create-again"), HttpStatusCode.Conflict); await Request("assigned-admin", "assigned-admin", Body("cannot-create-admin"), HttpStatusCode.Conflict);
        foreach (var (label, value) in new[] { ("blank-name", (string?)" "), ("null-name", null) }) await Request(label, "guard", new() { ["name"] = value }, HttpStatusCode.BadRequest);
        await Request("omitted-name", "guard", new(), HttpStatusCode.BadRequest); await Request("anonymous", null, Body("anonymous"), HttpStatusCode.Unauthorized);
        using (var scope = factory.Services.CreateScope()) { var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByIdAsync(usersByActor["inactive"].ToString()))!; user.IsActive = false; if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Inactive fixture failed"); }
        await Request("inactive-issued-token", "inactive", Body("inactive"), HttpStatusCode.Forbidden);

        // Real concurrent requests are held immediately before their first native
        // Identity UPDATE. Both carry the same original concurrency stamp; no mocked store.
        var raceBefore = await Snapshot(); using (var owned = await LinkedOwnedSqlAsync(manifest)) { }
        using (var scope = factory.Services.CreateScope()) fault.RaceOriginalStamp = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(usersByActor["race"].ToString()))!.ConcurrencyStamp!;
        fault.Arm("race", "qa-academy-race@example.invalid", ""); var raceBodies = new[] { Body("race-A"), Body("race-B") };
        (HttpStatusCode Status, string Text)[] outcomes;
        try { outcomes = await Task.WhenAll(Send("race", raceBodies[0]), Send("race", raceBodies[1])).WaitAsync(TimeSpan.FromSeconds(30)); } finally { fault.Disarm(); }
        var raceAfter = await Snapshot(); Check(fault.RaceArrivals == 2 && outcomes.Count(x => x.Status == HttpStatusCode.Created) == 1 && outcomes.Count(x => x.Status == HttpStatusCode.BadRequest) == 1, "Same-user concurrent creation did not produce one commit/one concurrency rejection");
        var winner = Array.FindIndex(outcomes, x => x.Status == HttpStatusCode.Created); var raceCreated = Success("same-user-race", raceBefore, raceAfter, raceBodies[winner], usersByActor["race"], outcomes[winner].Text); Check(raceCreated.Roles == 0 && raceCreated.Memberships == 1 && outcomes[1-winner].Text.Contains("No changes were saved."), "Concurrent creator rollback/membership mismatch");
        Console.WriteLine("ACADEMYPROVISION RACE " + JsonSerializer.Serialize(new { statuses = outcomes.Select(x => (int)x.Status).ToArray(), fault.RaceArrivals, sameOriginalStampVerified = true, newAcademy = raceCreated.Academy, academyAdditions = 1, userAdditions = 0, membershipAdditions = 1, noOrphanVerified = true, beforeDigest = Hash(raceBefore), afterDigest = Hash(raceAfter) }));
        await Request("race-replay", "race", Body("race-again"), HttpStatusCode.Conflict);
        foreach (var actor in new[] { "first", "sparse", "full", "race" })
        {
            var before = await Snapshot(); http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens[actor]); using var response = await http.GetAsync("/api/academies"); using var output = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            using var scope = factory.Services.CreateScope(); var user = (await scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().FindByIdAsync(usersByActor[actor].ToString()))!;
            Check(response.StatusCode == HttpStatusCode.OK && output.RootElement.GetArrayLength() == 1 && output.RootElement[0].GetProperty("id").GetGuid() == user.AcademyId && before == await Snapshot(), actor + ": original-token exact own academy read failed/changed data"); Console.WriteLine("ACADEMYPROVISION ACCESS " + actor + " PASS: original token GET200 exact academyId; captured snapshot unchanged.");
        }
        http.DefaultRequestHeaders.Authorization = null;
        Console.WriteLine($"ACADEMYPROVISION REGRESSION PASS: {count} sequential real Identity/HTTP-SQL cases plus one controlled pair(two requests); exact existing-user reassignment, checked role/user results, rollback/retry, preexisting roles/defaults, one-commit concurrent creation; four read controls separate. Accepted source baseline only; role-initialization races/browser/full-critical OPEN.");
    }

    private sealed class AcademyProvisioningFault : DbCommandInterceptor
    {
        public string Mode { get; private set; } = "none"; public string Email { get; private set; } = ""; private string academyName = "";
        public int Faults { get; private set; } public bool AcademyPersisted { get; set; } public bool OriginalUserUnassigned { get; set; } public bool UserAssociationPersisted { get; set; } public bool PendingMembership { get; set; }
        public string RaceOriginalStamp { get; set; } = ""; private int raceArrivals; public int RaceArrivals => raceArrivals; private TaskCompletionSource raceGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm(string mode, string email, string name) { if (Mode != "none") throw new InvalidOperationException("Self-service fault already armed"); Mode = mode; Email = email; academyName = name; Faults = 0; AcademyPersisted = OriginalUserUnassigned = UserAssociationPersisted = PendingMembership = false; raceArrivals = 0; raceGate = new(TaskCreationOptions.RunContinuationsAsynchronously); }
        public void Disarm() => Mode = "none"; public bool Trip(string mode) { if (Mode != mode) return false; Faults++; return true; }
        private async Task ObserveAsync(DbCommand command, CancellationToken token)
        {
            bool Has(object value) => command.Parameters.Cast<DbParameter>().Any(x => Equals(x.Value, value));
            if (Mode == "race" && command.CommandText.Contains("UPDATE [AspNetUsers]", StringComparison.Ordinal) && Has(Email) && Has(RaceOriginalStamp)) { if (Interlocked.Increment(ref raceArrivals) == 2) raceGate.TrySetResult(); await raceGate.Task.WaitAsync(TimeSpan.FromSeconds(15), token); return; }
            var table = Mode switch { "role-sql" => "INSERT INTO [AspNetRoles]", "academy-sql" => "INSERT INTO [Academies]", "update-sql" => "UPDATE [AspNetUsers]", "assignment-sql" => "INSERT INTO [AspNetUserRoles]", _ => null };
            if (table is null || !command.CommandText.Contains(table, StringComparison.Ordinal) || (Mode == "academy-sql" && !Has(academyName)) || (Mode == "update-sql" && !Has(Email))) return;
            Faults++; command.CommandText = "THROW 51004, 'Synthetic academy provisioning store fault', 1;\n" + command.CommandText;
        }
        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) { await ObserveAsync(command, cancellationToken); return result; }
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) { await ObserveAsync(command, cancellationToken); return result; }
    }
    private sealed class AcademyProvisioningRoleValidator(AcademyProvisioningFault fault) : IRoleValidator<ApplicationRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<ApplicationRole> manager, ApplicationRole role) => Task.FromResult(role.Name == "Owner" && fault.Trip("role-result") ? IdentityResult.Failed(new IdentityError { Code = "SyntheticOwnerRoleFailure", Description = "Synthetic role validation failure" }) : IdentityResult.Success);
    }
    private sealed class AcademyProvisioningUserValidator(AcademyProvisioningFault fault, IdentityDbContext db, AcademyDeskDbContext academyDb) : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
        {
            if (user.Email != fault.Email || fault.Mode is not ("update-result" or "assignment-result")) return IdentityResult.Success;
            var pending = db.ChangeTracker.Entries<IdentityUserRole<Guid>>().Any(x => x.Entity.UserId == user.Id && x.State == EntityState.Added);
            if ((fault.Mode == "assignment-result") != pending) return IdentityResult.Success;
            var persisted = await db.Users.AsNoTracking().SingleAsync(x => x.Id == user.Id); fault.AcademyPersisted = await academyDb.Academies.AsNoTracking().AnyAsync(x => x.Id == user.AcademyId); fault.OriginalUserUnassigned = persisted.AcademyId is null; fault.UserAssociationPersisted = persisted.AcademyId == user.AcademyId && user.AcademyId is not null; fault.PendingMembership = pending;
            fault.Trip(fault.Mode); return IdentityResult.Failed(new IdentityError { Code = "SyntheticOwnerAccountFailure", Description = "Synthetic account validation failure" });
        }
    }
}
