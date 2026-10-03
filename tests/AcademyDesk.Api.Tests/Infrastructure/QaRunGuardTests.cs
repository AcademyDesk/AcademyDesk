using Microsoft.Data.SqlClient;

namespace AcademyDesk.Api.Tests.Infrastructure;

public sealed class QaRunGuardTests
{
    [Fact]
    public void Generated_runs_have_distinct_names_tokens_and_isolated_paths()
    {
        var first = QaRunManifest.Create();
        var second = QaRunManifest.Create();

        Assert.NotEqual(first.RunId, second.RunId);
        Assert.NotEqual(first.Database, second.Database);
        Assert.NotEqual(first.Token, second.Token);
        Assert.StartsWith("AcademyDesk_QA_", first.Database);
        Assert.All(new[] { first.ContentRoot, first.WebRoot, first.StorageRoot, first.DataProtectionRoot },
            path => Assert.StartsWith(first.Root + Path.DirectorySeparatorChar, path));
    }

    [Fact]
    public void Approved_preflight_reaches_the_injected_start_boundary()
    {
        var manifest = QaRunManifest.Create();
        var calls = 0;

        var result = QaRunGuard.Start(manifest, ValidConfiguration(manifest), approved =>
        {
            calls++;
            Assert.Same(manifest, approved.Manifest);
            return "approved";
        });

        Assert.Equal("approved", result);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Invalid_targets_and_overrides_never_reach_start_boundary()
    {
        var manifest = QaRunManifest.Create();
        var valid = ValidConfiguration(manifest);
        var other = QaRunManifest.Create();
        var rejected = new (string Name, QaHostConfiguration Configuration)[]
        {
            ("remote server", valid with { ApplicationConnection = Connection(manifest, server: "example.invalid") }),
            ("server alias/port", valid with { ApplicationConnection = Connection(manifest, server: "tcp:127.0.0.1,15433") }),
            ("development database", valid with { ApplicationConnection = Connection(manifest, database: "AcademyDesk_Dev") }),
            ("lookalike database", valid with { ApplicationConnection = Connection(manifest, database: manifest.Database + "_copy") }),
            ("identity context override", valid with { IdentityConnection = Connection(manifest, database: other.Database) }),
            ("missing run token", valid with { RunToken = "" }),
            ("other run token", valid with { RunToken = other.Token }),
            ("wrong environment", valid with { Environment = "Development" }),
            ("startup migrations override", valid with { ApplyMigrationsOnStartup = true }),
            ("bootstrap override", valid with { BootstrapEmail = "owner@example.invalid" }),
            ("web root override", valid with { WebRoot = other.WebRoot }),
            ("storage traversal", valid with { StorageRoot = Path.Combine(manifest.Root, "..", "storage") }),
            ("unsafe key path", valid with { DataProtectionRoot = Path.GetTempPath() }),
            ("content root override", valid with { ContentRoot = Directory.GetCurrentDirectory() }),
            ("wrong SQL login", valid with { ApplicationConnection = Connection(manifest, login: "sa") }),
            ("wrong SQL password", valid with { ApplicationConnection = Connection(manifest, password: "example") }),
            ("integrated admin identity", valid with { ApplicationConnection = Connection(manifest, integrated: true) }),
            ("SQL file attachment", valid with { ApplicationConnection = Connection(manifest) + ";AttachDbFilename=example.mdf" }),
        };

        foreach (var (name, configuration) in rejected)
        {
            var calls = 0;
            Assert.Throws<InvalidOperationException>(() => QaRunGuard.Start(manifest, configuration, _ => { calls++; return true; }));
            Assert.True(calls == 0, $"The startup boundary ran for {name}.");
        }
    }

    [Fact]
    public void Cleanup_requires_matching_run_server_database_and_ownership_digest()
    {
        var manifest = QaRunManifest.Create();
        var other = QaRunManifest.Create();
        var valid = new QaOwnershipMarker(manifest.RunId, manifest.SqlServer, manifest.Database, manifest.TokenDigest);
        var rejected = new[]
        {
            valid with { RunId = other.RunId },
            valid with { Server = "example.invalid" },
            valid with { Database = other.Database },
            valid with { Database = "AcademyDesk_QA_" + other.RunId.ToString("N") },
            valid with { TokenDigest = other.TokenDigest }
        };

        foreach (var marker in rejected)
        {
            var calls = 0;
            Assert.Throws<InvalidOperationException>(() => QaRunGuard.Cleanup(manifest, marker, _ => { calls++; return true; }));
            Assert.Equal(0, calls);
        }

        var approvedCalls = 0;
        QaRunGuard.Cleanup(manifest, valid, _ => { approvedCalls++; return true; });
        Assert.Equal(1, approvedCalls);
    }

    [Fact]
    public void Host_folder_cleanup_refuses_a_mismatched_owner_marker()
    {
        var manifest = QaRunManifest.Create();
        Directory.CreateDirectory(manifest.Root);
        var marker = Path.Combine(manifest.Root, ".qa-owner");
        try
        {
            File.WriteAllText(marker, "wrong-owner");
            var calls = 0;
            Assert.Throws<InvalidOperationException>(() => QaRunGuard.CleanupHostFolders(manifest, _ => { calls++; return true; }));
            Assert.Equal(0, calls);
        }
        finally
        {
            File.WriteAllText(marker, $"{manifest.RunId:N}\n{manifest.TokenDigest}");
            QaRunGuard.CleanupHostFolders(manifest, root => { Directory.Delete(root, recursive: true); return true; });
        }
        Assert.False(Directory.Exists(manifest.Root));
    }

    private static QaHostConfiguration ValidConfiguration(QaRunManifest manifest) => new(
        "Testing", manifest.Token,
        Connection(manifest),
        Connection(manifest),
        manifest.ContentRoot, manifest.WebRoot, manifest.StorageRoot, manifest.DataProtectionRoot, false,
        null, null);

    private static string Connection(QaRunManifest manifest, string? server = null, string? database = null,
        string? login = null, string? password = null, bool integrated = false) => new SqlConnectionStringBuilder
    {
        DataSource = server ?? manifest.SqlServer,
        InitialCatalog = database ?? manifest.Database,
        IntegratedSecurity = integrated,
        UserID = login ?? manifest.RuntimeLogin,
        Password = password ?? manifest.RuntimePassword,
        Encrypt = true,
        TrustServerCertificate = true
    }.ConnectionString;
}
