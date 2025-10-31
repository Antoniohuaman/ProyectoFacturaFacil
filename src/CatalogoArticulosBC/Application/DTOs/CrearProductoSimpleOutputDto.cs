using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.CrearProductoSimple
{
    /// <summary>
    /// Resultado de la creación del producto. Útil para devolver al UI/API.
    /// </summary>
    public sealed class CrearProductoSimpleOutputDto
    {
        public Guid ProductoId { get; init; }

        public string Sku { get; init; } = default!;
        public bool Habilitado { get; init; }

        public TipoProducto Tipo { get; init; }
        public TipoExistencia TipoExistencia { get; init; }

        public string Nombre { get; init; } = default!;
    public string Descripcion { get; init; } = string.Empty;

    // Migración: exponer CategoriaId y snapshots
    public string? CategoriaId { get; init; }
    public string? CategoriaNombre { get; init; }
    public string? CategoriaColor { get; init; }

        public string Moneda { get; init; } = default!;
        public decimal? PrecioVentaMonto { get; init; }
        public bool? PrecioIncluyeIGV { get; init; }

        public string AfectacionImpuestoCodigo { get; init; } = default!;
        public decimal TasaImpuestoPercent { get; init; }

        public List<Guid> Establecimientos { get; init; } = new();
        public bool AsignarATodosLosEstablecimientos { get; init; }

        public Guid? ImagenPrincipalId { get; init; }

        // Nuevos opcionales
        public decimal? PrecioCompraMonto { get; init; }     // en la Moneda del producto
        public string? PrecioCompraMoneda { get; init; }
        public decimal? PorcentajeGanancia { get; init; }     // 0..100
        public string? Alias { get; init; }
    }
}
