using Microsoft.Data.SqlClient;

internal static partial class SqlHarnessEntryPoint
{
    private static async Task WaitForOwnedSqlAuthenticationAsync(string masterConnection, string container)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!deadline.IsCancellationRequested)
        {
            await using var connection = new SqlConnection(masterConnection);
            try
            {
                await connection.OpenAsync(deadline.Token);
                await using var command = new SqlCommand("SELECT @@SERVERNAME", connection);
                if (!string.Equals(await command.ExecuteScalarAsync(deadline.Token) as string, container, StringComparison.Ordinal))
                    throw new InvalidOperationException("SQL authentication readiness refused an unexpected server.");
                Console.WriteLine("PASS: SQL authentication readiness; exact owned server verified before database creation.");
                return;
            }
            catch (SqlException exception) when (exception.Number is 18456 or 18401)
            {
                // Retry startup authentication only, never a scenario/mutation.
                // A failed-login pool entry otherwise caches this transient error.
                SqlConnection.ClearPool(connection);
                try { await Task.Delay(1000, deadline.Token); }
                catch (OperationCanceledException) when (deadline.IsCancellationRequested) { break; }
            }
        }
        throw new InvalidOperationException("Run-owned SQL authentication readiness did not pass within sixty seconds.");
    }
}
