using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    public class ControlHorario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpleadoId { get; set; }
        
        [ForeignKey("EmpleadoId")]
        public virtual Empleado? Empleado { get; set; }

        // Compatibilidad: intervalo clásico (se mantiene)
        public DateTime Entrada { get; set; }
        public DateTime? Salida { get; set; }

        // Coordenadas o IP (opcional para teletrabajo) — compatibilidad
        public string? Ubicacion { get; set; }

        // --- Registro horario digital RDL 8/2019 + proyecto RD 2026 ---
        // Tipo de evento (para Pausas sin romper modelo Entrada/Salida)
        public TipoRegistroHorario TipoRegistro { get; set; } = TipoRegistroHorario.Entrada;

        public ModalidadTrabajo Modalidad { get; set; } = ModalidadTrabajo.Presencial;

        public OrigenFichaje Origen { get; set; } = OrigenFichaje.Web;

        [StringLength(45)]
        public string? DireccionIP { get; set; }

        // Lat,Lon o dirección estructurada
        [StringLength(100)]
        public string? Geolocalizacion { get; set; }

        [StringLength(100)]
        public string? DispositivoId { get; set; }

        // Inalterabilidad: hash encadenado SHA256 (análogo Verifactu)
        [StringLength(64)]
        public string? HashAnterior { get; set; }

        [Required, StringLength(64)]
        public string Hash { get; set; } = string.Empty;

        // Firma / PIN validado
        public string? FirmaEmpleadoBase64 { get; set; }
        public bool FirmaValidada { get; set; } = false; // Para auditoría validación firma

        // Correcciones trazables (proyecto RD 2026: autorización y trazabilidad)
        public EstadoRegistroHorario Estado { get; set; } = EstadoRegistroHorario.Valido;

        [StringLength(500)]
        public string? MotivoCorreccion { get; set; }

        [StringLength(100)]
        public string? UsuarioCorreccion { get; set; }
        public DateTime? FechaCorreccion { get; set; }

        // Jornada completa y horas totales
        public bool JornadaCompleta { get; set; } = false;
        [Column(TypeName = "decimal(18,4)")]
        public decimal HorasTotales { get; set; } = 0m;

        // Auditoría y conservación 4 años (art. 34.9 ET)
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaFinConservacion { get; set; } // 4 años art.34.9 ET

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }

        [NotMapped]
        public TimeSpan? TotalHoras => Salida.HasValue ? Salida - Entrada : null;

        [NotMapped]
        public bool EstaAbierto => Salida == null;

        [NotMapped]
        public bool EsInalterable => !string.IsNullOrEmpty(Hash);
    }

    public enum TipoRegistroHorario
    {
        Entrada = 0,
        Salida = 1,
        PausaInicio = 2,
        PausaFin = 3,
        Incidencia = 4
    }

    public enum ModalidadTrabajo
    {
        Presencial = 0,
        Teletrabajo = 1,
        Desplazamiento = 2,
        Hibrido = 3
    }

    public enum OrigenFichaje
    {
        Web = 0,
        Movil = 1,
        Terminal = 2,
        Api = 3,
        Correccion = 4,
        KioskoPin = 5
    }

    public enum EstadoRegistroHorario
    {
        Valido = 0,
        Corregido = 1,
        Anulado = 2
    }
}