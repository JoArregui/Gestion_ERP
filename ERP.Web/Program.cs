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

// --- 1. AUTENTICACIÓN Y ESTADO + POLÍTICAS (restaurado: sin esto [Authorize(Policy="X")] falla) ---
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

// --- 2. CONFIGURACIÓN HTTP CLIENT & HANDLER ---
// FIX sistémico: el commit 97808f6 rompió TODAS las conexiones al crear
// HttpClient con DelegatingHandler sin InnerHandler -> InvalidOperationException
// Esto afectaba a los 87 Http.Get/Post de Dashboard, Articulos, Compras, Tesoreria, etc.
// y provocaba el "Error inesperado al cargar..." en MaestroArticulos.razor:261
// + el resto de páginas. Se usa Scoped (no Transient) y se asigna InnerHandler
// solo si es null para evitar "InnerHandler already set" al reutilizar scope.
builder.Services.AddScoped<ErrorHandlerHandler>();

// Registrar el HttpClient genérico asignándole el Handler
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ErrorHandlerHandler>();
    if (handler.InnerHandler == null)
    {
        // En Blazor WASM HttpClientHandler delega al Fetch del navegador
        handler.InnerHandler = new HttpClientHandler();
    }
    return new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:5109/")
    };
});

// --- 3. SERVICIOS ADICIONALES ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<ComprasWebService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TesoreriaWebService>();
builder.Services.AddScoped<EmpleadoService>();

await builder.Build().RunAsync();

