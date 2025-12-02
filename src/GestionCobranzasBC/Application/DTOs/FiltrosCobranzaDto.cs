// src/GestionCobranzasBC/Application/DTOs/FiltrosCobranzaDto.cs
#nullable enable
namespace GestionCobranzasBC.Application.DTOs;

using System;

public sealed class FiltrosCobranzaDto
{
    public Guid TenantId { get; init; }
    public Guid EmpresaId { get; init; }
    public Guid? EstablecimientoId { get; init; }

    public DateTime? FechaDesdeUtc { get; init; }
    public DateTime? FechaHastaUtc { get; init; }

    public Guid? ClienteId { get; init; }

    public string? EstadoCuenta { get; init; }
    public string? EstadoCobranza { get; init; }

    public string? MedioPagoCodigo { get; init; }
}
