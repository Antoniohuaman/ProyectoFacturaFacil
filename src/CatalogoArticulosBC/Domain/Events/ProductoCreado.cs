using SharedKernel.ValueObjects;
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
        public SKU Sku { get; }
        public TipoProducto TipoProducto { get; }

        // Datos básicos
        public NombreProducto Nombre { get; }
        public string Descripcion { get; }
        public UnidadDeMedida UnidadMedida { get; }
        public AfectacionIGV AfectacionIgv { get; }
        public Categoria Categoria { get; }
        public Marca? Marca { get; }

        // Precios y moneda
        public Precio? PrecioVenta { get; }
        public Moneda Moneda { get; }

        // Impuestos especiales
    public bool TieneDetraccion { get; }
    public CodigoDetraccion? CodigoDetraccion { get; }

        // Códigos adicionales
        public CodigoSUNAT? CodigoSunat { get; }
        public BaseImponibleVentas? BaseImponibleVentas { get; }
    public SharedKernel.ValueObjects.CentroDeCosto? CentroDeCosto { get; }
        public CodigoBarras? CodigoBarras { get; }
        public CodigoFabrica? CodigoFabrica { get; }
        public CodigoLote? CodigoLote { get; }

        // Logística e inventario
        public Peso? Peso { get; }
        public Serie? Serie { get; }
        public TipoExistencia TipoExistencia { get; }
        public FechaVencimiento? FechaVencimiento { get; }
        public IReadOnlyCollection<Guid> AlmacenesAsignados { get; }
        public bool AsignarATodosLosAlmacenes { get; }

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
            Sku = producto.Sku;
            TipoProducto = producto.Tipo;

            Nombre = producto.Nombre;
            Descripcion = producto.Descripcion;
            UnidadMedida = producto.UnidadMedida;
            AfectacionIgv = producto.AfectacionIgv;
            Categoria = producto.Categoria;
            Marca = producto.Marca;

            PrecioVenta = producto.PrecioVenta;
            Moneda = producto.Moneda;

            TieneDetraccion = producto.TieneDetraccion;
            CodigoDetraccion = producto.CodigoDetraccion;
            TieneDetraccion = producto.TieneDetraccion;
            CodigoDetraccion = producto.CodigoDetraccion;

            CodigoSunat = producto.CodigoSunat;
            BaseImponibleVentas = producto.BaseImponibleVentas;
            CentroDeCosto = producto.CentroDeCosto;
            CodigoBarras = producto.CodigoBarras;
            CodigoFabrica = producto.CodigoFabrica;
            CodigoLote = producto.CodigoLote;

            Peso = producto.Peso;
            Serie = producto.Serie;
            TipoExistencia = producto.TipoExistencia;
            FechaVencimiento = producto.FechaVencimiento;
            AlmacenesAsignados = producto.AlmacenesAsignados.AsReadOnly();
            AsignarATodosLosAlmacenes = producto.AsignarATodosLosAlmacenes;

            ImagenPrincipalId = producto.ImagenPrincipalId;
        }
    }
}
