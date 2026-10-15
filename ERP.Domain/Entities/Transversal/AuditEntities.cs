using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Transversal
{
    /// <summary>
    /// Registro de auditoría de cambios (Fase G flujo ERP.txt §9).
    /// Captura quién, qué, cuándo, antes/después (JSON) para trazabilidad completa.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public long Id { get; set; } // BIGINT para alto volumen

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty; // "Articulo", "Cliente", "DocumentoComercial"...

        [Required]
        public int EntidadId { get; set; } // PK de la entidad afectada

        [Required]
        [StringLength(20)]
        public string Accion { get; set; } = string.Empty; // "INSERT", "UPDATE", "DELETE"

        [StringLength(100)]
        public string? UsuarioId { get; set; } // Claim sub/email

        [StringLength(100)]
        public string? UsuarioNombre { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string? Ip { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // JSON con valores ANTES (null en INSERT)
        public string? ValoresAntes { get; set; }

        // JSON con valores DESPUÉS (null en DELETE)
        public string? ValoresDespues { get; set; }

        // Diferencia calculada (opcional, solo UPDATE)
        public string? Diferencia { get; set; }
    }

    /// <summary>
    /// Configuración de qué entidades/propiedades auditar.
    /// </summary>
    public class AuditConfig
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        [StringLength(100)]
        public string Entidad { get; set; } = string.Empty; // "Articulo", "*" para todas

        // Props a ignorar (ej: "FechaCreacion", "PasswordHash")
        [StringLength(500)]
        public string? PropsIgnoradas { get; set; } // JSON array

        // Solo auditar estas props (vacío = todas salvo ignoradas)
        [StringLength(500)]
        public string? PropsIncluidas { get; set; } // JSON array

        public bool AuditarInsert { get; set; } = true;
        public bool AuditarUpdate { get; set; } = true;
        public bool AuditarDelete { get; set; } = true;

        public bool Activa { get; set; } = true;
    }
}