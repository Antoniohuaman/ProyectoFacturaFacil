using System;

namespace ComprobantesElectronicosBC.Application.DTOs
{
    /// <summary>
    /// Filtros + orden + paginación para listar comprobantes (proyección de resumen).
    /// Todos los filtros son OPCIONALES. PageNumber/PageSize tienen defaults.
    /// </summary>
    public sealed record ListarComprobantesInputDto
    {
        // ------- Filtros frecuentes -------
        public string? TipoCodigo { get; init; }          // "01" Factura, "03" Boleta
        public string? Serie { get; init; }               // ej. "F001" / "B001"
        public int?    Numero { get; init; }              // correlativo (1..99'999'999)
        public string? SerieNumero { get; init; }         // atajo: "F001-00000001"
        public DateOnly? Desde { get; init; }             // IssueDate desde
        public DateOnly? Hasta { get; init; }             // IssueDate hasta
        public string? ClienteDocTipo { get; init; }      // Cat.06 ("6","1","7",..)
        public string? ClienteDocNumero { get; init; }    // "20123456789", "42345678",..
        public string? Estado { get; init; }              // Borrador/Emitido/Anulado (si manejas estados)
        public string? Moneda { get; init; }              // "PEN","USD",...

        // ------- Ordenamiento -------
        /// <summary>Nombre de la columna de orden. Si es null, el UC usará "FechaEmision".</summary>
        public string? SortBy { get; init; }
        /// <summary>"ASC" o "DESC". Si es null, el UC usará "DESC".</summary>
        public string? SortDirection { get; init; }

        // ------- Paginación -------
        public int PageNumber { get; init; } = 1;         // 1-based
        public int PageSize  { get; init; } = 20;         // límite razonable; el UC valida máximo
    }
}
