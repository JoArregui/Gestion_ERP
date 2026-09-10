using ERP.Domain.Entities;
using System.Net.Http.Json;

namespace ERP.Web.Services
{
    public class EmpleadoService
    {
        private readonly HttpClient _http;
        private const string BaseUrl = "api/empleados";

        public EmpleadoService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<Empleado>> GetEmpleadosAsync()
        {
            return await _http.GetFromJsonAsync<List<Empleado>>(BaseUrl) ?? new List<Empleado>();
        }

        public async Task<Empleado?> GetEmpleadoByIdAsync(int id)
        {
            return await _http.GetFromJsonAsync<Empleado>($"{BaseUrl}/{id}");
        }

        public async Task<bool> CrearEmpleadoAsync(Empleado empleado)
        {
            var response = await _http.PostAsJsonAsync(BaseUrl, empleado);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarEmpleadoAsync(Empleado empleado)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}/{empleado.Id}", empleado);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EliminarEmpleadoAsync(int id)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}