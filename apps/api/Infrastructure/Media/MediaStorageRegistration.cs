using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace AcademyDesk.Api.Infrastructure.Media;

public static class MediaStorageRegistration
{
    public static IServiceCollection AddPrivateMediaStorage(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var options = configuration.GetSection("MediaStorage").Get<MediaStorageOptions>() ?? new();
        options.Validate();
        services.AddSingleton(options);
        services.AddSingleton<IMediaBlobStore>(_ =>
        {
            if (!options.Enabled) return new UnconfiguredMediaBlobStore();
            TokenCredential credential = environment.IsDevelopment()
                ? new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeInteractiveBrowserCredential = true })
                : string.IsNullOrWhiteSpace(options.ManagedIdentityClientId)
                    ? new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned)
                    : new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(options.ManagedIdentityClientId));
            var client = new BlobServiceClient(new Uri(options.ServiceUri), credential);
            return new AzureMediaBlobStore(client.GetBlobContainerClient(options.ContainerName), options);
        });
        return services;
    }

    private sealed class UnconfiguredMediaBlobStore : IMediaBlobStore
    {
        public bool IsConfigured => false;
        private static InvalidOperationException Unavailable() => new("Private class media storage is not configured.");
        public Task StageAsync(MediaUpload upload, int blockIndex, Stream content, CancellationToken token) => throw Unavailable();
        public Task<MediaUploadProgress> ProgressAsync(MediaUpload upload, CancellationToken token) => throw Unavailable();
        public Task<MediaBlobInfo> CommitAsync(MediaUpload upload, CancellationToken token) => throw Unavailable();
        public Task<MediaBlobInfo> PropertiesAsync(Guid academyId, Guid resourceId, CancellationToken token) => throw Unavailable();
        public Task<MediaBlobRead> ReadAsync(Guid academyId, Guid resourceId, long offset, long? length, CancellationToken token) => throw Unavailable();
    }
}
