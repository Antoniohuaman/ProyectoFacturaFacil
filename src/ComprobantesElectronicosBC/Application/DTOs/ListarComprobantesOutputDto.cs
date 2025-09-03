using System;
using System.Collections.Generic;
using ComprobantesElectronicosBC.Application.ReadModels;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>
    /// Resultado paginado del listado de comprobantes (proyección de resumen).
    /// </summary>
    public sealed record ListarComprobantesOutputDto
    {
        public IReadOnlyList<ComprobanteResumenDto> Items { get; init; } = Array.Empty<ComprobanteResumenDto>();

        public int TotalItems     { get; init; }
        public int PageNumber     { get; init; }
        public int PageSize       { get; init; }
        public int TotalPages     { get; init; }
        public bool HasPreviousPage { get; init; }
        public bool HasNextPage     { get; init; }

        public string? SortBy        { get; init; }
        public string? SortDirection { get; init; }
    }
}
