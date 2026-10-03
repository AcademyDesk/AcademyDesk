using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AcademyDesk.Api.Security;

/// <summary>
/// Only used immediately around native Identity role/account creation. These
/// conflicts abort the existing request transaction; no store retry or success
/// is fabricated. Other SQL failures, cancellation and commit errors propagate.
/// </summary>
internal static class IdentityProvisioningConflict
{
    internal static bool RoleName(Exception exception) => HasConflict(exception, "RoleNameIndex");
    internal static bool UserName(Exception exception) => HasConflict(exception, "UserNameIndex");

    private static bool HasConflict(Exception exception, string index)
    {
        // EF's non-retrying SQL strategy wraps native deadlock1205 in an
        // InvalidOperationException. Inspect the real provider cause, never
        // classify from the wrapper's text or catch cancellation/commit errors.
        if (exception is not (SqlException or DbUpdateException or InvalidOperationException)) return false;
        var sql = exception.GetBaseException() as SqlException;
        return sql is not null && sql.Errors.Cast<SqlError>().Any(error => MatchesSqlError(error.Number, error.Message, index));
    }

    internal static bool MatchesSqlError(int number, string message, string index) =>
        number == 1205 || (number is 2601 or 2627 && message.Contains("'" + index + "'", StringComparison.Ordinal));
}
