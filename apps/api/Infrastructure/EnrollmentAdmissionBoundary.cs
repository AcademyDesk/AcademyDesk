using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Infrastructure;

/// <summary>
/// Serializes capacity writers across enrollment, transfer, promotion and batch
/// edits. Normal academy actions reuse the required save/audit transaction;
/// existing platform-owner bypass paths own their SQL transaction explicitly.
/// This boundary grants no permission and does not interpret admission policy.
/// </summary>
internal static class EnrollmentAdmissionBoundary
{
    internal const string BusyMessage = "Academy enrolments are being updated. Please try again.";

    internal static async Task<IDbContextTransaction?> BeginOwnedAsync(AcademyDeskDbContext db, CancellationToken token) =>
        db.Database.IsSqlServer() && db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(token) : null;

    internal static async Task<bool> TryLockAsync(AcademyDeskDbContext db, Guid academyId, CancellationToken token)
    {
        // InMemory is used only by unit tests, never as SQL concurrency evidence.
        if (!db.Database.IsRelational()) return true;
        if (!db.Database.IsSqlServer() || db.Database.CurrentTransaction is null) return false;
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; SELECT @result;";
        var resource = command.CreateParameter(); resource.ParameterName = "@resource";
        // One academy-wide order also avoids inverse-source/target transfer locks.
        resource.Value = $"AcademyDesk:EnrollmentAdmission:{academyId:D}";
        command.Parameters.Add(resource);
        return await command.ExecuteScalarAsync(token) is int result && result >= 0;
    }
}
