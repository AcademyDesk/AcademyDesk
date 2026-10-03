using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace AcademyDesk.Api.Infrastructure.Media;

// Existing material bytes are retained in place during the storage transition,
// but must never be resolved by anonymous static-file middleware.
public sealed class PrivateMaterialFileProvider(IFileProvider inner) : IFileProvider
{
    public static bool IsPrivatePath(string path)
    {
        var segments = new List<string>();
        foreach (var part in path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".") continue;
            if (part == "..") { if (segments.Count > 0) segments.RemoveAt(segments.Count - 1); continue; }
            // Win32 aliases must not bypass the filter on the development host.
            segments.Add(part.TrimEnd(' ', '.'));
        }
        return segments.Count >= 2 && segments[0].Equals("uploads", StringComparison.OrdinalIgnoreCase) &&
            (segments.Skip(1).Any(x => x.Contains('~')) ||
             segments[1].Equals("teacher-materials", StringComparison.OrdinalIgnoreCase) ||
             segments[1].Equals("learning-resources", StringComparison.OrdinalIgnoreCase));
    }

    public IFileInfo GetFileInfo(string subpath) => IsPrivatePath(subpath) ? new NotFoundFileInfo(subpath) : inner.GetFileInfo(subpath);
    public IDirectoryContents GetDirectoryContents(string subpath) => IsPrivatePath(subpath) ? NotFoundDirectoryContents.Singleton : inner.GetDirectoryContents(subpath);
    public IChangeToken Watch(string filter) => IsPrivatePath(filter) ? NullChangeToken.Singleton : inner.Watch(filter);
}
