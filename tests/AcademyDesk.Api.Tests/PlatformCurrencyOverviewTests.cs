using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Entities;
using AcademyDesk.Api.Domain.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AcademyDesk.Api.Tests;

public sealed class PlatformCurrencyOverviewTests
{
    [Fact]
    public async Task Mixed_portfolio_has_no_fictional_scalar_total_and_retains_nonmoney_counts()
    {
        await using var f = await Fixture.Create();
        await f.Bills(("INR",1000.25m,"Issued"),("INR",200.10m,"Paid"),("INR",50.05m,"Payment submitted"),
            ("USD",100.15m,"Overdue"),("USD",20.01m,"Paid"),("EUR",5000m,"Draft"),("GBP",9000m,"Void"));
        var before = await f.Snapshot(); var json = await f.Overview();
        foreach (var key in new[] { "totalBilled", "collectedBilling", "outstandingBilling" }) Assert.Equal(JsonValueKind.Null, json.GetProperty(key).ValueKind);
        Assert.True(json.GetProperty("hasMixedCurrencies").GetBoolean()); Assert.Equal(JsonValueKind.Null, json.GetProperty("currency").ValueKind);
        var groups = Groups(json); Assert.Equal(new[] { "INR", "USD" }, groups.Keys);
        Money(groups["INR"],1250.40m,200.10m,1050.30m); Money(groups["USD"],120.16m,20.01m,100.15m);
        Assert.Equal(2,json.GetProperty("academies").GetInt32()); Assert.Equal(1,json.GetProperty("activeAcademies").GetInt32());
        Assert.Equal(1,json.GetProperty("overdueInvoices").GetInt32()); Assert.True(json.TryGetProperty("plans",out _)); Assert.True(json.TryGetProperty("recentAudit",out _));
        Assert.Equal(before,await f.Snapshot());
    }

    [Theory] [InlineData("INR")] [InlineData("USD")] [InlineData("GBP")]
    public async Task Single_currency_preserves_scalar_contract_and_cents(string currency)
    {
        await using var f = await Fixture.Create(); await f.Bills((currency,100.55m,"Issued"),(currency,20.10m,"Paid"),(currency,.05m,"Payment submitted"));
        var json = await f.Overview(); Assert.Equal(currency,json.GetProperty("currency").GetString()); Assert.False(json.GetProperty("hasMixedCurrencies").GetBoolean());
        Money(json,120.70m,20.10m,100.60m); Money(Assert.Single(Groups(json)).Value,120.70m,20.10m,100.60m);
    }

    [Theory]
    [InlineData("Draft",0,0,0,0)] [InlineData("Void",0,0,0,0)]
    [InlineData("Issued",100,0,100,0)] [InlineData("Overdue",100,0,100,1)]
    [InlineData("Payment submitted",100,0,100,0)] [InlineData("Paid",100,100,0,0)]
    [InlineData("Other",100,0,0,0)] [InlineData("issued",100,0,100,0)]
    [InlineData("paid",100,100,0,0)] [InlineData("draft",0,0,0,0)] [InlineData("void",0,0,0,0)]
    [InlineData(" Payment submitted ",100,0,100,0)] [InlineData("Paid ",100,100,0,0)] [InlineData("Draft ",0,0,0,0)]
    public async Task Invoice_states_never_turn_an_unverified_submission_into_collections(string status,int billed,int collected,int outstanding,int overdue)
    {
        await using var f = await Fixture.Create(); await f.Bills(("INR",100m,status)); var before = await f.Snapshot(); var json = await f.Overview();
        Money(json,billed,collected,outstanding); Assert.Equal(overdue,json.GetProperty("overdueInvoices").GetInt32()); Assert.Equal(before,await f.Snapshot());
    }

    [Fact]
    public async Task Empty_portfolio_has_no_currency_and_zero_amounts()
    {
        await using var f = await Fixture.Create(); var json = await f.Overview(); Assert.Empty(Groups(json));
        Assert.Equal(JsonValueKind.Null,json.GetProperty("currency").ValueKind); Assert.False(json.GetProperty("hasMixedCurrencies").GetBoolean()); Money(json,0,0,0);
    }

    [Fact]
    public async Task Draft_and_void_currencies_do_not_make_an_empty_active_portfolio_mixed()
    {
        await using var f = await Fixture.Create(); await f.Bills(("USD",10m,"Draft"),("INR",20m,"Void")); var json = await f.Overview();
        Assert.Empty(Groups(json)); Assert.False(json.GetProperty("hasMixedCurrencies").GetBoolean()); Money(json,0,0,0);
    }

    [Fact]
    public async Task Currency_case_and_whitespace_group_without_an_exchange_rate()
    {
        await using var f = await Fixture.Create(); await f.Bills((" usd ",5.10m,"Issued"),("USD",5.10m,"Paid")); var json = await f.Overview();
        Assert.Equal("USD",json.GetProperty("currency").GetString()); Assert.Single(Groups(json)); Money(json,10.20m,5.10m,5.10m);
    }

    [Theory] [InlineData("AcademyAdmin")] [InlineData("FinanceUser")] [InlineData("PlatformOwner")]
    public async Task Role_strings_without_current_platform_owner_flag_cannot_read_global_money(string role)
    {
        await using var f = await Fixture.Create(false,role); await f.Bills(("USD",100m,"Issued")); var before=await f.Snapshot();
        using var denied=await f.Client.GetAsync("/api/platform/overview"); Assert.Equal(HttpStatusCode.Forbidden,denied.StatusCode); Assert.Equal(before,await f.Snapshot());
    }

    [Fact]
    public async Task Anonymous_and_revoked_platform_authority_are_checked_on_the_current_request()
    {
        await using var f=await Fixture.Create(); var ticket=f.Client.DefaultRequestHeaders.Authorization; f.Client.DefaultRequestHeaders.Authorization=null;
        using(var denied=await f.Client.GetAsync("/api/platform/overview"))Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
        f.Client.DefaultRequestHeaders.Authorization=ticket; await f.Overview();
        await using(var scope=f.Factory.Services.CreateAsyncScope()){var db=scope.ServiceProvider.GetRequiredService<IdentityDbContext>();(await db.Users.SingleAsync()).IsPlatformOwner=false;await db.SaveChangesAsync();}
        using var revoked=await f.Client.GetAsync("/api/platform/overview"); Assert.Equal(HttpStatusCode.Forbidden,revoked.StatusCode);
    }

    private static Dictionary<string,JsonElement> Groups(JsonElement x)=>x.GetProperty("byCurrency").EnumerateArray().ToDictionary(r=>r.GetProperty("currency").GetString()!,r=>r);
    private static void Money(JsonElement x,decimal billed,decimal collected,decimal outstanding)
    {Assert.Equal(billed,x.GetProperty("totalBilled").GetDecimal());Assert.Equal(collected,x.GetProperty("collectedBilling").GetDecimal());Assert.Equal(outstanding,x.GetProperty("outstandingBilling").GetDecimal());}

    private sealed class Fixture:IAsyncDisposable
    {
        public HttpFactory Factory=new();public HttpClient Client=null!;public Guid[] Academies=[Guid.NewGuid(),Guid.NewGuid()];
        public static async Task<Fixture> Create(bool owner=true,string role="PlatformOwner")
        {
            var f=new Fixture();try{
                f.Client=f.Factory.CreateClient(new(){BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
                await using(var scope=f.Factory.Services.CreateAsyncScope()){
                    var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();db.AddRange(new Academy{Id=f.Academies[0],Name="Synthetic active"},new Academy{Id=f.Academies[1],Name="Synthetic inactive",IsActive=false});await db.SaveChangesAsync();
                    var roles=scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();Assert.True((await roles.CreateAsync(new ApplicationRole{Name=role,IsSystemRole=true})).Succeeded);
                    var users=scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();var actor=new ApplicationUser{UserName="platform-currency@example.invalid",Email="platform-currency@example.invalid",EmailConfirmed=true,DisplayName="Synthetic platform currency",AcademyId=f.Academies[0],IsPlatformOwner=owner};
                    Assert.True((await users.CreateAsync(actor,"Synthetic!39Ab")).Succeeded);Assert.True((await users.AddToRoleAsync(actor,role)).Succeeded);
                }
                using var login=await f.Client.PostAsJsonAsync("/api/auth/login",new{email="platform-currency@example.invalid",password="Synthetic!39Ab"});Assert.Equal(HttpStatusCode.OK,login.StatusCode);
                using var token=JsonDocument.Parse(await login.Content.ReadAsStringAsync());f.Client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token.RootElement.GetProperty("accessToken").GetString());return f;
            }catch{await f.DisposeAsync();throw;}
        }
        public async Task Bills(params (string Currency,decimal Amount,string Status)[] rows)
        {
            await using var scope=Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();var i=0;
            db.AddRange(rows.Select(x=>new PlatformBillingInvoice{AcademyId=Academies[i++%2],InvoiceNumber=Guid.NewGuid().ToString(),Amount=x.Amount,Currency=x.Currency,Status=x.Status,PeriodStart=new(2026,1,1),PeriodEnd=new(2026,1,31),DueDate=new(2026,2,1)}));await db.SaveChangesAsync();
        }
        public async Task<JsonElement> Overview(){using var response=await Client.GetAsync("/api/platform/overview");Assert.Equal(HttpStatusCode.OK,response.StatusCode);using var body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());return body.RootElement.Clone();}
        public async Task<string> Snapshot(){await using var scope=Factory.Services.CreateAsyncScope();var db=scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>();return JsonSerializer.Serialize(new{bills=await db.PlatformBillingInvoices.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),academies=await db.Academies.AsNoTracking().OrderBy(x=>x.Id).ToListAsync(),audit=await db.PlatformAuditEntries.AsNoTracking().OrderBy(x=>x.Id).ToListAsync()});}
        public async ValueTask DisposeAsync(){Client?.Dispose();await Factory.DisposeAsync();}
    }

    private sealed class HttpFactory:WebApplicationFactory<Program>
    {
        private readonly string database=$"platform-currency-{Guid.NewGuid():N}";
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?>{["Database:ApplyMigrationsOnStartup"]="false",["Bootstrap:PlatformOwnerEmail"]="",["Bootstrap:PlatformOwnerPassword"]="",["MediaStorage:Enabled"]="false",["Penta:Enabled"]="false"}));
            builder.ConfigureServices(s=>{s.RemoveAll<DbContextOptions<AcademyDeskDbContext>>();s.RemoveAll<DbContextOptions<IdentityDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<AcademyDeskDbContext>>();s.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();s.RemoveAll<AcademyDeskDbContext>();s.RemoveAll<IdentityDbContext>();s.AddDbContext<AcademyDeskDbContext>(o=>o.UseInMemoryDatabase(database+"-domain"));s.AddDbContext<IdentityDbContext>(o=>o.UseInMemoryDatabase(database+"-identity"));});
        }
    }
}
