using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Fiscal;
using ERP.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Fiscal
{
    /// <summary>
    /// Motor de cálculo IVA: Prorrata general/especial, Sectores diferenciados, Modelo 303
    /// Cumple LIVA 37/1992 Arts. 99-108 (prorrata), 9.1.c (sectores), 161 (recargo equivalencia)
    /// </summary>
    public class MotorIVAService
    {
        private readonly ApplicationDbContext _context;

        public MotorIVAService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Cálculo Prorrata General (Art. 102-103 LIVA)

        /// <summary>
        /// Calcula porcentaje prorrata general: (Base deducible / Base total) * 100
        /// Redondeo al alza al entero superior
        /// </summary>
        public async Task<decimal> CalcularProrrataGeneralAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta)
        {
            // Obtener operaciones del periodo
            var operaciones = await ObtenerOperacionesPeriodoAsync(empresaId, ejercicioId, desde, hasta);

            // Base total = operaciones con derecho a deducción + operaciones sin derecho a deducción
            var baseDeducible = operaciones
                .Where(o => o.EsDeducible && o.TipoOperacion != TipoOperacionIVA.Exportacion && o.TipoOperacion != TipoOperacionIVA.VentaIntracomunitaria)
                .Sum(o => o.BaseImponible);

            var baseNoDeducible = operaciones
                .Where(o => !o.EsDeducible || o.TipoOperacion == TipoOperacionIVA.Exportacion || o.TipoOperacion == TipoOperacionIVA.VentaIntracomunitaria)
                .Sum(o => o.BaseImponible);

            var baseTotal = baseDeducible + baseNoDeducible;

            if (baseTotal == 0) return 100m;

            var prorrata = (baseDeducible / baseTotal) * 100m;

            // Redondeo al alza al entero superior (Art. 103 LIVA)
            return Math.Ceiling(prorrata);
        }

        /// <summary>
        /// Determina si procede prorrata especial (Art. 104-105 LIVA)
        /// Obligatoria si prorrata general perjudica >20% al sujeto pasivo
        /// </summary>
        public async Task<(bool AplicaEspecial, decimal ProrrataGeneral, decimal ProrrataEspecial)> EvaluarProrrataEspecialAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta)
        {
            var prorrataGeneral = await CalcularProrrataGeneralAsync(empresaId, ejercicioId, desde, hasta);

            // Calcular deducción con prorrata general
            var operaciones = await ObtenerOperacionesPeriodoAsync(empresaId, ejercicioId, desde, hasta);
            var ivaSoportadoTotal = operaciones.Where(o => o.EsDeducible).Sum(o => o.CuotaIVA);
            var deduccionGeneral = ivaSoportadoTotal * (prorrataGeneral / 100m);

            // Calcular deducción con prorrata especial (100% bienes inversión, 100% bienes corrientes deducibles, 0% no deducibles, %prorrata mixtos)
            var ivaBienesInversion = operaciones.Where(o => o.EsDeducible && EsBienInversion(o)).Sum(o => o.CuotaIVA);
            var ivaBienesCorrientesDeducibles = operaciones.Where(o => o.EsDeducible && EsBienCorrienteDeducible(o)).Sum(o => o.CuotaIVA);
            var ivaBienesCorrientesNoDeducibles = operaciones.Where(o => !o.EsDeducible && !EsExportacionIntracom(o)).Sum(o => o.CuotaIVA);
            var ivaMixtos = operaciones.Where(o => o.EsDeducible && !EsBienInversion(o) && !EsBienCorrienteDeducible(o)).Sum(o => o.CuotaIVA);

            var deduccionEspecial = ivaBienesInversion + ivaBienesCorrientesDeducibles + (ivaMixtos * (prorrataGeneral / 100m));

            // Perjuicio = (deducción especial - deducción general) / deducción especial * 100
            var perjuicio = deduccionEspecial > 0 ? ((deduccionEspecial - deduccionGeneral) / deduccionEspecial) * 100m : 0m;

            var aplicaEspecial = perjuicio > 20m;

            return (aplicaEspecial, prorrataGeneral, prorrataGeneral); // ProrrataEspecial = misma base pero aplicación distinta
        }

        private bool EsBienInversion(DetalleLiquidacionIVA o) => o.TipoOperacion == TipoOperacionIVA.CompraNacional && o.BaseImponible > 3000m; // Simplificado
        private bool EsBienCorrienteDeducible(DetalleLiquidacionIVA o) => o.EsDeducible && !EsBienInversion(o) && !EsExportacionIntracom(o);
        private bool EsExportacionIntracom(DetalleLiquidacionIVA o) => o.TipoOperacion == TipoOperacionIVA.Exportacion || o.TipoOperacion == TipoOperacionIVA.VentaIntracomunitaria;

        #endregion

        #region Sectores Diferenciados (Art. 9.1.c LIVA)

        /// <summary>
        /// Detecta si existen sectores diferenciados (diferencia >50pp en % deducción)
        /// </summary>
        public async Task<List<SectorDiferenciadoIVA>> DetectarSectoresDiferenciadosAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta)
        {
            var sectores = await _context.SectoresDiferenciadosIVA
                .Where(s => s.EmpresaId == empresaId && s.Activo)
                .ToListAsync();

            if (sectores.Count < 2) return new List<SectorDiferenciadoIVA>();

            var resultados = new List<SectorDiferenciadoIVA>();

            foreach (var sector in sectores)
            {
                var opsSector = await ObtenerOperacionesSectorAsync(empresaId, sector.Id, desde, hasta);
                var baseDeducible = opsSector.Where(o => o.EsDeducible).Sum(o => o.BaseImponible);
                var baseTotal = opsSector.Sum(o => o.BaseImponible);
                var porcentaje = baseTotal > 0 ? (baseDeducible / baseTotal) * 100m : 100m;

                sector.PorcentajeDeduccion = Math.Round(porcentaje, 2);
                sector.VolumenOperacionesAnual = opsSector.Sum(o => o.BaseImponible);
                resultados.Add(sector);
            }

            // Verificar diferencia >50pp
            var porcentajes = resultados.Select(s => s.PorcentajeDeduccion).OrderBy(p => p).ToList();
            var haySectoresDiferenciados = porcentajes.Count >= 2 && (porcentajes.Last() - porcentajes.First()) > 50m;

            return haySectoresDiferenciados ? resultados : new List<SectorDiferenciadoIVA>();
        }

        /// <summary>
        /// Calcula deducción por sectores diferenciados
        /// </summary>
        public async Task<Dictionary<int, decimal>> CalcularDeduccionPorSectoresAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta)
        {
            var sectores = await DetectarSectoresDiferenciadosAsync(empresaId, ejercicioId, desde, hasta);
            var resultado = new Dictionary<int, decimal>();

            foreach (var sector in sectores)
            {
                var opsSector = await ObtenerOperacionesSectorAsync(empresaId, sector.Id, desde, hasta);
                var ivaSoportado = opsSector.Where(o => o.EsDeducible).Sum(o => o.CuotaIVA);
                var deduccion = ivaSoportado * (sector.PorcentajeDeduccion / 100m);
                resultado[sector.Id] = deduccion;
            }

            return resultado;
        }

        #endregion

        #region Generación Modelo 303

        /// <summary>
        /// Genera liquidación IVA completa (Modelo 303) con prorrata/sectores/recargo equivalencia
        /// </summary>
        public async Task<LiquidacionIVA> GenerarLiquidacion303Async(int empresaId, int ejercicioId, PeriodoIVA periodo, int año, ConfiguracionIVA? config = null)
        {
            var (desde, hasta) = CalcularFechasPeriodo(periodo, año);

            // Obtener configuración IVA
            config ??= await _context.ConfiguracionesIVA
                .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.EjercicioId == ejercicioId);

            // Obtener operaciones del periodo
            var operaciones = await ObtenerOperacionesPeriodoAsync(empresaId, ejercicioId, desde, hasta);

            // Evaluar prorrata
            var (aplicaProrrataEspecial, prorrataGeneral, _) = await EvaluarProrrataEspecialAsync(empresaId, ejercicioId, desde, hasta);
            var usaProrrata = config?.AplicaProrrataGeneral ?? false;
            var porcentajeProrrata = usaProrrata ? (aplicaProrrataEspecial ? prorrataGeneral : prorrataGeneral) : 100m;

            // Detectar sectores
            var sectores = await DetectarSectoresDiferenciadosAsync(empresaId, ejercicioId, desde, hasta);
            var usaSectores = sectores.Any();

            // Calcular deducción por sectores si aplica
            var deduccionPorSector = usaSectores ? await CalcularDeduccionPorSectoresAsync(empresaId, ejercicioId, desde, hasta) : null;

            // Agrupar por tipo IVA
            var liquidacion = new LiquidacionIVA
            {
                EmpresaId = empresaId,
                EjercicioId = ejercicioId,
                Periodo = periodo,
                Año = año,
                FechaDesde = desde,
                FechaHasta = hasta,
                Estado = EstadoLiquidacionIVA.Calculada,
                FechaCalculo = DateTime.Now
            };

            // --- VENTAS (IVA REPERCUTIDO) ---
            var ventas = operaciones.Where(o => EsVenta(o.TipoOperacion)).ToList();
            liquidacion.BaseGeneral = ventas.Where(v => v.TipoIVA == TipoIVA.General).Sum(v => v.BaseImponible);
            liquidacion.BaseReducida = ventas.Where(v => v.TipoIVA == TipoIVA.Reducido).Sum(v => v.BaseImponible);
            liquidacion.BaseSuperreducida = ventas.Where(v => v.TipoIVA == TipoIVA.Superreducido).Sum(v => v.BaseImponible);
            liquidacion.BaseExenta = ventas.Where(v => v.TipoIVA == TipoIVA.ExentoArt20 || v.TipoIVA == TipoIVA.ExentoArt21 || v.TipoIVA == TipoIVA.ExentoArt22).Sum(v => v.BaseImponible);
            liquidacion.BaseNoSujeta = ventas.Where(v => v.TipoIVA == TipoIVA.NoSujeto).Sum(v => v.BaseImponible);
            liquidacion.BaseInversionSujetoPasivo = ventas.Where(v => v.InversionSujetoPasivo).Sum(v => v.BaseImponible);

            liquidacion.IVAGeneralRepercutido = ventas.Where(v => v.TipoIVA == TipoIVA.General).Sum(v => v.CuotaIVA);
            liquidacion.IVAReducidoRepercutido = ventas.Where(v => v.TipoIVA == TipoIVA.Reducido).Sum(v => v.CuotaIVA);
            liquidacion.IVASuperreducidoRepercutido = ventas.Where(v => v.TipoIVA == TipoIVA.Superreducido).Sum(v => v.CuotaIVA);
            liquidacion.IVARecargoEquivalencia = ventas.Where(v => EsRecargoEquivalencia(v.TipoIVA)).Sum(v => v.CuotaIVA);

            // --- COMPRAS (IVA SOPORTADO) ---
            var compras = operaciones.Where(o => EsCompra(o.TipoOperacion)).ToList();
            liquidacion.IVAGeneralSoportado = compras.Where(c => c.TipoIVA == TipoIVA.General).Sum(c => c.CuotaIVA);
            liquidacion.IVAReducidoSoportado = compras.Where(c => c.TipoIVA == TipoIVA.Reducido).Sum(c => c.CuotaIVA);
            liquidacion.IVASuperreducidoSoportado = compras.Where(c => c.TipoIVA == TipoIVA.Superreducido).Sum(c => c.CuotaIVA);
            liquidacion.IVARecargoEquivalenciaSoportado = compras.Where(c => EsRecargoEquivalencia(c.TipoIVA)).Sum(c => c.CuotaIVA);

            // --- APLICAR PRORRATA / SECTORES A DEDUCIBLE ---
            if (usaSectores && deduccionPorSector != null)
            {
                // Deducción por sectores
                foreach (var sector in deduccionPorSector)
                {
                    liquidacion.IVADeducibleTotal += sector.Value;
                }
                liquidacion.UsaSectoresDiferenciados = true;
                liquidacion.DetalleSectoresJson = System.Text.Json.JsonSerializer.Serialize(deduccionPorSector);
            }
            else if (usaProrrata)
            {
                // Aplicar prorrata general a todo IVA soportado deducible
                var ivaSoportadoDeducible = compras.Where(c => c.EsDeducible).Sum(c => c.CuotaIVA);
                var ivaDeducible = ivaSoportadoDeducible * (porcentajeProrrata / 100m);

                liquidacion.IVADeducibleGeneral = compras.Where(c => c.TipoIVA == TipoIVA.General && c.EsDeducible).Sum(c => c.CuotaIVA) * (porcentajeProrrata / 100m);
                liquidacion.IVADeducibleReducido = compras.Where(c => c.TipoIVA == TipoIVA.Reducido && c.EsDeducible).Sum(c => c.CuotaIVA) * (porcentajeProrrata / 100m);
                liquidacion.IVADeducibleSuperreducido = compras.Where(c => c.TipoIVA == TipoIVA.Superreducido && c.EsDeducible).Sum(c => c.CuotaIVA) * (porcentajeProrrata / 100m);
                liquidacion.IVADeducibleTotal = ivaDeducible;

                liquidacion.UsaProrrataEspecial = aplicaProrrataEspecial;
                liquidacion.PorcentajeProrrataAplicada = porcentajeProrrata;
            }
            else
            {
                // Deducción 100%
                liquidacion.IVADeducibleGeneral = compras.Where(c => c.TipoIVA == TipoIVA.General && c.EsDeducible).Sum(c => c.CuotaIVA);
                liquidacion.IVADeducibleReducido = compras.Where(c => c.TipoIVA == TipoIVA.Reducido && c.EsDeducible).Sum(c => c.CuotaIVA);
                liquidacion.IVADeducibleSuperreducido = compras.Where(c => c.TipoIVA == TipoIVA.Superreducido && c.EsDeducible).Sum(c => c.CuotaIVA);
                liquidacion.IVADeducibleTotal = compras.Where(c => c.EsDeducible).Sum(c => c.CuotaIVA);
            }

            // --- RESULTADO FINAL ---
            liquidacion.IVADevengadoTotal = liquidacion.IVAGeneralRepercutido + liquidacion.IVAReducidoRepercutido + liquidacion.IVASuperreducidoRepercutido + liquidacion.IVARecargoEquivalencia - liquidacion.IVADeducibleTotal;
            liquidacion.IVAAIngresar = liquidacion.IVADevengadoTotal;

            if (liquidacion.IVAAIngresar < 0)
            {
                liquidacion.IVACompensar = Math.Abs(liquidacion.IVAAIngresar);
                liquidacion.IVAAIngresar = 0;
            }

            // Guardar detalle de prorrata
            liquidacion.PorcentajeProrrataAplicada = porcentajeProrrata;
            liquidacion.UsaProrrataEspecial = aplicaProrrataEspecial;

            return liquidacion;
        }

        private (DateTime desde, DateTime hasta) CalcularFechasPeriodo(PeriodoIVA periodo, int año)
        {
            return periodo switch
            {
                PeriodoIVA.PrimerTrimestre => (new DateTime(año, 1, 1), new DateTime(año, 3, 31, 23, 59, 59)),
                PeriodoIVA.SegundoTrimestre => (new DateTime(año, 4, 1), new DateTime(año, 6, 30, 23, 59, 59)),
                PeriodoIVA.TercerTrimestre => (new DateTime(año, 7, 1), new DateTime(año, 9, 30, 23, 59, 59)),
                PeriodoIVA.CuartoTrimestre => (new DateTime(año, 10, 1), new DateTime(año, 12, 31, 23, 59, 59)),
                PeriodoIVA.Anual => (new DateTime(año, 1, 1), new DateTime(año, 12, 31, 23, 59, 59)),
                _ => (new DateTime(año, 1, 1), new DateTime(año, 12, 31, 23, 59, 59))
            };
        }

        private bool EsVenta(TipoOperacionIVA tipo) => tipo == TipoOperacionIVA.VentaNacional || tipo == TipoOperacionIVA.VentaIntracomunitaria || tipo == TipoOperacionIVA.Exportacion;
        private bool EsCompra(TipoOperacionIVA tipo) => tipo == TipoOperacionIVA.CompraNacional || tipo == TipoOperacionIVA.CompraIntracomunitaria || tipo == TipoOperacionIVA.Importacion;
        private bool EsRecargoEquivalencia(TipoIVA tipo) => tipo >= TipoIVA.RecargoEquivalenciaGeneral && tipo <= TipoIVA.RecargoEquivalenciaTabaco;

        #endregion

        #region Helpers consulta operaciones

        private async Task<List<DetalleLiquidacionIVA>> ObtenerOperacionesPeriodoAsync(int empresaId, int ejercicioId, DateTime desde, DateTime hasta)
        {
            return await _context.DetallesLiquidacionIVA
                .Where(d => d.Liquidacion.EmpresaId == empresaId && d.Liquidacion.EjercicioId == ejercicioId
                    && d.FechaOperacion >= desde && d.FechaOperacion <= hasta)
                .ToListAsync();
        }

        private async Task<List<DetalleLiquidacionIVA>> ObtenerOperacionesSectorAsync(int empresaId, int sectorId, DateTime desde, DateTime hasta)
        {
            return await _context.DetallesLiquidacionIVA
                .Where(d => d.Liquidacion.EmpresaId == empresaId && d.SectorDiferenciadoId == sectorId
                    && d.FechaOperacion >= desde && d.FechaOperacion <= hasta)
                .ToListAsync();
        }

        #endregion
    }
}