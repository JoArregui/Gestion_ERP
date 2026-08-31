using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Fiscal
{
    /// <summary>
    /// Territorio fiscal para IVA/IGIC/IPSI (Península/Baleares IVA, Canarias IGIC Ley 20/1991, Ceuta-Melilla IPSI Ley 8/1991)
    /// </summary>
    public enum TerritorioFiscal
    {
        PeninsulaBaleares = 0, // IVA 21/10/4
        Canarias = 1,          // IGIC 7/3/0/9.5/13.5/20
        Ceuta = 2,             // IPSI 0.5-10%
        Melilla = 3,
        ExtranjeroUE = 4,      // Intracom VIES
        ExtranjeroNoUE = 5
    }

    /// <summary>
    /// Tarifa oficial por territorio y tipo (validación MotorIVA)
    /// </summary>
    public class TarifaImpuesto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public TerritorioFiscal Territorio { get; set; }

        [Required]
        public TipoIVA TipoIVA { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal Porcentaje { get; set; }

        // Recargo asociado (si aplica)
        [Column(TypeName = "decimal(5,2)")]
        public decimal RecargoEquivalencia { get; set; } = 0m;

        public bool Vigente { get; set; } = true;
        public DateTime FechaDesde { get; set; } = new DateTime(2026, 1, 1);
        public DateTime? FechaHasta { get; set; }
    }

    /// <summary>
    /// Libro registro IVA (soportado/repercutido) para modelos 303/390/347/349
    /// </summary>
    public class LibroRegistroIVA
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }
        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int EjercicioId { get; set; }
        [ForeignKey(nameof(EjercicioId))]
        public virtual Contabilidad.EjercicioContable? Ejercicio { get; set; }

        public TipoLibroIVA TipoLibro { get; set; } // Emitidas/Recibidas/BienesInversion/Intracom
        public DateTime FechaOperacion { get; set; }
        public string? NumeroFactura { get; set; }
        public string? NIFContraparte { get; set; }
        public string? NombreContraparte { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseImponible { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal TipoImpositivo { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaIVA { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaRecargo { get; set; }
        public bool EsDeducible { get; set; } = true;
        public string? ClaveOperacion { get; set; } // SII 01,02 etc.
    }

    public enum TipoLibroIVA
    {
        Emitidas = 0,
        Recibidas = 1,
        BienesInversion = 2,
        Intracomunitarias = 3,
        Caja = 4
    }
}
