using System;

namespace IndicadoresNegocioBC.Application.DTOs
{
    public sealed record SegmentoDto(
        Guid EmpresaId,
        Guid? EstablecimientoId,
        string Moneda // ISO 4217
    );
}