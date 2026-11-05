using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static partial class SqlHarnessEntryPoint
{
    private sealed class OnboardingDraftClock : TimeProvider
    {
        public DateTimeOffset Value { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Value;
    }
    private static async Task VerifyPentaOnboardingDraftsAsync(QaApiFactory factory, QaRunManifest manifest)
    {
        Guid academy, foreign, actor, teacher, session;
        var clock = new OnboardingDraftClock();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            academy = await db.Academies.Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
            foreign = await db.Academies.Where(x => x.Name == "Synthetic Academy B").Select(x => x.Id).SingleAsync();
            actor = await identity.Users.Where(x => x.Email == "qa-admin-a@example.invalid").Select(x => x.Id).SingleAsync();
            teacher = await identity.Users.Where(x => x.Email == "qa-teacher-a@example.invalid").Select(x => x.Id).SingleAsync();
            var row = new PentaMiniSession { AcademyId = academy, ActorUserId = actor, Version = 1,
                ExpiresAtUtc = clock.Value.UtcDateTime.AddHours(1), ProtectedState = "Synthetic fixture; no planner call" };
            db.PentaMiniSessions.Add(row); await db.SaveChangesAsync(); session = row.Id;
        }
        var principal = Principal(actor);
        var otherAdmin = await CreateAccessActorAsync(factory, "qa-draft-other-admin@example.invalid", "AcademyAdmin", academy);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Penta:OperationalDraftContractsEnabled"] = "true" }).Build();
        ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "SyntheticHost"));
        async Task<PentaOnboardingDraftOutcome> Call(string action, Guid id, long revision = 0, string? payload = null,
            string? hash = null, ClaimsPrincipal? user = null, Guid? tenant = null, long context = 1, QaApiFactory? host = null, Guid? conversation = null)
        {
            using var scope = (host ?? factory).Services.CreateScope();
            var service = new PentaOnboardingDraftService(scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>(),
                scope.ServiceProvider.GetRequiredService<IdentityDbContext>(), scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>(),
                clock, config, scope.ServiceProvider.GetRequiredService<IHostEnvironment>());
            return action switch {
                "save" => await service.SaveAsync(user ?? principal, tenant ?? academy, conversation ?? session, id, revision, context, payload!, default),
                "confirm" => await service.ConfirmAsync(user ?? principal, tenant ?? academy, conversation ?? session, id, revision, hash!, default),
                _ => await service.GetAsync(user ?? principal, tenant ?? academy, conversation ?? session, id, default)
            };
        }
        string Payload(string first = "Synthetic", string dob = "2000-01-01", bool partial = false) => JsonSerializer.Serialize(new {
            tool_key = PentaOnboardingDraftService.Tool, tool_revision = 1,
            fields = partial ? new Dictionary<string, object> { ["StudentFirstName"] = new { kind = "text", value = first } } :
                new Dictionary<string, object> { ["StudentFirstName"] = new { kind = "text", value = first },
                    ["StudentLastName"] = new { kind = "text", value = "Draft only" }, ["DateOfBirth"] = new { kind = "date", value = dob } }
        });
        async Task<string> Snapshot()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            return JsonSerializer.Serialize(new { drafts = await db.PentaOnboardingDrafts.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                audits = await db.AuditLogs.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync() });
        }
        var count = 0;
        void Check(string label, PentaOnboardingDraftOutcome result, int status)
        {
            if (result.HttpStatus != status || result.Draft?.CanExecute == true)
                throw new InvalidOperationException("Operational draft case failed: " + label + "; expected=" + status + " actual=" + result.HttpStatus);
            count++; Console.WriteLine("PASS: PENTAONBOARDINGDRAFT " + label + " status=" + status + "; no domain dispatch");
        }
        async Task Denial(string label, Task<PentaOnboardingDraftOutcome> result, int status, string before)
        {
            Check(label, await result, status);
            if (before != await Snapshot()) throw new InvalidOperationException("Denied draft call changed SQL/audit: " + label);
        }
        var domainBefore = "";
        async Task<string> DomainSnapshot()
        {
            using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            return JsonSerializer.Serialize(new { students = await db.Students.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                parents = await db.Guardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                links = await db.StudentGuardians.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(),
                users = await identity.Users.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.AcademyId, x.UserName }).ToArrayAsync() });
        }
        domainBefore = await DomainSnapshot();
        var id = Guid.NewGuid();
        var partial = await Call("save", id, payload: Payload(partial: true)); Check("create partial", partial, 201);
        var before = await Snapshot();
        await Denial("partial confirmation denied", Call("confirm", id, 1, hash: partial.Draft!.PayloadHash), 400, before);
        Check("identical save replay", await Call("save", id, payload: Payload(partial: true)), 200);
        if (before != await Snapshot()) throw new InvalidOperationException("Identical replay added a row or audit.");
        var complete = await Call("save", id, 1, Payload()); Check("complete revision2", complete, 201);
        var confirmed = await Call("confirm", id, 2, hash: complete.Draft!.PayloadHash); Check("confirm exact revision", confirmed, 200);
        if (confirmed.Draft?.Status != "Confirmed") throw new InvalidOperationException("Review not recorded.");
        before = await Snapshot();
        Check("confirmation replay", await Call("confirm", id, 2, hash: complete.Draft.PayloadHash), 200);
        if (before != await Snapshot()) throw new InvalidOperationException("Confirmation replay changed SQL.");
        var edit = await Call("save", id, 2, Payload("Correction")); Check("edit invalidates confirmation", edit, 201);
        if (edit.Draft?.Revision != 3 || edit.Draft.Status != "AwaitingReview") throw new InvalidOperationException("Stale confirmation survived edit.");
        before = await Snapshot();
        await Denial("old approval replay", Call("confirm", id, 2, hash: complete.Draft.PayloadHash), 409, before);
        await Denial("wrong hash", Call("confirm", id, 3, hash: new string('a', 64)), 409, before);
        await Denial("stale edit", Call("save", id, 1, Payload("Stale")), 409, before);
        await Denial("teacher", Call("get", id, user: Principal(teacher)), 403, before);
        await Denial("anonymous", Call("get", id, user: new ClaimsPrincipal()), 403, before);
        await Denial("foreign tenant", Call("get", id, tenant: foreign), 403, before);
        await Denial("other admin cannot read actor draft", Call("get", id, user: Principal(otherAdmin)), 404, before);
        await Denial("wrong conversation", Call("get", id, conversation: Guid.NewGuid()), 404, before);
        var forgedTeacher = Principal(teacher);
        ((ClaimsIdentity)forgedTeacher.Identity!).AddClaim(new Claim(ClaimTypes.Role, "Owner"));
        await Denial("forged role claim", Call("get", id, user: forgedTeacher), 403, before);
        var invalid = Payload().Replace("StudentFirstName", "StudentTemporaryPassword", StringComparison.Ordinal);
        await Denial("credential field rejected", Call("save", Guid.NewGuid(), payload: invalid), 400, before);
        await Denial("unknown tool", Call("save", Guid.NewGuid(), payload: Payload().Replace(PentaOnboardingDraftService.Tool, "RunSql", StringComparison.Ordinal)), 400, before);
        await Denial("malformed typed call", Call("save", Guid.NewGuid(), payload: Payload().Replace("\"kind\":\"date\"", "\"kind\":\"text\"", StringComparison.Ordinal)), 400, before);
        var minorId = Guid.NewGuid(); var minor = await Call("save", minorId, payload: Payload(dob: "2015-01-01"));
        Check("minor draft", minor, 201); before = await Snapshot();
        await Denial("minor missing guardian", Call("confirm", minorId, 1, hash: minor.Draft!.PayloadHash), 400, before);

        // Fresh service/scope proves encrypted durable state survives composition restart.
        var reloaded = await Call("get", id); Check("fresh scope reload", reloaded, 200);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaOnboardingDrafts.SingleAsync(x => x.Id == id);
            if (row.ProtectedBody.Contains("Correction", StringComparison.Ordinal) || row.ProtectedBody.Contains(edit.Draft!.PayloadHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Private draft payload/hash stored without encryption.");
            row.Status = "Confirmed"; await db.SaveChangesAsync();
        }
        before = await Snapshot();
        await Denial("metadata tamper", Call("get", id), 409, before);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaOnboardingDrafts.SingleAsync(x => x.Id == id); row.Status = "AwaitingReview"; await db.SaveChangesAsync();
        }
        // Parallel distinct replacements of revision3: one wins; the stale loser cannot confirm or overwrite it.
        var races = await Task.WhenAll(Call("save", id, 3, Payload("Race A")), Call("save", id, 3, Payload("Race B")));
        if (!races.Select(x => x.HttpStatus).Order().SequenceEqual(new[] { 201, 409 })) throw new InvalidOperationException("Draft CAS race did not produce 201/409.");
        Check("parallel revision CAS", races.Single(x => x.HttpStatus == 201), 201);
        var winner = races.Single(x => x.HttpStatus == 201).Draft!;
        var confirmationFault = new OnboardingDraftAuditFault();
        using (var faulty = new QaApiFactory(manifest, isolatedDomainInterceptor: confirmationFault))
        {
            before = await Snapshot(); var threw = false;
            try { await Call("confirm", id, 4, hash: winner.PayloadHash, host: faulty); }
            catch (DbUpdateException) { threw = true; }
            if (!threw || confirmationFault.Faults != 1 || before != await Snapshot()) throw new InvalidOperationException("Confirmation audit failure persisted confirmation.");
            count++; Console.WriteLine("PASS: PENTAONBOARDINGDRAFT required confirmation audit rollback");
        }
        var approvals = await Task.WhenAll(Call("confirm", id, 4, hash: winner.PayloadHash), Call("confirm", id, 4, hash: winner.PayloadHash));
        if (approvals.Any(x => x.HttpStatus != 200)) throw new InvalidOperationException("Confirmation idempotency race failed.");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            if (await db.AuditLogs.CountAsync(x => x.EntityId == id && x.Action == "PentaOnboardingDraftConfirmed") != 2)
                throw new InvalidOperationException("Parallel confirmation added duplicate review audit.");
        }
        Check("parallel confirmation one transition", approvals[0], 200);

        // Required audit failure rolls back the new draft as well as its review metadata.
        var fault = new OnboardingDraftAuditFault();
        using (var faulty = new QaApiFactory(manifest, isolatedDomainInterceptor: fault))
        {
            before = await Snapshot(); var threw = false;
            try { await Call("save", Guid.NewGuid(), payload: Payload(), host: faulty); }
            catch (DbUpdateException) { threw = true; }
            if (!threw || fault.Faults != 1 || before != await Snapshot()) throw new InvalidOperationException("Draft/audit failure did not roll back.");
            count++; Console.WriteLine("PASS: PENTAONBOARDINGDRAFT required audit failure rolls back draft");
        }
        // Revoked rights cannot be rescued by authenticated old principal or stored review.
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user = await identity.Users.SingleAsync(x => x.Id == actor); user.IsActive = false; await identity.SaveChangesAsync();
        }
        before = await Snapshot();
        await Denial("revoked current actor", Call("get", id), 403, before);
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var user = await identity.Users.SingleAsync(x => x.Id == actor); user.IsActive = true; user.SecurityStamp = Guid.NewGuid().ToString(); await identity.SaveChangesAsync();
        }
        before = await Snapshot();
        await Denial("changed security stamp", Call("get", id), 409, before);
        var expiryId = Guid.NewGuid(); var expiry = await Call("save", expiryId, payload: Payload()); Check("fresh authority new draft", expiry, 201);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaMiniSessions.SingleAsync(x => x.Id == session); row.Version++; await db.SaveChangesAsync();
        }
        before = await Snapshot();
        await Denial("stale conversation", Call("get", expiryId), 409, before);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();
            var row = await db.PentaMiniSessions.SingleAsync(x => x.Id == session); row.Version--; await db.SaveChangesAsync();
        }
        clock.Value = clock.Value.AddMinutes(11); before = await Snapshot();
        await Denial("expired confirmation", Call("confirm", expiryId, 1, hash: expiry.Draft!.PayloadHash), 410, before);
        if (domainBefore != await DomainSnapshot()) throw new InvalidOperationException("Draft/review changed onboarding domain or accounts.");
        if (count != 32) throw new InvalidOperationException("Incomplete operational onboarding draft SQL matrix: " + count);
        Console.WriteLine("PASS: PENTAONBOARDINGDRAFT all " + count + " direct real SQL cases; encrypted actor/session binding, current authority, CAS, replay and audit rollback; zero domain writes.");
    }
    private sealed class OnboardingDraftAuditFault : DbCommandInterceptor
    {
        public int Faults { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("INSERT INTO [AuditLogs]", StringComparison.Ordinal))
            { Faults++; command.CommandText = "THROW 51009, 'Synthetic draft audit fault', 1;\n" + command.CommandText; }
            return ValueTask.FromResult(result);
        }
    }
}
