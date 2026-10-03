using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace AcademyDesk.Api.Tests;

public sealed class PrivateMaterialFileProviderTests
{
    [Theory]
    [InlineData("/uploads/teacher-materials/file.pdf")]
    [InlineData("uploads/learning-resources/file.mp4")]
    [InlineData("UPLOADS/TEACHER-MATERIALS/file.pdf")]
    [InlineData("/uploads//teacher-materials/file.pdf")]
    [InlineData("/uploads/other/../teacher-materials/file.pdf")]
    [InlineData("uploads\\teacher-materials\\file.pdf")]
    [InlineData("uploads/teacher-materials./file.pdf")]
    [InlineData("uploads./teacher-materials/file.pdf")]
    [InlineData("uploads/TEACHE~1/file.pdf")]
    public void PrivateMaterialPathsNeverReachPublicProvider(string path)
    {
        var inner = new RecordingProvider();
        var provider = new PrivateMaterialFileProvider(inner);
        Assert.False(provider.GetFileInfo(path).Exists);
        Assert.False(provider.GetDirectoryContents(path).Exists);
        _ = provider.Watch(path);
        Assert.Equal(0, inner.Calls);
    }

    [Theory]
    [InlineData("/assets/logo.png")]
    [InlineData("/uploads/profile-images/file.png")]
    [InlineData("/uploads/teacher-materials-other/file.pdf")]
    public void UnrelatedPublicAssetsStillUseOriginalProvider(string path)
    {
        var inner = new RecordingProvider();
        var provider = new PrivateMaterialFileProvider(inner);
        _ = provider.GetFileInfo(path);
        _ = provider.GetDirectoryContents(path);
        _ = provider.Watch(path);
        Assert.Equal(3, inner.Calls);
    }

    private sealed class RecordingProvider : IFileProvider
    {
        public int Calls { get; private set; }
        public IFileInfo GetFileInfo(string path) { Calls++; return new NotFoundFileInfo(path); }
        public IDirectoryContents GetDirectoryContents(string path) { Calls++; return NotFoundDirectoryContents.Singleton; }
        public IChangeToken Watch(string path) { Calls++; return NullChangeToken.Singleton; }
    }
}
