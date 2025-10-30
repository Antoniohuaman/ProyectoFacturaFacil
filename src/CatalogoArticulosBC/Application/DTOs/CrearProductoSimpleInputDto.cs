using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Application.UseCases.CrearProductoSimple
{
    /// <summary>
    /// Datos de entrada para crear un ProductoSimple.
    /// Contiene campos obligatorios y opcionales. 
    /// NOTA: Moneda DEBE provenir de la configuración de empresa (aplicación/infra).
    /// </summary>
    public sealed class CrearProductoSimpleInputDto
    {
        // --- Identidad / SKU ---
        public bool AutogenerarSku { get; init; } = false;
        public string? Sku { get; init; } // requerido si AutogenerarSku = false

        // --- Datos básicos obligatorios ---
        public string Nombre { get; init; } = default!;
        public string UnidadMedidaCodigo { get; init; } = default!;
        /// <summary>Código Catálogo 07: "10","20","21","30"…</summary>
        public string AfectacionImpuestoCodigo { get; init; } = default!;
        /// <summary>
        /// Porcentaje de la tasa: 18 => 18%, 10 => 10%, 0 => 0%.
        /// Si la afectación NO grava impuesto, puede omitirse (se asume 0).
        /// Si la afectación GRAVA impuesto, DEBE ser 10 o 18.
        /// </summary>
        public decimal? TasaImpuestoPercent { get; init; }
        public string Categoria { get; init; } = default!;
        public string MonedaCodigoIso4217 { get; init; } = "PEN"; // normalmente viene de Configuración

        /// <summary>Si es Bien o Servicio.</summary>
        public TipoProducto Tipo { get; init; } = TipoProducto.Bien;

        /// <summary>Si no se envía, se deduce: Servicios para Tipo=Servicio; ProductosTerminados para Bien.</summary>
        public TipoExistencia? TipoExistencia { get; init; }

        /// <summary>Debe contener al menos un establecimiento.</summary>
        public List<Guid> Establecimientos { get; init; } = new();

        public bool AsignarATodosLosEstablecimientos { get; init; } = false;

        // --- Opcionales ---
        public string? Descripcion { get; init; }
        public string? Marca { get; init; }

        public decimal? PrecioVentaMonto { get; init; }
        public bool PrecioIncluyeIGV { get; init; } = true;

        public string? CodigoSUNAT { get; init; }           // 8 dígitos o omitido
        public string? CentroDeCostoCodigo { get; init; }   // si envías código, el nombre es obligatorio
        public string? CentroDeCostoNombre { get; init; }
        public decimal? PesoKg { get; init; }
        public string? CodigoBarras { get; init; }
        public string? CodigoFabrica { get; init; }
        public Guid? ImagenPrincipalId { get; init; }

        // Nuevos opcionales (compatibilidad hacia afuera)
        public decimal? PrecioCompraMonto { get; init; }      // Monto de compra
        public string? PrecioCompraMoneda { get; init; }      // Código ISO-4217, si no se envía se usa MonedaCodigoIso4217
        public decimal? PorcentajeGanancia { get; init; }     // 0..100
        public string? Alias { get; init; }

        // Backward-compat: campos antiguos (si existieran payloads previos). No documentar públicamente.
        public decimal? PrecioCompra { get; init; }
    }
}
