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
                    // En Blazor WASM el JS Interop puede fallar durante el arranque / desconexión
                    token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                }
                catch (InvalidOperationException)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                catch (JSException)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
                catch (Exception)
                {
                    // JSDisconnectedException y otros durante blazor-error-ui / reload
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var identity = CreateIdentityFromToken(token);

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
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var identity = CreateIdentityFromToken(token);
                var user = new ClaimsPrincipal(identity);

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

        private static ClaimsIdentity CreateIdentityFromToken(string token)
        {
            var claims = ParseClaimsFromJwt(token).ToList();

            // Especificar explícitamente NameType y RoleType para que IsInRole("Admin") funcione correctamente
            var identity = new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role);

            // --- DETECCIÓN ONBOARDING ---
            var empresaIdClaim = identity.FindFirst("EmpresaId");
            if (empresaIdClaim == null || string.IsNullOrWhiteSpace(empresaIdClaim.Value) || empresaIdClaim.Value == "0")
            {
                identity.AddClaim(new Claim("OnboardingRequired", "true"));
            }
            // --- FIN DETECCIÓN ONBOARDING ---

            return identity;
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

        private static string MapClaimType(string type) => type.ToLower() switch
        {
            "email" => ClaimTypes.Email,
            "unique_name" => ClaimTypes.Name,
            "name" => ClaimTypes.Name,
            "role" => ClaimTypes.Role,
            "roles" => ClaimTypes.Role,
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" => ClaimTypes.Role,
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" => ClaimTypes.Name,
            _ => type
        };
    }
}