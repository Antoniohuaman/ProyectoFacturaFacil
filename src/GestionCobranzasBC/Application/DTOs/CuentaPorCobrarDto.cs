// src/GestionCobranzasBC/Application/DTOs/CuentaPorCobrarDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

using System;
using System.Collections.Generic;

public sealed class CuentaPorCobrarDto
{
    public Guid Id { get; init; }

    public Guid TenantId { get; init; }
    public Guid EmpresaId { get; init; }
    public Guid? EstablecimientoId { get; init; }

    public Guid ComprobanteId { get; init; }
    public string NumeroComprobante { get; init; } = string.Empty;

    public string NumeroCuentaPorCobrar { get; init; } = string.Empty;

    public Guid ClienteId { get; init; }
    public string ClienteNombre { get; init; } = string.Empty;

    public string MonedaCodigo { get; init; } = "PEN";
    public decimal Total { get; init; }
    public decimal Cobrado { get; init; }
    public decimal Saldo { get; init; }

    public string Estado { get; init; } = "PENDIENTE";

    public DateTime FechaEmisionUtc { get; init; }
    public DateTime? FechaVencimientoUtc { get; init; }

    public IReadOnlyCollection<CuotaCreditoDto> Cuotas { get; init; } =
        Array.Empty<CuotaCreditoDto>();
}
