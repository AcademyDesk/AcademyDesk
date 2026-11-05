using System.Security.Cryptography;
using System.Text;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task VerifyPentaHostMigrationAsync(AcademyDeskDbContext db,
        string adminConnection, QaRunManifest manifest)
    {
        // Program calls this only for its newly created, marker-owned SQL DB.
        // Never accept a database or migration target from an environment setting.
        await VerifyDockerTargetAsync("academydesk-qa-" + manifest.RunId.ToString("N"), manifest);
        await using (var sql = new SqlConnection(adminConnection))
        {
            await sql.OpenAsync();
            var marker = await ReadMarkerAsync(sql, manifest);
            QaRunGuard.Cleanup(manifest, marker, _ => true);
        }
        const string previous = "20261028181901_ReconcilePayrollProvenance";
        const string latest = "20261104094235_AddProtectedPentaHost";
        string[] additions = ["PentaApprovals", "PentaAttempts", "PentaConversationMessages",
            "PentaConversations", "PentaConversationTurns", "PentaExecutions", "PentaMiniSessions",
            "PentaMiniTurns", "PentaOnboardingDrafts", "PentaTasks", "PentaUsageEntries", "PentaUsageReservations"];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(previous);
        await using var identity = new IdentityDbContext(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(adminConnection).Options);
        await identity.Database.MigrateAsync();

        var academy = new Academy { Name = "Synthetic migration academy" };
        var other = new Academy { Name = "Synthetic foreign migration academy" };
        var student = new Student { AcademyId = academy.Id, FirstName = "Synthetic", LastName = "Before upgrade",
            StudentNumber = "QA-PENTA-MIGRATION", AdminNotes = "Preserve existing café / छात्र notes" };
        var invoice = new Invoice { AcademyId = academy.Id, StudentId = student.Id,
            InvoiceNumber = "QA-PENTA-MIGRATION", TotalAmount = 1000m, AdjustedAmount = 100m };
        var completed = new Payment { AcademyId = academy.Id, InvoiceId = invoice.Id, Amount = 200m };
        var reconciled = new Payment { AcademyId = academy.Id, InvoiceId = invoice.Id, Amount = 100m,
            Status = "Reconciled", ReconciledAtUtc = DateTime.UtcNow, ReconciliationReference = "Synthetic historical reference" };
        var voided = new Payment { AcademyId = academy.Id, InvoiceId = invoice.Id, Amount = 75m, Status = "Voided" };
        var profile = new PayrollProfile { AcademyId = academy.Id, WorkerType = "Staff", WorkerName = "Legacy staff", MonthlyAmount = 1000m };
        // A preserved historical statement is distinct from creating a new payout.
        var payout = new PayrollPayout { AcademyId = academy.Id, PayrollProfileId = profile.Id,
            PayslipNumber = "QA-PENTA-MIGRATION", PeriodLabel = "Historical period", GrossAmount = 1000m,
            Deductions = 1000m, NetAmount = 0m, Status = "NoCashDue", PaidAtUtc = null,
            WorkerNameSnapshot = "Legacy staff", WorkerTypeSnapshot = "Staff" };
        var user = new ApplicationUser { Id = Guid.NewGuid(), AcademyId = academy.Id,
            UserName = "penta-migration@example.invalid", NormalizedUserName = "PENTA-MIGRATION@EXAMPLE.INVALID",
            Email = "penta-migration@example.invalid", NormalizedEmail = "PENTA-MIGRATION@EXAMPLE.INVALID",
            DisplayName = "Synthetic migration user", EmailConfirmed = true, SecurityStamp = Guid.NewGuid().ToString("N") };
        user.PasswordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, "Synthetic!39Ab");
        var role = new ApplicationRole { Id = Guid.NewGuid(), AcademyId = academy.Id,
            Name = "SyntheticMigrationRole", NormalizedName = "SYNTHETICMIGRATIONROLE", PermissionsJson = "[\"Core\"]" };
        var audit = new AuditLog { AcademyId = academy.Id, ActorUserId = user.Id,
            Action = "Synthetic.MigrationCanary", EntityType = "Invoice", EntityId = invoice.Id, MetadataJson = "{\"synthetic\":true}" };
        var notification = new Notification { AcademyId = academy.Id, RecipientId = student.Id,
            RecipientType = "Student", Title = "Legacy notice", Message = "Preserve delivery", Status = "Read", AttemptCount = 2 };
        object[] fixtures = [academy, other, student, invoice, completed, reconciled, voided, profile, payout, audit, notification];
        db.AddRange(fixtures); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        identity.AddRange(user, role, new IdentityUserRole<Guid> { UserId = user.Id, RoleId = role.Id },
            new IdentityUserClaim<Guid> { UserId = user.Id, ClaimType = "synthetic-migration", ClaimValue = "Preserve me" });
        await identity.SaveChangesAsync(); identity.ChangeTracker.Clear();

        var before = await RecoveryFingerprintAsync(adminConnection);
        var catalogBefore = await PentaMigrationCatalogAsync(adminConnection, before.Keys);
        var historyBefore = await PentaMigrationHistoryAsync(adminConnection);
        if (additions.Any(x => before.ContainsKey("dbo." + x)) || historyBefore.ContainsKey(latest))
            throw new InvalidOperationException("The protected-host migration baseline already contains new host state.");

        async Task VerifyPreserved(string phase, bool upgraded)
        {
            var after = await RecoveryFingerprintAsync(adminConnection);
            var expectedTables = before.Keys.Concat(upgraded ? additions.Select(x => "dbo." + x) : []);
            if (!after.Keys.Order(StringComparer.Ordinal).SequenceEqual(expectedTables.Order(StringComparer.Ordinal)))
                throw new InvalidOperationException("Protected-host migration changed the expected table inventory: " + phase);
            if (upgraded)
            {
                await using var sql = new SqlConnection(adminConnection); await sql.OpenAsync();
                foreach (var table in additions)
                    if (Convert.ToInt32(await ScalarAsync(sql, $"SELECT COUNT(*) FROM [dbo].[{table}]")) != 0)
                        throw new InvalidOperationException("Protected-host migration invented persisted host state: " + phase);
            }
            foreach (var (table, digest) in before)
                if (table != "dbo.__EFMigrationsHistory" && after[table] != digest)
                    throw new InvalidOperationException("Protected-host migration changed preexisting rows or columns: " + phase);
            var catalogAfter = await PentaMigrationCatalogAsync(adminConnection, before.Keys);
            if (catalogBefore.Any(x => !catalogAfter.TryGetValue(x.Key, out var digest) || digest != x.Value))
                throw new InvalidOperationException("Protected-host migration changed a preexisting key, index or constraint: " + phase);
            var historyAfter = await PentaMigrationHistoryAsync(adminConnection);
            if (historyAfter.Count != historyBefore.Count + (upgraded ? 1 : 0) ||
                historyBefore.Any(x => !historyAfter.TryGetValue(x.Key, out var version) || version != x.Value) ||
                (upgraded && (!historyAfter.TryGetValue(latest, out var currentVersion) || string.IsNullOrWhiteSpace(currentVersion))))
                throw new InvalidOperationException("Protected-host migration changed preexisting migration history: " + phase);
            Console.WriteLine($"PENTA MIGRATION {phase} PASS:old application/Identity rows, columns, keys, indexes, constraints and migration history retained; expected tables={after.Count}.");
        }

        await migrator.MigrateAsync(latest);
        await VerifyPreserved("upgrade", upgraded: true);
        var protector = new EphemeralDataProtectionProvider().CreateProtector("qa-penta-migration-canary");
        var session = new PentaMiniSession { AcademyId = academy.Id, ActorUserId = user.Id,
            ProtectedState = protector.Protect("Synthetic session canary"), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) };
        var draft = new PentaOnboardingDraft { AcademyId = academy.Id, ActorUserId = user.Id, SessionId = session.Id,
            ProtectedBody = protector.Protect("Synthetic draft canary"), Status = "Draft", ExpiresAtUtc = session.ExpiresAtUtc };
        db.AddRange(session, draft); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        const string scopedForeignKey = "FK_PentaOnboardingDrafts_PentaMiniSessions_AcademyId_ActorUserId_SessionId";
        async Task RejectForeignDraft(Guid tenantId, Guid actorId, Guid sessionId, string label)
        {
            var id = Guid.NewGuid();
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [PentaOnboardingDrafts] ([Id],[AcademyId],[ActorUserId],[SessionId],[ContextVersion],[Revision],[Status],[ExpiresAtUtc],[ProtectedBody],[CreatedAtUtc]) VALUES ({id},{tenantId},{actorId},{sessionId},{0L},{0L},{"Draft"},{draft.ExpiresAtUtc},{draft.ProtectedBody},{draft.CreatedAtUtc})");
            }
            catch (SqlException ex) when (ex.Number == 547 && ex.Message.Contains(scopedForeignKey, StringComparison.Ordinal))
            {
                Console.WriteLine("PENTA MIGRATION FK PASS:" + label); return;
            }
            throw new InvalidOperationException("Protected-host SQL accepted a draft outside its composite session scope: " + label);
        }
        await RejectForeignDraft(academy.Id, Guid.NewGuid(), session.Id, "different actor");
        await RejectForeignDraft(other.Id, user.Id, session.Id, "different academy");
        await RejectForeignDraft(academy.Id, user.Id, Guid.NewGuid(), "missing session");
        var restrictVerified = false;
        try { await db.PentaMiniSessions.Where(x => x.Id == session.Id).ExecuteDeleteAsync(); }
        catch (SqlException ex) when (ex.Number == 547 && ex.Message.Contains(scopedForeignKey, StringComparison.Ordinal)) { restrictVerified = true; }
        if (!restrictVerified || !await db.PentaOnboardingDrafts.AnyAsync(x => x.Id == draft.Id))
            throw new InvalidOperationException("Protected-host session deletion bypassed the restrictive draft foreign key.");
        Console.WriteLine("PENTA MIGRATION FK PASS:session deletion restricted while scoped draft exists.");
        await db.PentaOnboardingDrafts.Where(x => x.Id == draft.Id).ExecuteDeleteAsync();
        await db.PentaMiniSessions.Where(x => x.Id == session.Id).ExecuteDeleteAsync();

        await migrator.MigrateAsync(previous);
        await VerifyPreserved("rollback", upgraded: false);
        await migrator.MigrateAsync(latest);
        await VerifyPreserved("reapply", upgraded: true);
        if ((await db.Database.GetPendingMigrationsAsync()).Any())
            throw new InvalidOperationException("Protected-host migration reapply left pending application migrations.");
        // Leave the cohort's normal fresh fixture state, retaining failures for inspection.
        await identity.UserClaims.Where(x => x.UserId == user.Id).ExecuteDeleteAsync();
        await identity.UserRoles.Where(x => x.UserId == user.Id).ExecuteDeleteAsync();
        await identity.Users.Where(x => x.Id == user.Id).ExecuteDeleteAsync();
        await identity.Roles.Where(x => x.Id == role.Id).ExecuteDeleteAsync();
        db.RemoveRange(fixtures); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        Console.WriteLine("PENTA MIGRATION PASS:owned prior-schema upgrade, exact12-table addition, live actor/tenant/session FK, rollback and reapply; synthetic fixtures removed. Not a production rollback approval.");
    }

    private static async Task<Dictionary<string, string>> PentaMigrationHistoryAsync(string connection)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync();
        await using var command = new SqlCommand("SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId", sql);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        while (await reader.ReadAsync()) result.Add(reader.GetString(0), reader.GetString(1));
        return result;
    }

    private static async Task<Dictionary<string, string>> PentaMigrationCatalogAsync(string connection, IEnumerable<string> tables)
    {
        await using var sql = new SqlConnection(connection); await sql.OpenAsync();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            await using var command = new SqlCommand("""
                SELECT
                  (SELECT c.name,ty.name AS sqlType,c.max_length,c.precision,c.scale,c.is_nullable,c.is_identity
                   FROM sys.columns c JOIN sys.types ty ON ty.user_type_id=c.user_type_id
                   WHERE c.object_id=OBJECT_ID(@table) ORDER BY c.column_id FOR JSON PATH, INCLUDE_NULL_VALUES) AS columnsJson,
                  (SELECT i.name,i.type,i.is_unique,i.is_primary_key,i.is_unique_constraint,i.filter_definition,i.is_disabled,
                     (SELECT c.name,ic.key_ordinal,ic.is_descending_key,ic.is_included_column
                      FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                      WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id ORDER BY ic.index_column_id FOR JSON PATH, INCLUDE_NULL_VALUES) AS members
                   FROM sys.indexes i WHERE i.object_id=OBJECT_ID(@table) ORDER BY i.name FOR JSON PATH, INCLUDE_NULL_VALUES) AS indexesJson,
                  (SELECT f.name,OBJECT_SCHEMA_NAME(f.referenced_object_id) AS targetSchema,OBJECT_NAME(f.referenced_object_id) AS targetTable,
                     f.delete_referential_action,f.update_referential_action,f.is_disabled,f.is_not_trusted,
                     (SELECT COL_NAME(fc.parent_object_id,fc.parent_column_id) AS sourceColumn,
                             COL_NAME(fc.referenced_object_id,fc.referenced_column_id) AS targetColumn
                      FROM sys.foreign_key_columns fc WHERE fc.constraint_object_id=f.object_id ORDER BY fc.constraint_column_id FOR JSON PATH, INCLUDE_NULL_VALUES) AS members
                   FROM sys.foreign_keys f WHERE f.parent_object_id=OBJECT_ID(@table) ORDER BY f.name FOR JSON PATH, INCLUDE_NULL_VALUES) AS foreignKeysJson,
                  (SELECT name,definition,is_disabled,is_not_trusted FROM sys.check_constraints
                   WHERE parent_object_id=OBJECT_ID(@table) ORDER BY name FOR JSON PATH, INCLUDE_NULL_VALUES) AS checksJson,
                  (SELECT name,COL_NAME(parent_object_id,parent_column_id) AS columnName,definition FROM sys.default_constraints
                   WHERE parent_object_id=OBJECT_ID(@table) ORDER BY name FOR JSON PATH, INCLUDE_NULL_VALUES) AS defaultsJson
                FOR JSON PATH, INCLUDE_NULL_VALUES
                """, sql);
            command.Parameters.AddWithValue("@table", table);
            var text = new StringBuilder();
            await using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync()) text.Append(reader.GetString(0));
            result[table] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
        }
        return result;
    }
}
