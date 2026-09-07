using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ERP.Web.Services
{
    public class JwtAuthorizationHandler : DelegatingHandler
    {
        private readonly IJSRuntime _jsRuntime;

        public JwtAuthorizationHandler(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                string? token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception)
            {
                // Si localStorage no está disponible (ej. SSR o modo offline), continuamos sin token
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}