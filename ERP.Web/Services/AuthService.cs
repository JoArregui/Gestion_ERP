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
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            _authStateProvider.NotifyUserLogout();
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}