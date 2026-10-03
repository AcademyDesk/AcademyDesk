using AcademyDesk.Api.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace AcademyDesk.Api.Tests;

public sealed class DevelopmentSeedCredentialsTests
{
    [Fact]
    public void ReadsAllThreeValuesFromConfiguration()
    {
        var credentials = DevelopmentSeedCredentials.FromConfiguration(Configuration());

        Assert.Equal("test-only-default", credentials.DefaultPassword);
        Assert.Equal("test-only-owner", credentials.PlatformOwnerPassword);
        Assert.Equal("test-only-admin", credentials.AcademyAdminPassword);
    }

    [Theory]
    [InlineData("DevelopmentSeed:DefaultPassword")]
    [InlineData("DevelopmentSeed:PlatformOwnerPassword")]
    [InlineData("DevelopmentSeed:AcademyAdminPassword")]
    public void MissingValueFailsWithoutDisclosingOtherValues(string missingKey)
    {
        var values = Values();
        values.Remove(missingKey);

        var error = Assert.Throws<InvalidOperationException>(() =>
            DevelopmentSeedCredentials.FromConfiguration(Configuration(values)));

        Assert.Contains(missingKey, error.Message);
        Assert.DoesNotContain("test-only-", error.Message);
    }

    private static IConfiguration Configuration(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? Values()).Build();

    private static Dictionary<string, string?> Values() => new()
    {
        ["DevelopmentSeed:DefaultPassword"] = "test-only-default",
        ["DevelopmentSeed:PlatformOwnerPassword"] = "test-only-owner",
        ["DevelopmentSeed:AcademyAdminPassword"] = "test-only-admin"
    };
}
