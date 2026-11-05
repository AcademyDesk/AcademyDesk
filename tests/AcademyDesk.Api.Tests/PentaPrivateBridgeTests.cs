using System.Security.Cryptography;
using System.Text.Json;
using AcademyDesk.Api.Controllers;
using AcademyDesk.Api.Intelligence.Penta;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AcademyDesk.Api.Tests;

public sealed class PentaPrivateBridgeTests
{
    private static string Secret()=>Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private static PentaOnboardingInterpretation Input(Func<CancellationToken,Task<JsonElement>>? current=null)=>new(
        Guid.NewGuid(),"Synthetic prompt",JsonSerializer.SerializeToElement(new{draft=new{revision=1}}),
        current??(_=>Task.FromResult(JsonSerializer.SerializeToElement(new{synthetic=true}))));
    [Theory]
    [InlineData("http://localhost:1234/")][InlineData("https://127.0.0.1:1234/")]
    [InlineData("http://evil.invalid:1234/")][InlineData("http://127.0.0.1/")]
    [InlineData("http://user:pass@127.0.0.1:1234/")][InlineData("http://127.0.0.1:1234/path")]
    [InlineData("http://127.0.0.1:1234/?q=x")][InlineData("http://127.0.0.1:1234/#x")]
    public void Arbitrary_destination_rejected(string origin)=>Assert.Throws<ArgumentException>(()=>
        new PentaPrivateLoopbackInterpreter(new(origin),Secret(),new()));
    [Theory]
    [InlineData("")][InlineData("not-a-secret")][InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void No_default_credential(string value)=>Assert.Throws<ArgumentException>(()=>
        new PentaPrivateLoopbackInterpreter(new("http://127.0.0.1:1234/"),value,new()));
    [Fact]
    public async Task Exact_request_two_reads_only_then_revoked() {
        var leases=new PentaPrivateContextLeases();var input=Input();
        using(var owned=leases.Open(input,default,out var credential)){
            var hash=PentaPrivateContextLeases.RequestHash(input);
            Assert.Null(await leases.ReadAsync(Secret(),input.RequestId,hash,default));
            Assert.Null(await leases.ReadAsync(credential,Guid.NewGuid(),hash,default));
            Assert.Null(await leases.ReadAsync(credential,input.RequestId,Secret(),default));
            Assert.NotNull(await leases.ReadAsync(credential,input.RequestId,hash,default));
            Assert.NotNull(await leases.ReadAsync(credential,input.RequestId,hash,default));
            Assert.Null(await leases.ReadAsync(credential,input.RequestId,hash,default));
        }
        using var second=leases.Open(input,default,out var closed);second.Dispose();
        Assert.Null(await leases.ReadAsync(closed,input.RequestId,PentaPrivateContextLeases.RequestHash(input),default));
    }
    [Fact]
    public async Task Expired_or_cancelled_lease_never_calls_context() {
        var clock=new Clock();var leases=new PentaPrivateContextLeases(clock);var calls=0;
        var input=Input(_=>{calls++;return Task.FromResult(JsonSerializer.SerializeToElement(new{}));});
        using var cts=new CancellationTokenSource();using var owned=leases.Open(input,cts.Token,out var first);
        cts.Cancel();Assert.Null(await leases.ReadAsync(first,input.RequestId,PentaPrivateContextLeases.RequestHash(input),default));
        using var other=leases.Open(input,default,out var expired);clock.Now=clock.Now.AddSeconds(31);
        Assert.Null(await leases.ReadAsync(expired,input.RequestId,PentaPrivateContextLeases.RequestHash(input),default));Assert.Equal(0,calls);
    }
    [Fact]
    public async Task Concurrent_callback_not_dispatched_and_two_lease_limit() {
        var leases=new PentaPrivateContextLeases();var entered=new TaskCompletionSource();var release=new TaskCompletionSource();
        var input=Input(async _=>{entered.SetResult();await release.Task;return JsonSerializer.SerializeToElement(new{});});
        using var one=leases.Open(input,default,out var credential);using var two=leases.Open(Input(),default,out _);
        Assert.Throws<HttpRequestException>(()=>leases.Open(Input(),default,out _));
        var first=leases.ReadAsync(credential,input.RequestId,PentaPrivateContextLeases.RequestHash(input),default);await entered.Task;
        Assert.Null(await leases.ReadAsync(credential,input.RequestId,PentaPrivateContextLeases.RequestHash(input),default));release.SetResult();Assert.NotNull(await first);
    }
    [Theory]
    [InlineData("Production",true,true)][InlineData("Development",true,true)]
    [InlineData("Testing",false,true)][InlineData("Testing",true,false)]
    public async Task Controller_cannot_be_activated_by_flag_alone(string env,bool flag,bool registry) {
        var controller=Controller(env,flag,registry?new():null);
        Assert.IsType<NotFoundResult>(await controller.Current(default));
        Assert.Equal("no-store",controller.Response.Headers.CacheControl.ToString());
    }
    [Fact]
    public async Task Controller_uses_private_lease_not_user_bearer_or_query() {
        var leases=new PentaPrivateContextLeases();var input=Input();using var owned=leases.Open(input,default,out var credential);
        var c=Controller("Testing",true,leases);
        c.Request.Headers.Authorization="Bearer "+Secret();
        c.Request.Headers["X-Penta-Request"]=input.RequestId.ToString("D");
        c.Request.Headers[PentaPrivateContextLeases.VersionHeader]=PentaPrivateContextLeases.Version;
        c.Request.Headers[PentaPrivateContextLeases.BodyHeader]=PentaPrivateContextLeases.RequestHash(input);
        Assert.IsType<NotFoundResult>(await c.Current(default));
        c.Request.Headers.Authorization="Bearer "+credential;c.Request.QueryString=new("?tenant=other");
        Assert.IsType<NotFoundResult>(await c.Current(default));c.Request.QueryString=QueryString.Empty;
        Assert.IsType<OkObjectResult>(await c.Current(default));
    }
    [Fact]
    public void Private_context_delegate_never_serializes()=>Assert.DoesNotContain("Current",JsonSerializer.Serialize(Input()));
    private static PentaPrivateContextController Controller(string env,bool enabled,PentaPrivateContextLeases? leases) {
        var flags=new[]{"OperationalDraftContractsEnabled","OperationalOnboardingExecutionContractsEnabled","OperationalOnboardingHttpContractsEnabled",
            "OperationalOnboardingGatewayContractsEnabled","OperationalOnboardingPrivateBridgeContractsEnabled"};
        var config=new ConfigurationBuilder().AddInMemoryCollection(flags.ToDictionary(x=>"Penta:"+x,x=>(string?)enabled.ToString())).Build();
        return new(new Host(env),config,leases){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
    }
    private sealed class Clock:TimeProvider {public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
    private sealed class Host(string env):IHostEnvironment {
        public string EnvironmentName{get;set;}=env;public string ApplicationName{get;set;}="BridgeTest";
        public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();
    }
}
