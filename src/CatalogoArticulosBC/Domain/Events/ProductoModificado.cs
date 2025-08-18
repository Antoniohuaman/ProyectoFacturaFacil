using SharedKernel.ValueObjects;
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
    public sealed class ProductoModificado : DomainEvent
    {
        // Identidad
        public Guid ProductoId { get; }

        // Clave de negocio (puede cambiar si se permite)
        public SKU Sku { get; }

        // Datos básicos
        public NombreProducto Nombre { get; }
        public string Descripcion { get; }
    public UnidadDeMedida UnidadMedida { get; }
    public AfectacionImpuesto AfectacionImpuesto { get; }
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
        /// Crea un evento de dominio con el estado completo del agregado positivo tras su modificación.
        /// </summary>
        /// <param name="producto">Instancia del agregado modificado.</param>
        public ProductoModificado(ProductoSimple producto)
        {
            if (producto == null) throw new ArgumentNullException(nameof(producto));

            // Identidad
            ProductoId = producto.ProductoId;
            Sku = producto.Sku;

            // Datos básicos
            Nombre = producto.Nombre;
            Descripcion = producto.Descripcion;
            UnidadMedida = producto.UnidadMedida;
            AfectacionImpuesto = producto.AfectacionImpuesto;
            Categoria = producto.Categoria;
            Marca = producto.Marca;

            // Precios y moneda
            PrecioVenta = producto.PrecioVenta;
            Moneda = producto.Moneda;

            // Impuestos especiales
            TieneDetraccion = producto.TieneDetraccion;
            CodigoDetraccion = producto.CodigoDetraccion;

            // Códigos adicionales
            CodigoSunat = producto.CodigoSunat;
            BaseImponibleVentas = producto.BaseImponibleVentas;
            CentroDeCosto = producto.CentroDeCosto;
            CodigoBarras = producto.CodigoBarras;
            CodigoFabrica = producto.CodigoFabrica;
            CodigoLote = producto.CodigoLote;

            // Logística e inventario
            Peso = producto.Peso;
            Serie = producto.Serie;
            TipoExistencia = producto.TipoExistencia;
            FechaVencimiento = producto.FechaVencimiento;
            AlmacenesAsignados = producto.AlmacenesAsignados.AsReadOnly();
            AsignarATodosLosAlmacenes = producto.AsignarATodosLosAlmacenes;

            // Multimedia
            ImagenPrincipalId = producto.ImagenPrincipalId;
        }
    }
}
