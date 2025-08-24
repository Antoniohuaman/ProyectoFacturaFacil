using SharedKernel.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Exceptions;
using CatalogoArticulosBC.Domain.Services;


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
        public Sku Sku { get; private set; }

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
        // Propiedad TieneDetraccion eliminada

        // Códigos adicionales
        public CodigoSUNAT? CodigoSunat { get; private set; }
        public CentroDeCosto? CentroDeCosto { get; private set; }
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
        private readonly List<SharedKernel.Events.IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<SharedKernel.Events.IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
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
            Sku sku,
            NombreProducto nombre,
            UnidadDeMedida unidadMedida,
            AfectacionImpuesto afectacionImpuesto,
            Categoria categoria,
            List<Guid>? almacenesAsignados,
            string? descripcion = null,
            Marca? marca = null,
            PrecioVenta? precioVenta = null,
            // bool tieneDetraccion eliminado
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

            

            if (almacenesAsignados == null || !almacenesAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un almacén.", nameof(almacenesAsignados));

            // Asignaciones
            ProductoId = Guid.NewGuid();
            Activo = true;
            Marca = marca;
            PrecioVenta = precioVenta;
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda), "La moneda debe provenir de la configuración de empresa.");
            // TieneDetraccion eliminado
            // CodigoDetraccion eliminado
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
            // bool tieneDetraccion eliminado
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

            // Si aplica detracción, ya no se requiere código

            if (almacenesAsignados == null || !almacenesAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un almacén.", nameof(almacenesAsignados));

            // Asignaciones
            Marca = marca;
            PrecioVenta = precioVenta;
            CodigoSunat = codigoSunat;
            CentroDeCosto = centroDeCosto;
            Peso = peso;
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            Tipo = tipo;
            TipoExistencia = tipoExistencia;
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

        public void Habilitar(string usuario, string? motivo = null)
        {
            Activo = true;
            var ev = new ProductoHabilitado(ProductoId, usuario, motivo);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void CambiarCategoria(Categoria nuevaCategoria, string usuario)
        {
            if (nuevaCategoria == null) throw new ArgumentNullException(nameof(nuevaCategoria));
            var categoriaAnterior = Categoria?.Nombre ?? string.Empty;
            var categoriaNueva = nuevaCategoria.Nombre;
            Categoria = nuevaCategoria;
            var ev = new ProductoCategoriaCambiada(ProductoId, categoriaAnterior, categoriaNueva, usuario);
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
            // Emitir evento de dominio
            var ev = new CatalogoArticulosBC.Domain.Events.MultimediaAgregada(ProductoId, media.MultimediaId);
            AddDomainEvent(ev);
        }

        public void EliminarMultimedia(Guid multimediaId)
        {
            var media = _multimedia.FirstOrDefault(m => m.MultimediaId == multimediaId)
                        ?? throw new InvalidOperationException("Multimedia no encontrada.");
            _multimedia.Remove(media);
            // Emitir evento de dominio
            var ev = new CatalogoArticulosBC.Domain.Events.MultimediaEliminada(ProductoId, multimediaId);
            AddDomainEvent(ev);
        }

        private void AddDomainEvent(SharedKernel.Events.IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        private bool EsTipoPermitido(string tipo) =>
            new[] { "image/jpeg", "image/png", "application/pdf" }
            .Contains(tipo, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Asigna el SKU manualmente (por el usuario).
        /// </summary>
        public void AsignarSku(Sku sku)
        {
            if (sku == null) throw new ArgumentNullException(nameof(sku));
            this.Sku = sku;
            // Emitir evento de dominio
            var ev = new CatalogoArticulosBC.Domain.Events.SkuCambiado(ProductoId, sku);
            AddDomainEvent(ev);
        }

        /// <summary>
        /// Genera y asigna el SKU automáticamente usando un generador externo.
        /// </summary>
        public void GenerarSku(ISkuGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            var nuevoSku = generator.Generar();
            this.Sku = nuevoSku;
            // Emitir evento de dominio
            var ev = new CatalogoArticulosBC.Domain.Events.SkuCambiado(ProductoId, nuevoSku);
            AddDomainEvent(ev);
        }
        /// <summary>
        /// Elimina toda la multimedia asociada al producto.
        /// </summary>
        public void LimpiarMultimedia()
        {
            foreach (var media in _multimedia.ToList())
            {
                EliminarMultimedia(media.MultimediaId);
            }
        }
    }
}
