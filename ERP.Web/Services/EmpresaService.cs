using System;
using System.Collections.Generic;
using System.Linq;
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
                _empresas = await _http.GetFromJsonAsync<List<Empresa>>("api/empresas");
                if (_empresas == null || _empresas.Count == 0) return;

                // Recuperar última sede activa del storage, o usar la primera activa
                var guardadaRaw = await _js.InvokeAsync<string>("localStorage.getItem", StorageKey);
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
            }
        }

        public async Task<bool> SelectEmpresaAsync(int empresaId)
        {
            if (_empresas == null || !_empresas.Exists(e => e.Id == empresaId)) return false;
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, empresaId.ToString());
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