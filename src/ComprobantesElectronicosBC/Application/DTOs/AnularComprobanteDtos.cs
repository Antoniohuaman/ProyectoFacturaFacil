using System;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>Entrada para confirmar la baja de un comprobante.</summary>
    public readonly record struct AnularComprobanteInput(
        Guid      ComprobanteId,
        DateOnly? FechaBaja,   // null => hoy
        string?   Motivo       // opcional (auditoría interna)
    );

    /// <summary>Salida con el estado actualizado a CANCELLED.</summary>
    public readonly record struct AnularComprobanteOutput(
        Guid     ComprobanteId,
        string   Estado,   // "CANCELLED"
        DateOnly FechaBaja
    );
}
