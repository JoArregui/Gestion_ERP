using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Web.Services
{
    public class ErrorHandlerHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navManager;
        private readonly NotificationService _notify;

        public ErrorHandlerHandler(
            IJSRuntime jsRuntime, 
            NavigationManager navManager, 
            NotificationService notify)
        {
            _jsRuntime = jsRuntime;
            _navManager = navManager;
            _notify = notify;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout real (servidor colgado) - este caso no lo cubren las páginas
                _notify.Error($"Tiempo de espera agotado al conectar con {request.RequestUri}. Verifique que el API esté ejecutándose en {request.RequestUri?.Scheme}://{request.RequestUri?.Host}:{request.RequestUri?.Port}/");
                throw new HttpRequestException($"Timeout hacia {request.RequestUri}", ex);
            }
            catch (HttpRequestException)
            {
                // Dejar que la página lo notifique (evita duplicado con MaestroArticulos.razor:258)
                throw;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorizedAsync();
            }

            return response;
        }

        private async Task HandleUnauthorizedAsync()
        {
            bool alreadyOnLogin = false;
            try
            {
                alreadyOnLogin = _navManager.ToBaseRelativePath(_navManager.Uri)
                    .StartsWith("login", StringComparison.OrdinalIgnoreCase);
            }
            catch { alreadyOnLogin = false; }

            // Si ya estamos en login, no hacer nada para evitar bucle infinito / spinner
            if (alreadyOnLogin) return;

            try
            {
                try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken"); } catch { }
                // No resolvemos AuthenticationStateProvider aquí para evitar ciclo
                // CustomAuthenticationProvider <-> HttpClient <-> ErrorHandlerHandler
                // El logout se notificará en el próximo GetAuthenticationStateAsync
                _notify.Error("Sesión expirada. Por favor, vuelva a iniciar sesión.");
            }
            catch { }
            finally
            {
                try
                {
                    // forceLoad:false evita recarga completa que dispara blazor-error-ui
                    _navManager.NavigateTo("/login", forceLoad: false);
                }
                catch { }
            }
        }
}
}