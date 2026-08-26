using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using ERP.Domain.Constants;

namespace ERP.Web.Services
{
    public class CustomAuthenticationProvider : AuthenticationStateProvider
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;

        public CustomAuthenticationProvider(IJSRuntime jsRuntime, HttpClient httpClient)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                string? token = null;

                try
                {
                    // En Blazor Server (Prerendering), JS Interop falla al arrancar porque aún no hay conexión DOM/WebSocket.
                    token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                }
                catch (InvalidOperationException)
                {
                    // Captura la excepción cuando JS Interop no está listo durante el prerender
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                catch (JSException)
                {
                    // Captura errores específicos de JavaScript durante la inicialización
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var claims = ParseClaimsFromJwt(token);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var identity = new ClaimsIdentity(claims, "jwt");
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                }
                catch
                {
                    // Evita que un fallo de JS Interop al limpiar detenga la app
                }

                _httpClient.DefaultRequestHeaders.Authorization = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public void NotifyUserAuthentication(string token)
        {
            try
            {
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var authState = Task.FromResult(new AuthenticationState(user));
                NotifyAuthenticationStateChanged(authState);
            }
            catch
            {
                NotifyUserLogout();
            }
        }

        public void NotifyUserLogout()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymous));
            
            NotifyAuthenticationStateChanged(authState);
        }

        private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
        {
            var parts = token.Split('.');
            if (parts.Length != 3) throw new FormatException("El token JWT no es válido.");

            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                Convert.FromBase64String(payload)) ?? throw new FormatException("Payload JWT vacío.");

            foreach (var pair in values)
            {
                if (pair.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var value in pair.Value.EnumerateArray())
                        yield return new Claim(MapClaimType(pair.Key), value.ToString());
                }
                else
                {
                    yield return new Claim(MapClaimType(pair.Key), pair.Value.ToString());
                }
            }
        }

        private static string MapClaimType(string type) => type switch
        {
            "email" => ClaimTypes.Email,
            "unique_name" => ClaimTypes.Name,
            "role" => ClaimTypes.Role,
            ClaimTypes.Role => ClaimTypes.Role,
            _ => type
        };
    }
}