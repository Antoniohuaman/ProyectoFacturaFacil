using System;
using System.Threading;
using System.Threading.Tasks;
using ComprobantesElectronicosBC.Application.DTOs;
using ComprobantesElectronicosBC.Application.Interfaces;
using ComprobantesElectronicosBC.Application.ReadModels;

namespace ComprobantesElectronicosBC.Application.UseCases
{
    /// <summary>
    /// Lista comprobantes con filtros de fecha y ordenamiento, devolviendo un modelo de
    /// paginación estable (TotalItems, TotalPages, HasPrevious/HasNext, etc.).
    /// 
    /// - No impone filtros obligatorios: si no se pasan, delega a la infraestructura (query repo).
    /// - Normaliza entrada: PageNumber/PageSize, rango de fechas (swaps si Hasta &lt; Desde),
    ///   y el SortDirection a "ASC" o "DESC" (default "DESC" por antigüedad).
    /// - Retorna <see cref="ListarComprobantesOutputDto"/> con la página y metadatos.
    /// </summary>
    public sealed class ListarComprobantesUseCase
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        private readonly IComprobanteQueryRepository _queryRepo;

        public ListarComprobantesUseCase(IComprobanteQueryRepository queryRepo)
        {
            _queryRepo = queryRepo ?? throw new ArgumentNullException(nameof(queryRepo));
        }

        public async Task<ListarComprobantesOutputDto> ExecuteAsync(
            ListarComprobantesInputDto input,
            CancellationToken ct = default)
        {
            input ??= new ListarComprobantesInputDto();

            // ------- Normalización de paginación -------
            var pageNumber = input.PageNumber <= 0 ? 1 : input.PageNumber;
            var pageSize   = input.PageSize   <= 0 ? DefaultPageSize
                             : (input.PageSize > MaxPageSize ? MaxPageSize : input.PageSize);

            // ------- Normalización de fechas (swap si el rango llega invertido) -------
            DateOnly? desde = input.Desde;
            DateOnly? hasta = input.Hasta;
            if (desde.HasValue && hasta.HasValue && hasta.Value < desde.Value)
            {
                (desde, hasta) = (hasta, desde);
            }

            // ------- Normalización de orden -------
            var sortBy = string.IsNullOrWhiteSpace(input.SortBy)
                ? "IssueDate"                       // nombre lógico que manejará la infra
                : input.SortBy!.Trim();

            var sortDirRaw = string.IsNullOrWhiteSpace(input.SortDirection)
                ? "DESC"
                : input.SortDirection!.Trim().ToUpperInvariant();

            var sortDesc = sortDirRaw != "ASC";     // cualquier cosa que no sea ASC => DESC

            // ------- Consulta al puerto de lectura -------
            // Nota: este puerto devuelve (items, total). Las transformaciones de mapeo a
            // ComprobanteResumenDto las realiza la infraestructura de consultas.
            var (items, total) = await _queryRepo.ListarAsync(
                desde:      desde,
                hasta:      hasta,
                sortBy:     sortBy,
                sortDesc:   sortDesc,
                pageNumber: pageNumber,
                pageSize:   pageSize,
                ct:         ct);

            // ------- Cálculo de metadatos de paginación -------
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
            if (pageNumber > totalPages) pageNumber = totalPages;

            // ------- Salida canónica -------
            var output = new ListarComprobantesOutputDto
            {
                Items           = items,                 // IReadOnlyList<ComprobanteResumenDto>
                TotalItems      = total,
                PageNumber      = pageNumber,
                PageSize        = pageSize,
                TotalPages      = totalPages,
                HasPreviousPage = pageNumber > 1,
                HasNextPage     = pageNumber < totalPages,
                SortBy          = sortBy,
                SortDirection   = sortDesc ? "DESC" : "ASC"
            };

            return output;
        }
    }
}
