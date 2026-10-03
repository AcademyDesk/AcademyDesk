using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;

namespace AcademyDesk.Api.Infrastructure.Media;

public sealed class AzureMediaBlobStore(BlobContainerClient container, MediaStorageOptions options) : IMediaBlobStore
{
    public bool IsConfigured => true;
    // No implicit container creation or public-disk fallback. Provisioning and
    // access grants are deployment decisions, not a side effect of an upload.
    private async Task RequirePrivateContainerAsync(CancellationToken token)
    {
        var properties = await container.GetPropertiesAsync(cancellationToken: token);
        if (properties.Value.PublicAccess != PublicAccessType.None)
            throw new InvalidOperationException("Class media requires a private Blob container.");
    }

    private BlockBlobClient Blob(Guid academyId, Guid resourceId)
    {
        if (academyId == Guid.Empty || resourceId == Guid.Empty) throw new ArgumentException("Media owner is required.");
        return container.GetBlockBlobClient($"teacher-media/{academyId:N}/{resourceId:N}/content");
    }

    public async Task StageAsync(MediaUpload upload, int blockIndex, Stream content, CancellationToken token)
    {
        upload.Validate(options);
        var expected = upload.BlockLength(blockIndex);
        if (!content.CanSeek || content.Length - content.Position != expected)
            throw new ArgumentException("The uploaded chunk does not match its expected length.");
        await RequirePrivateContainerAsync(token);
        var blob = Blob(upload.AcademyId, upload.ResourceId);
        if (await blob.ExistsAsync(token))
            throw new InvalidOperationException("This upload is already complete.");
        await blob.StageBlockAsync(upload.BlockId(blockIndex), content, cancellationToken: token);
    }

    public async Task<MediaUploadProgress> ProgressAsync(MediaUpload upload, CancellationToken token)
    {
        upload.Validate(options);
        await RequirePrivateContainerAsync(token);
        var blob = Blob(upload.AcademyId, upload.ResourceId);
        if (await blob.ExistsAsync(token))
        {
            var properties = (await blob.GetPropertiesAsync(cancellationToken: token)).Value;
            VerifyManifest(upload, properties);
            return new(true, Enumerable.Range(0, upload.BlockCount).ToArray());
        }
        BlockList blocks;
        try { blocks = (await blob.GetBlockListAsync(BlockListTypes.Uncommitted, cancellationToken: token)).Value; }
        catch (RequestFailedException error) when (error.Status == 404) { return new(false, []); }
        var expected = Enumerable.Range(0, upload.BlockCount).ToDictionary(upload.BlockId);
        var indices = new List<int>();
        foreach (var block in blocks.UncommittedBlocks)
        {
            if (!expected.TryGetValue(block.Name, out var index) || block.Size != upload.BlockLength(index))
                throw new InvalidOperationException("The stored chunks do not match this upload.");
            indices.Add(index);
        }
        return new(false, indices.Order().ToArray());
    }

    public async Task<MediaBlobInfo> CommitAsync(MediaUpload upload, CancellationToken token)
    {
        var progress = await ProgressAsync(upload, token);
        if (progress.Committed) return await PropertiesAsync(upload.AcademyId, upload.ResourceId, token);
        if (progress.UploadedBlocks.Count != upload.BlockCount)
        {
            // Another completion can consume the uncommitted list between the
            // existence check and list read. Accept only a matching committed file.
            if (await Blob(upload.AcademyId, upload.ResourceId).ExistsAsync(token))
            {
                var completed = (await Blob(upload.AcademyId, upload.ResourceId).GetPropertiesAsync(cancellationToken: token)).Value;
                VerifyManifest(upload, completed);
                return Info(completed);
            }
            throw new InvalidOperationException("Upload all file chunks before confirming the upload.");
        }
        try
        {
            await Blob(upload.AcademyId, upload.ResourceId).CommitBlockListAsync(
                Enumerable.Range(0, upload.BlockCount).Select(upload.BlockId), new CommitBlockListOptions
                {
                    // Keep original bytes as an attachment. A viewer must select a
                    // safe type from verified content; client MIME is never trusted.
                    HttpHeaders = new BlobHttpHeaders { ContentType = "application/octet-stream", ContentDisposition = "attachment", CacheControl = "private, no-store" },
                    Metadata = new Dictionary<string, string>
                    {
                        ["academyid"] = upload.AcademyId.ToString("N"),
                        ["resourceid"] = upload.ResourceId.ToString("N"),
                        ["filenamebase64"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(upload.FileName))
                    },
                    Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                }, token);
        }
        catch (RequestFailedException error) when (error.Status == 412 || (error.Status == 409 && error.ErrorCode == "BlobAlreadyExists"))
        {
            // Another completion request can win after both inspected the blocks.
            // Never replace that committed object; validate it and return its state.
        }
        catch (RequestFailedException error) when (error.Status == 400 && error.ErrorCode == "InvalidBlockList")
        {
            // Some providers validate the now-consumed block list before the
            // If-None-Match condition. This is idempotent ONLY if a committed file
            // exists and matches the immutable session (verified below).
            if (!await Blob(upload.AcademyId, upload.ResourceId).ExistsAsync(token)) throw;
        }
        var stored = (await Blob(upload.AcademyId, upload.ResourceId).GetPropertiesAsync(cancellationToken: token)).Value;
        VerifyManifest(upload, stored);
        return Info(stored);
    }

    public async Task<MediaBlobInfo> PropertiesAsync(Guid academyId, Guid resourceId, CancellationToken token)
    {
        await RequirePrivateContainerAsync(token);
        var properties = (await Blob(academyId, resourceId).GetPropertiesAsync(cancellationToken: token)).Value;
        VerifyOwner(academyId, resourceId, properties);
        return Info(properties);
    }

    public async Task<MediaBlobRead> ReadAsync(Guid academyId, Guid resourceId, long offset, long? length, CancellationToken token)
    {
        var info = await PropertiesAsync(academyId, resourceId, token);
        if (offset < 0 || offset >= info.Length || length is <= 0 || length > info.Length - offset)
            throw new ArgumentException("The requested media byte range is invalid.");
        var response = await Blob(academyId, resourceId).DownloadStreamingAsync(new BlobDownloadOptions
        {
            Range = new HttpRange(offset, length),
            Conditions = new BlobRequestConditions { IfMatch = new ETag(info.ETag) }
        }, token);
        return new(response.Value.Content, response.Value.Details.ContentLength, info.ETag);
    }

    private static void VerifyOwner(Guid academyId, Guid resourceId, BlobProperties properties)
    {
        if (properties.ContentLength < 1 || properties.ContentLength > MediaStorageOptions.MaximumFileBytes ||
            !properties.Metadata.TryGetValue("academyid", out var owner) || owner != academyId.ToString("N") ||
            !properties.Metadata.TryGetValue("resourceid", out var resource) || resource != resourceId.ToString("N"))
            throw new InvalidOperationException("The stored media ownership is invalid.");
    }

    private static void VerifyManifest(MediaUpload upload, BlobProperties properties)
    {
        VerifyOwner(upload.AcademyId, upload.ResourceId, properties);
        if (properties.ContentLength != upload.Length || Info(properties).FileName != upload.FileName)
            throw new InvalidOperationException("The committed media does not match this upload.");
    }

    private static MediaBlobInfo Info(BlobProperties properties) => new(properties.ContentLength,
        Encoding.UTF8.GetString(Convert.FromBase64String(properties.Metadata["filenamebase64"])), properties.ETag.ToString());
}
