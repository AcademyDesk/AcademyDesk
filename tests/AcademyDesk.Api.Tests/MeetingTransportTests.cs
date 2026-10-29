using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AcademyDesk.Api.Infrastructure.Meetings;

namespace AcademyDesk.Api.Tests;

// Actual sockets, fresh loopback listeners and synthetic data only. These tests
// do not change production transport or trust stores, or contact any provider.
public sealed class MeetingTransportTests
{
    private sealed class Loopback : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource lifetime = new(TimeSpan.FromSeconds(10));
        private readonly Task worker;
        internal readonly ConcurrentQueue<string> Requests = new();
        internal Uri Origin { get; }
        internal Loopback(Func<Stream, int, CancellationToken, Task> reply, int requests = 1, X509Certificate2? certificate = null)
        {
            listener.Start(); Origin = new($"{(certificate is null ? "http" : "https")}://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
            worker = Serve();
            async Task Serve()
            {
                for (var index = 0; index < requests; index++)
                {
                    using var client = await listener.AcceptTcpClientAsync(lifetime.Token);
                    using var network = client.GetStream(); Stream stream = network;
                    using var tls = certificate is null ? null : new SslStream(network, leaveInnerStreamOpen: true);
                    if (tls is not null)
                    {
                        try { await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, lifetime.Token); }
                        catch (Exception ex) when (ex is AuthenticationException or IOException) { return; }
                        stream = tls;
                    }
                    var bytes = new List<byte>(); var one = new byte[1];
                    while (bytes.Count < 16384)
                    {
                        if (await stream.ReadAsync(one, lifetime.Token) == 0) return;
                        bytes.Add(one[0]);
                        if (bytes.Count >= 4 && bytes.TakeLast(4).SequenceEqual(new byte[] { 13, 10, 13, 10 })) break;
                    }
                    if (bytes.Count >= 16384) throw new InvalidOperationException("Synthetic request headers exceeded fixture limit.");
                    Requests.Enqueue(Encoding.ASCII.GetString(bytes.ToArray()));
                    await reply(stream, index, lifetime.Token);
                }
            }
        }
        internal static Task Write(Stream stream, string response, CancellationToken token) => stream.WriteAsync(Encoding.ASCII.GetBytes(response), token).AsTask();
        internal static string Response(string headers = "", string body = "") => $"HTTP/1.1 200 OK\r\nContent-Length: {Encoding.ASCII.GetByteCount(body)}\r\nConnection: close\r\n{headers}\r\n{body}";
        public async ValueTask DisposeAsync()
        {
            lifetime.Cancel(); listener.Stop();
            try { await worker.WaitAsync(TimeSpan.FromSeconds(3)); }
            catch (Exception ex) when (ex is OperationCanceledException or SocketException or IOException) { }
            finally { lifetime.Dispose(); }
        }
    }
    private static HttpMessageInvoker Transport() => new(MeetingOAuthCodeExchange.CreateSecureHandler());
    private static HttpRequestMessage Request(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "synthetic-local-transport-token");
        return request;
    }
    [Theory] [InlineData(301)] [InlineData(302)] [InlineData(303)] [InlineData(307)] [InlineData(308)]
    public async Task Redirect_is_returned_without_contacting_destination(int status)
    {
        await using var destination = new Loopback((s, _, t) => Loopback.Write(s, Loopback.Response(), t));
        await using var origin = new Loopback((s, _, t) => Loopback.Write(s, $"HTTP/1.1 {status} Synthetic\r\nLocation: {destination.Origin}\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", t));
        using var transport = Transport(); using var request = Request(origin.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await transport.SendAsync(request, timeout.Token);
        Assert.Equal(status, (int)response.StatusCode); Assert.Single(origin.Requests); Assert.Empty(destination.Requests);
    }
    [Fact] public async Task Provider_cookies_are_not_replayed_between_requests()
    {
        await using var server = new Loopback((s, _, t) => Loopback.Write(s, Loopback.Response("Set-Cookie: synthetic_session=private; Path=/\r\n"), t), requests: 2);
        using var transport = Transport(); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        for (var i = 0; i < 2; i++) { using var request = Request(server.Origin); using var response = await transport.SendAsync(request, timeout.Token); Assert.Equal(HttpStatusCode.OK, response.StatusCode); }
        Assert.Equal(2, server.Requests.Count); Assert.All(server.Requests, x => Assert.DoesNotContain("\r\nCookie:", x, StringComparison.OrdinalIgnoreCase));
    }
    [Fact] public async Task Compressed_body_is_not_silently_decoded_before_provider_validation()
    {
        using var bytes = new MemoryStream(); using (var gzip = new GZipStream(bytes, CompressionMode.Compress, leaveOpen: true)) gzip.Write(Encoding.ASCII.GetBytes("synthetic-response")); var compressed = bytes.ToArray();
        await using var server = new Loopback(async (s, _, t) => { await Loopback.Write(s, $"HTTP/1.1 200 OK\r\nContent-Encoding: gzip\r\nContent-Length: {compressed.Length}\r\nConnection: close\r\n\r\n", t); await s.WriteAsync(compressed, t); });
        using var transport = Transport(); using var request = Request(server.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await transport.SendAsync(request, timeout.Token);
        Assert.Contains("gzip", response.Content.Headers.ContentEncoding); Assert.Equal(compressed, await response.Content.ReadAsByteArrayAsync(timeout.Token));
        Assert.DoesNotContain("Accept-Encoding:", Assert.Single(server.Requests), StringComparison.OrdinalIgnoreCase);
    }
    [Fact] public async Task Oversized_response_headers_are_rejected_by_actual_socket_handler()
    {
        await using var server = new Loopback((s, _, t) => Loopback.Write(s, Loopback.Response("X-Synthetic: " + new string('a', 17000) + "\r\n"), t));
        using var transport = Transport(); using var request = Request(server.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<HttpRequestException>(() => transport.SendAsync(request, timeout.Token)); Assert.Single(server.Requests);
    }
    [Fact] public async Task Raw_invoker_returns_headers_without_prebuffering_unbounded_response_body()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new Loopback(async (s, _, t) => { await Loopback.Write(s, "HTTP/1.1 200 OK\r\nContent-Length: 1000000\r\nConnection: close\r\n\r\n", t); await s.FlushAsync(t); await release.Task.WaitAsync(t); });
        using var transport = Transport(); using var request = Request(server.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var response = await transport.SendAsync(request, timeout.Token).WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(1000000L, response.Content.Headers.ContentLength); Assert.False(release.Task.IsCompleted); Assert.Single(server.Requests);
        }
        finally { release.TrySetResult(); }
    }
    [Fact] public async Task Cancellation_interrupts_a_stalled_response_body()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var server = new Loopback(async (s, _, t) => { await Loopback.Write(s, "HTTP/1.1 200 OK\r\nContent-Length: 1\r\nConnection: close\r\n\r\n", t); await s.FlushAsync(t); entered.TrySetResult(); await Task.Delay(Timeout.InfiniteTimeSpan, t); });
        using var transport = Transport(); using var request = Request(server.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await transport.SendAsync(request, timeout.Token); await entered.Task.WaitAsync(timeout.Token);
        using var stream = await response.Content.ReadAsStreamAsync(timeout.Token); var read = stream.ReadAsync(new byte[1], timeout.Token).AsTask();
        timeout.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read); Assert.Single(server.Requests);
    }
    [Fact] public async Task Default_TLS_validation_rejects_untrusted_certificate_before_HTTP_credentials()
    {
        using var rsa = RSA.Create(2048); var request = new CertificateRequest("CN=Synthetic-Transport-Only", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder(); names.AddIpAddress(IPAddress.Loopback); request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(10));
        await using var server = new Loopback((s, _, t) => Loopback.Write(s, Loopback.Response(), t), certificate: certificate);
        using var transport = Transport(); using var message = Request(server.Origin); using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await Assert.ThrowsAsync<HttpRequestException>(() => transport.SendAsync(message, timeout.Token)); Assert.Empty(server.Requests);
    }
    [Fact] public void Factory_keeps_proxy_credentials_TLS_bypass_and_unbounded_headers_disabled()
    {
        using var handler = MeetingOAuthCodeExchange.CreateSecureHandler();
        Assert.False(handler.UseProxy); Assert.False(handler.UseCookies); Assert.False(handler.AllowAutoRedirect);
        Assert.Null(handler.Credentials); Assert.Null(handler.DefaultProxyCredentials); Assert.False(handler.PreAuthenticate);
        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback); Assert.Null(handler.ConnectCallback);
        Assert.Equal(16, handler.MaxResponseHeadersLength); Assert.Equal(TimeSpan.FromSeconds(5), handler.ConnectTimeout);
        Assert.Equal(DecompressionMethods.None, handler.AutomaticDecompression);
    }
}
