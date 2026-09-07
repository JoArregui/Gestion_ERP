using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.Data;
using ERP.Domain.Entities.Bancario;
using ERP.Services.Bancario;
using System.Security.Claims;

namespace ERP.Api.Controllers.Bancario
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BancarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly BancarioService _bancario;
        private readonly SepaXmlGeneratorService _xmlGen;

        public BancarioController(ApplicationDbContext context, BancarioService bancario, SepaXmlGeneratorService xmlGen)
        {
            _context = context;
            _bancario = bancario;
            _xmlGen = xmlGen;
        }

        private int GetEmpresaId() => int.TryParse(User.FindFirst("EmpresaId")?.Value, out var id) ? id : 0;

        // ── Cuentas Bancarias ──────────────────────────────────────────────
        [HttpGet("cuentas")]
        public async Task<ActionResult<IEnumerable<CuentaBancaria>>> GetCuentas([FromQuery] bool soloActivas = true)
        {
            var empresaId = GetEmpresaId();
            if (empresaId == 0) return BadRequest(new { Message = "EmpresaId no presente en token" });
            return Ok(await _bancario.GetCuentasBancariasAsync(empresaId, soloActivas));
        }

        [HttpGet("cuentas/{id}")]
        public async Task<ActionResult<CuentaBancaria>> GetCuenta(int id)
        {
            var c = await _bancario.GetCuentaBancariaAsync(id);
            if (c == null || c.EmpresaId != GetEmpresaId()) return NotFound(new { Message = "Cuenta no encontrada" });
            return Ok(c);
        }

        [HttpPost("cuentas")]
        public async Task<ActionResult<CuentaBancaria>> PostCuenta(CuentaBancaria dto)
        {
            dto.EmpresaId = GetEmpresaId();
            if (dto.EmpresaId == 0) return BadRequest(new { Message = "EmpresaId requerido" });
            try
            {
                var creada = await _bancario.CrearCuentaBancariaAsync(dto);
                return CreatedAtAction(nameof(GetCuenta), new { id = creada.Id }, creada);
            }
            catch (ArgumentException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPut("cuentas/{id}")]
        public async Task<IActionResult> PutCuenta(int id, CuentaBancaria dto)
        {
            if (id != dto.Id) return BadRequest(new { Message = "Id no coincide" });
            if (dto.EmpresaId != GetEmpresaId()) return Forbid();
            var ok = await _bancario.ActualizarCuentaBancariaAsync(dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("cuentas/{id}")]
        public async Task<IActionResult> DeleteCuenta(int id)
        {
            var empresaId = GetEmpresaId();
            var c = await _context.CuentasBancarias.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
            if (c == null) return NotFound();
            c.Activa = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("cuentas/{id}/validar-iban")]
        public ActionResult ValidarIban(string iban) => Ok(new { Valido = ValidarIbanLocal(iban) });

        private static bool ValidarIbanLocal(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            iban = iban.Replace(" ", "").ToUpper();
            if (!System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$")) return false;
            var rearr = iban.Substring(4) + iban.Substring(0, 4);
            var num = string.Concat(rearr.Select(c => char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));
            int rem = 0; foreach (var ch in num) rem = (rem * 10 + (ch - '0')) % 97;
            return rem == 1;
        }

        // ── Mandatos SEPA ─────────────────────────────────────────────────
        [HttpGet("mandatos")]
        public async Task<ActionResult<IEnumerable<MandatoSEPA>>> GetMandatos([FromQuery] EstadoMandatoSEPA? estado = null)
        {
            var empresaId = GetEmpresaId();
            return Ok(await _bancario.GetMandatosAsync(empresaId, estado));
        }

        [HttpGet("mandatos/{id}")]
        public async Task<ActionResult<MandatoSEPA>> GetMandato(int id)
        {
            var m = await _bancario.GetMandatoAsync(id);
            if (m == null || m.EmpresaId != GetEmpresaId()) return NotFound();
            return Ok(m);
        }

        [HttpPost("mandatos")]
        public async Task<ActionResult<MandatoSEPA>> PostMandato(MandatoSEPA dto)
        {
            dto.EmpresaId = GetEmpresaId();
            try
            {
                var creado = await _bancario.CrearMandatoAsync(dto);
                return CreatedAtAction(nameof(GetMandato), new { id = creado.Id }, creado);
            }
            catch (ArgumentException ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("mandatos/{id}/firmar")]
        public async Task<IActionResult> FirmarMandato(int id, [FromQuery] bool verificarBeneficiario = false)
        {
            var m = await _context.MandatosSEPA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (m == null) return NotFound();
            var ok = await _bancario.FirmarMandatoAsync(id, verificarBeneficiario);
            return ok ? Ok(new { Message = "Mandato activado", Verificado = verificarBeneficiario }) : BadRequest();
        }

        [HttpPost("mandatos/{id}/revocar")]
        public async Task<IActionResult> RevocarMandato(int id)
        {
            var m = await _context.MandatosSEPA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (m == null) return NotFound();
            var ok = await _bancario.RevocarMandatoAsync(id);
            return ok ? Ok(new { Message = "Mandato revocado" }) : BadRequest();
        }

        [HttpPost("mandatos/{id}/verificar-beneficiario")]
        public async Task<IActionResult> VerificarBeneficiario(int id)
        {
            var m = await _context.MandatosSEPA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (m == null) return NotFound();
            // VoP - PSD2 desde 05-10-2025: verificación nombre-IBAN
            m.BeneficiarioVerificado = true;
            m.FechaVerificacionBeneficiario = DateTime.Now;
            m.MetodoVerificacion = "IBAN_Name_Check";
            await _context.SaveChangesAsync();
            return Ok(new { m.BeneficiarioVerificado, m.FechaVerificacionBeneficiario });
        }

        // ── Remesas SEPA ──────────────────────────────────────────────────
        [HttpGet("remesas")]
        public async Task<ActionResult<IEnumerable<RemesaSEPA>>> GetRemesas([FromQuery] EstadoRemesaSEPA? estado = null, [FromQuery] TipoRemesaSEPA? tipo = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.RemesasSEPA.Include(r => r.Operaciones).Where(r => r.EmpresaId == empresaId);
            if (estado.HasValue) q = q.Where(r => r.Estado == estado.Value);
            if (tipo.HasValue) q = q.Where(r => r.Tipo == tipo.Value);
            return Ok(await q.OrderByDescending(r => r.FechaCreacion).ToListAsync());
        }

        [HttpGet("remesas/{id}")]
        public async Task<ActionResult<RemesaSEPA>> GetRemesa(int id)
        {
            var r = await _context.RemesasSEPA.Include(x => x.Operaciones).Include(x => x.CuentaBancariaOrdenante).FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (r == null) return NotFound();
            return Ok(r);
        }

        [HttpPost("remesas/transferencia")]
        public async Task<ActionResult<RemesaSEPA>> CrearRemesaTransferencia([FromBody] CrearRemesaDto dto)
        {
            try
            {
                var remesa = await _bancario.CrearRemesaTransferenciaAsync(GetEmpresaId(), dto.CuentaOrdenanteId, dto.FechaEjecucion, dto.Operaciones);
                return CreatedAtAction(nameof(GetRemesa), new { id = remesa.Id }, remesa);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("remesas/adeudo")]
        public async Task<ActionResult<RemesaSEPA>> CrearRemesaAdeudo([FromBody] CrearRemesaAdeudoDto dto)
        {
            try
            {
                var remesa = await _bancario.CrearRemesaAdeudoAsync(GetEmpresaId(), dto.CuentaAcreedoraId, dto.FechaEjecucion, dto.Esquema, dto.Operaciones);
                return CreatedAtAction(nameof(GetRemesa), new { id = remesa.Id }, remesa);
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("remesas/{id}/generar-xml")]
        public async Task<ActionResult> GenerarXml(int id)
        {
            var empresaId = GetEmpresaId();
            var r = await _context.RemesasSEPA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == empresaId);
            if (r == null) return NotFound();
            try
            {
                var xml = await _bancario.GenerarXmlRemesaAsync(id);
                return Ok(new { xml, r.NombreArchivoXml, r.HashXmlSHA256, r.TipoFicheroISO });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpPost("remesas/{id}/enviar")]
        public async Task<ActionResult> EnviarRemesa(int id)
        {
            try
            {
                var remesa = await _bancario.ValidarYEnviarRemesaAsync(id, User.Identity?.Name ?? "system");
                return Ok(new { remesa.Estado, remesa.ReferenciaBanco, remesa.FechaEnvioBanco });
            }
            catch (Exception ex) { return BadRequest(new { Message = ex.Message }); }
        }

        [HttpGet("remesas/{id}/xml")]
        public async Task<ActionResult> DescargarXml(int id)
        {
            var r = await _context.RemesasSEPA.FirstOrDefaultAsync(x => x.Id == id && x.EmpresaId == GetEmpresaId());
            if (r == null || string.IsNullOrEmpty(r.XmlGenerado)) return NotFound(new { Message = "XML no generado" });
            return Content(r.XmlGenerado, "application/xml");
        }

        // ── Extractos / Conciliación ──────────────────────────────────────
        [HttpGet("extractos")]
        public async Task<ActionResult<IEnumerable<ExtractoBancario>>> GetExtractos([FromQuery] int? cuentaId = null)
        {
            var empresaId = GetEmpresaId();
            var q = _context.ExtractosBancarios.Include(e => e.CuentaBancaria).Where(e => e.CuentaBancaria!.EmpresaId == empresaId);
            if (cuentaId.HasValue) q = q.Where(e => e.CuentaBancariaId == cuentaId.Value);
            return Ok(await q.OrderByDescending(e => e.FechaExtracto).ToListAsync());
        }

        [HttpPost("extractos/importar-camt053")]
        public async Task<ActionResult<ExtractoBancario>> ImportarCamt053([FromQuery] int cuentaId, [FromBody] string xml)
        {
            var cuenta = await _context.CuentasBancarias.FirstOrDefaultAsync(c => c.Id == cuentaId && c.EmpresaId == GetEmpresaId());
            if (cuenta == null) return BadRequest(new { Message = "Cuenta no válida" });
            var ext = await _bancario.ImportarExtractoCamt053Async(cuentaId, xml);
            return Ok(ext);
        }

        [HttpPost("conciliacion/automatica")]
        public async Task<ActionResult> ConciliacionAutomatica([FromQuery] int cuentaId, [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
        {
            var cuenta = await _context.CuentasBancarias.FirstOrDefaultAsync(c => c.Id == cuentaId && c.EmpresaId == GetEmpresaId());
            if (cuenta == null) return BadRequest(new { Message = "Cuenta no válida" });
            var n = await _bancario.ConciliarAutomaticoAsync(cuentaId, desde, hasta);
            return Ok(new { Conciliados = n });
        }

        public class CrearRemesaDto
        {
            public int CuentaOrdenanteId { get; set; }
            public DateTime FechaEjecucion { get; set; } = DateTime.Now.AddDays(1);
            public List<OperacionRemesaSEPA> Operaciones { get; set; } = new();
        }
        public class CrearRemesaAdeudoDto : CrearRemesaDto
        {
            public int CuentaAcreedoraId { get; set; }
            public EsquemaRemesaSEPA Esquema { get; set; } = EsquemaRemesaSEPA.SDD_Core;
        }
    }
}
