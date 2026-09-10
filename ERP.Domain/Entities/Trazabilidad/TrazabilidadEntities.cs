using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities.Trazabilidad
{
    /// <summary>
    /// Lote de trazabilidad (Reglamento CE 178/2002 Art. 18)
    /// "Un paso atrÃ¡s, un paso adelante" - IdentificaciÃ³n Ãºnica de lote
    /// </summary>
    public class LoteTrazabilidad
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        [StringLength(50)]
        public string CodigoLote { get; set; } = string.Empty; // Identificador Ãºnico trazable

        [Required]
        public int ArticuloId { get; set; } // Producto (materia prima, intermedio, final)

        [ForeignKey(nameof(ArticuloId))]
        public virtual Articulo? Articulo { get; set; }

        // --- UN PASO ATRÃS (Proveedor/Origen) ---
        public int? ProveedorId { get; set; }
        [ForeignKey(nameof(ProveedorId))]
        public virtual Proveedor? Proveedor { get; set; }

        [StringLength(50)]
        public string? LoteProveedor { get; set; } // CÃ³digo lote en origen

        [StringLength(100)]
        public string? DocumentoOrigen { get; set; } // AlbarÃ¡n recepciÃ³n, pedido, certificado anÃ¡lisis

        public DateTime? FechaRecepcion { get; set; }

        // --- DATOS PROPIOS DEL LOTE ---
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadInicial { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadActual { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? CantidadReservada { get; set; }

        public DateTime FechaProduccion { get; set; } = DateTime.Now;

        public DateTime? FechaCaducidad { get; set; }

        public DateTime? FechaConsumoPreferente { get; set; }

        // Condiciones de almacenamiento
        public string? CondicionesAlmacenamiento { get; set; } // "2-8Â°C", "Congelado -18Â°C", "Ambiente seco"

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TemperaturaMinima { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? TemperaturaMaxima { get; set; }

        // Certificaciones / AnÃ¡lisis
        public bool TieneAnalisisOficial { get; set; } = false;
        public string? NumeroCertificadoAnalisis { get; set; }
        public DateTime? FechaAnalisis { get; set; }
        public string? LaboratorioAnalisis { get; set; }

        // APPCC - Puntos de Control CrÃ­tico
        public bool EsPCC { get; set; } = false; // Punto de Control CrÃ­tico
        public int? PuntoControlCriticoId { get; set; }
        public string? ParametrosCriticos { get; set; } // JSON con lÃ­mites: pH, aw, TÂª, tiempo, etc.

        // Estado
        public EstadoLote Estado { get; set; } = EstadoLote.Activo;

        // --- UN PASO ADELANTE (Cliente/Destino) - Se rellena al expedir ---
        // Se relaciona via MovimientoLote (ver abajo)

        // AuditorÃ­a
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }

        // NavegaciÃ³n
        public virtual ICollection<MovimientoLote> MovimientosEntrada { get; set; } = new List<MovimientoLote>();
        public virtual ICollection<MovimientoLote> MovimientosSalida { get; set; } = new List<MovimientoLote>();
        public virtual ICollection<AlertaTrazabilidad> Alertas { get; set; } = new List<AlertaTrazabilidad>();
        public virtual ICollection<RetiradaLote> Retiradas { get; set; } = new List<RetiradaLote>();

        // Propiedades calculadas
        [NotMapped]
        public decimal CantidadDisponible => CantidadActual - (CantidadReservada ?? 0);

        [NotMapped]
        public bool EstaProximoCaducar => FechaCaducidad.HasValue && FechaCaducidad.Value <= DateTime.Now.AddDays(30);

        [NotMapped]
        public bool EstaCaducado => FechaCaducidad.HasValue && FechaCaducidad.Value < DateTime.Now;

        [NotMapped]
        public bool RequiereAnalisis => EsPCC && !TieneAnalisisOficial;
    }

    /// <summary>
    /// Movimiento de lote (Entrada/Salida/TransformaciÃ³n/Traslado)
    /// Registra la cadena de custodia completa para trazabilidad bidireccional
    /// </summary>
    public class MovimientoLote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int LoteId { get; set; }

        [ForeignKey(nameof(LoteId))]
        public virtual LoteTrazabilidad? Lote { get; set; }

        [Required]
        public TipoMovimientoLote Tipo { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Cantidad { get; set; }

        // Origen (UN PASO ATRÃS)
        public int? ProveedorId { get; set; }
        [ForeignKey(nameof(ProveedorId))]
        public virtual Proveedor? Proveedor { get; set; }

        public int? LoteOrigenId { get; set; } // Para transformaciones: lote padre
        [ForeignKey(nameof(LoteOrigenId))]
        public virtual LoteTrazabilidad? LoteOrigen { get; set; }

        public int? AlmacenOrigenId { get; set; }

        // Destino (UN PASO ADELANTE)
        public int? ClienteId { get; set; }
        [ForeignKey(nameof(ClienteId))]
        public virtual Cliente? Cliente { get; set; }

        public int? AlmacenDestinoId { get; set; }

        // Documento asociado
        public string? TipoDocumento { get; set; } // "Albaran", "Factura", "OrdenProduccion", "Traslado", "Devolucion", "Retirada"
        public int? DocumentoId { get; set; }
        public string? NumeroDocumento { get; set; }

        // Condiciones de transporte
        public string? Transportista { get; set; }
        public string? MatriculaVehiculo { get; set; }
        public string? TemperaturaTransporte { get; set; }
        public DateTime? FechaSalidaTransporte { get; set; }
        public DateTime? FechaEntrega { get; set; }

        // Responsable
        [StringLength(100)]
        public string? Responsable { get; set; }

        // Observaciones
        [StringLength(500)]
        public string? Observaciones { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        // Para transformaciones (producciÃ³n): lote resultado
        public int? LoteResultadoId { get; set; }
        [ForeignKey(nameof(LoteResultadoId))]
        public virtual LoteTrazabilidad? LoteResultado { get; set; }

        // ParÃ¡metros de control en el movimiento (APPCC)
        public string? ParametrosControlJson { get; set; } // TÂª, pH, tiempo, responsable control, etc.
    }

    /// <summary>
    /// Alerta de trazabilidad (caducidad, temperatura, stock mÃ­nimo, anÃ¡lisis pendiente)
    /// </summary>
    public class AlertaTrazabilidad
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int LoteId { get; set; }

        [ForeignKey(nameof(LoteId))]
        public virtual LoteTrazabilidad? Lote { get; set; }

        [Required]
        public TipoAlertaTrazabilidad Tipo { get; set; }

        [Required]
        public SeveridadAlerta Severidad { get; set; } = SeveridadAlerta.Media;

        [Required]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public DateTime FechaAlerta { get; set; } = DateTime.Now;

        public bool Leida { get; set; } = false;
        public DateTime? FechaLectura { get; set; }
        [StringLength(100)]
        public string? UsuarioLectura { get; set; }

        public bool Resuelta { get; set; } = false;
        public DateTime? FechaResolucion { get; set; }
        [StringLength(100)]
        public string? UsuarioResolucion { get; set; }
        [StringLength(500)]
        public string? AccionCorrectiva { get; set; }
    }

    /// <summary>
    /// Retirada/Alerta de producto (Reglamento 178/2002 Art. 19-20)
    /// GestiÃ³n de crisis: retirada quirÃºrgica por lote
    /// </summary>
    public class RetiradaLote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        [Required]
        public int LoteId { get; set; }

        [ForeignKey(nameof(LoteId))]
        public virtual LoteTrazabilidad? Lote { get; set; }

        [Required]
        public TipoRetirada Tipo { get; set; }

        [Required]
        [StringLength(200)]
        public string Motivo { get; set; } = string.Empty; // "AlÃ©rgeno no declarado", "Salmonela", "Cuerpo extraÃ±o", etc.

        [Required]
        public SeveridadRetirada Severidad { get; set; }

        [Required]
        public DateTime FechaDeteccion { get; set; } = DateTime.Now;

        public DateTime? FechaNotificacionAutoridades { get; set; } // AESAN, RASFF

        public string? NumeroExpedienteAESAN { get; set; }

        public string? NumeroNotificacionRASFF { get; set; } // Rapid Alert System for Food and Feed

        // Alcance
        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadAfectada { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadRetirada { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CantidadDestruida { get; set; }

        // Clientes afectados (UN PASO ADELANTE)
        public int? ClientesAfectados { get; set; }

        public bool NotificadosClientes { get; set; } = false;
        public DateTime? FechaNotificacionClientes { get; set; }

        public bool NotificadosProveedores { get; set; } = false; // UN PASO ATRÃS

        // Estado
        public EstadoRetirada Estado { get; set; } = EstadoRetirada.EnInvestigacion;

        public DateTime? FechaCierre { get; set; }

        [StringLength(100)]
        public string? UsuarioResponsable { get; set; }

        [StringLength(1000)]
        public string? AccionesCorrectivas { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }

    // Enums
    public enum EstadoLote
    {
        Activo = 0,
        EnCuarentena = 1,      // Pendiente anÃ¡lisis
        Bloqueado = 2,         // No usable
        Agotado = 3,           // Cantidad = 0
        Caducado = 4,
        Retirado = 5,          // Retirada mercado
        Destruido = 6
    }

    public enum TipoMovimientoLote
    {
        Recepcion = 0,          // Entrada proveedor
        Produccion = 1,         // TransformaciÃ³n (consume lotes origen, genera lote resultado)
        Traslado = 2,           // Entre almacenes
        Expedicion = 3,         // Salida a cliente
        Devolucion = 4,         // Entrada por devoluciÃ³n cliente
        Merma = 5,              // PÃ©rdida conocida
        AjusteInventario = 6,   // Ajuste por inventario
        MuestraAnalisis = 7,    // EnvÃ­o a laboratorio
        Destruccion = 8,        // DestrucciÃ³n controlada
        Retirada = 9            // Retirada mercado
    }

    public enum TipoAlertaTrazabilidad
    {
        ProximaCaducidad = 0,
        Caducado = 1,
        StockMinimo = 2,
        TemperaturaFueraRango = 3,
        AnalisisPendiente = 4,
        AnalisisNoConforme = 5,
        LoteEnCuarentena = 6,
        RetiradaMercado = 7,
        DocumentacionFaltante = 8
    }

    public enum SeveridadAlerta
    {
        Baja = 0,
        Media = 1,
        Alta = 2,
        Critica = 3
    }

    public enum TipoRetirada
    {
        AlergenoNoDeclarado = 0,
        ContaminacionMicrobiologica = 1, // Salmonella, E. coli, Listeria, etc.
        ContaminacionQuimica = 2,        // Residuos pesticidas, metales pesados, etc.
        CuerpoExtrano = 3,
        EtiquetadoIncorrecto = 4,
        CaducidadIncorrecta = 5,
        FraudeAlimentario = 6,
        Otro = 7
    }

    public enum SeveridadRetirada
    {
        ClaseI = 0,   // Peligro grave para salud (RASFF notificaciÃ³n inmediata)
        ClaseII = 1,  // Peligro moderado
        ClaseIII = 2  // Bajo riesgo / defecto calidad
    }

    public enum EstadoRetirada
    {
        EnInvestigacion = 0,
        Confirmada = 1,
        EnCurso = 2,
        Completada = 3,
        Cerrada = 4
    }
}