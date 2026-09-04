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
        private readonly IServiceProvider _serviceProvider;

        public ErrorHandlerHandler(
            IJSRuntime jsRuntime, 
            NavigationManager navManager, 
            NotificationService notify,
            IServiceProvider serviceProvider)
        {
            _jsRuntime = jsRuntime;
            _navManager = navManager;
            _notify = notify;
            _serviceProvider = serviceProvider;
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
            try
            {
                // Limpiar token caducado de localStorage
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");

                // Resolver AuthenticationStateProvider bajo demanda para evitar dependencia circular al arrancar
                var authProvider = _serviceProvider.GetService<AuthenticationStateProvider>();
                if (authProvider is CustomAuthenticationProvider provider)
                {
                    provider.NotifyUserLogout();
                }

                // Mostrar mensaje de sesión expirada
                _notify.Error("Sesión expirada. Por favor, vuelva a iniciar sesión.");
            }
            catch
            {
                // Prevenir que errores secundarios bloqueen la redirección
            }
            finally
            {
                // Redirigir al login
                _navManager.NavigateTo("/login", forceLoad: true);
            }
        }
    }
}