using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using ERP.Web;
using ERP.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- 1. AUTENTICACIÓN Y ESTADO ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthenticationProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationProvider>());

// --- 2. CONFIGURACIÓN HTTP CLIENT & HANDLER ---
builder.Services.AddTransient<ErrorHandlerHandler>();

// Registrar el HttpClient genérico asignándole el Handler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ErrorHandlerHandler>();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5109/")
    };
});

// --- 3. SERVICIOS ADICIONALES ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ComprasWebService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TesoreriaWebService>();
builder.Services.AddScoped<EmpleadoService>();

await builder.Build().RunAsync();