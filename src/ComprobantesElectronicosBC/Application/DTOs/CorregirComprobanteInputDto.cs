using System;
using ComprobantesElectronicosBC.Domain.ValueObjects; // Para snapshots si decides pasarlos

namespace ComprobantesElectronicosBC.Application.UseCases.CorregirComprobante
{
    /// <summary>
    /// Parámetros para corregir (actualizar) un comprobante existente.
    /// Solo orquesta: la lógica fina de cómo aplicar cada cambio vive en el corrector/adapters.
    /// Reglas en este DTO:
    /// - ComprobanteId es obligatorio.
    /// - Si se fija Serie/Número, deben venir ambos (el caso de uso valida formato y unicidad).
    /// </summary>
    public sealed record CorregirComprobanteInputDto
    {
        /// <summary>Id del comprobante a corregir.</summary>
        public Guid ComprobanteId { get; init; }

        /// <summary>Nueva serie (opcional). Debe venir junto con <see cref="Numero"/> si se usa.</summary>
        public string? Serie { get; init; }

        /// <summary>Nuevo número (opcional). Debe venir junto con <see cref="Serie"/> si se usa.</summary>
        public int? Numero { get; init; }

        /// <summary>Nueva fecha de emisión (opcional). La validación normativa la hace el dominio.</summary>
        public DateOnly? NuevaFechaEmision { get; init; }

        /// <summary>Nueva fecha de vencimiento (opcional). La validación normativa la hace el dominio.</summary>
        public DateOnly? NuevaFechaVencimiento { get; init; }

        /// <summary>Observaciones visibles (opcional). El corrector decidirá cómo mapear a VO.</summary>
        public string? Observaciones { get; init; }

        /// <summary>Número de guía de remisión externa (opcional).</summary>
        public string? NumeroGuiaRemision { get; init; }

        /// <summary>Número de orden de compra (opcional).</summary>
        public string? NumeroOrdenCompra { get; init; }

        /// <summary>
        /// Si corresponde, snapshot de cliente corregido (opcional).
        /// Mantener como VO permite que el dominio preserve su invariante.
        /// </summary>
        public ClienteSnapshot? NuevoCliente { get; init; }
    }
}
