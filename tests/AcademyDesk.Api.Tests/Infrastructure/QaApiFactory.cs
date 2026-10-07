using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using AcademyDesk.Api.Infrastructure.Media;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AcademyDesk.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AcademyDesk.Api.Tests.Infrastructure;

internal sealed class QaApiFactory : WebApplicationFactory<Program>
{
    private readonly QaRunManifest manifest;
    private readonly IReadOnlyDictionary<string, string?> overrides;
    private readonly IMediaBlobStore? isolatedMediaStore;
    private readonly IInterceptor? isolatedIdentityInterceptor;
    private readonly IInterceptor? isolatedDomainInterceptor;
    private readonly TimeProvider? isolatedClock;
    private readonly IPentaSyntheticProvider? isolatedPentaProvider;
    private readonly IPentaProvider? isolatedMiniProvider;

    public QaApiFactory(QaRunManifest manifest, IReadOnlyDictionary<string, string?>? overrides = null, IMediaBlobStore? isolatedMediaStore = null, IInterceptor? isolatedIdentityInterceptor = null, IPentaSyntheticProvider? isolatedPentaProvider = null, IInterceptor? isolatedDomainInterceptor = null, TimeProvider? isolatedClock = null, IPentaProvider? isolatedMiniProvider = null)
    {
        this.manifest = manifest;
        this.overrides = overrides ?? new Dictionary<string, string?>();
        this.isolatedMediaStore = isolatedMediaStore;
        this.isolatedIdentityInterceptor = isolatedIdentityInterceptor;
        this.isolatedDomainInterceptor = isolatedDomainInterceptor;
        this.isolatedClock = isolatedClock;
        this.isolatedPentaProvider = isolatedPentaProvider;
        this.isolatedMiniProvider = isolatedMiniProvider;
    }

    public bool PreflightPassed { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = Connection(manifest),
            ["Database:ApplyMigrationsOnStartup"] = "false",
            ["Bootstrap:PlatformOwnerEmail"] = "",
            ["Bootstrap:PlatformOwnerPassword"] = "",
            ["MediaStorage:Enabled"] = "false",
            ["QA:RunToken"] = manifest.Token,
            ["QA:StorageRoot"] = manifest.StorageRoot,
            ["QA:DataProtectionRoot"] = manifest.DataProtectionRoot
        };
        foreach (var pair in overrides) values[pair.Key] = pair.Value;
        var planned = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        if (planned.GetValue<bool>("MediaStorage:Enabled"))
            throw new InvalidOperationException("The isolated SQL host must not use Azure media storage.");
        QaRunGuard.Validate(manifest, new QaHostConfiguration(
            "Testing", planned["QA:RunToken"] ?? "",
            planned.GetConnectionString("DefaultConnection") ?? "",
            planned.GetConnectionString("DefaultConnection") ?? "",
            manifest.ContentRoot, manifest.WebRoot,
            planned["QA:StorageRoot"] ?? "", planned["QA:DataProtectionRoot"] ?? "",
            planned.GetValue<bool>("Database:ApplyMigrationsOnStartup"),
            planned["Bootstrap:PlatformOwnerEmail"], planned["Bootstrap:PlatformOwnerPassword"]));

        // PhysicalFileProvider requires ContentRoot to exist before Program begins.
        // No folder is created until the complete controlled provider set passes.
        Directory.CreateDirectory(manifest.Root);
        File.WriteAllText(Path.Combine(manifest.Root, ".qa-owner"), $"{manifest.RunId:N}\n{manifest.TokenDigest}");
        Directory.CreateDirectory(manifest.ContentRoot);
        Directory.CreateDirectory(manifest.WebRoot);
        Directory.CreateDirectory(manifest.StorageRoot);
        Directory.CreateDirectory(manifest.DataProtectionRoot);

        builder.UseEnvironment("Testing");
        builder.UseContentRoot(manifest.ContentRoot);
        builder.UseWebRoot(manifest.WebRoot);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(values);
        });
        builder.ConfigureServices((context, services) =>
        {
            var config = context.Configuration;
            if (config.GetValue<bool>("MediaStorage:Enabled"))
                throw new InvalidOperationException("The isolated SQL host must not use Azure media storage.");
            var effective = new QaHostConfiguration(
                context.HostingEnvironment.EnvironmentName,
                config["QA:RunToken"] ?? "",
                config.GetConnectionString("DefaultConnection") ?? "",
                config.GetConnectionString("DefaultConnection") ?? "",
                context.HostingEnvironment.ContentRootPath,
                context.HostingEnvironment.WebRootPath,
                config["QA:StorageRoot"] ?? "",
                config["QA:DataProtectionRoot"] ?? "",
                config.GetValue<bool>("Database:ApplyMigrationsOnStartup"),
                config["Bootstrap:PlatformOwnerEmail"],
                config["Bootstrap:PlatformOwnerPassword"]);
            QaRunGuard.Start(manifest, effective, _ => { PreflightPassed = true; return true; });

            // Trusted local QA injection only, after exact owned SQL preflight.
            if (isolatedIdentityInterceptor is not null)
                services.ConfigureDbContext<IdentityDbContext>(options => options.AddInterceptors(isolatedIdentityInterceptor));
            if (isolatedDomainInterceptor is not null)
                services.ConfigureDbContext<AcademyDeskDbContext>(options => options.AddInterceptors(isolatedDomainInterceptor));
            if (isolatedClock is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton(isolatedClock);
            }

            // Trusted harness injection only, after its labelled loopback emulator
            // guard. Configuration still cannot enable any live Azure provider.
            if (isolatedMediaStore is not null)
            {
                services.RemoveAll<IMediaBlobStore>();
                services.AddSingleton(isolatedMediaStore);
            }

            // Synthetic QA replacement only after the run-owned SQL host guard.
            if (isolatedPentaProvider is not null)
            {
                services.RemoveAll<IPentaSyntheticProvider>();
                services.AddSingleton(isolatedPentaProvider);
            }

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(manifest.DataProtectionRoot));
            if (isolatedMiniProvider is not null)
            {
                services.RemoveAll<IPentaProvider>();
                services.AddSingleton(isolatedMiniProvider);
            }
        });
    }

    private static string Connection(QaRunManifest manifest) => new SqlConnectionStringBuilder
    {
        DataSource = manifest.SqlServer,
        InitialCatalog = manifest.Database,
        IntegratedSecurity = false,
        UserID = manifest.RuntimeLogin,
        Password = manifest.RuntimePassword,
        Encrypt = true,
        TrustServerCertificate = true
    }.ConnectionString;
}
