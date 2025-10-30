using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.EditarProductoSimple
{
    /// <summary>
    /// DTO de salida con los datos relevantes tras la edición.
    /// </summary>
    public sealed class EditarProductoSimpleOutputDto
    {
        public Guid ProductoId { get; init; }
        public string Sku { get; init; } = default!;
        public string Nombre { get; init; } = default!;
        public bool Habilitado { get; init; }
        public TipoProducto TipoProducto { get; init; }
        public string Categoria { get; init; } = default!;
        public string AfectacionImpuestoCodigo { get; init; } = default!;
        public decimal TasaImpuestoFraccion { get; init; }
        public decimal? PrecioVentaMonto { get; init; }
        public bool? PrecioIncluyeIGV { get; init; }
        public string MonedaCodigo { get; init; } = default!;
        public TipoExistencia TipoExistencia { get; init; }
        public List<Guid> EstablecimientosAsignados { get; init; } = new();

        // Nuevos campos opcionales
        public decimal? PrecioCompraMonto { get; init; }
        public string? PrecioCompraMoneda { get; init; }
        public decimal? PorcentajeGanancia { get; init; }
        public string? Alias { get; init; }
    }
}
