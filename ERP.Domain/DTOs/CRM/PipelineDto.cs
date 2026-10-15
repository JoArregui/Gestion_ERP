using System.Collections.Generic;

namespace ERP.Domain.DTOs.CRM
{
    public sealed record EtapaResumen(string Etapa, int Count, decimal Importe, decimal Ponderado);

    public sealed record PipelineResumen(int Total, decimal ImporteTotal, decimal PonderadoTotal, List<EtapaResumen> Etapas);
}
