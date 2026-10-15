using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Proyectos
{
    /// <summary>
    /// Proyecto / Obra / Contrato (Fase F flujo ERP.txt §7).
    /// WBS/Gantt simplificado: hitos → tareas con dependencias.
    /// Rentabilidad: costes previstos vs reales vs facturación.
    /// </summary>
    public class Proyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // PRY-2026-0001

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        public DateTime FechaInicioPlanificada { get; set; } = DateTime.Today;
        public DateTime FechaFinPlanificada { get; set; } = DateTime.Today.AddMonths(3);
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        public EstadoProyecto Estado { get; set; } = EstadoProyecto.Borrador;

        // Presupuestos / Rentabilidad
        [Column(TypeName = "decimal(18,4)")]
        public decimal PresupuestoCosteTotal { get; set; } = 0; // Sumatorio coste previsto tareas

        [Column(TypeName = "decimal(18,4)")]
        public decimal PresupuestoVentaTotal { get; set; } = 0; // Precio venta al cliente

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteRealAcumulado { get; set; } = 0; // Horas imputadas × coste + materiales + subcontratados

        [Column(TypeName = "decimal(18,4)")]
        public decimal FacturadoAcumulado { get; set; } = 0; // Facturas emitidas vinculadas

        [StringLength(100)]
        public string? ResponsableId { get; set; } // Usuario jefe de proyecto

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<HitoProyecto> Hitos { get; set; } = new List<HitoProyecto>();
        public virtual ICollection<TareaProyecto> Tareas { get; set; } = new List<TareaProyecto>();
        public virtual ICollection<TimesheetProyecto> Timesheets { get; set; } = new List<TimesheetProyecto>();
        public virtual ICollection<DocumentoProyecto> Documentos { get; set; } = new List<DocumentoProyecto>();
    }

    public enum EstadoProyecto
    {
        Borrador = 0,
        Planificado = 1,
        EnEjecucion = 2,
        Parado = 3,
        Terminado = 4,
        Cancelado = 5
    }

    /// <summary>
    /// Hito / fase principal del proyecto (WBS nivel 1).
    /// </summary>
    public class HitoProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto? Proyecto { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public int Orden { get; set; } = 1;

        public DateTime FechaInicioPlanificada { get; set; }
        public DateTime FechaFinPlanificada { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        public EstadoHito Estado { get; set; } = EstadoHito.Pendiente;
        public decimal PorcentajeAvance { get; set; } = 0; // 0-100
    }

    public enum EstadoHito
    {
        Pendiente = 0,
        EnCurso = 1,
        Completado = 2,
        Retrasado = 3
    }

    /// <summary>
    /// Tarea / actividad del proyecto (WBS nivel 2+).
    /// Dependencias: Fin-Inicio (FS) por defecto.
    /// </summary>
    public class TareaProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto? Proyecto { get; set; }

        public int? HitoId { get; set; }
        [ForeignKey(nameof(HitoId))]
        public virtual HitoProyecto? Hito { get; set; }

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Descripcion { get; set; }

        public int Orden { get; set; } = 1;

        public DateTime FechaInicioPlanificada { get; set; }
        public DateTime FechaFinPlanificada { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;
        public decimal PorcentajeAvance { get; set; } = 0;

        // Planificación
        [Column(TypeName = "decimal(18,4)")]
        public decimal HorasEstimadas { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal HorasReales { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteHoraEstimado { get; set; } = 0; // Coste/hora estimado (media equipo)

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMaterialEstimado { get; set; } = 0;

        // Costes reales (se alimentan de timesheets + materiales)
        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteRealHoras { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteRealMaterial { get; set; } = 0;

        // Dependencias (simplificado: Fin-Inicio)
        public int? TareaPredecesoraId { get; set; }
        [ForeignKey(nameof(TareaPredecesoraId))]
        public virtual TareaProyecto? TareaPredecesora { get; set; }

        // Asignación
        [StringLength(100)]
        public string? ResponsableId { get; set; } // Usuario asignado
    }

    public enum EstadoTarea
    {
        Pendiente = 0,
        EnCurso = 1,
        Completada = 2,
        Bloqueada = 3,
        Cancelada = 4
    }

    /// <summary>
    /// Imputación de horas (timesheet) a proyecto/tarea por empleado.
    /// </summary>
    public class TimesheetProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto? Proyecto { get; set; }

        public int? TareaId { get; set; }
        [ForeignKey(nameof(TareaId))]
        public virtual TareaProyecto? Tarea { get; set; }

        [Required]
        public int EmpleadoId { get; set; }

        [ForeignKey(nameof(EmpleadoId))]
        public virtual Empleado? Empleado { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Horas { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteHora { get; set; } = 0; // Coste hora empleado en ese momento

        [StringLength(500)]
        public string? Descripcion { get; set; } // Qué se hizo

        public bool Facturable { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Documento vinculado al proyecto (facturas, contratos, planos, partes).
    /// </summary>
    public class DocumentoProyecto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProyectoId { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto? Proyecto { get; set; }

        public int? TareaId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; }

        [StringLength(100)]
        public string? Tipo { get; set; } // "Factura", "Contrato", "Plano", "Parte"

        public DateTime FechaSubida { get; set; } = DateTime.Now;
    }

    // ── SAT / Ticketing ──────────────────────────────────────────────

    /// <summary>
    /// Contrato de mantenimiento / SLA (Fase F §7).
    /// </summary>
    public class ContratoMantenimiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty; // MAN-001

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaInicio { get; set; } = DateTime.Today;
        public DateTime FechaFin { get; set; } = DateTime.Today.AddYears(1);

        public TipoSLA SLA { get; set; } = TipoSLA.Estandar;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CuotaAnual { get; set; } = 0;

        public bool Activo { get; set; } = true;

        [StringLength(1000)]
        public string? DescripcionCobertura { get; set; } // Qué incluye

        public virtual ICollection<TicketSAT> Tickets { get; set; } = new List<TicketSAT>();
    }

    public enum TipoSLA
    {
        Basico = 0,       // 8h laborables
        Estandar = 1,     // 4h laborables
        Prioritario = 2,  // 2h laborables
        Critico = 3       // 1h 24/7
    }

    /// <summary>
    /// Ticket / Parte de trabajo SAT (Fase F §7).
    /// </summary>
    public class TicketSAT
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        public int? ProyectoId { get; set; }
        [ForeignKey(nameof(ProyectoId))]
        public virtual Proyecto? Proyecto { get; set; }

        public int? ContratoMantenimientoId { get; set; }
        [ForeignKey(nameof(ContratoMantenimientoId))]
        public virtual ContratoMantenimiento? Contrato { get; set; }

        [Required]
        public int ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        [Required]
        [StringLength(30)]
        public string NumeroTicket { get; set; } = string.Empty; // SAT-2026-0001

        [Required]
        [StringLength(200)]
        public string Asunto { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Descripcion { get; set; }

        public PrioridadTicket Prioridad { get; set; } = PrioridadTicket.Normal;

        public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;

        [StringLength(100)]
        public string? TecnicoAsignadoId { get; set; }

        public DateTime FechaApertura { get; set; } = DateTime.Now;
        public DateTime? FechaAsignacion { get; set; }
        public DateTime? FechaInicioTrabajo { get; set; }
        public DateTime? FechaResolucion { get; set; }
        public DateTime? FechaCierre { get; set; }

        // SLA
        public DateTime? SLAFechaLimiteRespuesta { get; set; }
        public DateTime? SLAFechaLimiteResolucion { get; set; }
        public bool SLAIncumplido { get; set; } = false;

        // Resolución
        [StringLength(2000)]
        public string? Solucion { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal HorasTrabajadas { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMateriales { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteDesplazamiento { get; set; } = 0;

        public virtual ICollection<TicketComentario> Comentarios { get; set; } = new List<TicketComentario>();
    }

    public enum PrioridadTicket
    {
        Baja = 0,
        Normal = 1,
        Alta = 2,
        Critica = 3
    }

    public enum EstadoTicket
    {
        Abierto = 0,
        Asignado = 1,
        EnProceso = 2,
        PendienteCliente = 3,
        PendienteMaterial = 4,
        Resuelto = 5,
        Cerrado = 6,
        Cancelado = 7
    }

    public class TicketComentario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        [ForeignKey(nameof(TicketId))]
        public virtual TicketSAT? Ticket { get; set; }

        [Required]
        public int EmpleadoId { get; set; }
        [ForeignKey(nameof(EmpleadoId))]
        public virtual Empleado? Empleado { get; set; }

        [Required]
        [StringLength(2000)]
        public string Comentario { get; set; } = string.Empty;

        public bool EsInterno { get; set; } = true; // false = visible para cliente

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}