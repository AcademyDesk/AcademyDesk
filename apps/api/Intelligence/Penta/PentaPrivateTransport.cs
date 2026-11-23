using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Azure.Core;
using Azure.Identity;

namespace AcademyDesk.Api.Intelligence.Penta;

public sealed record PentaPrivateTransportOptions(Uri Origin, Guid AudienceAppId, string? ManagedIdentityClientId = null);

/// <summary>Unregistered transport foundation, not provider activation or model qualification.
/// The receiving gateway must independently validate the workload token and service role.
/// Existing host authorization, schema/runtime checks and production gates still apply.</summary>
public static class PentaPrivateTransport
{
    public const string Version = "penta-private.v1";
    public const string VersionHeader = "X-Penta-Transport";

    public static HttpClient CreateClient(PentaPrivateTransportOptions options)
    {
        Validate(options);
        var credential = new ManagedIdentityCredential(options.ManagedIdentityClientId is null
            ? ManagedIdentityId.SystemAssigned
            : ManagedIdentityId.FromUserAssignedClientId(Guid.Parse(options.ManagedIdentityClientId).ToString("D")));
        var sockets = new SocketsHttpHandler
        {
            AllowAutoRedirect = false, UseProxy = false, UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(10), MaxConnectionsPerServer = 2,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectCallback = (context, token) => ConnectPrivateAsync(context.DnsEndPoint, options.Origin, token)
        };
        return Client(options, credential, sockets, TimeProvider.System);
    }

    internal static HttpClient CreateForTests(PentaPrivateTransportOptions options, TokenCredential credential,
        HttpMessageHandler inner, TimeProvider? clock = null) => Client(options, credential, inner, clock ?? TimeProvider.System);

    private static HttpClient Client(PentaPrivateTransportOptions options, TokenCredential credential,
        HttpMessageHandler inner, TimeProvider clock)
    {
        Validate(options);
        return new HttpClient(new Authentication(options, credential, clock) { InnerHandler = inner })
        {
            BaseAddress = options.Origin, Timeout = TimeSpan.FromSeconds(125),
            DefaultRequestVersion = HttpVersion.Version11, DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            MaxResponseContentBufferSize = 16 * 1024
        };
    }

    private static void Validate(PentaPrivateTransportOptions? options)
    {
        if (options is null || options.AudienceAppId == Guid.Empty || options.Origin is not { IsAbsoluteUri: true } uri ||
            uri.Scheme != "https" || uri.Port != 443 || uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.HostNameType != UriHostNameType.Dns ||
            uri.IdnHost.Length > 253 || !uri.IdnHost.Contains('.') ||
            uri.IdnHost.Split('.').Any(label => label.Length is 0 or > 63 ||
                !char.IsAsciiLetterOrDigit(label[0]) || !char.IsAsciiLetterOrDigit(label[^1]) ||
                label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) ||
            options.ManagedIdentityClientId is not null &&
                (!Guid.TryParse(options.ManagedIdentityClientId, out var client) || client == Guid.Empty))
            throw new InvalidOperationException("Private Mini transport requires a valid origin and workload identity configuration.");
    }

    internal static bool IsPrivateAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var b = address.GetAddressBytes();
        return address.AddressFamily == AddressFamily.InterNetwork
            ? b[0] == 10 || b[0] == 172 && b[1] is >= 16 and <= 31 || b[0] == 192 && b[1] == 168
            : address.AddressFamily == AddressFamily.InterNetworkV6 && (b[0] & 0xfe) == 0xfc;
    }

    private static async ValueTask<Stream> ConnectPrivateAsync(DnsEndPoint endpoint, Uri origin, CancellationToken token)
    {
        if (endpoint.Port != 443 || !string.Equals(endpoint.Host, origin.IdnHost, StringComparison.OrdinalIgnoreCase))
            throw Unavailable();
        var addresses = await Dns.GetHostAddressesAsync(endpoint.Host, token);
        // Resolve once and connect the validated address itself. Never validate
        // DNS then give the hostname to a second, independently resolving client.
        var address = SelectPrivateAddress(endpoint, origin, addresses);
        var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), token);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch { socket.Dispose(); throw; }
    }

    internal static IPAddress SelectPrivateAddress(DnsEndPoint endpoint, Uri origin, IPAddress[]? addresses)
    {
        if (endpoint.Port != 443 || !string.Equals(endpoint.Host, origin.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            addresses is not { Length: > 0 and <= 8 } || addresses.Any(address => address is null || !IsPrivateAddress(address)))
            throw Unavailable();
        return addresses[0];
    }

    private static HttpRequestException Unavailable() => new("Private Mini transport is unavailable.");

    private sealed class Authentication(PentaPrivateTransportOptions options, TokenCredential credential, TimeProvider clock) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            var valid = uri is { IsAbsoluteUri: true } && uri.Scheme == "https" && uri.Port == 443 &&
                uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0 &&
                string.Equals(uri.IdnHost, options.Origin.IdnHost, StringComparison.OrdinalIgnoreCase) &&
                (request.Method == HttpMethod.Get && uri.AbsolutePath == "/readiness" && request.Content is null ||
                 request.Method == HttpMethod.Post && uri.AbsolutePath == "/v1/chat" &&
                 string.Equals(request.Content?.Headers.ContentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase)) &&
                request.Headers.All(header => header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase));
            if (!valid) throw new HttpRequestException("Private Mini transport request is invalid.");
            try
            {
                var access = await credential.GetTokenAsync(new TokenRequestContext([$"api://{options.AudienceAppId:D}/.default"]), cancellationToken);
                if (string.IsNullOrEmpty(access.Token) || access.Token.Length > 16384 ||
                    access.ExpiresOn <= clock.GetUtcNow().AddSeconds(60) ||
                    access.Token.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '_' or '-' or '+' or '/' or '=' or '~')))
                    throw Unavailable();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.Token);
                request.Headers.Add(VersionHeader, Version);
                // Keep every connection on the TCP/TLS handler whose address
                // binding we enforce; do not negotiate an alternate HTTP/3 path.
                request.Version = HttpVersion.Version11;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
                request.Headers.Accept.Clear(); request.Headers.Accept.Add(new("application/json"));
                request.Headers.AcceptEncoding.Clear(); request.Headers.AcceptEncoding.Add(new("identity"));
                var response = await base.SendAsync(request, cancellationToken);
                if ((int)response.StatusCode is >= 300 and <= 399)
                { response.Dispose(); throw Unavailable(); }
                return response;
            }
            catch (OperationCanceledException)
            { throw new OperationCanceledException("Private Mini transport was canceled.", cancellationToken); }
            catch (Exception)
            { throw Unavailable(); } // No credential/network exception or inner detail is retained.
            finally
            {
                request.Headers.Authorization = null;
                request.Headers.Remove(VersionHeader);
            }
        }
    }
}
