using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ERP.Data;
using ERP.Domain.Entities.Transversal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ERP.Services.Transversal
{
    /// <summary>
    /// Motor de informes custom + export Excel/PDF/CSV (Fase G3 §8-9).
    /// </summary>
    public class ReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context) => _context = context;

        // ── Definiciones ─────────────────────────────────────────────
        public async Task<List<ReportDefinicion>> ListarAsync(int empresaId, bool soloPublicos = false)
        {
            var q = _context.ReportDefiniciones.Where(r => r.EmpresaId == empresaId);
            if (soloPublicos) q = q.Where(r => r.Publico);
            return await q.OrderBy(r => r.Nombre).ToListAsync();
        }

        public async Task<ReportDefinicion> CrearAsync(ReportDefinicion dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Codigo)) throw new ArgumentException("Código obligatorio");
            if (string.IsNullOrWhiteSpace(dto.ConsultaSql)) throw new ArgumentException("Consulta SQL obligatoria");
            if (await _context.ReportDefiniciones.AnyAsync(r => r.EmpresaId == dto.EmpresaId && r.Codigo == dto.Codigo))
                throw new InvalidOperationException($"Código {dto.Codigo} ya existe");
            _context.ReportDefiniciones.Add(dto);
            await _context.SaveChangesAsync();
            return dto;
        }

        // ── Ejecución ────────────────────────────────────────────────
        public async Task<ReportEjecucion> EjecutarAsync(int empresaId, int definicionId, Dictionary<string, object>? parametros = null, string? usuarioId = null)
        {
            var def = await _context.ReportDefiniciones.FirstOrDefaultAsync(r => r.Id == definicionId && r.EmpresaId == empresaId)
                ?? throw new KeyNotFoundException("Informe no encontrado");

            var ejecucion = new ReportEjecucion
            {
                EmpresaId = empresaId,
                ReportDefinicionId = definicionId,
                ParametrosJson = parametros != null ? JsonSerializer.Serialize(parametros) : null,
                UsuarioId = usuarioId,
                Estado = EstadoEjecucionReport.Procesando
            };
            _context.ReportEjecuciones.Add(ejecucion);
            await _context.SaveChangesAsync();

            try
            {
                var dt = await EjecutarConsultaAsync(def.ConsultaSql, parametros ?? new Dictionary<string, object>());

                // Exportar a Excel por defecto
                var bytes = ExportarAExcel(dt, def.ColumnasJson);
                var ruta = $"/tmp/report_{ejecucion.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
                await File.WriteAllBytesAsync(ruta, bytes);

                ejecucion.Estado = EstadoEjecucionReport.Completado;
                ejecucion.ResultadoRuta = ruta;
                ejecucion.TamanoBytes = bytes.Length;
            }
            catch (Exception ex)
            {
                ejecucion.Estado = EstadoEjecucionReport.Fallido;
                ejecucion.Error = ex.Message;
            }
            finally
            {
                await _context.SaveChangesAsync();
            }
            return ejecucion;
        }

        private async Task<DataTable> EjecutarConsultaAsync(string sql, Dictionary<string, object> parametros)
        {
            // Usar conexión raw para máxima flexibilidad
            var connStr = _context.Database.GetConnectionString() ?? throw new InvalidOperationException("Sin connection string");
            await using var conn = new SqliteConnection(connStr);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var p in parametros)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = $"@{p.Key}";
                param.Value = p.Value ?? DBNull.Value;
                cmd.Parameters.Add(param);
            }

            var dt = new DataTable();
            await using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        // ── Exportadores ─────────────────────────────────────────────
        public byte[] ExportarAExcel(DataTable dt, string? columnasJson = null)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Informe");

            // Cabeceras
            var columnas = string.IsNullOrWhiteSpace(columnasJson)
                ? dt.Columns.Cast<DataColumn>().Select(c => new ColumnaReporte { Campo = c.ColumnName, Etiqueta = c.ColumnName, Formato = "" }).ToList()
                : JsonSerializer.Deserialize<List<ColumnaReporte>>(columnasJson) ?? new();

            for (int i = 0; i < columnas.Count; i++)
            {
                ws.Cell(1, i + 1).Value = columnas[i].Etiqueta;
                ws.Cell(1, i + 1).Style.Font.Bold = true;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Datos
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < columnas.Count; c++)
                {
                    var col = columnas[c];
                    var val = dt.Rows[r][col.Campo];
                    var cell = ws.Cell(r + 2, c + 1);

                    if (val != DBNull.Value && val != null)
                    {
                        cell.Value = val.ToString();
                        if (col.Formato == "currency") cell.Style.NumberFormat.Format = "#,##0.00 €";
                        else if (col.Formato == "date") cell.Style.NumberFormat.Format = "yyyy-mm-dd";
                        else if (col.Formato == "number") cell.Style.NumberFormat.Format = "#,##0.00";
                    }
                }
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] ExportarACsv(DataTable dt, char separador = ';')
        {
            var sb = new StringBuilder();
            // Cabeceras
            sb.AppendLine(string.Join(separador, dt.Columns.Cast<DataColumn>().Select(c => EscapeCsv(c.ColumnName))));
            // Filas
            foreach (DataRow row in dt.Rows)
            {
                sb.AppendLine(string.Join(separador, row.ItemArray.Select(v => EscapeCsv(v?.ToString() ?? ""))));
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] ExportarAPdf(DataTable dt, string? columnasJson = null)
        {
            // PDF simple usando ClosedXML no soporta PDF nativo.
            // En producción usar QuestPDF, PdfSharp o similar.
            // Aquí devolvemos Excel como fallback.
            return ExportarAExcel(dt, columnasJson);
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains(';') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        private class ColumnaReporte
        {
            public string Campo { get; set; } = "";
            public string Etiqueta { get; set; } = "";
            public string Formato { get; set; } = ""; // currency, date, number, ""
        }

        // ── Historial ────────────────────────────────────────────────
        public async Task<List<ReportEjecucion>> GetHistorialAsync(int empresaId, int? definicionId = null, int take = 50)
        {
            var q = _context.ReportEjecuciones.Where(e => e.EmpresaId == empresaId);
            if (definicionId.HasValue) q = q.Where(e => e.ReportDefinicionId == definicionId.Value);
            return await q.OrderByDescending(e => e.FechaEjecucion).Take(take).ToListAsync();
        }
    }
}