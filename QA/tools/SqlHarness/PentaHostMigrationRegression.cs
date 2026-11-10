using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaHostMigrationAsync(QaRunManifest manifest)
    {
        var container = "academydesk-qa-" + manifest.RunId.ToString("N");
        var connection = Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!);
        async Task Ownership()
        {
            await VerifyDockerTargetAsync(container, manifest);
            await using var sql = new SqlConnection(connection); await sql.OpenAsync();
            var marker = await ReadMarkerAsync(sql, manifest);
            QaRunGuard.Cleanup(manifest, marker, _ => true);
        }
        AcademyDeskDbContext Context() => new(new DbContextOptionsBuilder<AcademyDeskDbContext>().UseSqlServer(connection).Options);
        async Task<int> TableCount()
        {
            await using var sql = new SqlConnection(connection); await sql.OpenAsync();
            return Convert.ToInt32(await ScalarAsync(sql, "SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name LIKE N'Penta%'"));
        }
        static void Same(Dictionary<string, string> before, Dictionary<string, string> after, bool existingOnly = false)
        {
            var a = before.Where(x => !existingOnly || !x.Key.StartsWith("dbo.Penta", StringComparison.Ordinal) && !x.Key.Contains("__EFMigrationsHistory", StringComparison.Ordinal)).ToDictionary(x => x.Key, x => x.Value);
            var b = after.Where(x => !existingOnly || !x.Key.StartsWith("dbo.Penta", StringComparison.Ordinal) && !x.Key.Contains("__EFMigrationsHistory", StringComparison.Ordinal)).ToDictionary(x => x.Key, x => x.Value);
            if (a.Count != b.Count || a.Any(x => !b.TryGetValue(x.Key, out var digest) || digest != x.Value))
                throw new InvalidOperationException("PENTA migration changed an unexpected table/schema fingerprint.");
        }
        await Ownership();
        await using var db = Context();
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>().UseSqlServer(connection).Options);
        var appIds = db.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
        var identityIds = identity.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
        if (appIds.Count != 86 || identityIds.Count != 7 || appIds.Overlaps(identityIds))
            throw new InvalidOperationException("Unexpected combined application/Identity migration declarations.");
        async Task<string[]> AppliedApp()
        {
            // Both contexts intentionally use the same history table. Partition
            // by their declared migration IDs; reject unknown/missing Identity
            // rows rather than silently ignoring them during application rollback.
            var all = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            if (!all.Where(x => !appIds.Contains(x)).ToHashSet(StringComparer.Ordinal).SetEquals(identityIds))
                throw new InvalidOperationException("PENTA migration changed or encountered unexpected Identity history.");
            return all.Where(appIds.Contains).ToArray();
        }
        var latest = await AppliedApp();
        if (latest.Length != 86 || !latest[^1].EndsWith("_TrackClassSessionTeacherInheritance", StringComparison.Ordinal))
            throw new InvalidOperationException("Unexpected migration after the protected PENTA checkpoint.");
        var latestFingerprint = await RecoveryFingerprintAsync(connection);
        await db.GetService<IMigrator>().MigrateAsync(latest[^2]);
        var applied = await AppliedApp();
        if (applied.Length != 85 || !applied[^1].EndsWith("_AddCombinedPentaHostPersistence", StringComparison.Ordinal) || await TableCount() != 12)
            throw new InvalidOperationException("Combined PENTA migration chain/table count is not the expected cloud84plusone.");
        var baseline = await RecoveryFingerprintAsync(connection);
        Console.WriteLine("PASS: PENTAMIGRATION cloud84-to85 additive upgrade;12actual PENTA tables present.");

        var academy = await db.Academies.AsNoTracking().Where(x => x.Name == "Synthetic Academy A").Select(x => x.Id).SingleAsync();
        var row = new PentaMiniSession { AcademyId = academy, ActorUserId = Guid.NewGuid(), Version = 0,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10), PendingRequestId = Guid.NewGuid(), ProtectedState = "Synthetic migration guard only; no customer transcript or model state." };
        db.PentaMiniSessions.Add(row); await db.SaveChangesAsync();
        var nonempty = await RecoveryFingerprintAsync(connection);
        await Ownership();
        var blocked = false;
        try { await db.GetService<IMigrator>().MigrateAsync(applied[^2]); }
        catch (SqlException exception) when (exception.Number == 51028) { blocked = true; }
        if (!blocked || !(await AppliedApp()).SequenceEqual(applied) || await TableCount() != 12)
            throw new InvalidOperationException("Nonempty PENTA downgrade did not fail closed with unchanged history/schema.");
        Same(nonempty, await RecoveryFingerprintAsync(connection));
        Console.WriteLine("PASS: PENTAMIGRATION pending conversation blocks downgrade; all rows/schema/history unchanged.");

        // Remove only this method's run-owned synthetic fixture, never domain or
        // customer state. An existing application database is never accepted.
        await using (var cleanup = Context())
        {
            var own = await cleanup.PentaMiniSessions.SingleAsync(x => x.Id == row.Id && x.AcademyId == academy);
            cleanup.PentaMiniSessions.Remove(own); await cleanup.SaveChangesAsync();
        }
        Same(baseline, await RecoveryFingerprintAsync(connection));
        await Ownership();
        await db.GetService<IMigrator>().MigrateAsync(applied[^2]);
        if (await TableCount() != 0 || !(await AppliedApp()).SequenceEqual(applied.Take(applied.Length - 1)))
            throw new InvalidOperationException("Empty PENTA downgrade did not restore the exact prior cloud migration chain.");
        Same(baseline, await RecoveryFingerprintAsync(connection), existingOnly: true);
        Console.WriteLine("PASS: PENTAMIGRATION empty downgrade restores cloud84; existing domain/Identity/ownership rows and columns preserved.");

        await Ownership();
        await db.GetService<IMigrator>().MigrateAsync(applied[^1]);
        if (await TableCount() != 12 || !(await AppliedApp()).SequenceEqual(applied))
            throw new InvalidOperationException("PENTA re-upgrade did not restore exact combined migration history.");
        Same(baseline, await RecoveryFingerprintAsync(connection));
        Console.WriteLine("PASS: PENTAMIGRATION re-upgrade restores12empty PENTA tables and exact full table/schema fingerprints.");
        await Ownership();
        await db.GetService<IMigrator>().MigrateAsync(latest[^1]);
        if (!(await AppliedApp()).SequenceEqual(latest))
            throw new InvalidOperationException("Current additive migration was not restored.");
        Same(latestFingerprint, await RecoveryFingerprintAsync(connection));
        Console.WriteLine("PASS: PENTAMIGRATION all4real owned SQL upgrade/downgrade/guard checks; legacy local PENTA history and Azure migration remain unaccepted.");
    }
}
