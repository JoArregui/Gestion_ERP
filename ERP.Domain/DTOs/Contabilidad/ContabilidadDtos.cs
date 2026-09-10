using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.DTOs.Contabilidad
{
    /// <summary>
    /// DTO para crear/modificar cuenta contable
    /// </summary>
    public class CuentaContableDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public int Grupo { get; set; }
        public int Nivel { get; set; }
        public string? CodigoPadre { get; set; }
        public bool EsDetalle { get; set; } = true;
        public string Naturaleza { get; set; } = "Deudora"; // "Deudora" | "Acreedora"
        public string? Descripcion { get; set; }
        public bool Activa { get; set; } = true;
    }

    /// <summary>
    /// DTO para importar plan contable masivamente (Excel/CSV)
    /// </summary>
    public class ImportarPlanContableDto
    {
        public int EmpresaId { get; set; }
        public List<CuentaContableDto> Cuentas { get; set; } = new();
        public bool SobrescribirExistentes { get; set; } = false;
    }

    /// <summary>
    /// DTO para crear asiento contable
    /// </summary>
    public class CrearAsientoDto
    {
        public int EmpresaId { get; set; }
        public string Serie { get; set; } = "D";
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Concepto { get; set; } = string.Empty;
        public string TipoAsiento { get; set; } = "Normal";
        public string? OrigenTipo { get; set; }
        public int? OrigenId { get; set; }
        public List<CrearApunteDto> Apuntes { get; set; } = new();
    }

    /// <summary>
    /// DTO para línea de apunte
    /// </summary>
    public class CrearApunteDto
    {
        public int Orden { get; set; }
        public string CuentaContableCodigo { get; set; } = string.Empty;
        public string Tipo { get; set; } = "Debe"; // "Debe" | "Haber"
        public decimal Importe { get; set; }
        public string? Concepto { get; set; }
        public int? CentroCosteId { get; set; }
        public int? ProyectoId { get; set; }
        public string? DocumentoReferencia { get; set; }
    }

    /// <summary>
    /// DTO para generar Libro Diario
    /// </summary>
    public class GenerarLibroDiarioDto
    {
        public int EmpresaId { get; set; }
        public int EjercicioId { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public bool SoloContabilizados { get; set; } = true;
    }

    /// <summary>
    /// DTO para generar Libro Mayor
    /// </summary>
    public class GenerarLibroMayorDto
    {
        public int EmpresaId { get; set; }
        public int EjercicioId { get; set; }
        public string? CuentaDesde { get; set; }
        public string? CuentaHasta { get; set; }
        public bool IncluirSaldosCero { get; set; } = false;
    }

    /// <summary>
    /// DTO para generar Libro Inventarios y Cuentas Anuales
    /// </summary>
    public class GenerarLibroInventariosDto
    {
        public int EmpresaId { get; set; }
        public int EjercicioId { get; set; }
        public bool EsAbreviado { get; set; }
        public bool IncluirEFE { get; set; } = true; // Estado Flujos Efectivo
    }

    /// <summary>
    /// DTO respuesta: Libro generado
    /// </summary>
    public class LibroGeneradoDto
    {
        public int LibroId { get; set; }
        public string TipoLibro { get; set; } = string.Empty; // "Diario" | "Mayor" | "Inventarios"
        public int EjercicioId { get; set; }
        public string EjercicioCodigo { get; set; } = string.Empty;
        public DateTime FechaGeneracion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public long TamanoBytes { get; set; }
        public string HashSHA256 { get; set; } = string.Empty;
        public string? RutaArchivo { get; set; }
    }

    /// <summary>
    /// DTO para legalización telemática en Registro Mercantil
    /// </summary>
    public class LegalizarLibroDto
    {
        public int LibroId { get; set; }
        public string TipoLibro { get; set; } = string.Empty; // "Diario" | "Inventarios"
        public string NumeroPresentacion { get; set; } = string.Empty; // Número entrada Registro Mercantil
        public DateTime FechaPresentacion { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// DTO para cierre de ejercicio
    /// </summary>
    public class CerrarEjercicioDto
    {
        public int EjercicioId { get; set; }
        public bool GenerarAsientoCierre { get; set; } = true;
        public bool GenerarAsientoAperturaSiguiente { get; set; } = true;
        public string? UsuarioCierre { get; set; }
    }

    /// <summary>
    /// DTO para Balance de Comprobación
    /// </summary>
    public class BalanceComprobacionDto
    {
        public int EjercicioId { get; set; }
        public DateTime Fecha { get; set; }
        public List<LineaBalanceComprobacionDto> Lineas { get; set; } = new();
        public decimal TotalDebe { get; set; }
        public decimal TotalHaber { get; set; }
        public bool Cuadra => Math.Abs(TotalDebe - TotalHaber) < 0.005m;
    }

    public class LineaBalanceComprobacionDto
    {
        public string CuentaCodigo { get; set; } = string.Empty;
        public string CuentaNombre { get; set; } = string.Empty;
        public decimal SaldoInicialDebe { get; set; }
        public decimal SaldoInicialHaber { get; set; }
        public decimal MovimientosDebe { get; set; }
        public decimal MovimientosHaber { get; set; }
        public decimal SaldoFinalDebe { get; set; }
        public decimal SaldoFinalHaber { get; set; }
        public decimal SaldoFinal => SaldoFinalDebe - SaldoFinalHaber;
    }

    /// <summary>
    /// DTO para Cuentas Anuales (JSON para legalización)
    /// </summary>
    public class CuentasAnualesDto
    {
        public int EjercicioId { get; set; }
        public string EjercicioCodigo { get; set; } = string.Empty;
        public DateTime FechaCierre { get; set; }
        public bool EsAbreviado { get; set; }

        // Balance de Situación
        public BalanceSituacionDto BalanceSituacion { get; set; } = new();

        // Cuenta de Pérdidas y Ganancias
        public CuentaPerdidasGananciasDto CuentaPerdidasGanancias { get; set; } = new();

        // Estado de Cambios en el Patrimonio Neto
        public EstadoCambiosPatrimonioNetDto EstadoCambiosPatrimonioNet { get; set; } = new();

        // Estado de Flujos de Efectivo
        public EstadoFlujosEfectivoDto EstadoFlujosEfectivo { get; set; } = new();

        // Memoria
        public MemoriaAnualDto Memoria { get; set; } = new();
    }

    public class BalanceSituacionDto
    {
        public List<PartidaBalanceDto> Activo { get; set; } = new();
        public List<PartidaBalanceDto> Pasivo { get; set; } = new();
        public List<PartidaBalanceDto> PatrimonioNeto { get; set; } = new();
        public decimal TotalActivo => Activo.Sum(p => p.Importe);
        public decimal TotalPasivoPatrimonio => Pasivo.Sum(p => p.Importe) + PatrimonioNeto.Sum(p => p.Importe);
    }

    public class PartidaBalanceDto
    {
        public string CuentaCodigo { get; set; } = string.Empty;
        public string CuentaNombre { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public int Nivel { get; set; } // Para indentación en presentación
    }

    public class CuentaPerdidasGananciasDto
    {
        public List<PartidaPyGDto> Ingresos { get; set; } = new();
        public List<PartidaPyGDto> Gastos { get; set; } = new();
        public decimal TotalIngresos => Ingresos.Sum(p => p.Importe);
        public decimal TotalGastos => Gastos.Sum(p => p.Importe);
        public decimal Resultado => TotalIngresos - TotalGastos;
    }

    public class PartidaPyGDto
    {
        public string CuentaCodigo { get; set; } = string.Empty;
        public string CuentaNombre { get; set; } = string.Empty;
        public decimal Importe { get; set; }
        public int Nivel { get; set; }
    }

    public class EstadoCambiosPatrimonioNetDto
    {
        public decimal PatrimonioInicial { get; set; }
        public List<MovimientoPatrimonioDto> Movimientos { get; set; } = new();
        public decimal PatrimonioFinal => PatrimonioInicial + Movimientos.Sum(m => m.Importe);
    }

    public class MovimientoPatrimonioDto
    {
        public string Concepto { get; set; } = string.Empty; // "Resultado ejercicio", "Dividendos", "Aportaciones socios", etc.
        public decimal Importe { get; set; }
    }

    public class EstadoFlujosEfectivoDto
    {
        public List<FlujoEfectivoDto> ActividadesExplotacion { get; set; } = new();
        public List<FlujoEfectivoDto> ActividadesInversion { get; set; } = new();
        public List<FlujoEfectivoDto> ActividadesFinanciacion { get; set; } = new();
        public decimal FlujoNeto => ActividadesExplotacion.Sum(f => f.Importe) + ActividadesInversion.Sum(f => f.Importe) + ActividadesFinanciacion.Sum(f => f.Importe);
    }

    public class FlujoEfectivoDto
    {
        public string Concepto { get; set; } = string.Empty;
        public decimal Importe { get; set; }
    }

    public class MemoriaAnualDto
    {
        public string InformacionGeneral { get; set; } = string.Empty;
        public string NormasRegistroValoracion { get; set; } = string.Empty;
        public string InformacionActivos { get; set; } = string.Empty;
        public string InformacionPasivos { get; set; } = string.Empty;
        public string InformacionPatrimonioNeto { get; set; } = string.Empty;
        public string InformacionIngresosGastos { get; set; } = string.Empty;
        public string OtraInformacion { get; set; } = string.Empty;
    }
}