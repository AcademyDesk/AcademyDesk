using System.Diagnostics;
using System.Text.Json;
using AcademyDesk.Api.Infrastructure.Media;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

internal sealed class QaBlobFixture(BlobContainerClient container, string run)
{
    public AzureMediaBlobStore Store { get; } = new(container, new MediaStorageOptions());
    public BlobContainerClient Container => container;
    public static async Task<QaBlobFixture> CreateAsync(Guid runId)
    {
        var run = runId.ToString("N");
        var name = $"academydesk-blob-qa-{run}";
        if (!int.TryParse(Environment.GetEnvironmentVariable("QA_BLOB_PORT"), out var port) || port < 1024 || port > 65535)
            throw new InvalidOperationException("Exact loopback emulator port required.");
        using var p = new Process { StartInfo = new("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false } };
        p.StartInfo.ArgumentList.Add("inspect"); p.StartInfo.ArgumentList.Add(name); p.Start();
        var json = await p.StandardOutput.ReadToEndAsync(); _ = await p.StandardError.ReadToEndAsync(); await p.WaitForExitAsync();
        if (p.ExitCode != 0) throw new InvalidOperationException("Owned emulator is missing.");
        using var doc = JsonDocument.Parse(json); var item = doc.RootElement[0];
        var bindings = item.GetProperty("NetworkSettings").GetProperty("Ports").GetProperty("10000/tcp");
        if (!item.GetProperty("State").GetProperty("Running").GetBoolean() || item.GetProperty("Name").GetString() != "/" + name ||
            item.GetProperty("Config").GetProperty("Image").GetString() != "mcr.microsoft.com/azure-storage/azurite:latest" ||
            item.GetProperty("Config").GetProperty("Labels").GetProperty("academydesk.qa.run").GetString() != run ||
            bindings.GetArrayLength() != 1 || bindings[0].GetProperty("HostIp").GetString() != "127.0.0.1" || bindings[0].GetProperty("HostPort").GetString() != port.ToString())
            throw new InvalidOperationException("Emulator identity/loopback ownership mismatch.");
        var account = "qa" + run[..20];
        var key = Environment.GetEnvironmentVariable("QA_BLOB_KEY") ?? throw new InvalidOperationException("Ephemeral emulator key required.");
        var service = new BlobServiceClient(new Uri($"http://127.0.0.1:{port}/{account}"), new StorageSharedKeyCredential(account, key),
            new BlobClientOptions(BlobClientOptions.ServiceVersion.V2023_11_03));
        var container = service.GetBlobContainerClient("qa-" + run);
        if (await container.ExistsAsync()) throw new InvalidOperationException("Refusing to reuse Blob fixture.");
        await container.CreateAsync(PublicAccessType.None, metadata: new Dictionary<string, string> { ["qarun"] = run });
        return new(container, run);
    }
    public async Task CleanupAsync()
    {
        var properties = (await container.GetPropertiesAsync()).Value;
        if (container.Name != "qa-" + run || !properties.Metadata.TryGetValue("qarun", out var marker) || marker != run)
            throw new InvalidOperationException("Refused Blob cleanup: ownership mismatch.");
        await container.DeleteAsync();
        Console.WriteLine("BLOB cleanup PASS: exact run-owned private container removed.");
    }
}
