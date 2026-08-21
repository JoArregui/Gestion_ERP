/* using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using System.Text.Json;

namespace ERP.Web.Services
{
    public class CustomAuthenticationProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public CustomAuthenticationProvider(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            IEnumerable<Claim> claims;
            try
            {
                // SOPORTE DUAL: Bypass para desarrollo o JWT real
                if (token == "bypass-token")
                {
                    claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, "Usuario Desarrollo"),
                        new Claim("EmpresaId", "1"), // ID de empresa por defecto para pruebas
                        new Claim(ClaimTypes.Role, "Admin")
                    };
                }
                else
                {
                    claims = ParseClaimsFromJwt(token);
                }
            }
            catch
            {
                // Si el token está corrupto, limpiamos y devolvemos anónimo
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }

        public void NotifyUserAuthentication(string token)
        {
            IEnumerable<Claim> claims;

            if (token == "bypass-token")
            {
                claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Usuario Desarrollo"),
                    new Claim("EmpresaId", "1"),
                    new Claim(ClaimTypes.Role, "Admin")
                };
            }
            else
            {
                claims = ParseClaimsFromJwt(token);
            }

            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            NotifyAuthenticationStateChanged(authState);
        }

        public void NotifyUserLogout()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            NotifyAuthenticationStateChanged(authState);
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new Claim(kvp.Key, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty));
                    }
                }
            }

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
} */





using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using ERP.Domain.Constants; // Para los permisos

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
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            IEnumerable<Claim> claims;
            try
            {
                claims = ParseClaimsFromJwt(token);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            catch
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                _httpClient.DefaultRequestHeaders.Authorization = null;
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public void NotifyUserAuthentication(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void NotifyUserLogout()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
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