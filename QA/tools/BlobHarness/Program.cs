using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Media;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

if (args.Length != 2 || !Guid.TryParseExact(args[0], "N", out var runId) ||
    !int.TryParse(args[1], out var port) || port < 1024 || port > 65535)
    throw new InvalidOperationException("Provide a generated QA run ID and the exact loopback emulator port.");
var run = runId.ToString("N");
var containerName = $"academydesk-blob-qa-{run}";
var account = $"qa{run[..20]}";
var key = Environment.GetEnvironmentVariable("QA_BLOB_KEY") ?? throw new InvalidOperationException("QA emulator key is required.");
using (var inspect = new Process { StartInfo = new("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false } })
{
    inspect.StartInfo.ArgumentList.Add("inspect");
    inspect.StartInfo.ArgumentList.Add(containerName);
    inspect.Start();
    var json = await inspect.StandardOutput.ReadToEndAsync();
    _ = await inspect.StandardError.ReadToEndAsync();
    await inspect.WaitForExitAsync();
    if (inspect.ExitCode != 0) throw new InvalidOperationException("QA storage emulator was not found.");
    using var document = JsonDocument.Parse(json);
    var item = document.RootElement[0];
    var bindings = item.GetProperty("NetworkSettings").GetProperty("Ports").GetProperty("10000/tcp");
    if (!item.GetProperty("State").GetProperty("Running").GetBoolean() ||
        item.GetProperty("Name").GetString() != "/" + containerName ||
        item.GetProperty("Config").GetProperty("Image").GetString() != "mcr.microsoft.com/azure-storage/azurite:latest" ||
        item.GetProperty("Config").GetProperty("Labels").GetProperty("academydesk.qa.run").GetString() != run ||
        bindings.GetArrayLength() != 1 || bindings[0].GetProperty("HostIp").GetString() != "127.0.0.1" ||
        bindings[0].GetProperty("HostPort").GetString() != port.ToString())
        throw new InvalidOperationException("QA emulator identity or loopback port does not match this run.");
}

// Only this labelled disposable emulator receives an account key. The application
// uses managed identity against Azure; the emulator cannot prove Azure RBAC.
var service = new BlobServiceClient(new Uri($"http://127.0.0.1:{port}/{account}"),
    new StorageSharedKeyCredential(account, key), new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03));
var privateContainer = service.GetBlobContainerClient($"qa-{run}");
var publicContainer = service.GetBlobContainerClient($"public-{run}");
if (await privateContainer.ExistsAsync() || await publicContainer.ExistsAsync())
    throw new InvalidOperationException("QA containers already exist; refusing reuse.");
await privateContainer.CreateAsync(PublicAccessType.None, metadata: new Dictionary<string, string> { ["qarun"] = run });
await publicContainer.CreateAsync(PublicAccessType.Blob, metadata: new Dictionary<string, string> { ["qarun"] = run });
var options = new MediaStorageOptions();
var store = new AzureMediaBlobStore(privateContainer, options);
var bytes = RandomNumberGenerator.GetBytes(options.ChunkBytes + 37);
var academyId = Guid.NewGuid();
var resourceId = Guid.NewGuid();
var upload = MediaUpload.Create(academyId, resourceId, "பாடம்-phone-video.mov", bytes.Length, options);
var checks = 0;
try
{
    Require((await store.ProgressAsync(upload, default)).UploadedBlocks.Count == 0, "Fresh upload is empty.");
    await Reject<ArgumentException>(() => store.StageAsync(upload, 0, new MemoryStream(bytes[..30]), default), "Wrong chunk length is rejected.");
    Require((await store.ProgressAsync(upload, default)).UploadedBlocks.Count == 0, "Invalid chunk creates no staged data.");
    await store.StageAsync(upload, 0, new MemoryStream(bytes, 0, options.ChunkBytes, false), default);
    await store.StageAsync(upload, 0, new MemoryStream(bytes, 0, options.ChunkBytes, false), default);
    var resumed = await new AzureMediaBlobStore(privateContainer, options).ProgressAsync(upload, default);
    Require(!resumed.Committed && resumed.UploadedBlocks.SequenceEqual(new[] { 0 }), "Retry and a new store instance resume the same staged block.");
    await Reject<InvalidOperationException>(() => store.CommitAsync(upload, default), "Missing tail prevents commit.");
    Require(!await privateContainer.GetBlobClient(upload.BlobName).ExistsAsync(), "Incomplete file is not committed.");
    await store.StageAsync(upload, 1, new MemoryStream(bytes, options.ChunkBytes, 37, false), default);
    var info = await store.CommitAsync(upload, default);
    Require(info.Length == bytes.Length && info.FileName == upload.FileName, "Commit preserves length and Unicode filename.");
    Require((await store.ProgressAsync(upload, default)).Committed, "Completion is durably visible.");
    Require((await store.CommitAsync(upload, default)).ETag == info.ETag, "Completion retry does not replace the file.");
    await Reject<InvalidOperationException>(() => store.StageAsync(upload, 0, new MemoryStream(bytes, 0, options.ChunkBytes, false), default), "Completed file cannot be overwritten by another chunk.");
    await using (var download = await store.ReadAsync(academyId, resourceId, 0, null, default))
    {
        using var output = new MemoryStream();
        await download.Content.CopyToAsync(output);
        Require(output.ToArray().SequenceEqual(bytes), "Full read returns exact bytes.");
    }
    await using (var partial = await store.ReadAsync(academyId, resourceId, 11, 42, default))
    {
        using var output = new MemoryStream();
        await partial.Content.CopyToAsync(output);
        Require(partial.Length == 42 && output.ToArray().SequenceEqual(bytes.Skip(11).Take(42)), "Range read returns exactly the requested bytes.");
    }
    using (var client = new HttpClient())
    {
        var anonymous = await client.GetAsync(privateContainer.GetBlobClient(upload.BlobName).Uri);
        var response = await anonymous.Content.ReadAsByteArrayAsync();
        Require(anonymous.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized && !response.SequenceEqual(bytes), "Private container denies anonymous content.");
    }
    await Reject<RequestFailedException>(() => store.PropertiesAsync(Guid.NewGuid(), resourceId, default), "Another academy key cannot resolve this file.");
    await Reject<InvalidOperationException>(() => new AzureMediaBlobStore(publicContainer, options).StageAsync(upload, 0, new MemoryStream(bytes, 0, options.ChunkBytes, false), default), "Public container is refused.");
    Require((await publicContainer.GetBlobsAsync().ToListAsync()).Count == 0, "Public-container refusal stores no bytes.");
    Console.WriteLine($"PASS: {checks} Blob foundation checks; exact roundtrip SHA256={Convert.ToHexString(SHA256.HashData(bytes))}; private storage, retry/resume, range, incomplete/overwrite refusal.");
}
finally
{
    foreach (var owned in new[] { privateContainer, publicContainer })
    {
        var properties = await owned.GetPropertiesAsync();
        if (!properties.Value.Metadata.TryGetValue("qarun", out var marker) || marker != run)
            throw new InvalidOperationException("Refusing cleanup without the matching storage run marker.");
        await owned.DeleteAsync();
    }
    Console.WriteLine("Removed both exact run-owned emulator Blob containers after ownership checks.");
}

void Require(bool pass, string name)
{
    if (!pass) throw new InvalidOperationException(name);
    checks++;
    Console.WriteLine("PASS: " + name);
}

async Task Reject<T>(Func<Task> operation, string name) where T : Exception
{
    try { await operation(); }
    catch (T) { checks++; Console.WriteLine("PASS: " + name); return; }
    throw new InvalidOperationException("Expected rejection: " + name);
}

// Materialize only the tiny synthetic listing, not uploaded file content.
internal static class EnumerationExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source) result.Add(item);
        return result;
    }
}
