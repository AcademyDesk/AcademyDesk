using System.Text;

namespace AcademyDesk.Api.Infrastructure.Media;

// API authorization will bind this manifest to an authenticated user and teaching
// scope. Storage never accepts a client-supplied container, blob URL or key.
public sealed record MediaUpload(Guid AcademyId, Guid ResourceId, string FileName, long Length, int ChunkBytes)
{
    public string BlobName => $"teacher-media/{AcademyId:N}/{ResourceId:N}/content";
    public int BlockCount => checked((int)((Length + ChunkBytes - 1) / ChunkBytes));

    public static MediaUpload Create(Guid academyId, Guid resourceId, string fileName, long length, MediaStorageOptions options)
    {
        options.Validate();
        if (academyId == Guid.Empty || resourceId == Guid.Empty)
            throw new ArgumentException("Media must have an academy and resource owner.");
        if (length < 1 || length > options.MaxFileBytes)
            throw new ArgumentException($"Choose a nonempty file no larger than {options.MaxFileBytes / 1_000_000_000d:0.##} GB.");
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 250 ||
            fileName.Any(char.IsControl) || fileName.IndexOfAny(['/', '\\', ':']) >= 0 || fileName is "." or "..")
            throw new ArgumentException("Choose a file with a valid name of 250 characters or fewer.");
        // Executable/script uploads are outside the picture/audio/video/document
        // requirement. Other files can be retained as attachments, never active HTML.
        var extension = Path.GetExtension(fileName.Trim()).ToLowerInvariant();
        if (new[] { ".exe", ".com", ".bat", ".cmd", ".ps1", ".msi", ".dll", ".scr", ".hta", ".js", ".html", ".htm", ".svg" }.Contains(extension))
            throw new ArgumentException("Executable files and active web content are not supported class attachments.");
        return new(academyId, resourceId, fileName.Trim(), length, options.ChunkBytes);
    }

    public void Validate(MediaStorageOptions options)
    {
        var expected = Create(AcademyId, ResourceId, FileName, Length, options);
        if (this != expected) throw new ArgumentException("The media upload manifest is invalid.");
    }

    public long BlockLength(int index)
    {
        if (index < 0 || index >= BlockCount) throw new ArgumentOutOfRangeException(nameof(index));
        return Math.Min(ChunkBytes, Length - (long)index * ChunkBytes);
    }

    public string BlockId(int index)
    {
        _ = BlockLength(index);
        return Convert.ToBase64String(Encoding.ASCII.GetBytes(index.ToString("D8", System.Globalization.CultureInfo.InvariantCulture)));
    }
}
