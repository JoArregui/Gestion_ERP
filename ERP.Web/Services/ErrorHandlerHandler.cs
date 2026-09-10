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
            bool alreadyOnLogin = false;
            try
            {
                alreadyOnLogin = _navManager.ToBaseRelativePath(_navManager.Uri)
                    .StartsWith("login", StringComparison.OrdinalIgnoreCase);
            }
            catch { alreadyOnLogin = false; }

            if (alreadyOnLogin) return;

            try
            {
                // Purga RGPD completa — mismo que AuthService.Logout para evitar fuga entre empresas
                try { await _jsRuntime.InvokeVoidAsync("eval", "localStorage.removeItem('authToken'); Object.keys(localStorage).forEach(k=>{if(k.startsWith('erp_'))localStorage.removeItem(k)}); try{sessionStorage.clear();}catch(e){}"); } catch { }
                var authProvider = _serviceProvider.GetService<AuthenticationStateProvider>();
                if (authProvider is CustomAuthenticationProvider provider) provider.NotifyUserLogout();
                try { var es = _serviceProvider.GetService<EmpresaService>(); if (es != null) await es.ClearAsync(); } catch { }
                try { _serviceProvider.GetService<NotificationService>()?.ClearHistory(); } catch { }
                try
                {
                    var http = _serviceProvider.GetService<HttpClient>();
                    if (http != null) http.DefaultRequestHeaders.Authorization = null;
                } catch { }
                _notify.Error("Sesión expirada. Por favor, vuelva a iniciar sesión.");
            }
            catch { }
            finally
            {
                try { _navManager.NavigateTo("/login", forceLoad: true); } catch { }
            }
        }
}
}