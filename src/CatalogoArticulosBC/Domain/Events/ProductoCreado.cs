using SharedKernel.ValueObjects;
using SharedKernel.Events;
using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando se crea un nuevo ProductoSimple.
    /// Incluye el estado completo del agregado en el momento de la creación.
    /// </summary>
    public sealed class ProductoCreado : DomainEvent
    {
        // Identidad
    public Guid ProductoId { get; }
    public ProductoId ProductoIdVO => new ProductoId(ProductoId);
    public EmpresaId EmpresaId { get; }
    public Sku Sku { get; }
        public TipoProducto TipoProducto { get; }

        // Datos básicos
        public NombreProducto Nombre { get; }
        public string Descripcion { get; }
        public UnidadDeMedida UnidadMedida { get; }
    public AfectacionImpuesto AfectacionImpuesto { get; }
        public CategoriaId? CategoriaId { get; }
        public string? CategoriaNombreSnapshot { get; }
        public string? CategoriaColorSnapshot { get; }
        public Marca? Marca { get; }

        // Precios y moneda
    public PrecioVenta? PrecioVenta { get; }
        public Moneda Moneda { get; }

        // Impuestos especiales
    // Propiedad TieneDetraccion eliminada

        // Códigos adicionales
        public CodigoSUNAT? CodigoSunat { get; }
    // BaseImponibleVentas eliminado
    public SharedKernel.ValueObjects.CentroDeCosto? CentroDeCosto { get; }
        public CodigoBarras? CodigoBarras { get; }
        public CodigoFabrica? CodigoFabrica { get; }
    // ...existing code...

        // Logística e inventario
    public Peso? Peso { get; }
    // ...existing code...
    public TipoExistencia TipoExistencia { get; }
    public IReadOnlyCollection<EstablecimientoId> EstablecimientosAsignados { get; }
    public bool AsignarATodosLosEstablecimientos { get; }

        // Multimedia
        public Guid? ImagenPrincipalId { get; }

        /// <summary>
        /// Crea un evento de dominio con el estado completo del agregado ProductoSimple.
        /// </summary>
        /// <param name="producto">Instancia del agregado recién creado.</param>
        public ProductoCreado(ProductoSimple producto)
        {
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            ProductoId = producto.ProductoId;
            EmpresaId = producto.EmpresaId;
            Sku = producto.Sku;
            TipoProducto = producto.Tipo;

            Nombre = producto.Nombre;
            Descripcion = producto.Descripcion;
            UnidadMedida = producto.UnidadMedida;
            AfectacionImpuesto = producto.AfectacionImpuesto;
            CategoriaId = producto.CategoriaId;
            CategoriaNombreSnapshot = producto.CategoriaNombreSnapshot;
            CategoriaColorSnapshot = producto.CategoriaColorSnapshot;
            Marca = producto.Marca;

            PrecioVenta = producto.PrecioVenta;
            Moneda = producto.Moneda;

            // TieneDetraccion eliminado
            // CodigoDetraccion eliminado

            CodigoSunat = producto.CodigoSunat;
            // BaseImponibleVentas eliminado
            CentroDeCosto = producto.CentroDeCosto;
            CodigoBarras = producto.CodigoBarras;
            CodigoFabrica = producto.CodigoFabrica;
            // ...existing code...

            Peso = producto.Peso;
            // ...existing code...
            TipoExistencia = producto.TipoExistencia;
            EstablecimientosAsignados = producto.EstablecimientosAsignados.AsReadOnly();
            AsignarATodosLosEstablecimientos = producto.AsignarATodosLosEstablecimientos;

            ImagenPrincipalId = producto.ImagenPrincipalId;
        }
    }
}
