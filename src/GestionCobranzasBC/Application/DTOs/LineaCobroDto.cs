// src/GestionCobranzasBC/Application/DTOs/LineaCobroDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

public sealed class LineaCobroDto
{
    public int Orden { get; init; }

    /// <summary>
    /// Código SUNAT del medio de pago (catálogo 59).
    /// </summary>
    public string MedioPagoCodigo { get; init; } = string.Empty;

    public string MedioPagoDescripcion { get; init; } = string.Empty;

    public decimal Monto { get; init; }

    public string? CajaDestinoNombre { get; init; }

    public string? ReferenciaOperacion { get; init; }
}
