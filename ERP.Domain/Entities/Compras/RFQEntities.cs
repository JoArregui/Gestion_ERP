using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Compras
{
    /// <summary>
    /// Solicitud de oferta a proveedores - RFQ (Fase C flujo ERP.txt §3).
    /// Ciclo: Borrador → Enviada → (ofertas) → Adjudicada (genera Pedido) / Cancelada.
    /// </summary>
    public class SolicitudOferta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(30)]
        public string Codigo { get; set; } = string.Empty; // RFQ-0001

        public DateTime Fecha { get; set; } = DateTime.Today;
        public DateTime? FechaLimite { get; set; }

        public EstadoRFQ Estado { get; set; } = EstadoRFQ.Borrador;

        [StringLength(1000)]
        public string? Notas { get; set; }

        // Proveedores invitados (trazabilidad de a quién se pidió oferta)
        [StringLength(500)]
        public string? ProveedoresInvitadosJson { get; set; }

        public int? PedidoAdjudicadoId { get; set; }
        [ForeignKey(nameof(PedidoAdjudicadoId))]
        public virtual DocumentoComercial? PedidoAdjudicado { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public virtual ICollection<LineaSolicitudOferta> Lineas { get; set; } = new List<LineaSolicitudOferta>();
        public virtual ICollection<OfertaProveedor> Ofertas { get; set; } = new List<OfertaProveedor>();
    }

    public class LineaSolicitudOferta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SolicitudId { get; set; }

        [ForeignKey(nameof(SolicitudId))]
        public virtual SolicitudOferta? Solicitud { get; set; }

        [Required]
        public int ArticuloId { get; set; }

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Range(0.0001, 999999999)]
        public decimal Cantidad { get; set; }

        [StringLength(200)]
        public string? Notas { get; set; }
    }

    /// <summary>
    /// Oferta recibida de un proveedor para una RFQ.
    /// </summary>
    public class OfertaProveedor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SolicitudId { get; set; }

        [ForeignKey(nameof(SolicitudId))]
        public virtual SolicitudOferta? Solicitud { get; set; }

        [Required]
        public int ProveedorId { get; set; }

        [ForeignKey(nameof(ProveedorId))]
        public virtual Proveedor? Proveedor { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Today;

        public EstadoOferta Estado { get; set; } = EstadoOferta.Recibida;

        public int? PlazoEntregaDias { get; set; }

        [StringLength(500)]
        public string? Notas { get; set; }

        public virtual ICollection<LineaOferta> Lineas { get; set; } = new List<LineaOferta>();

        [NotMapped]
        public decimal Total => Lineas?.Sum(l => l.Cantidad * l.PrecioUnitario) ?? 0;
    }

    public class LineaOferta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OfertaId { get; set; }

        [ForeignKey(nameof(OfertaId))]
        public virtual OfertaProveedor? Oferta { get; set; }

        [Required]
        public int ArticuloId { get; set; }

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Range(0.0001, 999999999)]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Range(0, 999999999)]
        public decimal PrecioUnitario { get; set; }
    }

    public enum EstadoRFQ
    {
        Borrador = 0,
        Enviada = 1,
        Adjudicada = 2,
        Cancelada = 3
    }

    public enum EstadoOferta
    {
        Recibida = 0,
        Seleccionada = 1,
        Descartada = 2
    }
}
