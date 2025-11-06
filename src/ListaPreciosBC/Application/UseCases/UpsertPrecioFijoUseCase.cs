using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using ListaPreciosBC.Application.Interfaces;
using SharedKernel.Application.Interfaces;   // ITenantContext


namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Orquesta el alta/actualización (upsert) de un precio FIJO para un SKU y una columna.
    /// Reglas:
    ///  - Debe existir una Lista de Precios ACTIVA para (empresaId, sucursalId).
    ///  - La columna solicitada debe existir en la plantilla y ser MODO FIJO.
    ///  - El agregado PrecioProducto es multi-tenant (empresa/sucursal) y se crea si no existe.
    ///  - expectedVersion se verifica en el repositorio para manejo de concurrencia optimista.
    /// </summary>
    public sealed class UpsertPrecioFijoUseCase
    {
        // DTOs internos para bajo acoplamiento
        public readonly record struct Request(
            string Sku,
            byte ColumnaNumero,
            decimal Monto,
            bool IncluyeImpuesto,
            DateTimeOffset VigenciaDesde,
            DateTimeOffset? VigenciaHasta,
            string? Usuario = null,
            DateTimeOffset? Cuando = null,
            Guid? SucursalId = null
        );

        public readonly record struct Response(
            string Sku,
            byte ColumnaNumero,
            DateTimeOffset VigenciaDesde,
            DateTimeOffset? VigenciaHasta,
            decimal Monto,
            string Moneda,
            bool IncluyeImpuesto,
            int Version
        );

    private readonly IPrecioProductoRepository _precioRepo;
    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public UpsertPrecioFijoUseCase(
            IPrecioProductoRepository precioRepo,
            IListaPrecioRepository listaRepo,
            IUnitOfWork uow,
            ITenantContext tenant,
            ICatalogoReadModel catalogo)
        {
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _listaRepo  = listaRepo  ?? throw new ArgumentNullException(nameof(listaRepo));
            _uow        = uow        ?? throw new ArgumentNullException(nameof(uow));
            _tenant     = tenant     ?? throw new ArgumentNullException(nameof(tenant));
            _catalogo   = catalogo   ?? throw new ArgumentNullException(nameof(catalogo));
        }

        // Backward-compatible overload for tests without catalog
        public UpsertPrecioFijoUseCase(
            IPrecioProductoRepository precioRepo,
            IListaPrecioRepository listaRepo,
            IUnitOfWork uow,
            ITenantContext tenant)
            : this(precioRepo, listaRepo, uow, tenant, new NullCatalogoReadModel())
        { }

        private sealed class NullCatalogoReadModel : ICatalogoReadModel
        {
            public Task<SharedKernel.ValueObjects.ProductoId?> TryGetProductoIdBySkuAsync(SharedKernel.ValueObjects.EmpresaId empresaId, string sku, CancellationToken ct = default)
                => Task.FromResult<SharedKernel.ValueObjects.ProductoId?>(null);
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            // 0) Contexto de empresa
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Traer lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, req.SucursalId, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Validar columna y modo
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columnaCfg = lista.Plantilla.Columnas.SingleOrDefault(c => c.Id.Equals(colId));
            if (columnaCfg is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            if (!columnaCfg.Modo.Equals(ModoValorizacionColumna.Fijo))
                throw new BusinessRuleException($"La columna #{req.ColumnaNumero} no es de modo FIJO; operación no permitida.");

            // 3) Resolver ProductoId a partir de SKU y traer o crear agregado PrecioProducto
            var sku = Sku.Crear(req.Sku);
            var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku.Valor, ct)
                             ?? throw new NotFoundException("Producto", sku.Valor);
            EstablecimientoId? estId = req.SucursalId.HasValue ? SharedKernel.ValueObjects.EstablecimientoId.From(req.SucursalId.Value) : null;
            var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, estId, productoId, ct);
            if (agregado is null)
            {
                agregado = PrecioProducto.CrearNuevo(empresaId, productoId, req.SucursalId);
            }

            // Mantener expectedVersion ANTES de mutar
            var expectedVersion = agregado.Version;

            // 4) Construir VOs de precio y vigencia
            var moneda = Moneda.PEN();
            var valor  = ValorPrecio.DesdeMonto(req.Monto, moneda, req.IncluyeImpuesto);
            var vig    = PeriodoVigencia.Crear(req.VigenciaDesde, req.VigenciaHasta);

            // 5) Mutar el agregado
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            agregado.UpsertPrecioFijo(colId, valor, vig, req.Usuario, cuando);

            // 6) Persistencia + UoW
            await _precioRepo.GuardarAsync(agregado, empresaId, estId, expectedVersion, ct);
            await _uow.SaveChangesAsync();

            // 7) Respuesta
            return new Response(
                Sku: sku.Valor,
                ColumnaNumero: req.ColumnaNumero,
                VigenciaDesde: vig.Desde,
                VigenciaHasta: vig.Hasta,
                Monto: valor.Monto,
                Moneda: moneda.Codigo,
                IncluyeImpuesto: valor.IncluyeImpuesto,
                Version: agregado.Version
            );
        }
    }
}
