using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.CanalEtico
{
    /// <summary>
    /// Denuncia recibida a través del Canal Ético (Ley 2/2023).
    /// Protección del informante, confidencialidad, prohibición de represalias.
    /// </summary>
    public class DenunciaCanalEtico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required, StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // DEN-2026-00001

        // --- Datos del informante (puede ser anónimo) ---
        public bool EsAnonima { get; set; } = false;

        [StringLength(100)]
        public string? NombreInformante { get; set; }

        [StringLength(100)]
        public string? EmailInformante { get; set; }

        [StringLength(20)]
        public string? TelefonoInformante { get; set; }

        [StringLength(50)]
        public string? RelacionEmpresa { get; set; } // Empleado, Proveedor, Cliente, Tercero

        [StringLength(100)]
        public string? DepartamentoArea { get; set; }

        // --- Hechos denunciados ---
        [Required, StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        public string DescripcionHechos { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TipoInfraccion { get; set; } // Corrupción, Fraude, Acoso, Seguridad, MedioAmbiente, Otros

        [StringLength(50)]
        public string? Gravedad { get; set; } // Leve, Media, Grave, MuyGrave

        public DateTime? FechaHechos { get; set; }

        [StringLength(200)]
        public string? LugarHechos { get; set; }

        [StringLength(500)]
        public string? PersonasImplicadas { get; set; }

        [StringLength(500)]
        public string? PruebasAportadas { get; set; }

        // --- Gestión de la denuncia ---
        public EstadoDenuncia Estado { get; set; } = EstadoDenuncia.Recibida;

        public DateTime FechaRecepcion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaAdmision { get; set; }

        public DateTime? FechaInvestigacion { get; set; }

        public DateTime? FechaResolucion { get; set; }

        [StringLength(100)]
        public string? InstructorId { get; set; } // Usuario asignado a la investigación

        [StringLength(100)]
        public string? ResponsableResolucionId { get; set; }

        [StringLength(2000)]
        public string? Resolucion { get; set; }

        [StringLength(50)]
        public string? TipoResolucion { get; set; } // Archivado, Sancion, Derivado, MedidasCorrectoras

        [StringLength(2000)]
        public string? MedidasCorrectoras { get; set; }

        // --- Protección del informante ---
        public bool SolicitaProteccion { get; set; } = false;

        [StringLength(2000)]
        public string? MedidasProteccion { get; set; }

        public bool HaSidoRepresaliado { get; set; } = false;

        [StringLength(2000)]
        public string? DetalleRepresalias { get; set; }

        // --- Comunicaciones ---
        public bool NotificarInformante { get; set; } = true;

        [StringLength(2000)]
        public string? ComunicacionesInternasJson { get; set; } // Log de comunicaciones

        // --- Trazabilidad y auditoría ---
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaUltimaActualizacion { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [StringLength(100)]
        public string? UsuarioUltimaActualizacion { get; set; }

        // Navegación
        public virtual ICollection<ComentarioDenuncia> Comentarios { get; set; } = new List<ComentarioDenuncia>();
        public virtual ICollection<DocumentoDenuncia> Documentos { get; set; } = new List<DocumentoDenuncia>();
        public virtual ICollection<HistoricoEstadoDenuncia> HistorialEstados { get; set; } = new List<HistoricoEstadoDenuncia>();
    }

    public enum EstadoDenuncia
    {
        Recibida = 0,
        EnEvaluacion = 1,      // Evaluación preliminar admisibilidad
        Admitida = 2,          // Admitida a trámite
        EnInvestigacion = 3,   // En fase de investigación
        PendienteInformacion = 4, // Requiere más información del informante
        Resuelta = 5,          // Con resolución final
        Archivado = 6,          // Archivado sin investigación
        Derivada = 7           // Derivada a otra autoridad (Fiscalía, Policía, etc.)
    }

    /// <summary>
    /// Comentarios internos/externos sobre la denuncia.
    /// </summary>
    public class ComentarioDenuncia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DenunciaId { get; set; }

        [ForeignKey(nameof(DenunciaId))]
        public virtual DenunciaCanalEtico? Denuncia { get; set; }

        [Required]
        public bool EsInterno { get; set; } // true = visible solo para instructores, false = visible para informante

        [Required, StringLength(100)]
        public string AutorId { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AutorNombre { get; set; }

        [Required]
        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Documentos adjuntos a la denuncia.
    /// </summary>
    public class DocumentoDenuncia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DenunciaId { get; set; }

        [ForeignKey(nameof(DenunciaId))]
        public virtual DenunciaCanalEtico? Denuncia { get; set; }

        [Required, StringLength(200)]
        public string NombreOriginal { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; } // Ruta o URL del archivo

        [StringLength(100)]
        public string? TipoMime { get; set; }

        public long TamanoBytes { get; set; }

        [StringLength(64)]
        public string? HashSha256 { get; set; } // Integridad

        public DateTime FechaSubida { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? UsuarioSubida { get; set; }
    }

    /// <summary>
    /// Historial de cambios de estado de la denuncia (trazabilidad).
    /// </summary>
    public class HistoricoEstadoDenuncia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DenunciaId { get; set; }

        [ForeignKey(nameof(DenunciaId))]
        public virtual DenunciaCanalEtico? Denuncia { get; set; }

        [Required]
        public EstadoDenuncia EstadoAnterior { get; set; }

        [Required]
        public EstadoDenuncia EstadoNuevo { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        [StringLength(100)]
        public string? UsuarioId { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Configuración del Canal Ético por empresa.
    /// </summary>
    public class ConfiguracionCanalEtico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        // --- Configuración de canales de recepción ---
        public bool CanalWebActivo { get; set; } = true;

        public bool CanalEmailActivo { get; set; } = true;

        [StringLength(100)]
        public string? EmailCanalEtico { get; set; } // email dedicado

        public bool CanalTelefonoActivo { get; set; } = false;

        [StringLength(20)]
        public string? TelefonoCanalEtico { get; set; }

        public bool CanalPresencialActivo { get; set; } = false;

        public bool CanalAppMovilActivo { get; set; } = false;

        // --- Configuración de gestión ---
        public int DiasMaximosEvaluacion { get; set; } = 7; // Días para evaluar admisibilidad

        public int DiasMaximosInvestigacion { get; set; } = 90; // Días máximos investigación

        public bool PermiteAnonimas { get; set; } = true;

        public bool RequiereConfirmacionRecepcion { get; set; } = true;

        public bool NotificarInformanteCambios { get; set; } = true;

        // --- Protección del informante ---
        public bool MedidasProteccionAutomaticas { get; set; } = true;

        public bool ProhibicionRepresaliasExplicita { get; set; } = true;

        // --- Responsables ---
        [StringLength(100)]
        public string? ResponsableCanalId { get; set; } // Responsable del canal

        [StringLength(100)]
        public string? ResponsableProteccionDatosId { get; set; } // DPO

        [StringLength(100)]
        public string? ComiteEticoIds { get; set; } // JSON array de IDs

        // --- Comunicaciones ---
        [StringLength(500)]
        public string? TextoAcuseRecibo { get; set; }

        [StringLength(500)]
        public string? TextoConfirmacionAnonimato { get; set; }

        // --- Auditoría ---
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaUltimaActualizacion { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [StringLength(100)]
        public string? UsuarioUltimaActualizacion { get; set; }
    }
}