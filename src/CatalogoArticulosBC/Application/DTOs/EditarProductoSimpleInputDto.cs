using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.EditarProductoSimple
{
    /// <summary>
    /// DTO de entrada para editar un ProductoSimple.
    /// </summary>
    public sealed class EditarProductoSimpleInputDto
    {
        // Identificador del agregado
        public Guid ProductoId { get; init; }

        // (Opcional) Cambio de SKU: si se envía y difiere del actual, se valida unicidad y se actualiza.
        public string? NuevoSku { get; init; }

        // Datos obligatorios del agregado según tu método EditarDatos(...)
        public string Nombre { get; init; } = default!;
        public string UnidadMedidaCodigo { get; init; } = default!;
        public string AfectacionImpuestoCodigo { get; init; } = default!;
    /// <summary>Porcentaje de la tasa (0, 10, 18, etc.). Dominios soportan validación.</summary>
        public decimal TasaImpuestoPorcentaje { get; init; }
    // Migración: ahora se espera CategoriaId y snapshots opcionales
    public string CategoriaId { get; init; } = default!;
    public string? CategoriaNombreSnapshot { get; init; }
    public string? CategoriaColorSnapshot { get; init; }

        // Datos opcionales
        public string? Descripcion { get; init; }
        public string? MarcaNombre { get; init; }

        // Precio (opcional): si no se envía monto, no se cambia el PrecioVenta
        public decimal? PrecioVentaMonto { get; init; }
        public bool PrecioIncluyeIGV { get; init; } = true;

        // SUNAT / Códigos adicionales
        public string? CodigoSunat { get; init; }
        public string? CodigoBarras { get; init; }
        public string? CodigoFabrica { get; init; }

        // Centro de costo (opcional: requiere code + name si se informa)
        public string? CentroDeCostoCodigo { get; init; }
        public string? CentroDeCostoNombre { get; init; }

        // Logística / inventario (opcionales)
        public decimal? PesoKg { get; init; }

        public TipoProducto TipoProducto { get; init; } = TipoProducto.Bien;
        public TipoExistencia? TipoExistencia { get; init; } // si no viene, se conserva la actual

        // Establecimientos
        public List<Guid> EstablecimientosAsignados { get; init; } = new();
        public bool AsignarATodosLosEstablecimientos { get; init; }
        public Guid? ImagenPrincipalId { get; init; }

        // Nuevos opcionales
        public decimal? PrecioCompraMonto { get; init; }
        public string? PrecioCompraMoneda { get; init; }
        public decimal? PorcentajeGanancia { get; init; }     // 0..100
        public string? Alias { get; init; }
    }
}
