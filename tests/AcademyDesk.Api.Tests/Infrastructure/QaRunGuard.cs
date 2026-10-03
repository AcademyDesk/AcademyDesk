using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace AcademyDesk.Api.Tests.Infrastructure;

// This is a test-harness preflight, not an application startup guard. The HTTP/SQL
// harness must call Start only after assembling its final effective configuration.
internal sealed class QaRunManifest
{
    private QaRunManifest(Guid runId, int sqlPort, string token, string runtimePassword)
    {
        RunId = runId;
        SqlPort = sqlPort;
        Token = token;
        RuntimePassword = runtimePassword;
        Database = $"AcademyDesk_QA_{runId:N}";
        Root = Path.Combine(Path.GetTempPath(), "AcademyDesk-QA", runId.ToString("N"));
    }

    public Guid RunId { get; }
    public int SqlPort { get; }
    public string SqlServer => $"127.0.0.1,{SqlPort}";
    public string RuntimeLogin => $"qa_{RunId:N}";
    public string RuntimePassword { get; }
    public string Token { get; }
    public string Database { get; }
    public string Root { get; }
    public string ContentRoot => Path.Combine(Root, "content");
    public string WebRoot => Path.Combine(Root, "web");
    public string StorageRoot => Path.Combine(Root, "storage");
    public string DataProtectionRoot => Path.Combine(Root, "data-protection");
    public string TokenDigest => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Token)));

    public static QaRunManifest Create() => CreateForDocker(Guid.NewGuid(), 15433);

    public static QaRunManifest CreateForDocker(Guid runId, int sqlPort)
    {
        if (runId == Guid.Empty || sqlPort is < 1024 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(sqlPort), "A run ID and non-privileged local port are required.");
        return new(runId, sqlPort, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            "Qa!9" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)));
    }
}

internal sealed record QaHostConfiguration(
    string Environment,
    string RunToken,
    string ApplicationConnection,
    string IdentityConnection,
    string ContentRoot,
    string WebRoot,
    string StorageRoot,
    string DataProtectionRoot,
    bool ApplyMigrationsOnStartup,
    string? BootstrapEmail,
    string? BootstrapPassword);

internal sealed record QaOwnershipMarker(Guid RunId, string Server, string Database, string TokenDigest);

internal sealed record ApprovedQaRun(QaRunManifest Manifest, QaHostConfiguration Configuration);

internal static class QaRunGuard
{
    // The port is read from a run-labelled, loopback-bound Docker container.
    // Container identity must be checked by the provisioning step before use.

    public static TResult Start<TResult>(QaRunManifest manifest, QaHostConfiguration configuration, Func<ApprovedQaRun, TResult> start)
    {
        Validate(manifest, configuration);
        return start(new ApprovedQaRun(manifest, configuration));
    }

    public static TResult Cleanup<TResult>(QaRunManifest manifest, QaOwnershipMarker marker, Func<QaRunManifest, TResult> cleanup)
    {
        if (manifest.RunId == Guid.Empty || marker.RunId != manifest.RunId ||
            !string.Equals(marker.Server, manifest.SqlServer, StringComparison.Ordinal) ||
            !string.Equals(marker.Database, manifest.Database, StringComparison.Ordinal) ||
            !string.Equals(marker.TokenDigest, manifest.TokenDigest, StringComparison.Ordinal))
            throw new InvalidOperationException("QA cleanup ownership check failed.");

        ValidatePaths(manifest, manifest.ContentRoot, manifest.WebRoot, manifest.StorageRoot, manifest.DataProtectionRoot);
        return cleanup(manifest);
    }

    // Host-only folders exist before a SQL database is provisioned. Their separate
    // marker permits cleanup of those folders without pretending a DB marker exists.
    public static TResult CleanupHostFolders<TResult>(QaRunManifest manifest, Func<string, TResult> cleanup)
    {
        ValidatePaths(manifest, manifest.ContentRoot, manifest.WebRoot, manifest.StorageRoot, manifest.DataProtectionRoot);
        var markerPath = Path.Combine(manifest.Root, ".qa-owner");
        if (!File.Exists(markerPath) ||
            (File.GetAttributes(markerPath) & FileAttributes.ReparsePoint) != 0 ||
            !string.Equals(File.ReadAllText(markerPath), $"{manifest.RunId:N}\n{manifest.TokenDigest}", StringComparison.Ordinal))
            throw new InvalidOperationException("QA storage ownership check failed.");
        return cleanup(manifest.Root);
    }

    public static void Validate(QaRunManifest manifest, QaHostConfiguration configuration)
    {
        if (manifest.RunId == Guid.Empty ||
            !string.Equals(configuration.Environment, "Testing", StringComparison.Ordinal) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configuration.RunToken), Encoding.UTF8.GetBytes(manifest.Token)) ||
            configuration.ApplyMigrationsOnStartup ||
            !string.IsNullOrWhiteSpace(configuration.BootstrapEmail) ||
            !string.IsNullOrWhiteSpace(configuration.BootstrapPassword))
            throw new InvalidOperationException("QA run configuration check failed.");

        ValidateConnection(configuration.ApplicationConnection, manifest);
        ValidateConnection(configuration.IdentityConnection, manifest);
        ValidatePaths(manifest, configuration.ContentRoot, configuration.WebRoot, configuration.StorageRoot, configuration.DataProtectionRoot);
    }

    private static void ValidateConnection(string connection, QaRunManifest manifest)
    {
        try
        {
            var parsed = new SqlConnectionStringBuilder(connection);
            if (!string.Equals(parsed.DataSource, manifest.SqlServer, StringComparison.Ordinal) ||
                !string.Equals(parsed.InitialCatalog, manifest.Database, StringComparison.Ordinal) ||
                parsed.IntegratedSecurity ||
                !string.Equals(parsed.UserID, manifest.RuntimeLogin, StringComparison.Ordinal) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(parsed.Password), Encoding.UTF8.GetBytes(manifest.RuntimePassword)) ||
                !string.IsNullOrEmpty(parsed.AttachDBFilename) ||
                !string.IsNullOrEmpty(parsed.FailoverPartner))
                throw new InvalidOperationException("QA SQL target check failed.");
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("QA SQL target check failed.", ex);
        }
    }

    private static void ValidatePaths(QaRunManifest manifest, string content, string web, string storage, string keys)
    {
        var expectedRoot = Path.Combine(Path.GetTempPath(), "AcademyDesk-QA", manifest.RunId.ToString("N"));
        if (!SamePath(manifest.Root, expectedRoot) ||
            !SamePath(content, manifest.ContentRoot) ||
            !SamePath(web, manifest.WebRoot) ||
            !SamePath(storage, manifest.StorageRoot) ||
            !SamePath(keys, manifest.DataProtectionRoot))
            throw new InvalidOperationException("QA run path check failed.");

        // Refuse any existing redirect within the run-owned subtree. The setup step
        // will create the directories only after this preflight succeeds.
        foreach (var path in new[] { Path.Combine(Path.GetTempPath(), "AcademyDesk-QA"), manifest.Root, content, web, storage, keys })
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("QA run path check failed.");
            }
        }
    }

    private static bool SamePath(string candidate, string expected)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !Path.IsPathFullyQualified(candidate)) return false;
        var comparer = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(candidate, Path.GetFullPath(candidate), comparer) &&
               string.Equals(candidate, Path.GetFullPath(expected), comparer);
    }
}
