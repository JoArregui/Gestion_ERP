/* using System.Net.Http.Json;
using Microsoft.JSInterop;
using ERP.Domain.Dtos;
using Microsoft.AspNetCore.Components.Authorization;

namespace ERP.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly CustomAuthenticationProvider _authStateProvider;
        private readonly IJSRuntime _jsRuntime;

        public AuthService(HttpClient httpClient, 
                           AuthenticationStateProvider authStateProvider, 
                           IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _authStateProvider = (CustomAuthenticationProvider)authStateProvider;
            _jsRuntime = jsRuntime;
        }

        public async Task<AuthResponseDto?> Login(LoginDto loginDto)
        {
            // Ajusta la ruta a tu controlador real (ej: api/auth/login o api/usuarios/login)
            var result = await _httpClient.PostAsJsonAsync("api/usuarios/login", loginDto);
            
            var response = await result.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (result.IsSuccessStatusCode && response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", response.Token);
                _authStateProvider.NotifyUserAuthentication(response.Token);
                return response;
            }
            
            return response ?? new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Error de servidor" };
        }

        public async Task Logout()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            _authStateProvider.NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
} */




using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.JSInterop;
using ERP.Domain.Dtos;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Web.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly CustomAuthenticationProvider _authStateProvider;
        private readonly IJSRuntime _jsRuntime;
        private readonly IServiceProvider _sp;

        public AuthService(HttpClient httpClient, 
                           AuthenticationStateProvider authStateProvider, 
                           IJSRuntime jsRuntime,
                           IServiceProvider sp)
        {
            _httpClient = httpClient;
            _authStateProvider = (CustomAuthenticationProvider)authStateProvider;
            _jsRuntime = jsRuntime;
            _sp = sp;
        }

        public async Task<AuthResponseDto?> Login(LoginDto loginDto)
        {
            // Purga preventiva: evita que datos de sesión A (empresa A) queden en memoria/storage al entrar como B
            await PurgeSessionStorageAsync(keepToken: false);
            try { var es = _sp.GetService<EmpresaService>(); if (es != null) await es.ClearAsync(); } catch { }
            try { _sp.GetService<NotificationService>()?.ClearHistory(); } catch { }
            _httpClient.DefaultRequestHeaders.Authorization = null;

            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            if (!response.IsSuccessStatusCode || result is null || string.IsNullOrWhiteSpace(result.Token))
                return result ?? new AuthResponseDto { IsAuthSuccessful = false, ErrorMessage = "Error de servidor" };

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", result.Token);
            _authStateProvider.NotifyUserAuthentication(result.Token);
            return result;
        }

        public async Task Logout()
        {
            await PurgeSessionStorageAsync(keepToken: false);
            _authStateProvider.NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            // Purga servicios en memoria que cachean datos por empresa
            try { var es = _sp.GetService<EmpresaService>(); if (es != null) await es.ClearAsync(); } catch { }
            try { var ns = _sp.GetService<NotificationService>(); ns?.ClearHistory(); } catch { }
        }

        public async Task<(bool ok, string message, string? devToken)> ForgotPassword(string email)
        {
            try
            {
                var resp = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", new { Email = email });
                var body = await resp.Content.ReadAsStringAsync();
                if (resp.IsSuccessStatusCode)
                {
                    // Intentar extraer DevToken si estamos en dev
                    try { var doc = System.Text.Json.JsonDocument.Parse(body); if (doc.RootElement.TryGetProperty("DevToken", out var t)) return (true, body, t.GetString()); } catch {}
                    return (true, body, null);
                }
                return (false, body, null);
            }
            catch (Exception ex) { return (false, ex.Message, null); }
        }

        public async Task<(bool ok, string message)> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            try
            {
                var resp = await _httpClient.PostAsJsonAsync("api/auth/reset-password", new { Email = email, Token = token, NewPassword = newPassword, ConfirmPassword = confirmPassword });
                var body = await resp.Content.ReadAsStringAsync();
                return (resp.IsSuccessStatusCode, body);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>
        /// Limpieza total de storage del navegador para evitar fuga multi-tenant.
        /// Elimina authToken + claves erp_* + sessionStorage. Si keepToken=true preserva el token (uso interno).
        /// Protege RGPD: un usuario de empresa A nunca ve datos de empresa B tras re-login.
        /// </summary>
        private async Task PurgeSessionStorageAsync(bool keepToken)
        {
            try
            {
                // Borrado granular + genérico erp_* para cubrir futuros keys sin enumerar
                if (!keepToken) try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken"); } catch { }
                try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "erp_empresas_active_id"); } catch { }
                try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "erp_onboarding_dismissed"); } catch { }
                // Borrado genérico: recorre localStorage y elimina todo lo que empiece por erp_
                try { await _jsRuntime.InvokeVoidAsync("eval", "Object.keys(localStorage).forEach(k=>{if(k.startsWith('erp_'))localStorage.removeItem(k)}); try{sessionStorage.clear();}catch(e){}"); } catch { }
            }
            catch { }
        }
    }
}