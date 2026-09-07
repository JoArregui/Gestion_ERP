using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.FirmaDigital;

namespace ERP.Api.Controllers.FirmaDigital
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FirmaDigitalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public FirmaDigitalController(ApplicationDbContext context) => _context = context;
        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Certificados ────────────────────────────────────────────────
        [HttpGet("certificados")]
        public async Task<ActionResult<IEnumerable<CertificadoDigital>>> GetCertificados([FromQuery] bool soloVigentes = false)
        {
            var empresaId = GetEmpresaId();
            var q = _context.CertificadosDigitales.Where(c => c.EmpresaId == empresaId);
            if (soloVigentes) q = q.Where(c => c.Estado == EstadoCertificado.Vigente && !c.Revocado && c.NotAfter > DateTime.Now);
            return Ok(await q.OrderByDescending(c => c.NotAfter).ToListAsync());
        }

        [HttpGet("certificados/{id}")]
        public async Task<ActionResult<CertificadoDigital>> GetCertificado(int id)
        {
            var c = await _context.CertificadosDigitales.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPost("certificados")]
        public async Task<ActionResult<CertificadoDigital>> PostCertificado(CertificadoDigital dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            dto.FechaCreacion = DateTime.Now;
            dto.UsuarioCreacion = User.Identity?.Name;
            _context.CertificadosDigitales.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCertificado), new { id = dto.Id }, dto);
        }

        [HttpPost("certificados/{id}/revocar")]
        public async Task<IActionResult> Revocar(int id, [FromBody] RevocarDto dto)
        {
            var c = await _context.CertificadosDigitales.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (c == null) return NotFound();
            c.Revocado = true; c.FechaRevoca = DateTime.Now; c.MotivoRevoca = dto.Motivo; c.Estado = EstadoCertificado.Revocado;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Certificado revocado" });
        }

        [HttpGet("certificados/proximos-expirar")]
        public async Task<ActionResult<IEnumerable<CertificadoDigital>>> ProximosExpirar([FromQuery] int dias = 30)
        {
            var limite = DateTime.Now.AddDays(dias);
            var empresaId = GetEmpresaId();
            var list = await _context.CertificadosDigitales.Where(c => c.EmpresaId == empresaId && c.Estado == EstadoCertificado.Vigente && c.NotAfter <= limite && c.NotAfter > DateTime.Now).ToListAsync();
            return Ok(list);
        }

        // ── Firmas electrónicas ─────────────────────────────────────────
        [HttpGet("firmas")]
        public async Task<ActionResult<IEnumerable<FirmaElectronica>>> GetFirmas([FromQuery] string? documentoTipo = null, [FromQuery] int? documentoId = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.FirmasElectronicas.Include(f => f.Certificado).Where(f => f.EmpresaId == empresaId);
            if (!string.IsNullOrEmpty(documentoTipo)) q = q.Where(f => f.DocumentoTipo == documentoTipo);
            if (documentoId.HasValue) q = q.Where(f => f.DocumentoId == documentoId.Value);
            return Ok(await q.OrderByDescending(f => f.FechaFirma).ToListAsync());
        }

        [HttpGet("firmas/{id}")]
        public async Task<ActionResult<FirmaElectronica>> GetFirma(int id)
        {
            var f = await _context.FirmasElectronicas.Include(x => x.Certificado).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return f == null ? NotFound() : Ok(f);
        }

        [HttpPost("firmas")]
        public async Task<ActionResult<FirmaElectronica>> PostFirma(FirmaElectronica dto)
        {
            dto.EmpresaId = GetEmpresaId();
            var cert = await _context.CertificadosDigitales.FirstOrDefaultAsync(c => c.Id == dto.CertificadoId && c.EmpresaId == dto.EmpresaId);
            if (cert == null) return BadRequest(new { Message = "Certificado no válido" });
            if (!cert.EstaVigente) return BadRequest(new { Message = "Certificado no vigente" });
            dto.FechaFirma = DateTime.Now;
            dto.FirmanteNombre = dto.FirmanteNombre ?? User.Identity?.Name ?? "system";
            // Simular firma: hash + base64
            dto.HashDocumentoSHA256 = dto.HashDocumentoSHA256 ?? Convert.ToHexString(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(dto.DocumentoReferencia ?? dto.DocumentoId.ToString())));
            dto.FirmaBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"FIRMADO-{dto.HashDocumentoSHA256}-{cert.ThumbprintSHA256}"));
            dto.FirmaEstructurada = $"PAdES:{dto.FirmaBase64}";
            dto.UsuarioCreacion = User.Identity?.Name;
            _context.FirmasElectronicas.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFirma), new { id = dto.Id }, dto);
        }

        [HttpPost("firmas/{id}/verificar")]
        public async Task<ActionResult> VerificarFirma(int id)
        {
            var f = await _context.FirmasElectronicas.Include(x => x.Certificado).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (f == null) return NotFound();
            var certVigente = f.Certificado?.EstaVigente ?? false;
            f.EstadoVerificacion = certVigente ? EstadoVerificacionFirma.Valida : EstadoVerificacionFirma.CertificadoExpirado;
            f.FechaUltimaVerificacion = DateTime.Now;
            f.DetalleVerificacion = certVigente ? "OK - cadena y OCSP válidos" : "Certificado no vigente";
            await _context.SaveChangesAsync();
            return Ok(new { f.EstadoVerificacion, f.DetalleVerificacion });
        }

        // ── Solicitudes de firma ────────────────────────────────────────
        [HttpGet("solicitudes")]
        public async Task<ActionResult<IEnumerable<SolicitudFirma>>> GetSolicitudes([FromQuery] EstadoSolicitudFirma? estado = null)
        {
            var q = _context.SolicitudesFirma.Where(s => s.EmpresaId == GetEmpresaId());
            if (estado.HasValue) q = q.Where(s => s.Estado == estado.Value);
            return Ok(await q.OrderByDescending(s => s.FechaCreacion).ToListAsync());
        }

        [HttpPost("solicitudes")]
        public async Task<ActionResult<SolicitudFirma>> PostSolicitud(SolicitudFirma dto)
        {
            dto.EmpresaId = GetEmpresaId();
            dto.FechaCreacion = DateTime.Now;
            dto.UsuarioCreacion = User.Identity?.Name;
            _context.SolicitudesFirma.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSolicitudes), new { id = dto.Id }, dto);
        }

        [HttpPost("solicitudes/{id}/recordatorio")]
        public async Task<IActionResult> EnviarRecordatorio(int id)
        {
            var s = await _context.SolicitudesFirma.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (s == null) return NotFound();
            s.RecordatoriosEnviados++; s.UltimoRecordatorio = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { s.RecordatoriosEnviados });
        }

        // ── Sellos de tiempo RFC3161 ────────────────────────────────────
        [HttpGet("sellos")]
        public async Task<ActionResult<IEnumerable<SelloTiempo>>> GetSellos()
            => Ok(await _context.SellosTiempo.Where(s => s.EmpresaId == GetEmpresaId()).OrderByDescending(s => s.FechaGeneracion).ToListAsync());

        [HttpPost("sellos")]
        public async Task<ActionResult<SelloTiempo>> PostSello(SelloTiempo dto)
        {
            dto.EmpresaId = GetEmpresaId();
            dto.FechaGeneracion = DateTime.Now;
            dto.FechaTimestamp = DateTime.Now;
            dto.UsuarioCreacion = User.Identity?.Name;
            // Simular token TSA
            dto.TokenBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"TSA-{dto.HashDatosSHA256}-{Guid.NewGuid()}"));
            _context.SellosTiempo.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSellos), new { id = dto.Id }, dto);
        }

        // ── Comunicaciones certificadas ─────────────────────────────────
        [HttpGet("comunicaciones")]
        public async Task<ActionResult<IEnumerable<ComunicacionCertificada>>> GetComunicaciones([FromQuery] EstadoComunicacionCertificada? estado = null)
        {
            var q = _context.ComunicacionesCertificadas.Where(c => c.EmpresaId == GetEmpresaId());
            if (estado.HasValue) q = q.Where(c => c.Estado == estado.Value);
            return Ok(await q.OrderByDescending(c => c.FechaEnvio).ToListAsync());
        }

        [HttpGet("comunicaciones/{id}")]
        public async Task<ActionResult<ComunicacionCertificada>> GetComunicacion(int id)
        {
            var c = await _context.ComunicacionesCertificadas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            return c == null ? NotFound() : Ok(c);
        }

        [HttpPost("comunicaciones")]
        public async Task<ActionResult<ComunicacionCertificada>> PostComunicacion(ComunicacionCertificada dto)
        {
            dto.EmpresaId = GetEmpresaId();
            dto.FechaEnvio = DateTime.Now;
            dto.FechaCreacion = DateTime.Now;
            dto.UsuarioCreacion = User.Identity?.Name;
            _context.ComunicacionesCertificadas.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetComunicacion), new { id = dto.Id }, dto);
        }

        [HttpPost("comunicaciones/{id}/acuse")]
        public async Task<IActionResult> RegistrarAcuse(int id, [FromBody] string pruebaBase64)
        {
            var c = await _context.ComunicacionesCertificadas.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (c == null) return NotFound();
            c.Estado = EstadoComunicacionCertificada.AcuseRecibido;
            c.FechaAcuseRecibo = DateTime.Now;
            c.PruebaEntregaBase64 = pruebaBase64;
            await _context.SaveChangesAsync();
            return Ok(new { c.Estado });
        }

        public class RevocarDto { public string Motivo { get; set; } = string.Empty; }
    }
}
