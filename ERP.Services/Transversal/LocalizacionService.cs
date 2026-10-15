using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using ERP.Data;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Transversal
{
    /// <summary>
    /// Soporte multidioma / i18n (Fase G3 §9).
    /// Usa ResourceManager + fallback a código por defecto (es-ES).
    /// </summary>
    public class LocalizacionService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Dictionary<string, ResourceManager> _managers = new();

        public LocalizacionService(ApplicationDbContext context) => _context = context;

        public static readonly CultureInfo CulturaDefecto = new("es-ES");
        public static readonly string[] CulturasSoportadas = { "es-ES", "en-US", "ca-ES", "eu-ES", "gl-ES" };

        /// <summary>
        /// Obtiene texto localizado. Si no existe, devuelve la clave.
        /// </summary>
        public string T(string clave, CultureInfo? cultura = null, params object[] args)
        {
            cultura ??= CulturaDefecto;
            var rm = ObtenerManager(cultura);
            var valor = rm.GetString(clave, cultura);
            if (string.IsNullOrEmpty(valor)) return args.Length > 0 ? string.Format(clave, args) : clave;
            return args.Length > 0 ? string.Format(valor, args) : valor;
        }

        public string T(string clave, string culturaCodigo, params object[] args)
            => T(clave, CultureInfo.GetCultureInfo(culturaCodigo), args);

        private static ResourceManager ObtenerManager(CultureInfo cultura)
        {
            var key = cultura.Name;
            if (_managers.TryGetValue(key, out var rm)) return rm;

            // En producción: cargar desde .resx embebidos o BD
            // Aquí fallback simple
            rm = new ResourceManager($"ERP.Resources.Strings.{key}", typeof(LocalizacionService).Assembly);
            _managers[key] = rm;
            return rm;
        }

        /// <summary>
        /// Culturas disponibles para el usuario.
        /// </summary>
        public async Task<List<CulturaInfo>> GetCulturasAsync(int empresaId)
        {
            // En BD: tabla ConfiguracionCultura por empresa
            return CulturasSoportadas.Select(c => new CulturaInfo
            {
                Codigo = c,
                Nombre = new CultureInfo(c).NativeName,
                EsDefecto = c == CulturaDefecto.Name
            }).ToList();
        }
    }

    public class CulturaInfo
    {
        public string Codigo { get; set; } = "";
        public string Nombre { get; set; } = "";
        public bool EsDefecto { get; set; }
    }

    /// <summary>
    /// Soporte multidivisa / conversión (Fase G3 §9).
    /// </summary>
    public class DivisaService
    {
        private readonly ApplicationDbContext _context;
        private static readonly Dictionary<string, decimal> _cacheTasas = new();

        public DivisaService(ApplicationDbContext context) => _context = context;

        public static readonly string DivisaBase = "EUR";

        /// <summary>
        /// Convierte importe de divisa origen a destino usando tasa del día (o cache).
        /// </summary>
        public async Task<decimal> ConvertirAsync(decimal importe, string divisaOrigen, string divisaDestino, DateTime? fecha = null)
        {
            if (divisaOrigen == divisaDestino) return importe;
            if (importe == 0) return 0;

            var tasa = await ObtenerTasaAsync(divisaOrigen, divisaDestino, fecha ?? DateTime.Today);
            return Math.Round(importe * tasa, 4);
        }

        /// <summary>
        /// Formatea importe con símbolo y decimales según divisa.
        /// </summary>
        public string Formatear(decimal importe, string divisa, CultureInfo? cultura = null)
        {
            cultura ??= CultureInfo.CurrentCulture;
            var info = ObtenerInfoDivisa(divisa);
            return $"{importe.ToString($"N{info.Decimales}", cultura)} {info.Simbolo}";
        }

        private async Task<decimal> ObtenerTasaAsync(string desde, string hacia, DateTime fecha)
        {
            var key = $"{desde}-{hacia}-{fecha:yyyyMMdd}";
            if (_cacheTasas.TryGetValue(key, out var cached)) return cached;

            // TODO: En producción llamar a API externa (ECB, fixer.io, etc.) y guardar en BD tabla TasasCambio
            // Fallback hardcoded para demo
            var tasasDemo = new Dictionary<string, decimal>
            {
                ["EUR-USD"] = 1.08m,
                ["USD-EUR"] = 0.9259m,
                ["EUR-GBP"] = 0.86m,
                ["GBP-EUR"] = 1.1628m,
            };

            var tasa = tasasDemo.GetValueOrDefault($"{desde}-{hacia}", 1m);
            _cacheTasas[key] = tasa;
            return tasa;
        }

        private static InfoDivisa ObtenerInfoDivisa(string codigo)
        {
            return codigo switch
            {
                "EUR" => new InfoDivisa { Codigo = "EUR", Simbolo = "€", Decimales = 2 },
                "USD" => new InfoDivisa { Codigo = "USD", Simbolo = "$", Decimales = 2 },
                "GBP" => new InfoDivisa { Codigo = "GBP", Simbolo = "£", Decimales = 2 },
                "JPY" => new InfoDivisa { Codigo = "JPY", Simbolo = "¥", Decimales = 0 },
                _ => new InfoDivisa { Codigo = codigo, Simbolo = codigo, Decimales = 2 }
            };
        }

        private class InfoDivisa
        {
            public string Codigo { get; set; } = "";
            public string Simbolo { get; set; } = "";
            public int Decimales { get; set; }
        }
    }
}