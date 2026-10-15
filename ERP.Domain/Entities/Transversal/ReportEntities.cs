using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Transversal
{
    /// <summary>
    /// Definición de informe custom (Fase G3 §8-9).
    /// </summary>
    public class ReportDefinicion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty; // RPT-VENTAS-MENSUAL

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        // SQL o consulta parametrizada
        [StringLength(4000)]
        public string ConsultaSql { get; set; } = string.Empty;

        // Parámetros esperados (JSON: [{"nombre":"fechaDesde","tipo":"date","requerido":true}])
        [StringLength(2000)]
        public string? ParametrosJson { get; set; }

        // Columnas a mostrar (JSON: [{"campo":"fecha","etiqueta":"Fecha","formato":"date"},{"campo":"total","etiqueta":"Total","formato":"currency"}])
        [StringLength(2000)]
        public string? ColumnasJson { get; set; }

        // Agrupaciones, totales, ordenación (JSON)
        [StringLength(2000)]
        public string? OpcionesJson { get; set; }

        public bool Publico { get; set; } = false; // visible para todos los roles

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Ejecución guardada de un informe (para programación/historial).
    /// </summary>
    public class ReportEjecucion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int ReportDefinicionId { get; set; }

        [ForeignKey(nameof(ReportDefinicionId))]
        public virtual ReportDefinicion? Definicion { get; set; }

        // Parámetros usados (JSON)
        [StringLength(2000)]
        public string? ParametrosJson { get; set; }

        public DateTime FechaEjecucion { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UsuarioId { get; set; }

        // Resultado (ruta archivo, base64, o metadatos)
        [StringLength(500)]
        public string? ResultadoRuta { get; set; }

        public long TamanoBytes { get; set; } = 0;

        public EstadoEjecucionReport Estado { get; set; } = EstadoEjecucionReport.Pendiente;

        [StringLength(1000)]
        public string? Error { get; set; }
    }

    public enum EstadoEjecucionReport
    {
        Pendiente = 0,
        Procesando = 1,
        Completado = 2,
        Fallido = 3
    }
}