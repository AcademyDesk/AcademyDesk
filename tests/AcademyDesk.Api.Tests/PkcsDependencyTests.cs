using System.Reflection;
using System.Security.Cryptography.Pkcs;

namespace AcademyDesk.Api.Tests;

public sealed class PkcsDependencyTests
{
    [Fact]
    public void LoadedCmsAssemblyUsesPatchedNet10ServicingRelease()
    {
        // Checking only NuGet's graph misses older shared-framework asset selection.
        var assembly = typeof(SignedCms).Assembly;
        Assert.Same(assembly, typeof(EnvelopedCms).Assembly);
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Assert.NotNull(informationalVersion);
        Assert.True(Version.TryParse(informationalVersion.InformationalVersion.Split('+')[0], out var version));
        Assert.NotNull(version);
        Assert.Equal(10, version.Major);
        Assert.Equal(0, version.Minor);
        Assert.True(version.Build >= 13, "Loaded PKCS assembly must include the 10.0.13 security fixes.");
    }
}
