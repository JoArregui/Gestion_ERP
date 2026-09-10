public class GastoCaja
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public decimal Importe { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty; // Quién sacó el dinero
    public int EmpresaId { get; set; }
    public string Terminal { get; set; } = string.Empty;
    
    // Para saber si este gasto ya se incluyó en un cierre Z
    public bool ContabilizadoEnCierre { get; set; } = false;
    public int? CierreCajaId { get; set; } 
}