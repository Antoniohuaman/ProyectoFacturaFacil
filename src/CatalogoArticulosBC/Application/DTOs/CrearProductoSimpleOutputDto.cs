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

        public string Categoria { get; init; } = default!;

        public string Moneda { get; init; } = default!;
        public decimal? PrecioVentaMonto { get; init; }
        public bool? PrecioIncluyeIGV { get; init; }

        public string AfectacionImpuestoCodigo { get; init; } = default!;
        public decimal TasaImpuestoPercent { get; init; }

        public List<Guid> Establecimientos { get; init; } = new();
        public bool AsignarATodosLosEstablecimientos { get; init; }

        public Guid? ImagenPrincipalId { get; init; }
    }
}
