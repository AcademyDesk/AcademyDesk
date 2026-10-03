using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace AcademyDesk.Api.Infrastructure.Media;

// Read-only credentials, never account bearer/refresh tokens or public Blob URLs.
// Purpose isolation prevents use as Identity authentication or upload credentials.
public sealed class ClassMaterialDownloadTickets(IDataProtectionProvider protection, IConfiguration configuration, IWebHostEnvironment environment)
{
    public const string Purpose = "AcademyDesk.PrivateMaterial.NativeDownload.v1";
    public const string Scheme = "PrivateMaterialDownload";
    public const int LifetimeSeconds = 60;
    private readonly ITimeLimitedDataProtector protector = protection.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public static bool IsContentPath(string? path) => path is not null &&
        (Regex.IsMatch(path, @"^/api/class-media/[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}/content$", RegexOptions.CultureInvariant) ||
         Regex.IsMatch(path, @"^/uploads/(teacher-materials|learning-resources)/[0-9a-f]{32}\.[a-zA-Z0-9]+$", RegexOptions.CultureInvariant));

    public bool IsApprovedOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.GetLeftPart(UriPartial.Authority) == origin &&
        (uri.Scheme == "https" || ((environment.IsDevelopment() || environment.IsEnvironment("Testing")) && uri.IsLoopback && uri.Scheme == "http")) &&
        (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []).Contains(origin, StringComparer.Ordinal);

    public bool IsSafeTransport(HttpRequest request) => request.IsHttps ||
        ((environment.IsDevelopment() || environment.IsEnvironment("Testing")) &&
         Uri.TryCreate($"http://{request.Host}", UriKind.Absolute, out var uri) && uri.IsLoopback);

    public string Create(ApplicationUser user, string path, string origin)
    {
        if (!user.IsActive || !user.AcademyId.HasValue || string.IsNullOrEmpty(user.SecurityStamp) || !IsContentPath(path) || !IsApprovedOrigin(origin))
            throw new InvalidOperationException("Invalid download scope.");
        return protector.Protect(JsonSerializer.Serialize(new DownloadScope(user.Id, user.AcademyId.Value, user.SecurityStamp, path, origin)), TimeSpan.FromSeconds(LifetimeSeconds));
    }

    public DownloadScope? Read(string value, string path, string origin)
    {
        if (value.Length is < 1 or > 3000 || !IsContentPath(path) || !IsApprovedOrigin(origin)) return null;
        try
        {
            var scope = JsonSerializer.Deserialize<DownloadScope>(protector.Unprotect(value, out var expiry));
            return scope is not null && scope.UserId != Guid.Empty && scope.AcademyId != Guid.Empty &&
                !string.IsNullOrEmpty(scope.SecurityStamp) && scope.Path == path && scope.Origin == origin &&
                expiry <= DateTimeOffset.UtcNow.AddSeconds(LifetimeSeconds + 1) ? scope : null;
        }
        catch (CryptographicException) { return null; }
        catch (JsonException) { return null; }
        catch (FormatException) { return null; }
    }
    public sealed record DownloadScope(Guid UserId, Guid AcademyId, string SecurityStamp, string Path, string Origin);
}
