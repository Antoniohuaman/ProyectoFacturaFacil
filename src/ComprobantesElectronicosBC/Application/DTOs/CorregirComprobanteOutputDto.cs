using System;

namespace ComprobantesElectronicosBC.Application.UseCases.CorregirComprobante
{
    /// <summary>
    /// Resultado tras aplicar correcciones a un comprobante.
    /// </summary>
    public sealed record CorregirComprobanteOutputDto
    {
        public Guid ComprobanteId { get; init; }
        public string TipoComprobante { get; init; } = ""; // "01" Factura / "03" Boleta
        public string Serie { get; init; } = "";
        public int Numero { get; init; }
        public string Estado { get; init; } = "Borrador";  // esperado tras corrección en la mayoría de flujos
        public bool CorreccionAplicada => true;
    }
}
