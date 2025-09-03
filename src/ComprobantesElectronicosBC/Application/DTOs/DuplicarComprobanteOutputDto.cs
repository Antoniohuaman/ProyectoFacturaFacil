using System;

namespace ComprobantesElectronicosBC.Application.UseCases.DuplicarComprobante
{
    /// <summary>
    /// Resultado de la operación de duplicado.
    /// </summary>
    public sealed record DuplicarComprobanteOutputDto
    {
        /// <summary>Id del nuevo comprobante creado.</summary>
        public Guid NuevoId { get; init; }

        /// <summary>Código SUNAT del tipo de comprobante del duplicado: "01" Factura / "03" Boleta.</summary>
        public string TipoComprobante { get; init; } = "";

        /// <summary>Serie del nuevo comprobante.</summary>
        public string Serie { get; init; } = "";

        /// <summary>Número del nuevo comprobante.</summary>
        public int Numero { get; init; }

        /// <summary>Estado lógico luego del clonado (esperado: "Borrador").</summary>
        public string Estado { get; init; } = "Borrador";

        /// <summary>Bandera de conveniencia para la UI/telemetría.</summary>
        public bool EsDuplicado => true;
    }
}
