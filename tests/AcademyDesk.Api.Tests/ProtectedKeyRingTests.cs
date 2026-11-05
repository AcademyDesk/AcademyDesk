using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AcademyDesk.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace AcademyDesk.Api.Tests;

public sealed class ProtectedKeyRingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void DisabledPreservesExistingRegistrations(string? enabled)
    {
        var services = new ServiceCollection();
        services.AddProtectedKeyRing(Config(new() { ["Enabled"] = enabled, ["CertificatePath"] = "not-read" }), new Environment());
        Assert.Empty(services);
    }

    [Fact]
    public void MalformedFlagFailsWithoutEchoingInput()
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddProtectedKeyRing(
            Config(new() { ["Enabled"] = "invalid-private-value" }), new Environment()));
        Assert.DoesNotContain("invalid-private-value", error.ToString());
        Assert.Null(error.InnerException);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("relative")]
    [InlineData("not-provisioned")]
    [InlineData("content")]
    [InlineData("web")]
    [InlineData("repository")]
    [InlineData("volume")]
    public void UnsafeKeyDirectoryFailsBeforeKeyWrites(string scenario)
    {
        using var fixture = new Fixture();
        var values = fixture.Values();
        if (scenario == "repository") Directory.CreateDirectory(Path.Combine(fixture.Keys, ".git"));
        values["KeyRingDirectory"] = scenario switch
        {
            "missing" => null, "relative" => "keys", "not-provisioned" => Path.Combine(fixture.Root, "absent"),
            "content" => fixture.Environment.ContentRootPath, "web" => fixture.Environment.WebRootPath,
            "volume" => Path.GetPathRoot(fixture.Root), _ => fixture.Keys
        };
        AssertSafeFailure(fixture, values);
        Assert.Empty(Directory.GetFiles(fixture.Keys));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("relative")]
    [InlineData("not-provisioned")]
    [InlineData("content")]
    [InlineData("in-key-ring")]
    [InlineData("no-password")]
    [InlineData("wrong-password")]
    [InlineData("public-only")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("non-rsa")]
    public void UnsafeEncryptionMaterialFailsBeforeKeyWrites(string scenario)
    {
        using var fixture = new Fixture();
        var values = fixture.Values();
        values["CertificatePath"] = scenario switch
        {
            "missing" => null, "relative" => "private.pfx", "not-provisioned" => Path.Combine(fixture.Root, "absent.pfx"),
            "content" => Path.Combine(fixture.Environment.ContentRootPath, "private.pfx"),
            "in-key-ring" => Path.Combine(fixture.Keys, "private.pfx"),
            "public-only" or "expired" or "future" or "non-rsa" => fixture.Certificate(scenario), _ => fixture.Active
        };
        if (scenario == "no-password") values["CertificatePassword"] = null;
        if (scenario == "wrong-password") values["CertificatePassword"] = "synthetic-wrong-value";
        AssertSafeFailure(fixture, values);
        Assert.Empty(Directory.GetFiles(fixture.Keys));
    }

    [Fact]
    public void InvalidRetiredCertificateDoesNotDowngradeEncryption()
    {
        using var fixture = new Fixture();
        var values = fixture.Values();
        values["DecryptionCertificates:0:Path"] = Path.Combine(fixture.Root, "absent.pfx");
        values["DecryptionCertificates:0:Password"] = fixture.Password;
        AssertSafeFailure(fixture, values);
        Assert.Empty(Directory.GetFiles(fixture.Keys));
    }

    [Fact]
    public void PersistedEncryptedKeysRecoverTicketsAndScopedStateAcrossProviders()
    {
        using var fixture = new Fixture();
        var academy = Guid.NewGuid().ToString("D"); var actor = Guid.NewGuid().ToString("D"); var conversation = Guid.NewGuid().ToString("D");
        string state, access, refresh;
        using (var first = fixture.Provider())
        {
            var options = first.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actor)], IdentityConstants.BearerScheme)),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, IdentityConstants.BearerScheme);
            access = options.BearerTokenProtector.Protect(ticket);
            refresh = options.RefreshTokenProtector.Protect(ticket);
            state = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("PentaMini.local.v1", academy, actor, conversation)
                .Protect("Synthetic scoped state");
        }
        var files = Directory.GetFiles(fixture.Keys, "key-*.xml");
        Assert.NotEmpty(files);
        foreach (var file in files)
        {
            var xml = File.ReadAllText(file);
            Assert.Contains("encryptedSecret", xml);
            Assert.DoesNotContain("<masterKey", xml);
            Assert.DoesNotContain(fixture.Password, xml);
            Assert.DoesNotContain("Synthetic scoped state", xml);
        }
        using var restarted = fixture.Provider();
        var recovered = restarted.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(IdentityConstants.BearerScheme);
        Assert.Equal(actor, recovered.BearerTokenProtector.Unprotect(access)!.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Equal(actor, recovered.RefreshTokenProtector.Unprotect(refresh)!.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        Assert.Null(recovered.BearerTokenProtector.Unprotect(refresh));
        var protection = restarted.GetRequiredService<IDataProtectionProvider>();
        Assert.Equal("Synthetic scoped state", protection.CreateProtector("PentaMini.local.v1", academy, actor, conversation).Unprotect(state));
        Assert.Throws<CryptographicException>(() => protection.CreateProtector("PentaMini.local.v1", academy, Guid.NewGuid().ToString("D"), conversation).Unprotect(state));
        Assert.Throws<CryptographicException>(() => protection.CreateProtector("PentaMini.local.v1", Guid.NewGuid().ToString("D"), actor, conversation).Unprotect(state));
        Assert.Throws<CryptographicException>(() => protection.CreateProtector("PentaMini.local.v1", academy, actor, Guid.NewGuid().ToString("D")).Unprotect(state));
        using var otherEnvironment = fixture.Provider(environment: "Production");
        Assert.Throws<CryptographicException>(() => otherEnvironment.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("PentaMini.local.v1", academy, actor, conversation).Unprotect(state));
    }

    [Fact]
    public void CertificateRotationRetainsOldReceiptsOnlyWithApprovedOldKeyMaterial()
    {
        using var fixture = new Fixture();
        string oldPayload;
        using (var first = fixture.Provider()) oldPayload = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation").Protect("before");
        var values = fixture.Values();
        values["CertificatePath"] = fixture.Certificate("replacement");
        values["DecryptionCertificates:0:Path"] = fixture.Active;
        values["DecryptionCertificates:0:Password"] = fixture.Password;
        string newPayload;
        Guid replacementKey;
        using (var rotating = fixture.Provider(values))
        {
            var protector = rotating.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation");
            Assert.Equal("before", protector.Unprotect(oldPayload));
            replacementKey = rotating.GetRequiredService<IKeyManager>()
                .CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90)).KeyId;
        }
        // A running provider can retain its cached default key. A reviewed rotation
        // restarts writers after provisioning the newly activated key, before retirement.
        using (var writer = fixture.Provider(values))
            newPayload = writer.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation").Protect("after");
        Assert.Equal(replacementKey, new Guid(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(newPayload).AsSpan(4, 16)));
        using (var restarted = fixture.Provider(values))
        {
            var protector = restarted.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation");
            Assert.Equal("before", protector.Unprotect(oldPayload));
            Assert.Equal("after", protector.Unprotect(newPayload));
        }
        values.Remove("DecryptionCertificates:0:Path"); values.Remove("DecryptionCertificates:0:Password");
        using var retired = fixture.Provider(values);
        var newProtector = retired.GetRequiredService<IDataProtectionProvider>().CreateProtector("rotation");
        Assert.Equal("after", newProtector.Unprotect(newPayload));
        Assert.Throws<CryptographicException>(() => newProtector.Unprotect(oldPayload));
    }

    private static void AssertSafeFailure(Fixture fixture, Dictionary<string, string?> values)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddProtectedKeyRing(Config(values), fixture.Environment));
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(fixture.Root, error.ToString());
        Assert.DoesNotContain(fixture.Password, error.ToString());
        Assert.DoesNotContain("synthetic-wrong-value", error.ToString());
    }

    private static IConfiguration Config(Dictionary<string, string?> values) => new ConfigurationBuilder()
        .AddInMemoryCollection(values.ToDictionary(x => ProtectedKeyRingRegistration.Section + ":" + x.Key, x => x.Value)).Build();

    private sealed class Environment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "KeyRingTests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class Fixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "AcademyDesk-KeyRing-QA-" + Guid.NewGuid().ToString("N"));
        public string Password { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        public string Keys => Path.Combine(Root, "keys");
        public string Active { get; }
        public Environment Environment { get; }
        public Fixture()
        {
            Directory.CreateDirectory(Keys);
            Environment = new() { ContentRootPath = Path.Combine(Root, "app"), WebRootPath = Path.Combine(Root, "public") };
            Directory.CreateDirectory(Environment.ContentRootPath); Directory.CreateDirectory(Environment.WebRootPath);
            Active = Certificate("active");
        }
        public string Certificate(string variant)
        {
            using var rsa = RSA.Create(2048);
            using var ec = ECDsa.Create();
            var request = variant == "non-rsa" ? new CertificateRequest("CN=Synthetic-QA", ec, HashAlgorithmName.SHA256)
                : new CertificateRequest("CN=Synthetic-QA", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var now = DateTimeOffset.UtcNow;
            using var certificate = request.CreateSelfSigned(variant == "future" ? now.AddDays(1) : now.AddDays(-2),
                variant == "expired" ? now.AddDays(-1) : now.AddDays(30));
            var path = Path.Combine(Root, variant + ".pfx");
            if (variant == "public-only")
            {
                using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
                File.WriteAllBytes(path, publicCertificate.Export(X509ContentType.Pfx, Password));
            }
            else File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, Password));
            return path;
        }
        public Dictionary<string, string?> Values() => new()
        {
            ["Enabled"] = "true", ["KeyRingDirectory"] = Keys, ["CertificatePath"] = Active, ["CertificatePassword"] = Password
        };
        public ServiceProvider Provider(Dictionary<string, string?>? values = null, string environment = "Testing")
        {
            var services = new ServiceCollection(); services.AddLogging();
            services.AddProtectedKeyRing(Config(values ?? Values()), new Environment
                { ContentRootPath = Environment.ContentRootPath, WebRootPath = Environment.WebRootPath, EnvironmentName = environment });
            services.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
            return services.BuildServiceProvider();
        }
        public void Dispose()
        {
            var full = Path.GetFullPath(Root);
            if (Path.GetDirectoryName(full) != Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) ||
                !Path.GetFileName(full).StartsWith("AcademyDesk-KeyRing-QA-", StringComparison.Ordinal)) throw new InvalidOperationException("Unsafe QA cleanup target.");
            Directory.Delete(full, true); // Only this fixture's synthetic certificates/key ring.
        }
    }
}
