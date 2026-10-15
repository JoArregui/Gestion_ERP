using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Produccion
{
    /// <summary>
    /// Lista de Materiales (BOM) - Fase E flujo ERP.txt §5.
    /// ArticuloPadre (terminado) → Componentes (materias primas/semielaborados) con cantidad.
    /// </summary>
    public class BOM
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int ArticuloPadreId { get; set; } // Producto terminado

        [ForeignKey(nameof(ArticuloPadreId))]
        public virtual Articulo? ArticuloPadre { get; set; }

        [Required]
        public int ComponenteId { get; set; } // Materia prima / semielaborado

        [ForeignKey(nameof(ComponenteId))]
        public virtual Articulo? Componente { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; } = 1; // Cantidad de componente por unidad de padre

        public int? SustitutoComponenteId { get; set; } // Componente alternativo

        [ForeignKey(nameof(SustitutoComponenteId))]
        public virtual Articulo? SustitutoComponente { get; set; }

        public int Version { get; set; } = 1;

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
    }

    /// <summary>
    /// Centro de trabajo / máquina / sección de producción.
    /// </summary>
    public class CentroTrabajo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty; // CT-01

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Descripcion { get; set; }

        public TipoCentroTrabajo Tipo { get; set; } = TipoCentroTrabajo.Maquina;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CosteHoraMaquina { get; set; } = 0; // Coste/hora maquinaria

        [Column(TypeName = "decimal(18,2)")]
        public decimal CosteHoraManoObra { get; set; } = 0; // Coste/hora operario

        public int CapacidadDiariaMinutos { get; set; } = 480; // 8h = 480 min

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    public enum TipoCentroTrabajo
    {
        Maquina = 0,
        Seccion = 1,
        Operario = 2,
        Subcontratado = 3
    }

    /// <summary>
    /// Hoja de ruta / proceso: secuencia de operaciones para fabricar un artículo.
    /// Cada operación tiene centro de trabajo, tiempo teórico y centro coste.
    /// </summary>
    public class HojaRuta
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int ArticuloId { get; set; } // Artículo que se fabrica

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Required]
        public int NumeroOperacion { get; set; } // 10, 20, 30...

        [Required]
        public int CentroTrabajoId { get; set; }

        [ForeignKey(nameof(CentroTrabajoId))]
        public virtual CentroTrabajo? CentroTrabajo { get; set; }

        [StringLength(200)]
        public string? DescripcionOperacion { get; set; } // "Corte", "Mecanizado", "Montaje"...

        [Column(TypeName = "decimal(18,2)")]
        public decimal TiempoPreparacionMin { get; set; } = 0; // Setup

        [Column(TypeName = "decimal(18,4)")]
        public decimal TiempoPorUnidadMin { get; set; } = 0; // Ciclo por unidad

        public int? CentroCosteId { get; set; } // Para imputación analítica

        public bool Activa { get; set; } = true;

        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Orden de Fabricación (OF) - documento de producción.
    /// Estados: Planificada → EnProceso → Terminada / Cancelada.
    /// Consume materiales (BOM) y genera terminado; imputa horas y coste.
    /// </summary>
    public class OrdenFabricacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(30)]
        public string NumeroOF { get; set; } = string.Empty; // OF-2026-0001

        [Required]
        public int ArticuloId { get; set; } // Artículo a fabricar

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadPlanificada { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadTerminada { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadRechazada { get; set; } = 0;

        public EstadoOF Estado { get; set; } = EstadoOF.Planificada;

        public DateTime FechaPlanificadaInicio { get; set; } = DateTime.Today;
        public DateTime? FechaPlanificadaFin { get; set; }
        public DateTime? FechaRealInicio { get; set; }
        public DateTime? FechaRealFin { get; set; }

        // Costes
        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMaterialTeorico { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMaterialReal { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteManoObraReal { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMaquinaReal { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteIndirectoReal { get; set; } = 0;

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        public virtual ICollection<OFMaterial> Materiales { get; set; } = new List<OFMaterial>();
        public virtual ICollection<OFOperacion> Operaciones { get; set; } = new List<OFOperacion>();
    }

    public enum EstadoOF
    {
        Planificada = 0,
        EnProceso = 1,
        Terminada = 2,
        Cancelada = 3
    }

    /// <summary>
    /// Material consumido en la OF (explosión BOM + ajustes).
    /// </summary>
    public class OFMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrdenFabricacionId { get; set; }

        [ForeignKey(nameof(OrdenFabricacionId))]
        public virtual OrdenFabricacion? OrdenFabricacion { get; set; }

        [Required]
        public int ArticuloId { get; set; }

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadTeorica { get; set; } // Según BOM × cantidad OF

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadReal { get; set; } = 0; // Lo que se consume realmente

        public int? LoteId { get; set; } // Trazabilidad lote
    }

    /// <summary>
    /// Operación realizada en la OF (imputación horas / coste).
    /// </summary>
    public class OFOperacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrdenFabricacionId { get; set; }

        [ForeignKey(nameof(OrdenFabricacionId))]
        public virtual OrdenFabricacion? OrdenFabricacion { get; set; }

        [Required]
        public int HojaRutaId { get; set; } // Vinculado a hoja ruta (operación + centro)

        [ForeignKey(nameof(HojaRutaId))]
        public virtual HojaRuta? HojaRuta { get; set; }

        [Required]
        public int CentroTrabajoId { get; set; }

        [ForeignKey(nameof(CentroTrabajoId))]
        public virtual CentroTrabajo? CentroTrabajo { get; set; }

        public int NumeroOperacion { get; set; } // Copia de HojaRuta para histórico

        [Column(TypeName = "decimal(18,2)")]
        public decimal TiempoPreparacionRealMin { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal TiempoUnidadRealMin { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal UnidadesProducidas { get; set; } = 0;

        // Costes calculados
        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteManoObra { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CosteMaquina { get; set; } = 0;

        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }

        [StringLength(100)]
        public string? OperarioId { get; set; } // Usuario que imputa
    }
}