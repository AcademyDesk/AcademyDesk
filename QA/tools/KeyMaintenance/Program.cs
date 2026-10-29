using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;

// Disposable fixture only; no SQL, application startup, production keys or network.
try
{
    var run = Environment.GetEnvironmentVariable("ACADEMY_QA_CERT_RUN") ?? "";
    var password = Environment.GetEnvironmentVariable("ACADEMY_QA_CERT_PASSWORD") ?? "";
    if (args.Length != 1 || args[0] != "create-synthetic-key" ||
        !Regex.IsMatch(run, "^[0-9a-f]{32}$") || !Regex.IsMatch(password, "^Qa!a9[0-9a-f]{40}$") ||
        File.ReadAllText("/qa-private/owner") != run)
        throw new InvalidOperationException();
    var files = Directory.GetFiles("/qa-private/keys", "key-*.xml");
    if (files.Length is < 1 or > 8) throw new InvalidOperationException();
    using var certificate = X509CertificateLoader.LoadPkcs12FromFile(
        "/qa-private/certs/replacement.pfx", password, X509KeyStorageFlags.EphemeralKeySet);
    if (certificate.Subject != "CN=AcademyDesk-QA-" + run || !certificate.HasPrivateKey)
        throw new InvalidOperationException();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddDataProtection().SetApplicationName("AcademyDesk:Production")
        .PersistKeysToFileSystem(new DirectoryInfo("/qa-private/keys"))
        .ProtectKeysWithCertificate(certificate);
    using var provider = services.BuildServiceProvider();
    var now = DateTimeOffset.UtcNow;
    var created = provider.GetRequiredService<IKeyManager>().CreateNewKey(now, now.AddDays(90));
    Console.WriteLine(JsonSerializer.Serialize(new { keyId = created.KeyId, syntheticOnly = true }));
}
catch
{
    Console.Error.WriteLine("Synthetic key maintenance failed; private details withheld.");
    Environment.ExitCode = 1;
}
