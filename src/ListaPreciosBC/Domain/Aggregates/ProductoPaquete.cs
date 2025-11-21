using System;
using System.Collections.Generic;
using System.Linq;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Aggregates
{
    /// <summary>
    /// Representa un paquete de productos con un porcentaje de descuento sobre el subtotal.
    /// </summary>
    public sealed class ProductoPaquete
    {
        private readonly List<LineaProductoPaquete> _productos;

        public Guid Id { get; }
        public EmpresaId EmpresaId { get; }
        public NombrePaquete Nombre { get; private set; }
        public string? Descripcion { get; private set; }
        public PorcentajeDescuentoPaquete Descuento { get; private set; }
        public DateTime FechaCreacionUtc { get; }

        public IReadOnlyCollection<LineaProductoPaquete> Productos => _productos.AsReadOnly();

        public decimal Subtotal => _productos.Sum(p => p.Subtotal);

        public decimal DescuentoMonto => Descuento.CalcularDescuento(Subtotal);

        public decimal Total => Subtotal - DescuentoMonto;

        private ProductoPaquete(
            Guid id,
            EmpresaId empresaId,
            NombrePaquete nombre,
            PorcentajeDescuentoPaquete descuento,
            string? descripcion,
            IEnumerable<LineaProductoPaquete> productos,
            DateTime fechaCreacionUtc)
        {
            Id = id;
            EmpresaId = empresaId;
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descuento = descuento ?? throw new ArgumentNullException(nameof(descuento));
            Descripcion = descripcion;
            FechaCreacionUtc = fechaCreacionUtc;

            if (productos is null)
            {
                throw new ArgumentNullException(nameof(productos));
            }

            _productos = productos.ToList();

            if (_productos.Count == 0)
            {
                throw new InvalidOperationException(
                    "Un paquete de productos debe contener al menos un producto.");
            }
        }

        public static ProductoPaquete Crear(
            EmpresaId empresaId,
            Guid paqueteId,
            NombrePaquete nombre,
            PorcentajeDescuentoPaquete descuento,
            string? descripcion,
            IEnumerable<LineaProductoPaquete> productos,
            DateTime? fechaCreacionUtc = null)
        {
            var fecha = fechaCreacionUtc ?? DateTime.UtcNow;
            return new ProductoPaquete(paqueteId, empresaId, nombre, descuento, descripcion, productos, fecha);
        }

        public void ReemplazarProductos(IEnumerable<LineaProductoPaquete> nuevosProductos)
        {
            if (nuevosProductos is null)
            {
                throw new ArgumentNullException(nameof(nuevosProductos));
            }

            var lista = nuevosProductos.ToList();

            if (lista.Count == 0)
            {
                throw new InvalidOperationException(
                    "Un paquete de productos debe contener al menos un producto.");
            }

            _productos.Clear();
            _productos.AddRange(lista);
        }

        public void ActualizarNombre(NombrePaquete nuevoNombre)
        {
            Nombre = nuevoNombre ?? throw new ArgumentNullException(nameof(nuevoNombre));
        }

        public void ActualizarDescripcion(string? nuevaDescripcion)
        {
            Descripcion = nuevaDescripcion;
        }

        public void ActualizarDescuento(PorcentajeDescuentoPaquete nuevoDescuento)
        {
            Descuento = nuevoDescuento ?? throw new ArgumentNullException(nameof(nuevoDescuento));
        }

        public static LineaProductoPaquete CrearLinea(
            ProductoId productoId,
            UnidadDeMedida unidadDeMedida,
            CantidadProductoPaquete cantidad,
            decimal precioUnitario)
        {
            return new LineaProductoPaquete(productoId, unidadDeMedida, cantidad, precioUnitario);
        }

        public sealed class LineaProductoPaquete
        {
            public ProductoId ProductoId { get; }
            public UnidadDeMedida UnidadDeMedida { get; }
            public CantidadProductoPaquete Cantidad { get; }
            public decimal PrecioUnitario { get; }

            public decimal Subtotal => PrecioUnitario * Cantidad.Valor;

            internal LineaProductoPaquete(
                ProductoId productoId,
                UnidadDeMedida unidadDeMedida,
                CantidadProductoPaquete cantidad,
                decimal precioUnitario)
            {
                // ProductoId y UnidadDeMedida pueden ser struct en SharedKernel,
                // por eso no usamos el operador ?? aquí.
                ProductoId = productoId;
                UnidadDeMedida = unidadDeMedida;
                Cantidad = cantidad ?? throw new ArgumentNullException(nameof(cantidad));

                if (precioUnitario < 0m)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(precioUnitario),
                        "El precio unitario no puede ser negativo.");
                }

                PrecioUnitario = decimal.Round(precioUnitario, 2, MidpointRounding.AwayFromZero);
            }
        }
    }
}
