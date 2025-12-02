// src/GestionCobranzasBC/Application/DTOs/HistorialCobranzaDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

using System;
using System.Collections.Generic;

public sealed class HistorialCobranzaDto
{
    public Guid CuentaPorCobrarId { get; init; }

    public string NumeroCuentaPorCobrar { get; init; } = string.Empty;
    public string NumeroComprobante { get; init; } = string.Empty;

    public string ClienteNombre { get; init; } = string.Empty;
    public string MonedaCodigo { get; init; } = "PEN";

    public decimal Total { get; init; }
    public decimal Cobrado { get; init; }
    public decimal Saldo { get; init; }

    public IReadOnlyCollection<CobranzaDto> Cobranzas { get; init; } =
        Array.Empty<CobranzaDto>();
}
