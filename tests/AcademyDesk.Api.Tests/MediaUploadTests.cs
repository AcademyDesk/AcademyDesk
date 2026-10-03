using AcademyDesk.Api.Infrastructure.Media;

namespace AcademyDesk.Api.Tests;

public sealed class MediaUploadTests
{
    private static readonly MediaStorageOptions Options = new();

    [Theory]
    [InlineData("phone-picture.heic")]
    [InlineData("video-from-iPhone.mov")]
    [InlineData("audio-recording.ogg")]
    [InlineData("lesson.flac")]
    [InlineData("class-recording.webm")]
    [InlineData("material.zip")]
    [InlineData("பாடம்.pdf")]
    public void MediaAndOtherAttachmentsDoNotDependOnTheOldExtensionAllowlist(string name)
    {
        var upload = MediaUpload.Create(Guid.NewGuid(), Guid.NewGuid(), name, 1, Options);
        Assert.Equal(name, upload.FileName);
        Assert.Equal(1, upload.BlockCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2_000_000_001)]
    [InlineData(long.MaxValue)]
    public void InvalidSizeIsRejectedBeforeAnyStorageAccess(long length) =>
        Assert.Throws<ArgumentException>(() => MediaUpload.Create(Guid.NewGuid(), Guid.NewGuid(), "file.mp4", length, Options));

    [Fact]
    public void FullTwoGbPolicyUsesBoundedChunksAndAnExactTail()
    {
        var upload = MediaUpload.Create(Guid.NewGuid(), Guid.NewGuid(), "long-video.mp4", 2_000_000_000, Options);
        Assert.Equal(239, upload.BlockCount);
        Assert.Equal(8_388_608, upload.BlockLength(0));
        Assert.Equal(2_000_000_000, Enumerable.Range(0, upload.BlockCount).Sum(upload.BlockLength));
        Assert.Equal(upload.BlockId(17), upload.BlockId(17));
        Assert.Equal(upload.BlockId(0).Length, upload.BlockId(upload.BlockCount - 1).Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => upload.BlockLength(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => upload.BlockLength(upload.BlockCount));
    }

    [Theory]
    [InlineData("../private.mov")]
    [InlineData("C:\\private.mov")]
    [InlineData("file\n.mp4")]
    [InlineData("run.EXE")]
    [InlineData("run.EXE ")]
    [InlineData("page.html ")]
    [InlineData("page.html")]
    [InlineData("active.svg")]
    public void InvalidNamesAndActiveContentAreRejected(string name) =>
        Assert.Throws<ArgumentException>(() => MediaUpload.Create(Guid.NewGuid(), Guid.NewGuid(), name, 1, Options));

    [Theory]
    [InlineData("http://academydeskmedia.blob.core.windows.net")]
    [InlineData("https://academydeskmedia.blob.core.windows.net?sig=secret")]
    [InlineData("https://academydeskmedia.blob.core.windows.net/container")]
    [InlineData("https://academydeskmedia.blob.core.windows.net:444")]
    [InlineData("https://academydeskmedia.blob.core.windows.net.evil.example")]
    [InlineData("https://localhost")]
    public void AzureRegistrationRejectsUntrustedOrCredentialBearingEndpoints(string uri) =>
        Assert.Throws<InvalidOperationException>(() => new MediaStorageOptions { Enabled = true, ServiceUri = uri, ContainerName = "academydesk-media" }.Validate());

    [Fact]
    public void ValidAzureEndpointDoesNotRequireAnAccountKey() =>
        new MediaStorageOptions { Enabled = true, ServiceUri = "https://academydeskmedia.blob.core.windows.net", ContainerName = "academydesk-media" }.Validate();

    [Fact]
    public void AManuallyAlteredManifestCannotChangeTheApprovedChunkSize()
    {
        var upload = MediaUpload.Create(Guid.NewGuid(), Guid.NewGuid(), "lesson.mp4", 100, Options);
        Assert.Throws<ArgumentException>(() => (upload with { ChunkBytes = 1 }).Validate(Options));
        Assert.Throws<ArgumentException>(() => (upload with { FileName = " lesson.mp4 " }).Validate(Options));
    }
}
