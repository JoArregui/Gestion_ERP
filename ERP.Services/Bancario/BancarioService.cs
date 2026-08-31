using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ERP.Data;
using ERP.Domain.Entities.Bancario;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Bancario
{
    /// <summary>
    /// Servicio de gestiÃ³n bancaria SEPA: remesas, mandatos, conciliaciÃ³n
    /// </summary>
    public class BancarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly SepaXmlGeneratorService _xmlGenerator;

        public BancarioService(ApplicationDbContext context, SepaXmlGeneratorService xmlGenerator)
        {
            _context = context;
            _xmlGenerator = xmlGenerator;
        }

        #region Cuentas Bancarias

        public async Task<List<CuentaBancaria>> GetCuentasBancariasAsync(int empresaId, bool soloActivas = true)
        {
            var query = _context.CuentasBancarias.Where(c => c.EmpresaId == empresaId);
            if (soloActivas) query = query.Where(c => c.Activa);
            return await query.OrderBy(c => c.NombreCuenta).ToListAsync();
        }

        public async Task<CuentaBancaria?> GetCuentaBancariaAsync(int id)
        {
            return await _context.CuentasBancarias
                .Include(c => c.Empresa)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CuentaBancaria> CrearCuentaBancariaAsync(CuentaBancaria cuenta)
        {
            // Validar IBAN
            if (!ValidarIBAN(cuenta.IBAN))
                throw new ArgumentException("IBAN invÃ¡lido");

            // Si es principal, desmarcar otras
            if (cuenta.EsPrincipal)
            {
                var otras = await _context.CuentasBancarias
                    .Where(c => c.EmpresaId == cuenta.EmpresaId && c.EsPrincipal && c.Id != cuenta.Id)
                    .ToListAsync();
                foreach (var o in otras) o.EsPrincipal = false;
            }

            cuenta.FechaCreacion = DateTime.Now;
            _context.CuentasBancarias.Add(cuenta);
            await _context.SaveChangesAsync();
            return cuenta;
        }

        public async Task<bool> ActualizarCuentaBancariaAsync(CuentaBancaria cuenta)
        {
            var existente = await _context.CuentasBancarias.FindAsync(cuenta.Id);
            if (existente == null) return false;

            if (!ValidarIBAN(cuenta.IBAN))
                throw new ArgumentException("IBAN invÃ¡lido");

            // Actualizar campos
            existente.NombreCuenta = cuenta.NombreCuenta;
            existente.IBAN = cuenta.IBAN;
            existente.BIC = cuenta.BIC;
            existente.EntidadBancaria = cuenta.EntidadBancaria;
            existente.CodigoEntidad = cuenta.CodigoEntidad;
            existente.CodigoOficina = cuenta.CodigoOficina;
            existente.DigitosControl = cuenta.DigitosControl;
            existente.NumeroCuenta = cuenta.NumeroCuenta;
            existente.Tipo = cuenta.Tipo;
            existente.CreditorIdentifier = cuenta.CreditorIdentifier;
            existente.PermiteTransferenciasSEPA = cuenta.PermiteTransferenciasSEPA;
            existente.PermiteAdeudosSEPA = cuenta.PermiteAdeudosSEPA;
            existente.PermiteTransferenciasInstant = cuenta.PermiteTransferenciasInstant;
            existente.LimiteDiarioTransferencias = cuenta.LimiteDiarioTransferencias;
            existente.LimiteDiarioAdeudos = cuenta.LimiteDiarioAdeudos;
            existente.Activa = cuenta.Activa;
            existente.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        private bool ValidarIBAN(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban)) return false;
            iban = iban.Replace(" ", "").ToUpper();
            if (iban.Length < 15 || iban.Length > 34) return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(iban, @"^[A-Z]{2}[0-9]{2}[A-Z0-9]+$")) return false;

            // Algoritmo de validaciÃ³n IBAN (MOD 97-10)
            var rearranged = iban.Substring(4) + iban.Substring(0, 4);
            var numeric = "";
            foreach (char c in rearranged)
            {
                numeric += char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString();
            }

            // Calcular MOD 97
            int remainder = 0;
            foreach (char c in numeric)
            {
                remainder = (remainder * 10 + (c - '0')) % 97;
            }
            return remainder == 1;
        }

        #endregion

        #region Mandatos SEPA

        public async Task<List<MandatoSEPA>> GetMandatosAsync(int empresaId, EstadoMandatoSEPA? estado = null)
        {
            var query = _context.MandatosSEPA
                .Include(m => m.CuentaBancariaAcreedor)
                .Include(m => m.CuentaBancariaDeudor)
                .Where(m => m.EmpresaId == empresaId);

            if (estado.HasValue) query = query.Where(m => m.Estado == estado.Value);

            return await query.OrderByDescending(m => m.FechaCreacion).ToListAsync();
        }

        public async Task<MandatoSEPA?> GetMandatoAsync(int id)
        {
            return await _context.MandatosSEPA
                .Include(m => m.CuentaBancariaAcreedor)
                .Include(m => m.CuentaBancariaDeudor)
                .Include(m => m.Remesas)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<MandatoSEPA> CrearMandatoAsync(MandatoSEPA mandato)
        {
            // Generar RUM si no tiene
            if (string.IsNullOrEmpty(mandato.ReferenciaUnicaMandato))
            {
                mandato.ReferenciaUnicaMandato = GenerarRUM();
            }

            // Validar unicidad RUM
            var existeRum = await _context.MandatosSEPA
                .AnyAsync(m => m.ReferenciaUnicaMandato == mandato.ReferenciaUnicaMandato);
            if (existeRum) throw new ArgumentException("RUM duplicado");

            // Validar IBAN deudor
            if (!ValidarIBAN(mandato.DeudorIBAN))
                throw new ArgumentException("IBAN deudor invÃ¡lido");

            mandato.FechaCreacion = DateTime.Now;
            if (mandato.FechaFirma == default) mandato.FechaFirma = DateTime.Now;

            _context.MandatosSEPA.Add(mandato);
            await _context.SaveChangesAsync();
            return mandato;
        }

        public async Task<bool> FirmarMandatoAsync(int mandatoId, bool verificadoBeneficiario = false)
        {
            var mandato = await _context.MandatosSEPA.FindAsync(mandatoId);
            if (mandato == null) return false;

            mandato.Estado = EstadoMandatoSEPA.Activo;
            if (verificadoBeneficiario)
            {
                mandato.BeneficiarioVerificado = true;
                mandato.FechaVerificacionBeneficiario = DateTime.Now;
                mandato.MetodoVerificacion = "IBAN_Name_Check";
            }
            mandato.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RevocarMandatoAsync(int mandatoId)
        {
            var mandato = await _context.MandatosSEPA.FindAsync(mandatoId);
            if (mandato == null) return false;

            mandato.Estado = EstadoMandatoSEPA.Revocado;
            mandato.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerarRUM()
        {
            // Formato: ES + 2 dÃ­gitos control + 3 chars entidad + 3 chars oficina + 2 dÃ­gitos control + 10 dÃ­gitos cuenta + 14 alfanumÃ©ricos aleatorios
            // Simplificado: ES + timestamp + random
            var random = new Random();
            var parte = random.Next(10000000, 99999999).ToString("D8");
            return $"ES{DateTime.Now:yyyyMMdd}{parte}";
        }

        #endregion

        #region Remesas SEPA

        public async Task<RemesaSEPA> CrearRemesaTransferenciaAsync(int empresaId, int cuentaOrdenanteId, DateTime fechaEjecucion, List<OperacionRemesaSEPA> operaciones)
        {
            var cuenta = await _context.CuentasBancarias.FindAsync(cuentaOrdenanteId);
            if (cuenta == null || !cuenta.PermiteTransferenciasSEPA)
                throw new ArgumentException("Cuenta no vÃ¡lida o no permite transferencias SEPA");

            var remesa = new RemesaSEPA
            {
                EmpresaId = empresaId,
                Referencia = GenerarReferenciaRemesa("TRF"),
                Tipo = TipoRemesaSEPA.Transferencia,
                Esquema = EsquemaRemesaSEPA.SCT,
                CuentaBancariaOrdenanteId = cuentaOrdenanteId,
                FechaEjecucion = fechaEjecucion,
                Operaciones = operaciones,
                NumeroOperaciones = operaciones.Count,
                ImporteTotal = operaciones.Sum(o => o.Importe),
                Estado = EstadoRemesaSEPA.Borrador,
                FechaCreacion = DateTime.Now
            };

            // Asignar orden y remesa a operaciones
            for (int i = 0; i < operaciones.Count; i++)
            {
                operaciones[i].Orden = i + 1;
                operaciones[i].RemesaId = remesa.Id; // Se asignarÃ¡ tras SaveChanges
            }

            _context.RemesasSEPA.Add(remesa);
            await _context.SaveChangesAsync();

            // Actualizar RemesaId en operaciones
            foreach (var op in operaciones)
            {
                op.RemesaId = remesa.Id;
            }
            await _context.SaveChangesAsync();

            return remesa;
        }

        public async Task<RemesaSEPA> CrearRemesaAdeudoAsync(int empresaId, int cuentaAcreedoraId, DateTime fechaEjecucion, EsquemaRemesaSEPA esquema, List<OperacionRemesaSEPA> operaciones)
        {
            var cuenta = await _context.CuentasBancarias.FindAsync(cuentaAcreedoraId);
            if (cuenta == null || !cuenta.PermiteAdeudosSEPA)
                throw new ArgumentException("Cuenta no vÃ¡lida o no permite adeudos SEPA");

            if (esquema != EsquemaRemesaSEPA.SDD_Core && esquema != EsquemaRemesaSEPA.SDD_B2B)
                throw new ArgumentException("Esquema debe ser SDD_Core o SDD_B2B");

            var remesa = new RemesaSEPA
            {
                EmpresaId = empresaId,
                Referencia = GenerarReferenciaRemesa("SDD"),
                Tipo = TipoRemesaSEPA.Adeudo,
                Esquema = esquema,
                CuentaBancariaOrdenanteId = cuentaAcreedoraId, // Para adeudos, la cuenta ordenante es la acreedora (donde se reciben fondos)
                CuentaBancariaAcreedoraId = cuentaAcreedoraId,
                FechaEjecucion = fechaEjecucion,
                Operaciones = operaciones,
                NumeroOperaciones = operaciones.Count,
                ImporteTotal = operaciones.Sum(o => o.Importe),
                Estado = EstadoRemesaSEPA.Borrador,
                FechaCreacion = DateTime.Now
            };

            for (int i = 0; i < operaciones.Count; i++)
            {
                operaciones[i].Orden = i + 1;
            }

            _context.RemesasSEPA.Add(remesa);
            await _context.SaveChangesAsync();

            foreach (var op in operaciones)
            {
                op.RemesaId = remesa.Id;
            }
            await _context.SaveChangesAsync();

            return remesa;
        }

        public async Task<string> GenerarXmlRemesaAsync(int remesaId)
        {
            var remesa = await _context.RemesasSEPA
                .Include(r => r.Operaciones)
                    .ThenInclude(o => o.Mandato)
                .Include(r => r.CuentaBancariaOrdenante)
                .Include(r => r.CuentaBancariaAcreedora)
                .Include(r => r.Empresa)
                .FirstOrDefaultAsync(r => r.Id == remesaId);

            if (remesa == null) throw new ArgumentException("Remesa no encontrada");

            string xml;
            if (remesa.Tipo == TipoRemesaSEPA.Transferencia)
            {
                xml = _xmlGenerator.GenerarPain001(remesa);
            }
            else
            {
                xml = _xmlGenerator.GenerarPain008(remesa);
            }

            // Guardar XML en remesa
            remesa.XmlGenerado = xml;
            remesa.NombreArchivoXml = $"{(remesa.Tipo == TipoRemesaSEPA.Transferencia ? "pain.001" : "pain.008")}_{DateTime.Now:yyyyMMdd}_{remesa.Referencia}.xml";
            remesa.HashXmlSHA256 = CalcularSHA256(xml);
            remesa.TamanoBytes = System.Text.Encoding.UTF8.GetByteCount(xml);
            remesa.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return xml;
        }

        public async Task<RemesaSEPA> ValidarYEnviarRemesaAsync(int remesaId, string usuarioEnvio)
        {
            var remesa = await _context.RemesasSEPA
                .Include(r => r.Operaciones)
                .FirstOrDefaultAsync(r => r.Id == remesaId);

            if (remesa == null) throw new ArgumentException("Remesa no encontrada");
            if (remesa.Estado != EstadoRemesaSEPA.Borrador && remesa.Estado != EstadoRemesaSEPA.Validada)
                throw new InvalidOperationException("Remesa no estÃ¡ en estado vÃ¡lido para envÃ­o");

            // Validaciones
            var errores = ValidarRemesa(remesa);
            if (errores.Any())
                throw new InvalidOperationException($"Remesa con errores: {string.Join("; ", errores)}");

            // Generar XML si no existe
            if (string.IsNullOrEmpty(remesa.XmlGenerado))
            {
                await GenerarXmlRemesaAsync(remesaId);
            }

            // AquÃ­ irÃ­a la integraciÃ³n real con banco (EBICS, API bancaria, fichero SFTP, etc.)
            // Por ahora simulamos envÃ­o
            remesa.Estado = EstadoRemesaSEPA.Enviada;
            remesa.FechaEnvioBanco = DateTime.Now;
            remesa.UsuarioEnvio = usuarioEnvio;
            remesa.ReferenciaBanco = $"BANK{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

            await _context.SaveChangesAsync();
            return remesa;
        }

        private List<string> ValidarRemesa(RemesaSEPA remesa)
        {
            var errores = new List<string>();

            if (remesa.Operaciones == null || !remesa.Operaciones.Any())
                errores.Add("Remesa sin operaciones");

            if (remesa.ImporteTotal <= 0)
                errores.Add("Importe total debe ser > 0");

            if (remesa.FechaEjecucion < DateTime.Today)
                errores.Add("Fecha ejecuciÃ³n no puede ser pasada");

            foreach (var op in remesa.Operaciones)
            {
                if (op.Importe <= 0)
                    errores.Add($"OperaciÃ³n {op.Orden}: importe debe ser > 0");

                if (remesa.Tipo == TipoRemesaSEPA.Transferencia)
                {
                    if (string.IsNullOrEmpty(op.BeneficiarioNombre))
                        errores.Add($"Op {op.Orden}: falta nombre beneficiario");
                    if (string.IsNullOrEmpty(op.BeneficiarioIBAN) || !ValidarIBAN(op.BeneficiarioIBAN))
                        errores.Add($"Op {op.Orden}: IBAN beneficiario invÃ¡lido");
                }
                else
                {
                    if (string.IsNullOrEmpty(op.DeudorNombre))
                        errores.Add($"Op {op.Orden}: falta nombre deudor");
                    if (string.IsNullOrEmpty(op.DeudorIBAN) || !ValidarIBAN(op.DeudorIBAN))
                        errores.Add($"Op {op.Orden}: IBAN deudor invÃ¡lido");
                    if (string.IsNullOrEmpty(op.ReferenciaUnicaMandato) && op.Mandato == null)
                        errores.Add($"Op {op.Orden}: falta RUM (mandato)");
                }

                if (op.Importe > 999999999.99m)
                    errores.Add($"Op {op.Orden}: importe excede mÃ¡ximo SEPA");
            }

            return errores;
        }

        private string GenerarReferenciaRemesa(string prefijo)
        {
            return $"{prefijo}{DateTime.Now:yyyyMMdd}{new Random().Next(10000, 99999):D5}";
        }

        private string CalcularSHA256(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        #endregion

        #region ConciliaciÃ³n Bancaria

        public async Task<ExtractoBancario> ImportarExtractoCamt053Async(int cuentaBancariaId, string xmlCamt053)
        {
            // Parsear XML camt.053 y crear ExtractoBancario + Movimientos
            // ImplementaciÃ³n simplificada - en producciÃ³n usar serializaciÃ³n XSD
            var extracto = new ExtractoBancario
            {
                CuentaBancariaId = cuentaBancariaId,
                ReferenciaExtracto = $"EXT{DateTime.Now:yyyyMMddHHmmss}",
                FechaExtracto = DateTime.Today,
                FechaValor = DateTime.Today,
                XmlOriginal = xmlCamt053,
                HashXmlSHA256 = CalcularSHA256(xmlCamt053),
                FechaRecepcion = DateTime.Now
            };

            _context.ExtractosBancarios.Add(extracto);
            await _context.SaveChangesAsync();

            // AquÃ­ parsear XML y crear MovimientosExtracto
            // ...

            return extracto;
        }

        public async Task<int> ConciliarAutomaticoAsync(int cuentaBancariaId, DateTime fechaDesde, DateTime fechaHasta)
        {
            // LÃ³gica de conciliaciÃ³n automÃ¡tica:
            // 1. Obtener movimientos sin conciliar del extracto
            // 2. Obtener operaciones de remesas enviadas/ejecutadas
            // 3. Obtener asientos contables pendientes
            // 4. Matching por: importe, fecha (+/- 2 dÃ­as), concepto, IBAN contrapartida, EndToEndId, RUM

            var movimientos = await _context.MovimientosExtracto
                .Where(m => m.Extracto.CuentaBancariaId == cuentaBancariaId
                    && m.FechaValor >= fechaDesde && m.FechaValor <= fechaHasta
                    && !m.Conciliado)
                .ToListAsync();

            var operacionesRemesa = await _context.OperacionesRemesaSEPA
                .Where(o => o.Remesa.CuentaBancariaOrdenanteId == cuentaBancariaId
                    || o.Remesa.CuentaBancariaAcreedoraId == cuentaBancariaId)
                .Where(o => (o.Estado == EstadoOperacionRemesa.Ejecutada || o.Estado == EstadoOperacionRemesa.Aceptada)
                    && o.FechaCreacion >= fechaDesde && o.FechaCreacion <= fechaHasta)
                .ToListAsync();

            int conciliados = 0;
            foreach (var mov in movimientos)
            {
                var match = operacionesRemesa.FirstOrDefault(o =>
                    Math.Abs(o.Importe - mov.Importe) < 0.01m &&
                    (o.Remesa?.FechaEjecucion ?? o.FechaCreacion).Date == mov.FechaValor.Date &&
                    (!string.IsNullOrEmpty(o.ReferenciaPropia) && mov.EndToEndId == o.ReferenciaPropia ||
                     !string.IsNullOrEmpty(o.ReferenciaUnicaMandato) && mov.MandateId == o.ReferenciaUnicaMandato)
                );

                if (match != null)
                {
                    mov.Conciliado = true;
                    mov.OperacionRemesaId = match.Id;
                    mov.FechaConciliacion = DateTime.Now;
                    conciliados++;
                }
            }

            await _context.SaveChangesAsync();
            return conciliados;
        }

        #endregion
    }
}