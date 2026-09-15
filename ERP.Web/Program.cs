using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using ERP.Web;
using ERP.Web.Services;
using ERP.Domain.Constants;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddAuthorizationCore(options =>
{
    foreach (var permission in AppPermissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("Permission", permission));
    }
});

builder.Services.AddScoped<CustomAuthenticationProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationProvider>());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<ComprasWebService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TesoreriaWebService>();
builder.Services.AddScoped<EmpleadoService>();

await builder.Build().RunAsync();