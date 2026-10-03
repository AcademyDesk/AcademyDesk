using AcademyDesk.Api.Data;
using AcademyDesk.Api.Domain.Identity;
using AcademyDesk.Api.Security;
using AcademyDesk.Api.Infrastructure;
using AcademyDesk.Api.Infrastructure.Media;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
// Validate development credentials before migrations, bootstrap, or seeding can write data.
var developmentSeedCredentials = builder.Environment.IsDevelopment()
    ? DevelopmentSeedCredentials.FromConfiguration(builder.Configuration)
    : null;
var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
// The isolated test host supplies its own web root. Do not create a directory
// until that host has validated its final effective configuration.
if (!builder.Environment.IsEnvironment("Testing")) Directory.CreateDirectory(webRootPath);

// Add services to the container.

builder.Services.AddScoped<AcademyAccessFilter>();
builder.Services.AddControllers(options => options.Filters.AddService<AcademyAccessFilter>());
builder.Services.AddPrivateMediaStorage(builder.Configuration, builder.Environment);
builder.Services.AddScoped<ClassMaterialAccess>();
builder.Services.AddScoped<ClassMediaUploadPolicy>();
builder.Services.AddCors(options => options.AddPolicy("WebClient", policy =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (allowedOrigins.Length > 0) policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition");
}));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddDbContext<AcademyDeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddSignInManager<AcademySignInManager>();
builder.Services.AddScoped<ClassMaterialDownloadTickets>();
builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ClassMaterialDownloadHandler>(ClassMaterialDownloadTickets.Scheme, _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy("PrivateMaterialRead", policy =>
    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme, ClassMaterialDownloadTickets.Scheme).RequireAuthenticatedUser()));
// Platform, admin, teacher, and family portals are operated throughout a working day.
// Keep the short-lived access token secure while allowing the client to renew it for a
// reasonable remembered-session period without repeatedly asking users to sign in.
builder.Services.Configure<BearerTokenOptions>(IdentityConstants.BearerScheme, options =>
{
    options.BearerTokenExpiration = TimeSpan.FromHours(8);
    options.RefreshTokenExpiration = TimeSpan.FromDays(30);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Azure SQL starts empty for a new tenant environment. The deployment enables this
// explicitly so the first API revision prepares both application and identity data.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AcademyDeskDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
}

await ProductionIdentityBootstrapper.EnsurePlatformOwnerAsync(app.Services, builder.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PrivateMaterialFileProvider(app.Environment.WebRootFileProvider)
});
app.UseCors("WebClient");
app.UseRateLimiter();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errors => errors.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected server error occurred." });
    }));
}
app.UseAuthentication();

// Opaque Identity access tickets validate expiry, but do not query the account.
// Recheck the account and stamp on every authenticated request, including routes
// outside the academy filter. Private download credentials validate their own
// stamp in their handler when the endpoint's authorization policy selects it.
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user?.IsActive == false) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return; }
        var signIn = context.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        var stamp = context.User.FindFirst(userManager.Options.ClaimsIdentity.SecurityStampClaimType)?.Value;
        if (!await signIn.ValidateSecurityStampAsync(user, stamp))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>();
app.MapGet("/health", async (AcademyDeskDbContext db, CancellationToken token) =>
{
    // SqlClient may ignore cancellation during a TCP connect, so also bound the await.
    using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
    budget.CancelAfter(TimeSpan.FromSeconds(5));
    try
    {
        var connected = await Task.Run(async () => await db.Database.CanConnectAsync(budget.Token), token)
            .WaitAsync(TimeSpan.FromSeconds(5), token);
        return connected ? Results.Ok(new { status = "Healthy" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    catch { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
}).AllowAnonymous();

if (developmentSeedCredentials is not null) await DevelopmentIdentitySeeder.SeedAsync(app.Services, developmentSeedCredentials);

app.Run();

public partial class Program { }
