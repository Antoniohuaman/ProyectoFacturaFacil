    using SharedKernel.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Exceptions;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public enum TipoProducto
    {
        Bien,
        Servicio
    }

    public class ProductoSimple
    {
        // Identidad y estado
        public Guid ProductoId { get; private set; }
        public bool Activo { get; private set; } = true;

        // Clave de negocio
        public SKU Sku { get; private set; }

        // Datos básicos
        public NombreProducto Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public UnidadDeMedida UnidadMedida { get; private set; }
    public AfectacionImpuesto AfectacionImpuesto { get; private set; }
        public Categoria Categoria { get; private set; }
        public Marca? Marca { get; private set; }

        // Precios y moneda
    public PrecioVenta? PrecioVenta { get; private set; }
        public Moneda Moneda { get; private set; }

        // Impuestos especiales
        public bool TieneDetraccion { get; private set; }
        public CodigoDetraccion? CodigoDetraccion { get; private set; }

        // Códigos adicionales
    public CodigoSUNAT? CodigoSunat { get; private set; }
    public SharedKernel.ValueObjects.CentroDeCosto? CentroDeCosto { get; private set; }
        public CodigoBarras? CodigoBarras { get; private set; }
        public CodigoFabrica? CodigoFabrica { get; private set; }
    // ...existing code...
    
        // Logística e inventario
    public Peso? Peso { get; private set; }
    // ...existing code...
    public TipoExistencia TipoExistencia { get; private set; }
    // ...existing code...
        public List<Guid> AlmacenesAsignados { get; private set; }
        public bool AsignarATodosLosAlmacenes { get; private set; }

        // Multimedia
        private readonly List<MultimediaProducto> _multimedia = new();
        public IReadOnlyCollection<MultimediaProducto> Multimedia => _multimedia.AsReadOnly();
        public Guid? ImagenPrincipalId { get; private set; }

        // <-- colección para eventos de dominio
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        // FIN 

        // Tipo de producto
        public TipoProducto Tipo { get; private set; }

        // Peso
        public decimal PesoValor => Peso?.Valor ?? 0m;

        /// <summary>
        /// Constructor principal para crear un ProductoSimple con todos sus VOs.
        /// </summary>
        public ProductoSimple(
            Moneda moneda,
            SKU sku,
            NombreProducto nombre,        
            UnidadDeMedida unidadMedida,
            AfectacionImpuesto afectacionImpuesto,
            Categoria categoria,
            List<Guid>? almacenesAsignados,
            string? descripcion = null,
            Marca? marca = null,
            PrecioVenta? precioVenta = null,
            bool tieneDetraccion = false,
            CodigoDetraccion? codigoDetraccion = null,
            CodigoSUNAT? codigoSunat = null,
            SharedKernel.ValueObjects.CentroDeCosto? centroDeCosto = null,
            Peso? peso = null,
            // ...existing code...
            CodigoBarras? codigoBarras = null,
            CodigoFabrica? codigoFabrica = null,
            // ...existing code...
            TipoProducto tipo = TipoProducto.Bien,
            TipoExistencia tipoExistencia = TipoExistencia.ProductosTerminados,
            // ...existing code...
            bool asignarATodosLosAlmacenes = false,
            Guid? imagenPrincipalId = null)
        {
            // Validaciones de parámetros obligatorios
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion = descripcion?.Trim() ?? string.Empty;
            UnidadMedida = unidadMedida ?? throw new ArgumentNullException(nameof(unidadMedida));
            AfectacionImpuesto = afectacionImpuesto ?? throw new ArgumentNullException(nameof(afectacionImpuesto));
            Categoria = categoria ?? throw new ArgumentNullException(nameof(categoria));

            if (tieneDetraccion && codigoDetraccion is null)
                throw new ArgumentException("Si aplica detracción, debe especificarse el código.", nameof(codigoDetraccion));

            if (almacenesAsignados == null || !almacenesAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un almacén.", nameof(almacenesAsignados));    

            // Asignaciones
            ProductoId = Guid.NewGuid();
            Activo = true;
            Marca = marca;
            PrecioVenta = precioVenta;
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda), "La moneda debe provenir de la configuración de empresa.");
            TieneDetraccion = tieneDetraccion;
            CodigoDetraccion = codigoDetraccion;
            CodigoSunat = codigoSunat;
            // BaseImponibleVentas eliminado
            CentroDeCosto = centroDeCosto;
            Peso = peso;
            // ...existing code...
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            // ...existing code...
            Tipo = tipo;
            TipoExistencia = tipoExistencia;
            // ...existing code...
            AlmacenesAsignados = almacenesAsignados;
            AsignarATodosLosAlmacenes = asignarATodosLosAlmacenes;
            ImagenPrincipalId = imagenPrincipalId;

            // Evento de dominio
            var ev = new ProductoCreado(this);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        /// <summary>
        /// Edita los datos básicos y VOs del producto.
        /// </summary>
        public void EditarDatos(
            NombreProducto nombre,
            UnidadDeMedida unidadMedida,
            AfectacionImpuesto afectacionImpuesto,
            Categoria categoria,
            Marca? marca,
            PrecioVenta? precioVenta,
            bool tieneDetraccion,
            CodigoDetraccion? codigoDetraccion,
            CodigoSUNAT? codigoSunat,
            SharedKernel.ValueObjects.CentroDeCosto? centroDeCosto,
            Peso? peso,
            // ...existing code...
            CodigoBarras? codigoBarras,
            CodigoFabrica? codigoFabrica,
            // ...existing code...
            TipoProducto tipo,
            // ...existing code...
            List<Guid>? almacenesAsignados = null,
            bool asignarATodosLosAlmacenes = false,
            Guid? imagenPrincipalId = null,
            string? descripcion = null,
            TipoExistencia tipoExistencia = TipoExistencia.ProductosTerminados)
        {
            // Validaciones
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion = descripcion?.Trim() ?? string.Empty;
            UnidadMedida = unidadMedida ?? throw new ArgumentNullException(nameof(unidadMedida));
            AfectacionImpuesto = afectacionImpuesto ?? throw new ArgumentNullException(nameof(afectacionImpuesto));
            Categoria = categoria ?? throw new ArgumentNullException(nameof(categoria));

            if (tieneDetraccion && codigoDetraccion is null)
                throw new ArgumentException("Si aplica detracción, debe especificarse el código.", nameof(codigoDetraccion));

            if (almacenesAsignados == null || !almacenesAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un almacén.", nameof(almacenesAsignados));

            // Asignaciones
            Marca = marca;
            PrecioVenta = precioVenta;
            TieneDetraccion = tieneDetraccion;
            CodigoDetraccion = codigoDetraccion;
            CodigoSunat = codigoSunat;
            // BaseImponibleVentas eliminado
            CentroDeCosto = centroDeCosto;
            Peso = peso;
            // ...existing code...
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            // ...existing code...
            Tipo = tipo;
            TipoExistencia = tipoExistencia;
            // ...existing code...
            AlmacenesAsignados = almacenesAsignados;
            AsignarATodosLosAlmacenes = asignarATodosLosAlmacenes;
            ImagenPrincipalId = imagenPrincipalId;

            var ev = new ProductoActualizado(this);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Activo = false;
            var ev = new ProductoInhabilitado(ProductoId, motivo);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void AsignarImagenPrincipal(Guid multimediaId)
        {
            if (!_multimedia.Any(m => m.MultimediaId == multimediaId))
                throw new InvalidOperationException("La imagen principal debe existir en multimedia.");
            ImagenPrincipalId = multimediaId;
        }

        public void AgregarMultimedia(MultimediaProducto media)
        {
            if (_multimedia.Count >= 5)
                throw new LimiteMultimediaException();
            if (!EsTipoPermitido(media.TipoMime))
                throw new MultimediaInvalidaException("Tipo no permitido.");
            _multimedia.Add(media);
        }

        public void EliminarMultimedia(Guid multimediaId)
        {
            var media = _multimedia.FirstOrDefault(m => m.MultimediaId == multimediaId)
                        ?? throw new InvalidOperationException("Multimedia no encontrada.");
            _multimedia.Remove(media);
        }

        private void AddDomainEvent(IDomainEvent domainEvent)
        {
        _domainEvents.Add(domainEvent);
        }

        private bool EsTipoPermitido(string tipo) =>
            new[] { "image/jpeg", "image/png", "application/pdf" }
            .Contains(tipo, StringComparer.OrdinalIgnoreCase);
    }
}
