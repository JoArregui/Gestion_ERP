using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ERP.Domain.Entities;
using Microsoft.JSInterop;

namespace ERP.Web.Services
{
    /// <summary>
    /// Contexto de empresa activa (emisora) para el ciclo de facturación.
    /// Permite cambiar de sede corporativa desde cualquier página sin re-inicializar el singleton.
    /// Persistencia en localStorage + broadcast de cambios vía eventos para que los componentes se refresquen.
    /// </summary>
    public class EmpresaService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;
        private const string StorageKey = "erp_empresas_active_id";

        private List<Empresa>? _empresas;
        private int _empresaId;

        public Empresa? EmpresaActual { get; private set; }

        public event Action? OnChange;

        public EmpresaService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        public async Task LoadAsync()
        {
            try
            {
                var response = await _http.GetAsync("api/empresas");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"EmpresaService.LoadAsync warning: La API devolvió el estado {response.StatusCode}");
                    _empresas = new List<Empresa>();
                    return;
                }

                _empresas = await response.Content.ReadFromJsonAsync<List<Empresa>>() ?? new List<Empresa>();
                if (_empresas.Count == 0) return;

                // Recuperar última sede activa del storage de forma segura
                string? guardadaRaw = null;
                try
                {
                    guardadaRaw = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
                }
                catch (Exception jsEx)
                {
                    Console.WriteLine($"EmpresaService.LoadAsync JSInterop warning: {jsEx.Message}");
                }

                if (!string.IsNullOrEmpty(guardadaRaw) &&
                    int.TryParse(guardadaRaw, out var guardada) &&
                    (_empresas?.Exists(e => e.Id == guardada) ?? false))
                {
                    await SetEmpresaAsync(guardada, notify: false);
                }
                else
                {
                    var activa = _empresas.Find(e => e.IsActiva) ?? _empresas[0];
                    await SetEmpresaAsync(activa.Id, notify: false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EmpresaService.LoadAsync error: {ex.Message}");
                _empresas = new List<Empresa>();
            }
        }

        public async Task<bool> SelectEmpresaAsync(int empresaId)
        {
            if (_empresas == null || !_empresas.Exists(e => e.Id == empresaId)) return false;
            
            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, empresaId.ToString());
            }
            catch (Exception jsEx)
            {
                Console.WriteLine($"EmpresaService.SelectEmpresaAsync JSInterop warning: {jsEx.Message}");
            }

            await SetEmpresaAsync(empresaId, notify: true);
            return true;
        }

        private async Task SetEmpresaAsync(int id, bool notify)
        {
            _empresaId = id;
            EmpresaActual = _empresas?.Find(e => e.Id == id);
            if (notify) OnChange?.Invoke();
            await Task.CompletedTask;
        }

        public List<Empresa>? Empresas => _empresas;
        public int EmpresaId => _empresaId;
        public bool IsLoaded => _empresas != null && _empresas.Count > 0;

        /// <summary>
        /// Borrado completo de estado en memoria + localStorage. Llamar en Logout para evitar remanentes de sesión.
        /// Garantiza que el pasillo (admin@erp.local) vuelva virgen tras cerrar sesión.
        /// </summary>
        public async Task ClearAsync()
        {
            _empresas = null;
            EmpresaActual = null;
            _empresaId = 0;
            try { await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey); } catch { }
            OnChange?.Invoke();
        }

        public void ClearSync()
        {
            _empresas = null;
            EmpresaActual = null;
            _empresaId = 0;
            OnChange?.Invoke();
        }

        public async Task<Empresa?> GetEmpresaActualAsync()
        {
            if (EmpresaActual != null) return EmpresaActual;
            if (_empresas == null || _empresas.Count == 0)
            {
                try { await LoadAsync(); } catch { }
            }
            return EmpresaActual ?? _empresas?.FirstOrDefault(e => e.IsActiva) ?? _empresas?.FirstOrDefault();
        }
    }
}