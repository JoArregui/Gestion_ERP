using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El código interno es vital para la organización")]
        [StringLength(20)]
        public string CodigoCliente { get; set; } = string.Empty; // Ej: CLI-0001

        [Required(ErrorMessage = "La Razón Social es obligatoria para facturar")]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NombreComercial { get; set; }

        [Required(ErrorMessage = "El CIF/NIF es obligatorio para la legalidad fiscal")]
        [StringLength(20)]
        public string CIF { get; set; } = string.Empty;

        // --- UBICACIÓN ---
        public string? Direccion { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Poblacion { get; set; }
        public string? Provincia { get; set; }

        // --- CONTACTO ---
        [EmailAddress(ErrorMessage = "El formato del email no es válido")]
        public string? Email { get; set; }
        public string? Telefono { get; set; }

        // --- CONFIGURACIÓN FINANCIERA PROFESIONAL ---
        public bool TieneRecargoEquivalencia { get; set; } = false; 
        
        [Range(1, 31)]
        public int DiaPagoHabitual { get; set; } = 1;
        
        public string? FormaPago { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal DescuentoFijo { get; set; } = 0; 

        // --- ESTADO Y CONTROL ---
        public bool IsActivo { get; set; } = true;
        public bool IsBloqueado { get; set; } = false; 
        public string? MotivoBloqueo { get; set; }

        // --- FACTURA ELECTRÓNICA / DIR3 (FACe) ---
        public bool EsAdministracionPublica { get; set; } = false;
        [StringLength(20)]
        public string? DIR3_OficinaContable { get; set; }
        [StringLength(20)]
        public string? DIR3_OrganoGestor { get; set; }
        [StringLength(20)]
        public string? DIR3_UnidadTramitadora { get; set; }
        [StringLength(20)]
        public string? NIF_UE { get; set; } // VIES
        [StringLength(2)]
        public string? PaisISO { get; set; } = "ES";

        // --- MULTI-TENANT ---
        public int EmpresaId { get; set; }
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }

        public DateTime FechaAlta { get; set; } = DateTime.Now;

        // --- LOGICA DE VALIDACION PRO ---
        public static bool ValidarCIF(string cif)
        {
            if (string.IsNullOrWhiteSpace(cif) || cif.Length != 9) return false;
            
            // Validación por Regex de formato oficial (Letra inicial + 7 dígitos + Control)
            return System.Text.RegularExpressions.Regex.IsMatch(cif.ToUpper(), @"^[ABCDEFGHJNPQRSUVW][0-9]{7}[A-Z0-9]$|^[0-9]{8}[A-Z]$");
        }
    }
}