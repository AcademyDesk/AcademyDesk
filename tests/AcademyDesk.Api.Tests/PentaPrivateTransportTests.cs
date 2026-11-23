using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Intelligence.Penta;
using Azure.Core;

namespace AcademyDesk.Api.Tests;

public sealed class PentaPrivateTransportTests
{
    private const string Secret = "synthetic-token.must-not-escape";
    private static readonly Guid Audience = Guid.Parse("11111111-2222-4333-8444-555555555555");
    private static PentaPrivateTransportOptions Options => new(new("https://mini.internal.example.invalid/"), Audience);
    private sealed class Credential(string value = Secret, int expirySeconds = 600, Exception? failure = null) : TokenCredential
    {
        public int Calls; public string[]? Scopes;
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) => throw new NotSupportedException();
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Calls++; Scopes = requestContext.Scopes; cancellationToken.ThrowIfCancellationRequested();
            if (failure is not null) throw failure;
            return ValueTask.FromResult(new AccessToken(value, DateTimeOffset.UtcNow.AddSeconds(expirySeconds)));
        }
    }
    private sealed class Transport(HttpStatusCode status = HttpStatusCode.OK, Exception? failure = null) : HttpMessageHandler
    {
        public int Calls; public string? Auth, Version, Path, Body; public string? Cookie;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++; Auth = request.Headers.Authorization?.ToString(); Version = request.Headers.GetValues(PentaPrivateTransport.VersionHeader).Single();
            Path = request.RequestUri!.AbsolutePath; Cookie = request.Headers.Contains("Cookie") ? "present" : null;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Assert.Equal("application/json", request.Headers.Accept.Single().MediaType);
            Assert.Equal("identity", request.Headers.AcceptEncoding.Single().Value);
            Assert.Equal(HttpVersion.Version11, request.Version);
            Assert.Equal(HttpVersionPolicy.RequestVersionExact, request.VersionPolicy);
            if (failure is not null) throw failure;
            return new(status) { Content = new StringContent("{\"inference_ready\":true}") };
        }
    }
    private static HttpRequestMessage Chat(string path = "/v1/chat") => new(HttpMethod.Post, path)
    { Content = JsonContent.Create(new MiniPlanRequest(Guid.NewGuid().ToString("D"), "Find synthetic learner", MiniState.Empty, PentaMiniTools.Available.ToArray())) };

    [Fact]
    public async Task Real_v0_1_request_shape_is_preserved_without_identity_or_credential_payload_fields()
    {
        var transport = new Transport(); using var client = PentaPrivateTransport.CreateForTests(Options, new Credential(), transport);
        using var request = Chat(); using var response = await client.SendAsync(request);
        using var body = JsonDocument.Parse(transport.Body!);
        Assert.Equal(new[] { "available_tools", "conversation_id", "state", "user_message" }, body.RootElement.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(new[] { "SearchLearners", "GetLearner" }, body.RootElement.GetProperty("available_tools").EnumerateArray().Select(v => v.GetString()));
        Assert.DoesNotContain(Secret, transport.Body!);
    }
    [Fact]
    public async Task Caller_cannot_select_an_alternate_http3_connection_path()
    {
        using var client = PentaPrivateTransport.CreateForTests(Options, new Credential(), new Transport());
        using var request = Chat(); request.Version = HttpVersion.Version30; request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpVersion.Version11, request.Version); Assert.Equal(HttpVersionPolicy.RequestVersionExact, request.VersionPolicy);
    }
    [Fact]
    public void Factory_configuration_retains_a_small_default_buffer_and_no_default_credentials()
    {
        using var client = PentaPrivateTransport.CreateForTests(Options, new Credential(), new Transport());
        Assert.Equal(16 * 1024, client.MaxResponseContentBufferSize);
        Assert.Equal(HttpVersion.Version11, client.DefaultRequestVersion);
        Assert.Equal(HttpVersionPolicy.RequestVersionExact, client.DefaultVersionPolicy);
        Assert.Empty(client.DefaultRequestHeaders);
    }

    [Theory]
    [InlineData("/v1/chat")][InlineData("/readiness")]
    public async Task Workload_identity_is_scoped_attached_per_request_and_removed_after_send(string path)
    {
        var credential = new Credential(); var transport = new Transport();
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = path == "/v1/chat" ? Chat() : new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(1, transport.Calls); Assert.Equal(1, credential.Calls);
        Assert.Equal(new[] { $"api://{Audience:D}/.default" }, credential.Scopes);
        Assert.Equal("Bearer " + Secret, transport.Auth); Assert.Equal(PentaPrivateTransport.Version, transport.Version);
        Assert.Equal(path, transport.Path); Assert.Null(transport.Cookie);
        Assert.Null(request.Headers.Authorization); Assert.False(request.Headers.Contains(PentaPrivateTransport.VersionHeader));
        Assert.Empty(client.DefaultRequestHeaders);
    }
    [Theory]
    [InlineData("http://mini.internal.example.invalid/")][InlineData("https://127.0.0.1/")]
    [InlineData("https://localhost/")][InlineData("https://mini.internal.example.invalid:444/")]
    [InlineData("https://user:secret@mini.internal.example.invalid/")][InlineData("https://mini.internal.example.invalid/path")]
    [InlineData("https://mini.internal.example.invalid/?secret=value")][InlineData("https://mini.internal.example.invalid/#fragment")]
    public void Invalid_origins_never_create_a_client(string origin)
    {
        var error = Assert.Throws<InvalidOperationException>(() => PentaPrivateTransport.CreateForTests(
            Options with { Origin = new(origin) }, new Credential(), new Transport()));
        Assert.DoesNotContain(origin, error.Message); Assert.Null(error.InnerException);
    }
    [Fact]
    public void Malformed_host_syntax_is_rejected_by_the_uri_type_before_configuration()
    {
        Assert.False(Uri.TryCreate("https://mini..example.invalid/", UriKind.Absolute, out _));
    }
    [Fact]
    public void Invalid_audience_and_identity_configuration_fail_closed()
    {
        foreach (var options in new[] { Options with { AudienceAppId = Guid.Empty }, Options with { ManagedIdentityClientId = Secret },
            Options with { ManagedIdentityClientId = Guid.Empty.ToString("D") }, Options with { Origin = null! } })
            Assert.Throws<InvalidOperationException>(() => PentaPrivateTransport.CreateForTests(options, new Credential(), new Transport()));
    }
    [Theory]
    [InlineData("https://other.internal.example.invalid/v1/chat")][InlineData("http://mini.internal.example.invalid/v1/chat")]
    [InlineData("/v1/chat?secret=value")][InlineData("/v1/chat#fragment")][InlineData("/health")][InlineData("/v1/capabilities")]
    [InlineData("/v1/chat/")][InlineData("https://mini.internal.example.invalid:444/v1/chat")]
    public async Task Unapproved_destinations_and_paths_use_no_token_or_network(string path)
    {
        var credential = new Credential(); var transport = new Transport();
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(path); await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(0, credential.Calls); Assert.Equal(0, transport.Calls);
    }
    [Theory]
    [InlineData("Authorization")][InlineData("Proxy-Authorization")][InlineData("Cookie")][InlineData("Host")]
    [InlineData("X-Penta-Transport")][InlineData("X-MS-CLIENT-PRINCIPAL")][InlineData("X-Actor-Id")]
    public async Task Caller_authentication_and_identity_headers_are_rejected(string header)
    {
        var credential = new Credential(); var transport = new Transport();
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(); request.Headers.TryAddWithoutValidation(header, "synthetic-value");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(0, credential.Calls); Assert.Equal(0, transport.Calls);
    }
    [Theory]
    [InlineData("PUT", "/v1/chat")][InlineData("GET", "/v1/chat")][InlineData("POST", "/readiness")]
    [InlineData("POST", "/v1/chat")]
    public async Task Wrong_methods_and_content_do_not_dispatch(string method, string path)
    {
        var credential = new Credential(); var transport = new Transport();
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = new HttpRequestMessage(new(method), path);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(0, credential.Calls); Assert.Equal(0, transport.Calls);
    }
    [Theory]
    [InlineData("", 600)][InlineData(Secret, -1)][InlineData(Secret, 30)]
    [InlineData("unsafe\ncredential", 600)]
    public async Task Missing_short_lived_or_malformed_tokens_cannot_reach_the_service(string token, int expiry)
    {
        var transport = new Transport(); using var client = PentaPrivateTransport.CreateForTests(Options, new Credential(token, expiry), transport);
        using var request = Chat(); var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Calls); Assert.Null(request.Headers.Authorization); Assert.DoesNotContain(Secret, error.ToString());
    }
    [Fact]
    public async Task Oversized_tokens_and_readiness_bodies_are_rejected_without_network()
    {
        var transport = new Transport(); var credential = new Credential(new string('x', 16385));
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(); await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Calls); Assert.Null(request.Headers.Authorization);
        using var readiness = new HttpRequestMessage(HttpMethod.Get, "/readiness") { Content = new StringContent("unexpected") };
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(readiness));
        Assert.Equal(1, credential.Calls); Assert.Equal(0, transport.Calls);
    }
    [Theory]
    [InlineData(301)][InlineData(302)][InlineData(307)][InlineData(308)]
    public async Task Redirects_are_unavailable_without_retry_or_credential_forwarding(int status)
    {
        var transport = new Transport((HttpStatusCode)status); var credential = new Credential();
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(); await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.Equal(1, transport.Calls); Assert.Equal(1, credential.Calls); Assert.Null(request.Headers.Authorization);
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task Credential_and_network_failures_do_not_expose_original_exception_or_retry(bool credentialFailure)
    {
        var failure = new InvalidOperationException(Secret);
        var credential = new Credential(failure: credentialFailure ? failure : null);
        var transport = new Transport(failure: credentialFailure ? null : failure);
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(); var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request));
        Assert.DoesNotContain(Secret, error.ToString()); Assert.Null(error.InnerException);
        Assert.Equal(credentialFailure ? 0 : 1, transport.Calls); Assert.Null(request.Headers.Authorization);
    }
    [Fact]
    public async Task Cancellation_stays_canceled_without_dispatch_or_credential_details()
    {
        var transport = new Transport(); var credential = new Credential(failure: new OperationCanceledException(Secret));
        using var client = PentaPrivateTransport.CreateForTests(Options, credential, transport);
        using var request = Chat(); var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.SendAsync(request));
        Assert.DoesNotContain(Secret, error.ToString()); Assert.Equal(0, transport.Calls); Assert.Null(request.Headers.Authorization);
    }
    [Theory]
    [InlineData("10.0.0.1", true)][InlineData("172.16.0.1", true)][InlineData("172.31.255.254", true)]
    [InlineData("192.168.1.1", true)][InlineData("fd12::1", true)][InlineData("fc00::1", true)]
    [InlineData("::ffff:10.0.0.1", true)][InlineData("127.0.0.1", false)][InlineData("100.98.98.13", false)]
    [InlineData("169.254.169.254", false)][InlineData("168.63.129.16", false)][InlineData("8.8.8.8", false)]
    [InlineData("172.15.0.1", false)][InlineData("172.32.0.1", false)][InlineData("192.169.1.1", false)]
    [InlineData("::1", false)][InlineData("fe80::1", false)][InlineData("2001:4860:4860::8888", false)]
    [InlineData("0.0.0.0", false)][InlineData("::", false)][InlineData("224.0.0.1", false)]
    public void Serving_connections_allow_only_explicit_private_address_ranges(string address, bool expected) =>
        Assert.Equal(expected, PentaPrivateTransport.IsPrivateAddress(IPAddress.Parse(address)));

    [Fact]
    public void Dns_selection_pins_the_actual_private_address_and_rejects_mixed_public_or_invalid_results()
    {
        var endpoint = new System.Net.DnsEndPoint(Options.Origin.Host, 443); var address = IPAddress.Parse("10.1.2.3");
        Assert.Same(address, PentaPrivateTransport.SelectPrivateAddress(endpoint, Options.Origin, [address, IPAddress.Parse("10.1.2.4")]));
        foreach (var invalid in new IPAddress[]?[] { null, [], [null!], [address, IPAddress.Parse("8.8.8.8")],
            [IPAddress.Parse("127.0.0.1")], Enumerable.Repeat(address, 9).ToArray() })
            Assert.Throws<HttpRequestException>(() => PentaPrivateTransport.SelectPrivateAddress(endpoint, Options.Origin, invalid));
        Assert.Throws<HttpRequestException>(() => PentaPrivateTransport.SelectPrivateAddress(new("other.internal.example.invalid", 443), Options.Origin, [address]));
        Assert.Throws<HttpRequestException>(() => PentaPrivateTransport.SelectPrivateAddress(new(Options.Origin.Host, 80), Options.Origin, [address]));
    }
}
