using ERP.Domain.Entities;
using System.Net.Http.Json;

namespace ERP.Web.Services
{
    public class TesoreriaWebService
    {
        private readonly HttpClient _http;

        public TesoreriaWebService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Vencimiento>> GetPendientesCobro()
        {
            return await _http.GetFromJsonAsync<List<Vencimiento>>("api/tesoreria/pendientes-cobro") ?? new();
        }

        public async Task<List<Vencimiento>> GetPendientesPago()
        {
            return await _http.GetFromJsonAsync<List<Vencimiento>>("api/tesoreria/pendientes-pago") ?? new();
        }

        public async Task<bool> LiquidarVencimiento(int id, string metodoPago)
        {
            var response = await _http.PostAsync($"api/tesoreria/liquidar/{id}?metodoPago={metodoPago}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<(bool ok, string? error)> ModificarVencimiento(Vencimiento v)
        {
            var resp = await _http.PutAsJsonAsync($"api/tesoreria/vencimiento/{v.Id}", v);
            if (resp.IsSuccessStatusCode) return (true, null);
            return (false, await resp.Content.ReadAsStringAsync());
        }

        public async Task<(bool ok, string? error)> AnularVencimiento(int id)
        {
            var resp = await _http.PostAsync($"api/tesoreria/anular/{id}", null);
            if (resp.IsSuccessStatusCode) return (true, null);
            return (false, await resp.Content.ReadAsStringAsync());
        }
    }
}