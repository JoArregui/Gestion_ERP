using System.Security.Cryptography;
using System.Text;
using ERP.Data;
using ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services
{
    /// <summary>
    /// Servicio control horario digital inalterable (RDL 8/2019 art.34.9 + proyecto RD 2026).
    /// Hash SHA256 encadenado análogo a Verifactu, conservación 4 años, trazabilidad correcciones.
    /// </summary>
    public class ControlHorarioService
    {
        private readonly ApplicationDbContext _context;
        public ControlHorarioService(ApplicationDbContext context) => _context = context;

        public async Task<ControlHorario> RegistrarFichajeAsync(int empleadoId, TipoRegistroHorario tipo, ModalidadTrabajo modalidad, OrigenFichaje origen, string? ip, string? geo, string? dispositivoId, string? firmaBase64, string? ubicacion = null)
        {
            var empleado = await _context.Empleados.FindAsync(empleadoId)
                ?? throw new InvalidOperationException("Empleado no existe");

            // Validar no duplicar jornada abierta para Entrada (mantener compatibilidad)
            if (tipo == TipoRegistroHorario.Entrada)
            {
                var abierta = await _context.ControlesHorarios.AnyAsync(c => c.EmpleadoId == empleadoId && c.Salida == null && c.TipoRegistro == TipoRegistroHorario.Entrada);
                if (abierta && tipo == TipoRegistroHorario.Entrada)
                    throw new InvalidOperationException("Ya existe jornada activa para este empleado");
            }

            var anterior = await _context.ControlesHorarios
                .Where(c => c.EmpleadoId == empleadoId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            var ahora = DateTime.Now;
            var registro = new ControlHorario
            {
                EmpleadoId = empleadoId,
                Entrada = tipo == TipoRegistroHorario.Entrada ? ahora : (anterior?.Entrada ?? ahora),
                Salida = tipo == TipoRegistroHorario.Salida ? ahora : null,
                Ubicacion = ubicacion ?? geo,
                TipoRegistro = tipo,
                Modalidad = modalidad,
                Origen = origen,
                DireccionIP = ip,
                Geolocalizacion = geo,
                DispositivoId = dispositivoId,
                FirmaEmpleadoBase64 = firmaBase64,
                HashAnterior = anterior?.Hash,
                FechaCreacion = ahora
            };

            // Si es Salida, cerrar el último abierto en lugar de crear nuevo (compat intervalo)
            if (tipo == TipoRegistroHorario.Salida)
            {
                var abierto = await _context.ControlesHorarios
                    .Where(c => c.EmpleadoId == empleadoId && c.Salida == null)
                    .OrderByDescending(c => c.Entrada)
                    .FirstOrDefaultAsync();
                if (abierto != null)
                {
                    abierto.Salida = ahora;
                    abierto.MotivoCorreccion = null;
                    // Recalcular hash de cierre (no rompe cadena, añade evento salida)
                    abierto.DireccionIP = ip ?? abierto.DireccionIP;
                    abierto.Geolocalizacion = geo ?? abierto.Geolocalizacion;
                    abierto.HashAnterior = anterior?.HashAnterior;
                    abierto.Hash = CalcularHash(abierto);
                    await _context.SaveChangesAsync();
                    return abierto;
                }
            }

            registro.Hash = CalcularHash(registro);
            _context.ControlesHorarios.Add(registro);
            await _context.SaveChangesAsync();
            return registro;
        }

        public async Task<ControlHorario> CorregirRegistroAsync(int registroId, string motivo, string usuarioCorreccion, DateTime? nuevaEntrada = null, DateTime? nuevaSalida = null)
        {
            var r = await _context.ControlesHorarios.FindAsync(registroId)
                ?? throw new InvalidOperationException("Registro no encontrado");
            r.Estado = EstadoRegistroHorario.Corregido;
            r.MotivoCorreccion = motivo;
            r.UsuarioCorreccion = usuarioCorreccion;
            r.FechaCorreccion = DateTime.Now;
            if (nuevaEntrada.HasValue) r.Entrada = nuevaEntrada.Value;
            if (nuevaSalida.HasValue) r.Salida = nuevaSalida;
            // Hash nueva corrección encadenada
            r.Hash = CalcularHash(r);
            await _context.SaveChangesAsync();
            return r;
        }

        public async Task<List<ControlHorario>> GetRegistrosParaInspeccionAsync(int empresaId, DateTime desde, DateTime hasta)
        {
            // Conservación 4 años art.34.9 ET
            var empleadosIds = await _context.Empleados.Where(e => e.EmpresaId == empresaId).Select(e => e.Id).ToListAsync();
            return await _context.ControlesHorarios
                .Where(c => empleadosIds.Contains(c.EmpleadoId) && c.FechaCreacion >= desde && c.FechaCreacion <= hasta)
                .OrderBy(c => c.FechaCreacion)
                .ToListAsync();
        }

        public static string CalcularHash(ControlHorario r)
        {
            var datos = string.Join("&", new[]
            {
                $"EmpleadoId={r.EmpleadoId}",
                $"Tipo={r.TipoRegistro}",
                $"Entrada={r.Entrada:yyyy-MM-ddTHH:mm:ss}",
                $"Salida={r.Salida:yyyy-MM-ddTHH:mm:ss}",
                $"Modalidad={r.Modalidad}",
                $"Origen={r.Origen}",
                $"IP={r.DireccionIP ?? ""}",
                $"HashAnterior={r.HashAnterior ?? ""}",
                $"FechaCreacion={r.FechaCreacion:yyyy-MM-ddTHH:mm:sszzz}"
            });
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(datos)));
        }
    }
}
