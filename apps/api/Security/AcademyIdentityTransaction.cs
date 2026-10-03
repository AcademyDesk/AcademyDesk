using System.Data;
using AcademyDesk.Api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace AcademyDesk.Api.Security;

/// <summary>
/// Request-local enlistment of the existing Identity store in an academy SQL
/// transaction. Never changes global connection configuration or owns the
/// academy connection. Only explicitly opted-in same-store actions use this.
/// </summary>
internal sealed class AcademyIdentityTransaction(IdentityDbContext identityDb, string originalConnectionString) : IAsyncDisposable
{
    internal static string Validate(AcademyDeskDbContext academyDb, IdentityDbContext identityDb)
    {
        if (!academyDb.Database.IsSqlServer() || !identityDb.Database.IsSqlServer() ||
            academyDb.Database.CurrentTransaction is not null || identityDb.Database.CurrentTransaction is not null ||
            identityDb.Database.GetDbConnection().State != ConnectionState.Closed)
            throw new InvalidOperationException("Linked updates require idle SQL Server contexts in the same configured store.");

        // Read immutable configured options, not an already-open SqlConnection's
        // string (which may have removed its password after authentication).
        var academyConnection = ConfiguredConnection(academyDb);
        var identityConnection = ConfiguredConnection(identityDb);
        if (!new SqlConnectionStringBuilder(academyConnection).EquivalentTo(new SqlConnectionStringBuilder(identityConnection)))
            throw new InvalidOperationException("Linked updates require matching academy and Identity SQL configuration.");
        return identityConnection;
    }

    private static string ConfiguredConnection(DbContext db) =>
        db.GetService<IDbContextOptions>().Extensions.OfType<RelationalOptionsExtension>().Single().ConnectionString is { Length: > 0 } configured
            ? configured : throw new InvalidOperationException("Linked updates require an explicitly configured SQL connection string.");

    internal static async Task<AcademyIdentityTransaction> EnlistAsync(AcademyDeskDbContext academyDb, IdentityDbContext identityDb,
        IDbContextTransaction transaction, string originalConnectionString, CancellationToken token)
    {
        var enlistment = new AcademyIdentityTransaction(identityDb, originalConnectionString);
        identityDb.Database.SetDbConnection(academyDb.Database.GetDbConnection(), contextOwnsConnection: false);
        try
        {
            await identityDb.Database.UseTransactionAsync(transaction.GetDbTransaction(), token);
            return enlistment;
        }
        catch
        {
            await enlistment.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Detach before the domain boundary disposes/rolls back its transaction.
        // Do not use a canceled request token for cleanup. SetDbConnection may
        // dispose the original EF-owned connection, so restore a NEW owned one.
        await identityDb.Database.UseTransactionAsync(null);
        identityDb.Database.SetDbConnection(new SqlConnection(originalConnectionString), contextOwnsConnection: true);
    }
}
