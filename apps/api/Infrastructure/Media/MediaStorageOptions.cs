using System.Text.RegularExpressions;

namespace AcademyDesk.Api.Infrastructure.Media;

public sealed class MediaStorageOptions
{
    public const long MaximumFileBytes = 2_000_000_000;
    public const int DefaultChunkBytes = 8 * 1024 * 1024;
    public bool Enabled { get; set; }
    public string ServiceUri { get; set; } = "";
    public string ContainerName { get; set; } = "";
    public string? ManagedIdentityClientId { get; set; }
    public long MaxFileBytes { get; set; } = MaximumFileBytes;
    public int ChunkBytes { get; set; } = DefaultChunkBytes;

    public void Validate()
    {
        if (MaxFileBytes is < 1 or > MaximumFileBytes || ChunkBytes is < 65536 or > DefaultChunkBytes)
            throw new InvalidOperationException("Media storage limits are invalid.");
        if (!Enabled) return;
        if (!Uri.TryCreate(ServiceUri, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.Port != 443 || uri.AbsolutePath != "/" || uri.Query.Length != 0 ||
            uri.Fragment.Length != 0 || uri.UserInfo.Length != 0 ||
            !Regex.IsMatch(uri.Host, @"^[a-z0-9]{3,24}\.blob\.core\.windows\.net$"))
            throw new InvalidOperationException("Media storage requires an Azure Blob HTTPS service endpoint without credentials.");
        if (!Regex.IsMatch(ContainerName, @"^[a-z0-9](?:[a-z0-9-]{1,61})[a-z0-9]$") || ContainerName.Contains("--"))
            throw new InvalidOperationException("The media Blob container name is invalid.");
        if (!string.IsNullOrEmpty(ManagedIdentityClientId) && !Guid.TryParse(ManagedIdentityClientId, out _))
            throw new InvalidOperationException("The media managed identity client ID is invalid.");
    }
}
