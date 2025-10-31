using SharedKernel.ValueObjects;
using SharedKernel.Events;
using System;
using System.Collections.Generic;
using CatalogoArticulosBC.Domain.Aggregates;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Events
{
    /// <summary>
    /// Evento que se dispara cuando un ProductoSimple es modificado.
    /// Incluye el estado completo del agregado tras la modificación.
    /// </summary>
    public sealed class ProductoActualizado : DomainEvent
    {
        // Identidad
        public Guid ProductoId { get; }
        public EmpresaId EmpresaId { get; }

    // Clave de negocio (puede cambiar si se permite)
    public Sku Sku { get; }

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
        /// Crea un evento de dominio con el estado completo del agregado positivo tras su modificación.
        /// </summary>
        /// <param name="producto">Instancia del agregado modificado.</param>
    public ProductoActualizado(ProductoSimple producto)
        {
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            // Identidad
            ProductoId = producto.ProductoId;
            EmpresaId = producto.EmpresaId;
            Sku = producto.Sku;

            // Datos básicos
            Nombre = producto.Nombre;
            Descripcion = producto.Descripcion;
            UnidadMedida = producto.UnidadMedida;
            AfectacionImpuesto = producto.AfectacionImpuesto;
            CategoriaId = producto.CategoriaId;
            CategoriaNombreSnapshot = producto.CategoriaNombreSnapshot;
            CategoriaColorSnapshot = producto.CategoriaColorSnapshot;
            Marca = producto.Marca;

            // Precios y moneda
            PrecioVenta = producto.PrecioVenta;
            Moneda = producto.Moneda;

            // Impuestos especiales
            // TieneDetraccion eliminado
            // CodigoDetraccion eliminado

            // Códigos adicionales
            CodigoSunat = producto.CodigoSunat;
            // BaseImponibleVentas eliminado
            CentroDeCosto = producto.CentroDeCosto;
            CodigoBarras = producto.CodigoBarras;
            CodigoFabrica = producto.CodigoFabrica;
            // ...existing code...

            // Logística e inventario
            Peso = producto.Peso;
            // ...existing code...
            TipoExistencia = producto.TipoExistencia;
            EstablecimientosAsignados = producto.EstablecimientosAsignados.AsReadOnly();
            AsignarATodosLosEstablecimientos = producto.AsignarATodosLosEstablecimientos;

            // Multimedia
            ImagenPrincipalId = producto.ImagenPrincipalId;
        }
    }
}
