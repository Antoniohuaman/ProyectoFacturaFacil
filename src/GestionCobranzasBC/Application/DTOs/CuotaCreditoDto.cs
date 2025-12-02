// src/GestionCobranzasBC/Application/DTOs/CuotaCreditoDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

using System;

public sealed class CuotaCreditoDto
{
    public int NumeroCuota { get; init; }

    public DateTime FechaVencimientoUtc { get; init; }

    public decimal Importe { get; init; }
    public decimal Pagado { get; init; }
    public decimal Saldo { get; init; }

    public string Estado { get; init; } = "PENDIENTE";
}
