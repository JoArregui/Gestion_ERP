using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Contabilidad
{
    /// <summary>
    /// Plan General Contable 2007/2008 - Cuenta contable (PGC 9 grupos)
    /// Grupos: 1 Financiación básica | 2 Inmovilizado | 3 Existencias | 4 Acreedores/Deudores | 5 Financiera | 6 Compras/Gastos | 7 Ventas/Ingresos | 8 Gastos/Ingresos patrimoniales | 9 Órdenes
    /// </summary>
    public class CuentaContable
    {
        [Key]
        [StringLength(9)]
        public string Codigo { get; set; } = string.Empty; // Ej: 43000000, 430000, 4300, 43, 4

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public int Grupo { get; set; } // 1-9

        public int Nivel { get; set; } // 1 (grupo) a 9 (subcuenta máxima)

        public string? CodigoPadre { get; set; }

        [ForeignKey(nameof(CodigoPadre))]
        public virtual CuentaContable? Padre { get; set; }

        public virtual ICollection<CuentaContable> Subcuentas { get; set; } = new List<CuentaContable>();

        public bool EsDetalle { get; set; } = true; // true = admite movimientos, false = solo agrupación

        public NaturalezaCuenta Naturaleza { get; set; } = NaturalezaCuenta.Deudora; // Deudora / Acreedora

        public bool EsPGCOficial { get; set; } = true; // true = del PGC oficial, false = usuario

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaModificacion { get; set; }

        // Propiedades calculadas
        [NotMapped]
        public string CodigoConPuntos => FormatearConPuntos(Codigo);

        private static string FormatearConPuntos(string codigo)
        {
            // Formato PGC: 4.3.0.0.00.000 (según nivel)
            if (codigo.Length <= 1) return codigo;
            if (codigo.Length == 2) return $"{codigo[0]}.{codigo[1]}";
            if (codigo.Length == 4) return $"{codigo.Substring(0,2)}.{codigo.Substring(2,2)}";
            if (codigo.Length == 6) return $"{codigo.Substring(0,2)}.{codigo.Substring(2,2)}.{codigo.Substring(4,2)}";
            if (codigo.Length == 8) return $"{codigo.Substring(0,2)}.{codigo.Substring(2,2)}.{codigo.Substring(4,2)}.{codigo.Substring(6,2)}";
            return codigo;
        }
    }

    public enum NaturalezaCuenta
    {
        Deudora,
        Acreedora
    }
}