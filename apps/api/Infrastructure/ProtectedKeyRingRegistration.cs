using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace AcademyDesk.Api.Infrastructure;

/// <summary>Explicitly provisioned private storage only; never silently falls back.</summary>
public static class ProtectedKeyRingRegistration
{
    public const string Section = "Security:DataProtection";

    public static void AddProtectedKeyRing(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        var settings = configuration.GetSection(Section);
        var flag = settings["Enabled"];
        if (flag is null) return; // Preserve existing local/QA and deployment behavior.
        if (!bool.TryParse(flag, out var enabled)) throw Invalid("Enabled");
        if (!enabled) return;

        var directory = PrivatePath(settings["KeyRingDirectory"], environment, "KeyRingDirectory");
        if (!Directory.Exists(directory)) throw Invalid("KeyRingDirectory");
        var previous = settings.GetSection("DecryptionCertificates").GetChildren().ToArray();
        if (previous.Length > 64) throw Invalid("DecryptionCertificates");
        var certificates = new List<X509Certificate2>();
        try
        {
            certificates.Add(Load(settings, "CertificatePath", "CertificatePassword", environment, directory, true));
            foreach (var entry in previous)
                certificates.Add(Load(entry, "Path", "Password", environment, directory, false));

            var material = new KeyMaterial(certificates.ToArray());
            services.AddSingleton(_ => material);
            services.AddDataProtection()
                .SetApplicationName($"AcademyDesk:{environment.EnvironmentName}")
                .PersistKeysToFileSystem(new DirectoryInfo(directory))
                .ProtectKeysWithCertificate(material.Certificates[0])
                .UnprotectKeysWithAnyCertificate(material.Certificates);
            // Resolve the owner whenever key-management options are built, so the DI
            // container disposes private-key handles along with its protection provider.
            services.AddOptions<KeyManagementOptions>().Configure<KeyMaterial>((_, _) => { });
        }
        catch
        {
            foreach (var certificate in certificates) certificate.Dispose();
            throw;
        }
    }

    private static X509Certificate2 Load(IConfiguration settings, string pathKey, string passwordKey,
        IWebHostEnvironment environment, string directory, bool encryptNewKeys)
    {
        X509Certificate2? certificate = null;
        try
        {
            var path = PrivatePath(settings[pathKey], environment, "Certificate");
            if (Within(path, directory) || !File.Exists(path) ||
                !(Path.GetExtension(path).Equals(".pfx", StringComparison.OrdinalIgnoreCase) ||
                  Path.GetExtension(path).Equals(".p12", StringComparison.OrdinalIgnoreCase)) ||
                string.IsNullOrEmpty(settings[passwordKey])) throw Invalid("Certificate");
            certificate = X509CertificateLoader.LoadPkcs12FromFile(path, settings[passwordKey], X509KeyStorageFlags.EphemeralKeySet);
            using var rsa = certificate.GetRSAPrivateKey();
            var now = DateTime.UtcNow;
            if (rsa is null || rsa.KeySize < 2048 ||
                (encryptNewKeys && (certificate.NotBefore.ToUniversalTime() > now || certificate.NotAfter.ToUniversalTime() <= now)))
                throw Invalid("Certificate");
            return certificate;
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            certificate?.Dispose();
            // Never attach a loader exception, configured private path or password.
            throw Invalid("Certificate");
        }
    }

    private static string PrivatePath(string? path, IWebHostEnvironment environment, string setting)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) throw Invalid(setting);
            var full = Path.GetFullPath(path);
            var root = Path.GetPathRoot(full)!;
            if (Path.TrimEndingDirectorySeparator(full) == Path.TrimEndingDirectorySeparator(root) ||
                Within(full, environment.ContentRootPath) ||
                Within(full, string.IsNullOrWhiteSpace(environment.WebRootPath)
                    ? Path.Combine(environment.ContentRootPath, "wwwroot") : environment.WebRootPath)) throw Invalid(setting);
            // Reject links and repository ancestry rather than allowing a seemingly
            // private location to resolve into publicly served or committed files.
            for (var current = full; current is not null; current = Path.GetDirectoryName(current))
            {
                if ((File.Exists(current) || Directory.Exists(current)) &&
                    (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw Invalid(setting);
                if (Directory.Exists(Path.Combine(current, ".git")) || File.Exists(Path.Combine(current, ".git"))) throw Invalid(setting);
            }
            return full;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        { throw Invalid(setting); }
    }

    private static bool Within(string path, string parent)
    {
        var relative = Path.GetRelativePath(Path.GetFullPath(parent), path);
        return relative == "." || (!Path.IsPathRooted(relative) && relative != ".." &&
            !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }

    private static InvalidOperationException Invalid(string setting) =>
        new($"{Section}:{setting} requires valid, separately provisioned private key storage and encryption material. No fallback is permitted.");

    private sealed class KeyMaterial(X509Certificate2[] certificates) : IDisposable
    {
        public X509Certificate2[] Certificates { get; } = certificates;
        public void Dispose() { foreach (var certificate in Certificates) certificate.Dispose(); }
    }
}
