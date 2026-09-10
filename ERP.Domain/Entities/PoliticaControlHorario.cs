using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP.Domain.Entities
{
    /// <summary>
    /// Política de control horario por empresa (RDL 8/2019 art.34.9)
    /// Se define por negociación colectiva / acuerdo empresa tras consulta.
    /// </summary>
    public class PoliticaControlHorario
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public virtual Empresa? Empresa { get; set; }

        // ¿Requiere geolocalización en fichaje?
        public bool RequiereGeolocalizacion { get; set; } = false;

        // ¿Permite correcciones por empleado o solo manager?
        public bool PermiteAutoCorreccion { get; set; } = false;

        // Margen tolerancia entrada (minutos)
        public int MargenToleranciaMinutos { get; set; } = 5;

        // Horas extra permitidas al mes antes de alerta
        public int HorasExtraMaxMes { get; set; } = 80;

        // ¿Requiere firma empleado en corrección?
        public bool RequiereFirmaCorreccion { get; set; } = true;

        // Conservación mínima 4 años (no configurable por debajo)
        public int AniosConservacion { get; set; } = 4;

        // Referencia convenio / acuerdo
        [StringLength(200)]
        public string? ConvenioReferencia { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        [StringLength(100)]
        public string? UsuarioCreacion { get; set; }
        [StringLength(100)]
        public string? UsuarioModificacion { get; set; }
    }
}
