using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Serilog;
using SpearSoft.NeighborhoodToolCoop.Server.Auth;
using SpearSoft.NeighborhoodToolCoop.Server.Data;
using SpearSoft.NeighborhoodToolCoop.Server.Endpoints;
using SpearSoft.NeighborhoodToolCoop.Server.Extensions;
using SpearSoft.NeighborhoodToolCoop.Server.Middleware;
using SpearSoft.NeighborhoodToolCoop.Server.Services;
using SpearSoft.NeighborhoodToolCoop.Server.Services.Labels;

// Bootstrap logger catches fatal startup errors before full Serilog is configured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Logging ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services));

    // ── Data ───────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<DbConnectionFactory>();
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddRepositories();

    // ── Labels ─────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<QrCodeGenerator>();  // stateless
    builder.Services.AddScoped<LabelService>();

    // ── Authentication: cookie session + Google OIDC ───────────────────────
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme       = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = GoogleDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.Name       = "toolcoop.auth";
            options.Cookie.HttpOnly   = true;
            options.Cookie.SameSite   = SameSiteMode.Lax;
            options.ExpireTimeSpan    = TimeSpan.FromDays(14);
            options.SlidingExpiration = true;
            options.LoginPath         = "/auth/login-required";
        })
        .AddGoogle(options =>
        {
            options.ClientId     = builder.Configuration["Authentication:Google:ClientId"]!;
            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            options.CallbackPath = "/signin-google";
            options.Events.OnCreatingTicket = GoogleAuthEvents.OnCreatingTicket;
        });

    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("ManagerOrAbove", p =>
            p.RequireAuthenticatedUser()
             .RequireAssertion(ctx =>
                 ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Manager")));

        opts.AddPolicy("AdminOnly", p =>
            p.RequireAuthenticatedUser()
             .RequireRole("Admin"));
    });

    // ── Background jobs (Hangfire) ─────────────────────────────────────────
    var connStr = builder.Configuration.GetConnectionString("Default")!;
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(connStr)));
    builder.Services.AddHangfireServer();

    // ── CORS (development: allow the WASM dev server) ─────────────────────
    builder.Services.AddCors(options =>
        options.AddPolicy("BlazorDev", policy =>
            policy.WithOrigins(
                      "https://localhost:7001",
                      "http://localhost:5001")
                  .AllowCredentials()
                  .AllowAnyHeader()
                  .AllowAnyMethod()));

    // ══════════════════════════════════════════════════════════════════════
    var app = builder.Build();
    // ══════════════════════════════════════════════════════════════════════

    // ── DB migrations (run before accepting traffic) ───────────────────────
    DatabaseMigrator.RunMigrations(
        app.Configuration,
        app.Environment,
        app.Logger);

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("BlazorDev");

    // Auth must run BEFORE TenantResolutionMiddleware so that claim-based
    // tenant resolution (for /api/v1 routes) can read the authenticated principal
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();

    // Hangfire dashboard — restrict to Admin role in production
    app.MapHangfireDashboard("/hangfire");

    app.MapAuthEndpoints();
    app.MapLabelEndpoints();
    app.MapToolEndpoints();
    app.MapLoanEndpoints();
    app.MapLocationEndpoints();
    app.MapMemberEndpoints();

    // TODO (hosted model): app.UseBlazorFrameworkFiles();
    // TODO (hosted model): app.UseStaticFiles();
    // TODO (hosted model): app.MapFallbackToFile("index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
