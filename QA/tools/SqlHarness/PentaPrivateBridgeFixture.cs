using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using AcademyDesk.Api.Intelligence.Penta;
using AcademyDesk.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed class PentaPrivateBridgeFixture:IAsyncDisposable
{
    private readonly WebApplication callback;private readonly Process process;
    private readonly Task<string> stdout,stderr;
    public PentaPrivateLoopbackInterpreter Interpreter{get;}
    public Func<int,Task>? BeforeCurrent;public int CurrentCalls;
    public Uri Origin{get;}
    public string Channel{get;}
    private PentaPrivateBridgeFixture(WebApplication callback,Process process,PentaPrivateLoopbackInterpreter interpreter,
        Uri origin,string channel,Task<string> stdout,Task<string> stderr) {
        this.callback=callback;this.process=process;Interpreter=interpreter;Origin=origin;Channel=channel;this.stdout=stdout;this.stderr=stderr;
    }
    public static async Task<PentaPrivateBridgeFixture> StartAsync(QaApiFactory factory,PentaPrivateContextLeases leases,string run) {
        var builder=WebApplication.CreateSlimBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        var callback=builder.Build();PentaPrivateBridgeFixture? owned=null;
        // Fixed loopback proxy into the actual private controller/SQL pipeline.
        // Never a generic destination or a browser/user authentication bypass.
        callback.Run(async context=>{
            if(context.Request.Method!="GET"||context.Request.Path!="/internal/penta/onboarding/current"||context.Request.QueryString.HasValue) {
                context.Response.StatusCode=404;return;
            }
            if(owned is not null){var count=Interlocked.Increment(ref owned.CurrentCalls);if(owned.BeforeCurrent is {} action)await action(count);}
            using var client=factory.CreateClient();using var request=new HttpRequestMessage(HttpMethod.Get,"/internal/penta/onboarding/current");
            foreach(var name in new[]{"Authorization","X-Penta-Request",PentaPrivateContextLeases.VersionHeader,PentaPrivateContextLeases.BodyHeader})
                if(context.Request.Headers.TryGetValue(name,out var value))request.Headers.TryAddWithoutValidation(name,value.ToArray());
            using var response=await client.SendAsync(request,context.RequestAborted);context.Response.StatusCode=(int)response.StatusCode;
            context.Response.ContentType="application/json";context.Response.Headers.CacheControl="no-store";
            if(response.Headers.TryGetValues(PentaPrivateContextLeases.VersionHeader,out var versions))context.Response.Headers[PentaPrivateContextLeases.VersionHeader]=versions.Single();
            await response.Content.CopyToAsync(context.Response.Body,context.RequestAborted);
        });
        await callback.StartAsync();
        var address=callback.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var reserve=new TcpListener(IPAddress.Loopback,0);reserve.Start();var port=((IPEndPoint)reserve.LocalEndpoint).Port;reserve.Stop();
        var channel=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));var origin=new Uri($"http://127.0.0.1:{port}/");
        var start=new ProcessStartInfo("python"){WorkingDirectory=Directory.GetCurrentDirectory(),UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
        start.ArgumentList.Add(Path.GetFullPath("QA/tools/penta-private-bridge-fixture.py"));start.ArgumentList.Add(port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        start.Environment["QA_PENTA_BRIDGE_RUN_ID"]=run;start.Environment["QA_PENTA_BRIDGE_SOURCE"]=@"D:\PENTA AI Models";
        start.Environment["QA_PENTA_BRIDGE_CALLBACK"]=address;start.Environment["QA_PENTA_BRIDGE_CHANNEL"]=channel;
        start.Environment["PYTHONDONTWRITEBYTECODE"]="1";
        var process=new Process{StartInfo=start};
        try {
            if(!process.Start())throw new InvalidOperationException("Bridge fixture did not start.");
            owned=new(callback,process,new(origin,channel,leases),origin,channel,process.StandardOutput.ReadToEndAsync(),process.StandardError.ReadToEndAsync());
            using var client=new HttpClient(new SocketsHttpHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(1)};
            client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",channel);
            client.DefaultRequestHeaders.Add(PentaPrivateContextLeases.VersionHeader,PentaPrivateContextLeases.Version);
            for(var attempt=0;attempt<50;attempt++){
                if(process.HasExited)throw new InvalidOperationException("Bridge fixture exited before readiness; details retained privately by the owning process.");
                try {using var response=await client.GetAsync(new Uri(origin,"/readiness"));if((int)response.StatusCode==200&&
                    response.Headers.TryGetValues(PentaPrivateContextLeases.VersionHeader,out var v)&&v.Single()==PentaPrivateContextLeases.Version)return owned;}
                catch(HttpRequestException){}catch(TaskCanceledException){}
                await Task.Delay(100);
            }
            throw new InvalidOperationException("Run-owned bridge did not become ready.");
        }catch{if(owned is not null)await owned.DisposeAsync();else{await callback.DisposeAsync();process.Dispose();}throw;}
    }
    public async Task StopInterpreterAsync(){if(!process.HasExited){process.Kill();await process.WaitForExitAsync();}}
    public async ValueTask DisposeAsync(){await StopInterpreterAsync();await Task.WhenAll(stdout,stderr);Interpreter.Dispose();await callback.StopAsync();await callback.DisposeAsync();process.Dispose();}
}
