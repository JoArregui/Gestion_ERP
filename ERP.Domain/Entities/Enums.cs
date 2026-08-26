namespace ERP.Domain.Entities
{
    public enum TipoDocumento
    {
        Presupuesto,
        FacturaProforma, // Añadido: Documento informativo previo a la factura
        Pedido,
        Albaran,
        Factura,
        FacturaRectificativa,
        AjusteStock // <--- Crucial para Scanpal y auditoría de almacén
    }

    public enum EstadoDocumento
    {
        Borrador,
        Emitido,
        Pagado,
        Anulado
    }
}