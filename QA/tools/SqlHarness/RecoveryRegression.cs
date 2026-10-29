using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.Data.SqlClient;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyRecoveryAsync(QaApiFactory factory, HttpClient client, QaRunManifest manifest)
    {
        // Seed real domain/Identity/ledger/audit rows through the existing HTTP
        // regression. Only this run-created, marker-owned container DB is used.
        await VerifyReconciledBalanceAsync(factory, client, enforceRegression: true);
        var container = "academydesk-qa-" + manifest.RunId.ToString("N");
        await VerifyDockerTargetAsync(container, manifest);
        var admin = Connection(manifest.SqlServer, manifest.Database, "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!);
        var master = Connection(manifest.SqlServer, "master", "sa", Environment.GetEnvironmentVariable("QA_SQL_SA_PASSWORD")!);
        var backup = "/var/opt/mssql/data/qa-recovery-" + manifest.RunId.ToString("N") + ".bak";
        QaOwnershipMarker marker;
        await using (var db = new SqlConnection(admin))
        {
            await db.OpenAsync(); marker = await ReadMarkerAsync(db, manifest);
            QaRunGuard.Cleanup(manifest, marker, _ => true);
            await ExecuteAsync(db, "CREATE TABLE dbo.QaRecoveryCanary (Id int NOT NULL PRIMARY KEY, Value nvarchar(40) NOT NULL); INSERT INTO dbo.QaRecoveryCanary VALUES (1, N'before-backup');");
        }
        var before = await RecoveryFingerprintAsync(admin);
        Console.WriteLine($"RECOVERY CASE snapshot PASS:tables={before.Count}; ordered full-row SHA256 captured privately; no raw SQL rows or backup contents exported.");
        await using (var sql = new SqlConnection(master))
        {
            await sql.OpenAsync();
            if (!string.Equals((string?)await ScalarAsync(sql, "SELECT @@SERVERNAME"), container, StringComparison.Ordinal))
                throw new InvalidOperationException("Recovery refused an unexpected SQL Server.");
            await using var command = new SqlCommand($"BACKUP DATABASE [{manifest.Database}] TO DISK=@backup WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM;", sql) { CommandTimeout = 120 };
            command.Parameters.AddWithValue("@backup", backup); await command.ExecuteNonQueryAsync();
            await using var header = new SqlCommand("RESTORE HEADERONLY FROM DISK=@backup", sql);
            header.Parameters.AddWithValue("@backup", backup);
            await using var reader = await header.ExecuteReaderAsync();
            if (!await reader.ReadAsync() || reader["DatabaseName"] as string != manifest.Database ||
                Convert.ToInt32(reader["BackupType"]) != 1 || !Convert.ToBoolean(reader["HasBackupChecksums"]) ||
                !Convert.ToBoolean(reader["IsCopyOnly"]) || await reader.ReadAsync())
                throw new InvalidOperationException("Recovery backup header does not identify the exact owned full checksum/copy-only backup.");
        }
        Console.WriteLine("RECOVERY CASE backup-verification PASS:full copy-only/checksum backup; VERIFYONLY and exact source header validated.");
        await using (var sql = new SqlConnection(admin))
        {
            await sql.OpenAsync(); await ExecuteAsync(sql, "UPDATE dbo.QaRecoveryCanary SET Value=N'after-backup' WHERE Id=1");
        }
        var changed = await RecoveryFingerprintAsync(admin);
        if (before["dbo.QaRecoveryCanary"] == changed["dbo.QaRecoveryCanary"])
            throw new InvalidOperationException("Recovery canary did not change after the backup.");
        Console.WriteLine("RECOVERY CASE post-backup-canary PASS:independent table fingerprint changed before restore.");

        // REPLACE is destructive and therefore uses the existing exact run,
        // loopback container, marker and digest guard immediately beforehand.
        // The manifest DB was created by this invocation; no target is accepted
        // from configuration, arguments or an existing external database.
        await VerifyDockerTargetAsync(container, manifest);
        await using (var sql = new SqlConnection(admin)) { await sql.OpenAsync(); marker = await ReadMarkerAsync(sql, manifest); }
        SqlConnection.ClearAllPools();
        await QaRunGuard.Cleanup(manifest, marker, async _ =>
        {
            await using var sql = new SqlConnection(master); await sql.OpenAsync();
            if (!string.Equals((string?)await ScalarAsync(sql, "SELECT @@SERVERNAME"), container, StringComparison.Ordinal))
                throw new InvalidOperationException("Recovery target identity changed before restore.");
            await ExecuteAsync(sql, $"ALTER DATABASE [{manifest.Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
            await using var restore = new SqlCommand($"RESTORE DATABASE [{manifest.Database}] FROM DISK=@backup WITH REPLACE, CHECKSUM; ALTER DATABASE [{manifest.Database}] SET MULTI_USER;", sql) { CommandTimeout = 120 };
            restore.Parameters.AddWithValue("@backup", backup); await restore.ExecuteNonQueryAsync(); return true;
        });
        var restored = await RecoveryFingerprintAsync(admin);
        if (before.Count != restored.Count || before.Any(x => !restored.TryGetValue(x.Key, out var hash) || hash != x.Value))
            throw new InvalidOperationException("Restored domain/Identity/audit/migration rows differ from their backup snapshot.");
        Console.WriteLine("RECOVERY CASE full-restore PASS:all ordered table/schema fingerprints exactly match pre-backup; post-backup canary removed by real RESTORE.");
        await using (var sql = new SqlConnection(admin))
        {
            await sql.OpenAsync(); var restoredMarker = await ReadMarkerAsync(sql, manifest);
            QaRunGuard.Cleanup(manifest, restoredMarker, _ => true);
            await ExecuteAsync(sql, "DROP TABLE dbo.QaRecoveryCanary");
        }
        await VerifyMigrationsAsync(admin);
        await VerifyRuntimePrivilegesAsync(Connection(manifest.SqlServer, manifest.Database, manifest.RuntimeLogin, manifest.RuntimePassword), manifest);
        Console.WriteLine("RECOVERY CASE ownership-migrations-runtime PASS:restored marker valid; both migration sets complete; scoped runtime login remains unprivileged.");
        using (var health = await client.GetAsync("/health")) RequireFinanceStatus(health, HttpStatusCode.OK, "post-restore health");
        client.DefaultRequestHeaders.Authorization = null;
        using (var anonymous = await client.GetAsync("/api/academies")) RequireFinanceStatus(anonymous, HttpStatusCode.Unauthorized, "post-restore anonymous guard");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAsync(client, "qa-admin-a@example.invalid", "Synthetic!39Ab"));
        using var own = await client.GetAsync("/api/academies"); RequireFinanceStatus(own, HttpStatusCode.OK, "post-restore authenticated academy read");
        Console.WriteLine("RECOVERY CASE HTTP PASS:healthy200; anonymous401; actual restored Identity login and authorized academy read200.");
        Console.WriteLine("RECOVERY REGRESSION PASS:6 guarded backup/restore cases on fresh synthetic SQL only. Azure production backup configuration, PITR, media restore and deployment rollback remain separate gates.");
    }

    private static async Task<Dictionary<string, string>> RecoveryFingerprintAsync(string connection)
    {
        await using var db = new SqlConnection(connection); await db.OpenAsync();
        var tables = new List<(string Schema, string Table, string Key)>();
        await using (var list = new SqlCommand("""
            SELECT s.name, t.name,
                   STRING_AGG(CONVERT(nvarchar(max), QUOTENAME(c.name)), N',') WITHIN GROUP (ORDER BY ic.key_ordinal)
            FROM sys.tables t
            JOIN sys.schemas s ON s.schema_id=t.schema_id
            JOIN sys.indexes i ON i.object_id=t.object_id AND i.is_primary_key=1
            JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
            JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
            WHERE t.is_ms_shipped=0
            GROUP BY s.name,t.name ORDER BY s.name,t.name
            """, db))
        {
            await using var reader = await list.ExecuteReaderAsync();
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }
        var actualTables = Convert.ToInt32(await ScalarAsync(db, "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0"));
        if (actualTables == 0 || tables.Count != actualTables) throw new InvalidOperationException("Recovery fingerprint requires a stable primary key for every table.");
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var t in tables)
        {
            static string Quote(string name) => "[" + name.Replace("]", "]]", StringComparison.Ordinal) + "]";
            var content = new StringBuilder();
            await using var command = new SqlCommand($"SELECT * FROM {Quote(t.Schema)}.{Quote(t.Table)} ORDER BY {t.Key} FOR JSON PATH, INCLUDE_NULL_VALUES", db) { CommandTimeout = 60 };
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) content.Append(reader.GetString(0));
            // Include column schema as well as every ordered value. A same-row
            // digest cannot hide a dropped/renamed/changed nullable column.
            await using var schema = new SqlCommand("SELECT c.name, ty.name AS sqlType, c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity FROM sys.columns c JOIN sys.types ty ON ty.user_type_id=c.user_type_id JOIN sys.tables t ON t.object_id=c.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=@schema AND t.name=@table ORDER BY c.column_id FOR JSON PATH, INCLUDE_NULL_VALUES", db);
            schema.Parameters.AddWithValue("@schema", t.Schema); schema.Parameters.AddWithValue("@table", t.Table);
            content.Append('\n');
            await using (var reader = await schema.ExecuteReaderAsync())
                while (await reader.ReadAsync()) content.Append(reader.GetString(0));
            hashes[t.Schema + "." + t.Table] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content.ToString())));
        }
        return hashes;
    }
}
