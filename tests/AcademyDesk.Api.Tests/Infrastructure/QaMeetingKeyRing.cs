using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace AcademyDesk.Api.Tests.Infrastructure;

// Synthetic fixture only. Never load application configuration or a real key ring.
// Generated private material stays outside Git/evidence and is ownership-cleaned.
internal sealed class QaMeetingKeyRing : IDisposable
{
    private readonly string owner = Guid.NewGuid().ToString("N");
    private readonly string password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    internal string Root { get; }
    internal string Keys => Path.Combine(Root, "keys");
    private string CertificateA => Path.Combine(Root, "original.pfx");
    private string CertificateB => Path.Combine(Root, "replacement.pfx");
    internal QaMeetingKeyRing()
    {
        Root = Path.Combine(Path.GetTempPath(), "AcademyDesk-MeetingKeys-QA-" + owner);
        Directory.CreateDirectory(Keys); File.WriteAllText(Path.Combine(Root, ".qa-owner"), owner);
        try { CreateCertificate(CertificateA); CreateCertificate(CertificateB); }
        catch { Dispose(); throw; }
    }
    private void CreateCertificate(string path)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Synthetic-Meeting-Key-QA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
    }
    internal ServiceProvider Provider(bool rotated = false, bool keepOriginal = true, string? directory = null, string environment = "Testing")
    {
        directory ??= Keys; RequireOwned(directory);
        var values = new Dictionary<string, string?> {
            ["Enabled"] = "true", ["KeyRingDirectory"] = directory,
            ["CertificatePath"] = rotated ? CertificateB : CertificateA, ["CertificatePassword"] = password };
        if (rotated && keepOriginal) { values["DecryptionCertificates:0:Path"] = CertificateA; values["DecryptionCertificates:0:Password"] = password; }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(x => ProtectedKeyRingRegistration.Section + ":" + x.Key, x => x.Value)).Build();
        var services = new ServiceCollection(); services.AddLogging();
        services.AddProtectedKeyRing(configuration, new HostEnvironment(Root, environment));
        return services.BuildServiceProvider();
    }
    internal Guid Rotate()
    {
        using var provider = Provider(rotated: true);
        var now = DateTimeOffset.UtcNow;
        return provider.GetRequiredService<IKeyManager>().CreateNewKey(now, now.AddDays(90)).KeyId;
        // Callers must construct a fresh writer; no fleet-wide cache guarantee.
    }
    internal string Empty(string name)
    {
        if (name.Length is < 1 or > 40 || !name.All(c => char.IsAsciiLetterLower(c) || c == '-')) throw new InvalidOperationException("Invalid synthetic key directory label.");
        var path = Path.Combine(Root, name); if (Directory.Exists(path)) throw new InvalidOperationException("Synthetic key directory already exists.");
        Directory.CreateDirectory(path); RequireOwned(path); return path;
    }
    internal string Snapshot(string name, string? source = null)
    {
        source ??= Keys; RequireOwned(source); var destination = Empty(name);
        foreach (var file in Directory.GetFiles(source, "*.xml"))
        {
            RequireOwned(file); var leaf = Path.GetFileName(file);
            if (!leaf.StartsWith("key-", StringComparison.Ordinal) && !leaf.StartsWith("revocation-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected synthetic key file.");
            File.Copy(file, Path.Combine(destination, leaf)); // New owned snapshot only; never overwrite.
        }
        return destination;
    }
    internal bool EncryptedFilesOnly()
    {
        var files = Directory.GetFiles(Keys, "key-*.xml");
        return files.Length >= 2 && files.All(path => { var xml = File.ReadAllText(path); return xml.Contains("encryptedSecret", StringComparison.Ordinal) && !xml.Contains("<masterKey", StringComparison.Ordinal) && !xml.Contains(password, StringComparison.Ordinal); });
    }
    private void RequireOwned(string path)
    {
        var full = Path.GetFullPath(path); var root = Path.GetFullPath(Root);
        var relative = Path.GetRelativePath(root, full);
        if (Path.IsPathRooted(relative) || relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            Path.GetDirectoryName(root) != Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) ||
            Path.GetFileName(root) != "AcademyDesk-MeetingKeys-QA-" + owner || File.ReadAllText(Path.Combine(root, ".qa-owner")) != owner)
            throw new InvalidOperationException("Refused synthetic key fixture path.");
        for (var current = full; current is not null && current.Length >= root.Length; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Refused linked synthetic key fixture path.");
    }
    internal bool Exists => Directory.Exists(Root);
    public void Dispose()
    {
        if (!Directory.Exists(Root)) return; RequireOwned(Root);
        void CheckTree(string path)
        {
            foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos())
            {
                RequireOwned(entry.FullName);
                if (entry is DirectoryInfo) CheckTree(entry.FullName);
            }
        }
        CheckTree(Root); Directory.Delete(Path.GetFullPath(Root), recursive: true); // Exact owned temp synthetic certificates/key files only.
    }
    public override string ToString() => "[Disposable synthetic meeting key fixture; material redacted]";
    private sealed class HostEnvironment(string root, string environment) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environment;
        public string ApplicationName { get; set; } = "SyntheticMeetingKeyQA";
        public string ContentRootPath { get; set; } = Path.Combine(root, "host");
        public string WebRootPath { get; set; } = Path.Combine(root, "public");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
