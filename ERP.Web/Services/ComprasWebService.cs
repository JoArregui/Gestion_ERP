using ERP.Domain.Entities;
using System.Net.Http.Json;

namespace ERP.Web.Services
{
    public class ComprasWebService
    {
        private readonly HttpClient _http;

        public ComprasWebService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<DocumentoComercial> pedidos, string? errorMessage)> GetPedidosPendientes()
        {
            try
            {
                var pedidos = await _http.GetFromJsonAsync<List<DocumentoComercial>>("api/compras/pendientes");
                return (pedidos ?? new List<DocumentoComercial>(), null);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return (new List<DocumentoComercial>(), $"Error de conexión: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (new List<DocumentoComercial>(), $"Error al cargar pedidos: {ex.Message}");
            }
        }

        public async Task<(bool success, string? errorMessage)> RecepcionarPedido(int id, string numeroAlbaran)
{
    try
    {
        var response = await _http.PostAsync($"api/compras/recepcionar/{id}?numeroAlbaran={numeroAlbaran}", null);
        if (response.IsSuccessStatusCode)
        {
            return (true, null);
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(errorBody) ? "Error desconocido al procesar la recepción." : errorBody);
        }
    }
    catch (HttpRequestException)
    {
        return (false, "Error de conexión con el servidor");
    }
    catch (Exception ex)
    {
        return (false, "Error inesperado: " + ex.Message);
    }
}

        /// <summary>Recepción 3-way con factura del proveedor (Fase C3).</summary>
        public async Task<(bool success, string? errorMessage)> RecepcionarConFactura(int id, string numeroAlbaran, string numeroFactura, decimal totalFactura)
        {
            try
            {
                var url = $"api/compras/recepcionar-con-factura/{id}?numeroAlbaran={Uri.EscapeDataString(numeroAlbaran)}&numeroFactura={Uri.EscapeDataString(numeroFactura)}&totalFactura={totalFactura.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                var response = await _http.PostAsync(url, null);
                if (response.IsSuccessStatusCode) return (true, null);
                return (false, await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                return (false, "Error inesperado: " + ex.Message);
            }
        }
    }
}