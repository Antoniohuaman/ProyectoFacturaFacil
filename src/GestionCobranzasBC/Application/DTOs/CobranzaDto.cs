// src/GestionCobranzasBC/Application/DTOs/CobranzaDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

using System;
using System.Collections.Generic;

public sealed class CobranzaDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }
    public Guid EmpresaId { get; init; }
    public Guid? EstablecimientoId { get; init; }

    public Guid CuentaPorCobrarId { get; init; }

    public string NumeroDocumento { get; init; } = string.Empty;
    public DateTime FechaDocumentoUtc { get; init; }

    public string MonedaCodigo { get; init; } = "PEN";
    public decimal MontoTotal { get; init; }

    /// <summary>
    /// Estado de la cobranza (p.ej. REGISTRADA, ANULADA).
    /// </summary>
    public string Estado { get; init; } = "REGISTRADA";

    public string? CajaDestino { get; init; }

    public IReadOnlyCollection<LineaCobroDto> LineasCobro { get; init; } =
        Array.Empty<LineaCobroDto>();
}
