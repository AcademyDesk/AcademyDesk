using System.Text.Json;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace AcademyDesk.Api.Tests;

public sealed class ClassMaterialDownloadTicketTests
{
    private const string Origin = "http://localhost:3000";
    private const string Path = "/api/class-media/11111111-1111-1111-1111-111111111111/content";
    private static ApplicationUser User() => new() { Id = Guid.NewGuid(), AcademyId = Guid.NewGuid(), SecurityStamp = "synthetic-stamp", IsActive = true };
    private static ClassMaterialDownloadTickets Service(IDataProtectionProvider provider, string environment = "Testing") =>
        new(provider, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = Origin }).Build(), new TestEnvironment { EnvironmentName = environment });

    [Fact]
    public void CredentialIsShortLivedAndBoundToCurrentIdentityPathOriginAndPurpose()
    {
        var provider = new EphemeralDataProtectionProvider(); var service = Service(provider); var user = User();
        var value = service.Create(user, Path, Origin);
        var result = service.Read(value, Path, Origin);
        Assert.NotNull(result); Assert.Equal(user.Id, result.UserId); Assert.Equal(user.AcademyId, result.AcademyId); Assert.Equal(user.SecurityStamp, result.SecurityStamp);
        Assert.Null(service.Read(value, Path.Replace("11111111", "22222222"), Origin));
        Assert.Null(service.Read(value, Path, "https://foreign.example.invalid"));
        var plaintext = provider.CreateProtector(ClassMaterialDownloadTickets.Purpose).ToTimeLimitedDataProtector().Unprotect(value, out var expiry);
        Assert.Contains("synthetic-stamp", plaintext); Assert.InRange((expiry - DateTimeOffset.UtcNow).TotalSeconds, 50, 60);
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() => provider.CreateProtector("Identity.Bearer").Unprotect(value));
    }

    [Fact]
    public void ExpiryTamperingMalformedPayloadAndLongLifetimeAreRejected()
    {
        var provider = new EphemeralDataProtectionProvider(); var service = Service(provider); var user = User();
        var protector = provider.CreateProtector(ClassMaterialDownloadTickets.Purpose).ToTimeLimitedDataProtector();
        var payload = JsonSerializer.Serialize(new ClassMaterialDownloadTickets.DownloadScope(user.Id, user.AcademyId!.Value, user.SecurityStamp!, Path, Origin));
        Assert.Null(service.Read(protector.Protect(payload, DateTimeOffset.UtcNow.AddSeconds(-1)), Path, Origin));
        Assert.Null(service.Read(protector.Protect(payload, TimeSpan.FromHours(1)), Path, Origin));
        Assert.Null(service.Read(protector.Protect("not-json", TimeSpan.FromSeconds(60)), Path, Origin));
        var value = service.Create(user, Path, Origin);
        Assert.Null(service.Read("X" + value[1..], Path, Origin));
        Assert.Null(service.Read(new string('a', 3001), Path, Origin));
        Assert.Null(service.Read("A", Path, Origin)); Assert.Null(service.Read("!", Path, Origin));
    }

    [Theory]
    [InlineData("/api/teacher/me")]
    [InlineData("/api/class-media/11111111-1111-1111-1111-111111111111/content?ticket=x")]
    [InlineData("https://evil.example.invalid/content")]
    [InlineData("//evil.example.invalid/content")]
    [InlineData("/uploads/teacher-materials/../file.mov")]
    [InlineData("/uploads/teacher-materials/11111111111111111111111111111111.mov?x=1")]
    [InlineData("/api/class-media/------------------------------------/content")]
    public void PathsCannotEscapeReadOnlyAttachmentRoutes(string path) => Assert.False(ClassMaterialDownloadTickets.IsContentPath(path));

    [Theory]
    [InlineData("/uploads/teacher-materials/11111111111111111111111111111111.mov")]
    [InlineData("/uploads/learning-resources/11111111111111111111111111111111.pdf")]
    [InlineData(Path)]
    public void ExactLegacyAndBlobPathsAreAccepted(string path) => Assert.True(ClassMaterialDownloadTickets.IsContentPath(path));

    [Fact]
    public void UnconfiguredUntrustedAndProductionPlaintextOriginsFailClosed()
    {
        var service = Service(new EphemeralDataProtectionProvider());
        Assert.True(service.IsApprovedOrigin(Origin));
        Assert.False(service.IsApprovedOrigin(Origin + "/")); Assert.False(service.IsApprovedOrigin("null")); Assert.False(service.IsApprovedOrigin("https://foreign.example.invalid"));
        Assert.False(Service(new EphemeralDataProtectionProvider(), "Production").IsApprovedOrigin(Origin));
        var production = Service(new EphemeralDataProtectionProvider(), "Production");
        var context = new DefaultHttpContext(); context.Request.Host = new HostString("localhost", 5092);
        Assert.True(service.IsSafeTransport(context.Request)); Assert.False(production.IsSafeTransport(context.Request));
        context.Request.Scheme = "https"; Assert.True(production.IsSafeTransport(context.Request));
    }

    [Fact]
    public void InactiveOrUnscopedIdentityCannotMintDownloadCredential()
    {
        var service = Service(new EphemeralDataProtectionProvider()); var user = User();
        user.IsActive = false; Assert.Throws<InvalidOperationException>(() => service.Create(user, Path, Origin));
        user.IsActive = true; user.AcademyId = null; Assert.Throws<InvalidOperationException>(() => service.Create(user, Path, Origin));
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Synthetic Ticket Unit Test";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
