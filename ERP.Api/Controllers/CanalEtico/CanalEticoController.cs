using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ERP.Domain.Entities.CanalEtico;
using ERP.Services.CanalEtico;

namespace ERP.Api.Controllers.CanalEtico
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CanalEticoController : ControllerBase
    {
        private readonly CanalEticoService _service;

        public CanalEticoController(CanalEticoService service) => _service = service;

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // Configuración
        [HttpGet("config")]
        public async Task<IActionResult> GetConfig()
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _service.GetConfigAsync(GetEmpresaId()));
        }

        [HttpPost("config")]
        public async Task<IActionResult> PostConfig([FromBody] ConfiguracionCanalEtico dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _service.UpsertConfigAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // Denuncias
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] EstadoDenuncia? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int take = 100)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            return Ok(await _service.ListarAsync(GetEmpresaId(), estado, desde, hasta, take));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var d = await _service.ObtenerAsync(GetEmpresaId(), id);
            return d == null ? NotFound() : Ok(d);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DenunciaCanalEtico dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try { return Ok(await _service.CrearAsync(dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromQuery] EstadoDenuncia estado, [FromQuery] string? motivo = null)
        {
            try { return Ok(await _service.CambiarEstadoAsync(GetEmpresaId(), id, estado, motivo, User.Identity?.Name)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/instructor")]
        public async Task<IActionResult> AsignarInstructor(int id, [FromQuery] string instructorId)
        {
            try { return Ok(await _service.AsignarInstructorAsync(GetEmpresaId(), id, instructorId)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/resolver")]
        public async Task<IActionResult> Resolver(int id, [FromBody] ResolverDenunciaDto dto)
        {
            try { return Ok(await _service.RegistrarResolucionAsync(GetEmpresaId(), id, dto.Resolucion, dto.TipoResolucion, dto.MedidasCorrectoras, User.Identity?.Name)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("{id}/represalia")]
        public async Task<IActionResult> RegistrarRepresalia(int id, [FromQuery] string detalle)
        {
            try { return Ok(await _service.RegistrarRepresaliaAsync(GetEmpresaId(), id, detalle, User.Identity?.Name)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // Comentarios
        [HttpPost("{id}/comentarios")]
        public async Task<IActionResult> AddComentario(int id, [FromBody] ComentarioDenuncia dto)
        {
            try { return Ok(await _service.AddComentarioAsync(GetEmpresaId(), id, dto)); }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        // Documentos
        [HttpPost("{id}/documentos")]
        public async Task<IActionResult> AddDocumento(int id, [FromQuery] string nombre, [FromQuery] string tipoMime, [FromBody] byte[] contenido)
        {
            if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = Convert.ToHexString(sha256.ComputeHash(contenido));
                var dto = new DocumentoDenuncia
                {
                    NombreOriginal = nombre,
                    TipoMime = tipoMime,
                    TamanoBytes = contenido.Length,
                    HashSha256 = hash
                };
                var doc = await _service.AddDocumentoAsync(GetEmpresaId(), id, dto, contenido, hash);
                return Ok(doc);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
}

    // Estadísticas
    [HttpGet("estadisticas")]
    public async Task<IActionResult> GetEstadisticas([FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
    {
        if (GetEmpresaId() == 0) return BadRequest(new { Message = "EmpresaId requerido" });
        return Ok(await _service.GetEstadisticasAsync(GetEmpresaId(), desde, hasta));
    }

    public sealed class ResolverDenunciaDto
    {
        public string Resolucion { get; set; } = "";
        public string TipoResolucion { get; set; } = "";
        public string? MedidasCorrectoras { get; set; }
    }
    }
}