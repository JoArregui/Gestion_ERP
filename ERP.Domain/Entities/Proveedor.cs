using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El CIF/NIF es obligatorio")]
        [StringLength(20, ErrorMessage = "El CIF no puede superar los 20 caracteres")]
        public string CIF { get; set; } = string.Empty;

        [Required(ErrorMessage = "La Razón Social es obligatoria")]
        [StringLength(150, ErrorMessage = "La Razón Social no puede superar los 150 caracteres")]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El nombre de contacto es demasiado largo")]
        public string? NombreContacto { get; set; }

        [EmailAddress(ErrorMessage = "El formato del Email no es válido")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "El formato del teléfono no es válido")]
        [StringLength(20)]
        public string? Telefono { get; set; }

        /// <summary>
        /// true: Acreedor (Servicios como Luz, Agua, Alquiler)
        /// false: Proveedor (Mercaderías para Stock)
        /// </summary>
        public bool EsAcreedor { get; set; } 

        public bool IsActivo { get; set; } = true;

        // --- HOMOLOGACIÓN (Fase C flujo ERP.txt §3) ---
        /// <summary>Solo homologados pueden recibir pedidos (C2). Los preexistentes se homologan en migración.</summary>
        public bool Homologado { get; set; } = false;
        [StringLength(50)]
        public string? CertificadoCalidad { get; set; } // ISO9001, ISO22000, BRC, IFS...
        public DateTime? FechaHomologacion { get; set; }
        [Range(0, 5)]
        public int? EvaluacionCalidad { get; set; } // 0-5
        [Range(0, 5)]
        public int? EvaluacionPlazo { get; set; } // 0-5 puntualidad
        /// <summary>Bloqueo cautelar de compras (impagos, litigio, calidad).</summary>
        public bool BloqueadoCompras { get; set; } = false;
        [StringLength(500)]
        public string? MotivoBloqueo { get; set; }

        // Multi-tenant: aislamiento por empresa (RGPD) — existente sin EmpresaId era fuga entre inquilinos
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        // --- FACTURA ELECTRÓNICA / VIES ---
        [StringLength(20)]
        public string? NIF_UE { get; set; }
        [StringLength(2)]
        public string? PaisISO { get; set; } = "ES";
        public bool EsAdministracionPublica { get; set; } = false;

        // Auditoría básica
        public DateTime FechaAlta { get; set; } = DateTime.Now;
    }
}