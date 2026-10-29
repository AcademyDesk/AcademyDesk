using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AcademyDesk.Api.Intelligence.Penta;

// Explicit, inactive local integration candidate. No config/env secret loader,
// production DI registration, remote DNS/TLS exception or fallback.
public sealed class PentaPrivateContextLeases(TimeProvider? time=null)
{
    public const string Version="penta-private-loopback-1";
    public const string VersionHeader="X-Penta-Private-Bridge";
    public const string BodyHeader="X-Penta-Turn-SHA256";
    private readonly TimeProvider clock=time??TimeProvider.System;
    private readonly ConcurrentDictionary<string,Entry> entries=new();
    private readonly object admission=new();
    private sealed class Entry(Guid request,string bodyHash,Func<CancellationToken,Task<JsonElement>> current,DateTimeOffset until,CancellationToken token) {
        public Guid Request=request;public Func<CancellationToken,Task<JsonElement>> Current=current;
        public string BodyHash=bodyHash;
        public DateTimeOffset Until=until;public CancellationToken Token=token;public int Reads;
        public readonly SemaphoreSlim Gate=new(1,1);
    }
    private sealed class Removal(Action remove):IDisposable {public void Dispose()=>remove();}
    public static bool IsToken(string value)=>value.Length==64&&value.All(c=>c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static string Key(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(value)));
    public static byte[] RequestBytes(PentaOnboardingInterpretation input)=>JsonSerializer.SerializeToUtf8Bytes(new{
        request_ref=input.RequestId.ToString("D"),text=input.Text,language="en",capability="Executor",
        base_revision=input.Snapshot.GetProperty("draft").GetProperty("revision").GetInt64()});
    public static string RequestHash(PentaOnboardingInterpretation input)=>Convert.ToHexStringLower(SHA256.HashData(RequestBytes(input)));
    public IDisposable Open(PentaOnboardingInterpretation input,CancellationToken token,out string credential) {
        if(input.Current is null||input.RequestId==Guid.Empty)throw new HttpRequestException("Private context unavailable.");
        token.ThrowIfCancellationRequested();credential=Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var key=Key(credential);
        lock(admission) {
            if(entries.Count>=2||!entries.TryAdd(key,new(input.RequestId,RequestHash(input),input.Current,clock.GetUtcNow().AddSeconds(30),token)))
                throw new HttpRequestException("Private context unavailable.");
        }
        return new Removal(()=>entries.TryRemove(key,out _));
    }
    public async Task<JsonElement?> ReadAsync(string credential,Guid request,string bodyHash,CancellationToken token) {
        if(!IsToken(credential)||!entries.TryGetValue(Key(credential),out var entry)||entry.Request!=request||
            !IsToken(bodyHash)||!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(entry.BodyHash),Encoding.ASCII.GetBytes(bodyHash))||
            entry.Token.IsCancellationRequested||entry.Until<=clock.GetUtcNow()||!await entry.Gate.WaitAsync(0,token))return null;
        try {
            if(++entry.Reads>2)return null; // exactly the Core before/after checks
            using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,entry.Token);
            var snapshot=await entry.Current(linked.Token);
            linked.Token.ThrowIfCancellationRequested();
            return entry.Until>clock.GetUtcNow()&&entries.ContainsKey(Key(credential))?snapshot:null;
        }finally{entry.Gate.Release();}
    }
}

public sealed class PentaPrivateLoopbackInterpreter:IPentaOnboardingInterpreter,IDisposable
{
    private readonly HttpClient client;private readonly PentaPrivateContextLeases leases;private readonly string channel;
    public PentaPrivateLoopbackInterpreter(Uri origin,string channelCredential,PentaPrivateContextLeases leases) {
        if(origin.Scheme!="http"||origin.Host!="127.0.0.1"||origin.IsDefaultPort||origin.AbsolutePath!="/"||
            origin.UserInfo.Length!=0||origin.Query.Length!=0||origin.Fragment.Length!=0||!PentaPrivateContextLeases.IsToken(channelCredential))
            throw new ArgumentException("Explicit literal loopback origin and private channel required.");
        this.leases=leases;channel=channelCredential;
        client=new HttpClient(new SocketsHttpHandler{AllowAutoRedirect=false,UseProxy=false,AutomaticDecompression=System.Net.DecompressionMethods.None})
            {BaseAddress=origin,Timeout=TimeSpan.FromSeconds(30)};
    }
    public async Task<JsonElement> InterpretAsync(PentaOnboardingInterpretation input,CancellationToken token) {
        using var lease=leases.Open(input,token,out var capability);
        using var request=new HttpRequestMessage(HttpMethod.Post,"/candidate/interpret");
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",channel);
        request.Headers.Add(PentaPrivateContextLeases.VersionHeader,PentaPrivateContextLeases.Version);
        request.Headers.Add("X-Penta-Context-Lease",capability);request.Headers.AcceptEncoding.ParseAdd("identity");
        request.Content=new ByteArrayContent(PentaPrivateContextLeases.RequestBytes(input));
        request.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json");
        using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        if(!response.IsSuccessStatusCode||!response.Headers.TryGetValues(PentaPrivateContextLeases.VersionHeader,out var versions)||
            !versions.SequenceEqual(new[]{PentaPrivateContextLeases.Version})||response.Content.Headers.ContentEncoding.Count!=0||
            response.Content.Headers.ContentType?.MediaType!="application/json")throw new HttpRequestException("Private interpretation unavailable.");
        await using var stream=await response.Content.ReadAsStreamAsync(token);using var bytes=new MemoryStream();
        var buffer=new byte[8192];int read;
        while((read=await stream.ReadAsync(buffer,token))!=0){if(bytes.Length+read>65536)throw new JsonException("Invalid private response.");bytes.Write(buffer,0,read);}
        using var document=JsonDocument.Parse(bytes.ToArray(),new JsonDocumentOptions{MaxDepth=32});
        // The host completion parser still rejects duplicate/unknown fields and
        // reauthorizes the decision. No model response is a business receipt.
        return document.RootElement.Clone();
    }
    public void Dispose()=>client.Dispose();
}
