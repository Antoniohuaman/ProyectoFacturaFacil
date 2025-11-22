using System;
using System.Collections.Generic;
using System.Linq;
using ListaPreciosBC.Domain.Events;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Events;
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Domain.Aggregates
{
    /// <summary>
    /// Representa un paquete de productos con un porcentaje de descuento sobre el subtotal.
    /// </summary>
    public sealed class ProductoPaquete
    {
        private readonly List<LineaProductoPaquete> _productos;
        private readonly List<IDomainEvent> _domainEvents = new();

        public Guid Id { get; }
        public EmpresaId EmpresaId { get; }
        public NombrePaquete Nombre { get; private set; }
        public string? Descripcion { get; private set; }
        public PorcentajeDescuentoPaquete Descuento { get; private set; }
        public DateTime FechaCreacionUtc { get; }
        public DateTime? FechaUltimaActualizacionUtc { get; private set; }
        public bool EstaEliminado { get; private set; }
        public DateTime? FechaEliminacionUtc { get; private set; }

        public IReadOnlyCollection<LineaProductoPaquete> Productos => _productos.AsReadOnly();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();

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
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descuento = descuento ?? throw new ArgumentNullException(nameof(descuento));
            Descripcion = descripcion;
            FechaCreacionUtc = fechaCreacionUtc;
            FechaUltimaActualizacionUtc = fechaCreacionUtc;

            _productos = new List<LineaProductoPaquete>();
            EstablecerProductos(productos);
        }

        private void EstablecerProductos(IEnumerable<LineaProductoPaquete> productos)
        {
            if (productos is null)
            {
                throw new ArgumentNullException(nameof(productos));
            }

            var lista = productos.ToList();

            if (lista.Count == 0)
            {
                throw new InvalidOperationException(
                    "Un paquete de productos debe contener al menos un producto.");
            }

            _productos.Clear();
            _productos.AddRange(lista);
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
            var fecha = (fechaCreacionUtc ?? DateTime.UtcNow).ToUniversalTime();
            var paquete = new ProductoPaquete(paqueteId, empresaId, nombre, descuento, descripcion, productos, fecha);
            paquete.EmitirPaqueteCreado(fecha);
            return paquete;
        }

        public void ReemplazarProductos(IEnumerable<LineaProductoPaquete> nuevosProductos)
        {
            EstablecerProductos(nuevosProductos);
            EmitirPaqueteActualizado();
        }

        public void ActualizarDatos(
            NombrePaquete nuevoNombre,
            string? nuevaDescripcion,
            PorcentajeDescuentoPaquete nuevoDescuento,
            IEnumerable<LineaProductoPaquete> nuevosProductos,
            DateTime? fechaActualizacionUtc = null)
        {
            Nombre = nuevoNombre ?? throw new ArgumentNullException(nameof(nuevoNombre));
            Descripcion = nuevaDescripcion;
            Descuento = nuevoDescuento ?? throw new ArgumentNullException(nameof(nuevoDescuento));
            EstablecerProductos(nuevosProductos);
            EmitirPaqueteActualizado(fechaActualizacionUtc);
        }

        public void ActualizarNombre(NombrePaquete nuevoNombre)
        {
            Nombre = nuevoNombre ?? throw new ArgumentNullException(nameof(nuevoNombre));
            EmitirPaqueteActualizado();
        }

        public void ActualizarDescripcion(string? nuevaDescripcion)
        {
            Descripcion = nuevaDescripcion;
            EmitirPaqueteActualizado();
        }

        public void ActualizarDescuento(PorcentajeDescuentoPaquete nuevoDescuento)
        {
            Descuento = nuevoDescuento ?? throw new ArgumentNullException(nameof(nuevoDescuento));
            EmitirPaqueteActualizado();
        }

        public void MarcarComoEliminado(DateTime? fechaEliminacionUtc = null)
        {
            if (EstaEliminado)
            {
                return;
            }

            EstaEliminado = true;
            EmitirPaqueteEliminado(fechaEliminacionUtc);
        }

        public static LineaProductoPaquete CrearLinea(
            ProductoId productoId,
            UnidadDeMedida unidadDeMedida,
            CantidadProductoPaquete cantidad,
            decimal precioUnitario)
        {
            return new LineaProductoPaquete(productoId, unidadDeMedida, cantidad, precioUnitario);
        }

        private void EmitirPaqueteCreado(DateTime fechaUtc)
        {
            var cuando = fechaUtc.Kind == DateTimeKind.Utc ? fechaUtc : fechaUtc.ToUniversalTime();
            FechaUltimaActualizacionUtc = cuando;
            _domainEvents.Add(new PaqueteCreado(EmpresaId, Id, Nombre, Descripcion, Descuento, occurredOnUtc: cuando));
        }

        private void EmitirPaqueteActualizado(DateTime? fechaActualizacionUtc = null)
        {
            var cuando = (fechaActualizacionUtc ?? DateTime.UtcNow).ToUniversalTime();
            FechaUltimaActualizacionUtc = cuando;
            _domainEvents.Add(new PaqueteActualizado(EmpresaId, Id, Nombre, Descripcion, Descuento, occurredOnUtc: cuando));
        }

        private void EmitirPaqueteEliminado(DateTime? fechaEliminacionUtc = null)
        {
            var cuando = (fechaEliminacionUtc ?? DateTime.UtcNow).ToUniversalTime();
            FechaEliminacionUtc = cuando;
            _domainEvents.Add(new PaqueteEliminado(EmpresaId, Id, Nombre, Descripcion, Descuento, occurredOnUtc: cuando));
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
