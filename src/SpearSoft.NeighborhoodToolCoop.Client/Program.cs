using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SpearSoft.NeighborhoodToolCoop.Client;
using SpearSoft.NeighborhoodToolCoop.Client.Auth;
using SpearSoft.NeighborhoodToolCoop.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HttpClient uses the server origin — cookies are sent automatically
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Auth: custom state provider calls /api/me to reflect the server cookie
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthorizationCore();

// API services
builder.Services.AddScoped<ToolApiService>();
builder.Services.AddScoped<LoanApiService>();
builder.Services.AddScoped<LocationApiService>();
builder.Services.AddScoped<MemberApiService>();

await builder.Build().RunAsync();
