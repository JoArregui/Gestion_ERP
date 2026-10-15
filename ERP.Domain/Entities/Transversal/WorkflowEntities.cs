using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Transversal
{
    /// <summary>
    /// Definición de workflow (flujo de aprobación/alerta).
    /// Reglas: si condición → acción (aprobar, rechazar, notificar, crear tarea).
    /// </summary>
    public class WorkflowDefinicion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Codigo { get; set; } = string.Empty; // WF-PRESUPUESTO-APROBAR

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string EntidadObjetivo { get; set; } = string.Empty; // "DocumentoComercial", "OrdenFabricacion"...

        [StringLength(20)]
        public string? AccionDisparadora { get; set; } // "INSERT", "UPDATE", "ESTADO_CAMBIO"

        // Condición en JSON (ej: {"campo":"Total","operador":">","valor":10000})
        [StringLength(2000)]
        public string? CondicionJson { get; set; }

        // Acciones a ejecutar (JSON array)
        [StringLength(2000)]
        public string? AccionesJson { get; set; } // [{"tipo":"APROBAR","aprobador":"ROL_GERENTE"},{"tipo":"NOTIFICAR","destinatario":"compras@empresa.com"}]

        public bool Activo { get; set; } = true;

        public int Prioridad { get; set; } = 10; // Menor = primero

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Instancia de ejecución de un workflow.
    /// </summary>
    public class WorkflowInstancia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int WorkflowDefinicionId { get; set; }

        [ForeignKey(nameof(WorkflowDefinicionId))]
        public virtual WorkflowDefinicion? Definicion { get; set; }

        [Required]
        [StringLength(100)]
        public string EntidadObjetivo { get; set; } = string.Empty;

        [Required]
        public int EntidadObjetivoId { get; set; }

        public EstadoWorkflowInstancia Estado { get; set; } = EstadoWorkflowInstancia.Pendiente;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        [StringLength(100)]
        public string? UsuarioAsignado { get; set; } // Para aprobaciones manuales

        public virtual ICollection<WorkflowPaso> Pasos { get; set; } = new List<WorkflowPaso>();
    }

    public enum EstadoWorkflowInstancia
    {
        Pendiente = 0,
        EnEjecucion = 1,
        Completado = 2,
        Fallido = 3,
        Cancelado = 4
    }

    /// <summary>
    /// Paso individual ejecutado dentro de una instancia.
    /// </summary>
    public class WorkflowPaso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InstanciaId { get; set; }

        [ForeignKey(nameof(InstanciaId))]
        public virtual WorkflowInstancia? Instancia { get; set; }

        [Required]
        public int Orden { get; set; } = 1;

        [Required]
        [StringLength(50)]
        public string TipoAccion { get; set; } = string.Empty; // "APROBAR", "NOTIFICAR", "CREAR_TAREA", "CAMBIAR_ESTADO"

        [StringLength(500)]
        public string? ParametrosJson { get; set; } // Parámetros de la acción

        public EstadoWorkflowPaso Estado { get; set; } = EstadoWorkflowPaso.Pendiente;

        public DateTime? FechaEjecucion { get; set; }

        [StringLength(100)]
        public string? EjecutadoPor { get; set; }

        [StringLength(2000)]
        public string? ResultadoJson { get; set; } // Resultado de la ejecución

        [StringLength(2000)]
        public string? ErrorJson { get; set; }
    }

    public enum EstadoWorkflowPaso
    {
        Pendiente = 0,
        Ejecutando = 1,
        Completado = 2,
        Fallido = 3,
        Saltado = 4
    }

    /// <summary>
    /// Plantilla de notificación/alerta reutilizable.
    /// </summary>
    public class NotificacionPlantilla
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Codigo { get; set; } = string.Empty; // NOTIF-PRESUPUESTO-APROBADO

        [Required]
        [StringLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Asunto { get; set; }

        [StringLength(2000)]
        public string CuerpoHtml { get; set; } = string.Empty; // Con placeholders {{Entidad.Codigo}}, {{Usuario.Nombre}}...

        public TipoCanalNotificacion Canal { get; set; } = TipoCanalNotificacion.Email;

        public bool Activa { get; set; } = true;
    }

    public enum TipoCanalNotificacion
    {
        Email = 0,
        Sistema = 1, // Notificación in-app
        Sms = 2,
        Webhook = 3
    }
}