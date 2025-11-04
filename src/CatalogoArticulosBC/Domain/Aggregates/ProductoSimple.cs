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
    public ProductoId ProductoIdVO => new ProductoId(ProductoId);
    public bool Habilitado { get; private set; } = true;
    /// <summary>
    /// Empresa (tenant) propietaria del producto.
    /// </summary>
    public EmpresaId EmpresaId { get; private set; }

        // Clave de negocio
        public Sku Sku { get; private set; }

        // Datos básicos
        public NombreProducto Nombre { get; private set; }
        public string Descripcion { get; private set; }
        public UnidadDeMedida UnidadMedida { get; private set; }
    public AfectacionImpuesto AfectacionImpuesto { get; private set; }
        /// <summary>
        /// Tasa de impuesto explícita seleccionada por el usuario.
        /// Ejemplo: IGV 18% (gravado general), IGV 10% (gravado especial restaurantes/hoteles), 0% (exonerado/inafecto).
        /// Nota: IVAP no aplica aquí.
        /// </summary>
    public TasaImpuesto TasaImpuesto { get; private set; }
    // Migración: Categoria (VO) -> CategoriaId + snapshots
    public CategoriaId? CategoriaId { get; private set; }
    public string? CategoriaNombreSnapshot { get; private set; }
    public string? CategoriaColorSnapshot { get; private set; }
        public Marca? Marca { get; private set; }

        // Precios y moneda
        public PrecioVenta? PrecioVenta { get; private set; }
        public Moneda Moneda { get; private set; }

    // Nuevos opcionales
    public PrecioCompra? PrecioCompra { get; private set; }
    public PorcentajeGanancia? PorcentajeGanancia { get; private set; }
    public AliasProducto? Alias { get; private set; }

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
    public List<EstablecimientoId> EstablecimientosAsignados { get; private set; }
    public bool AsignarATodosLosEstablecimientos { get; private set; }

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
            EmpresaId empresaId,
            Moneda moneda,
            Sku sku,
            NombreProducto nombre,
            UnidadDeMedida unidadMedida,
            AfectacionImpuesto afectacionImpuesto,
            TasaImpuesto tasaImpuesto,
            CategoriaId categoriaId,
            List<EstablecimientoId>? establecimientosAsignados,
            string? descripcion = null,
            Marca? marca = null,
            PrecioVenta? precioVenta = null,
            // bool tieneDetraccion eliminado
            CodigoSUNAT? codigoSunat = null,
            CentroDeCosto? centroDeCosto = null,
            Peso? peso = null,
            // ...existing code...
            CodigoBarras? codigoBarras = null,
            CodigoFabrica? codigoFabrica = null,
            // ...existing code...
            TipoProducto tipo = TipoProducto.Bien,
            TipoExistencia tipoExistencia = TipoExistencia.ProductosTerminados,
            // ...existing code...
            bool asignarATodosLosEstablecimientos = false,
            Guid? imagenPrincipalId = null,
            decimal? precioCompraDecimal = null,
            decimal? porcentajeGananciaDecimal = null,
            string? alias = null)
        {
            // Validaciones de parámetros obligatorios
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            Sku = sku ?? throw new ArgumentNullException(nameof(sku));
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion = descripcion?.Trim() ?? string.Empty;
            UnidadMedida = unidadMedida ?? throw new ArgumentNullException(nameof(unidadMedida));
            AfectacionImpuesto = afectacionImpuesto ?? throw new ArgumentNullException(nameof(afectacionImpuesto));
            CategoriaId = categoriaId; // opcionalmente validar no vacío en capas superiores

            if (establecimientosAsignados == null || !establecimientosAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un establecimiento.", nameof(establecimientosAsignados));


            // Validación de coherencia entre afectación y tasa
            if (!afectacionImpuesto.GravaImpuesto && !tasaImpuesto.EsCero)
                throw new ArgumentException("Si la afectación no grava impuesto, la tasa debe ser 0%.");
            if (afectacionImpuesto.GravaImpuesto && tasaImpuesto.EsCero)
                throw new ArgumentException("Si la afectación grava impuesto, la tasa no puede ser 0%.");
            // Solo se permiten tasas gravadas de 18% (IGV general) o 10% (IGV especial restaurantes/hoteles)
            if (afectacionImpuesto.GravaImpuesto && tasaImpuesto.Fraccion != 0.18m && tasaImpuesto.Fraccion != 0.10m)
                throw new ArgumentException("Solo se permite IGV 18% o IGV 10% como tasas gravadas en este contexto.");

            // Asignaciones
            ProductoId = Guid.NewGuid();
            Habilitado = true;
            Marca = marca;
            PrecioVenta = precioVenta;
            Moneda = moneda ?? throw new ArgumentNullException(nameof(moneda), "La moneda debe provenir de la configuración de empresa.");
            // Mapear nuevos opcionales a VOs
            PrecioCompra = precioCompraDecimal.HasValue ? PrecioCompra.Desde(precioCompraDecimal.Value, Moneda) : null;
            PorcentajeGanancia = porcentajeGananciaDecimal.HasValue ? PorcentajeGanancia.Desde(porcentajeGananciaDecimal.Value) : null;
            Alias = !string.IsNullOrWhiteSpace(alias) ? AliasProducto.Desde(alias) : null;
            CodigoSunat = codigoSunat;
            CentroDeCosto = centroDeCosto;
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            Tipo = tipo;
            TipoExistencia = tipoExistencia;
            EstablecimientosAsignados = establecimientosAsignados;
            AsignarATodosLosEstablecimientos = asignarATodosLosEstablecimientos;
            ImagenPrincipalId = imagenPrincipalId;
            TasaImpuesto = tasaImpuesto ?? throw new ArgumentNullException(nameof(tasaImpuesto));

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
            TasaImpuesto tasaImpuesto,
            CategoriaId categoriaId,
            Marca? marca,
            PrecioVenta? precioVenta,
            CentroDeCosto? centroDeCosto,
            Peso? peso,
            CodigoBarras? codigoBarras,
            CodigoFabrica? codigoFabrica,
            TipoProducto tipo,
            CodigoSUNAT? codigoSunat = null,
            List<EstablecimientoId>? establecimientosAsignados = null,
            bool asignarATodosLosEstablecimientos = false,
            Guid? imagenPrincipalId = null,
            string? descripcion = null,
            TipoExistencia tipoExistencia = TipoExistencia.ProductosTerminados,
            decimal? precioCompraDecimal = null,
            decimal? porcentajeGananciaDecimal = null,
            string? alias = null)
        {
            // Validaciones
            Nombre = nombre ?? throw new ArgumentNullException(nameof(nombre));
            Descripcion = descripcion?.Trim() ?? string.Empty;
            UnidadMedida = unidadMedida ?? throw new ArgumentNullException(nameof(unidadMedida));
            AfectacionImpuesto = afectacionImpuesto ?? throw new ArgumentNullException(nameof(afectacionImpuesto));
            CategoriaId = categoriaId;

            if (establecimientosAsignados == null || !establecimientosAsignados.Any())
                throw new ArgumentException("Debe asignar al menos un establecimiento.", nameof(establecimientosAsignados));


            // Validación de coherencia entre afectación y tasa
            if (!afectacionImpuesto.GravaImpuesto && !tasaImpuesto.EsCero)
                throw new ArgumentException("Si la afectación no grava impuesto, la tasa debe ser 0%.");
            if (afectacionImpuesto.GravaImpuesto && tasaImpuesto.EsCero)
                throw new ArgumentException("Si la afectación grava impuesto, la tasa no puede ser 0%.");
            // Solo se permiten tasas gravadas de 18% (IGV general) o 10% (IGV especial restaurantes/hoteles)
            if (afectacionImpuesto.GravaImpuesto && tasaImpuesto.Fraccion != 0.18m && tasaImpuesto.Fraccion != 0.10m)
                throw new ArgumentException("Solo se permite IGV 18% o IGV 10% como tasas gravadas en este contexto.");

            // Asignaciones
            Marca = marca;
            PrecioVenta = precioVenta;
            // Mapear nuevos opcionales (si vienen) a VOs
            PrecioCompra = precioCompraDecimal.HasValue ? PrecioCompra.Desde(precioCompraDecimal.Value, Moneda) : null;
            PorcentajeGanancia = porcentajeGananciaDecimal.HasValue ? PorcentajeGanancia.Desde(porcentajeGananciaDecimal.Value) : null;
            Alias = !string.IsNullOrWhiteSpace(alias) ? AliasProducto.Desde(alias) : null;
            CodigoSunat = codigoSunat;
            CentroDeCosto = centroDeCosto;
            Peso = peso;
            CodigoBarras = codigoBarras;
            CodigoFabrica = codigoFabrica;
            Tipo = tipo;
            TipoExistencia = tipoExistencia;
            EstablecimientosAsignados = establecimientosAsignados;
            AsignarATodosLosEstablecimientos = asignarATodosLosEstablecimientos;
            ImagenPrincipalId = imagenPrincipalId;
            TasaImpuesto = tasaImpuesto ?? throw new ArgumentNullException(nameof(tasaImpuesto));

            var ev = new ProductoActualizado(this);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void Deshabilitar(string motivo)
        {
            Habilitado = false;
            var ev = new ProductoInhabilitado(ProductoId, EmpresaId, motivo);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void Habilitar(string usuario, string? motivo = null)
        {
            Habilitado = true;
            var ev = new ProductoHabilitado(ProductoId, EmpresaId, usuario, motivo);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void CambiarCategoria(CategoriaId categoriaId, string usuario, string? nombreSnapshot = null, string? colorSnapshot = null)
        {
            var categoriaAnterior = CategoriaNombreSnapshot ?? string.Empty;
            var categoriaNueva = nombreSnapshot ?? string.Empty;
            AsignarCategoria(categoriaId, nombreSnapshot, colorSnapshot);
            var ev = new ProductoCategoriaCambiada(ProductoId, EmpresaId, categoriaAnterior, categoriaNueva, usuario);
            AddDomainEvent(ev);
            // Dispatch(ev);
        }

        public void AsignarCategoria(CategoriaId categoriaId, string? nombreSnapshot = null, string? colorSnapshot = null)
        {
            CategoriaId = categoriaId;
            CategoriaNombreSnapshot = nombreSnapshot;
            CategoriaColorSnapshot = colorSnapshot;
        }

        public void QuitarCategoria()
        {
            CategoriaId = null;
            CategoriaNombreSnapshot = null;
            CategoriaColorSnapshot = null;
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
            var ev = new MultimediaAgregada(ProductoId, EmpresaId, media.MultimediaId);
            AddDomainEvent(ev);
        }

        public void EliminarMultimedia(Guid multimediaId)
        {
            var media = _multimedia.FirstOrDefault(m => m.MultimediaId == multimediaId)
                        ?? throw new InvalidOperationException("Multimedia no encontrada.");
            _multimedia.Remove(media);
            // Emitir evento de dominio
            var ev = new MultimediaEliminada(ProductoId, EmpresaId, multimediaId);
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
            var ev = new SkuActualizado(ProductoId, EmpresaId, sku);
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
            var ev = new SkuActualizado(ProductoId, EmpresaId, nuevoSku);
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
