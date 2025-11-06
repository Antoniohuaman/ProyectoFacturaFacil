using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork, ICatalogoReadModel
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // PrecioProducto, ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, ModoValorizacionColumna
using System.Text.RegularExpressions;
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException
using SharedKernel.ValueObjects;
using SharedKernel.Application.Interfaces;   // ITenantContext
// ICatalogoReadModel included above

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Caso de uso: Eliminar precio FIJO de un SKU en la columna indicada dentro de la lista activa.
    /// Reglas:
    ///  - Debe existir lista activa.
    ///  - La columna debe existir y ser de modo FIJO.
    ///  - Si el PrecioProducto no existe:
    ///      * LanzarSiNoExiste = true -> NotFoundException.
    ///      * LanzarSiNoExiste = false -> idempotente (retorna versión 0).
    ///  - Concurrencia optimista con expectedVersion.
    /// </summary>
    public sealed class EliminarPrecioFijoUseCase
    {
        public readonly record struct Request(
            string Sku,
            byte ColumnaNumero,
            bool LanzarSiNoExiste = true,
            string? Usuario = null,
            DateTimeOffset? Cuando = null
        );

        public readonly record struct Response(
            string Sku,
            byte ColumnaNumero,
            int Version
        );

    private readonly IPrecioProductoRepository _precioRepo;
    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public EliminarPrecioFijoUseCase(
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
        public EliminarPrecioFijoUseCase(
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
            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Columna existente y modo FIJO
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columnaCfg = lista.Plantilla
                                   .Columnas
                                   .SingleOrDefault(c => c.Id.Equals(colId));
            if (columnaCfg is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            if (!columnaCfg.Modo.Equals(ModoValorizacionColumna.Fijo))
                throw new BusinessRuleException($"La columna #{req.ColumnaNumero} no es de modo FIJO; operación no permitida.");

            // 3) Resolver ProductoId y recuperar agregado PrecioProducto por ProductoId
            var skuNorm = NormalizarSku(req.Sku);
            var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, skuNorm, ct)
                             ?? throw new NotFoundException("Producto", skuNorm);
            SharedKernel.ValueObjects.EstablecimientoId? estId = null; // este UC trabaja con lista activa global
            var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, estId, productoId, ct);
            if (agregado is null)
            {
                if (req.LanzarSiNoExiste)
                    throw new NotFoundException($"No existe PrecioProducto para el SKU {req.Sku}.");

                // Idempotente: nada que eliminar
                return new Response(req.Sku, req.ColumnaNumero, Version: 0);
            }

            var expectedVersion = agregado.Version; // captura antes de mutar

            // 4) Mutación del agregado (nombre del método según tu dominio)
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
                agregado.EliminarPrecioFijo(colId, req.Usuario, cuando);

            // 5) Persistencia + UoW
            await _precioRepo.GuardarAsync(agregado, empresaId, estId, expectedVersion, ct);
            await _uow.CommitAsync(ct);

            // 6) Respuesta
            return new Response(
                Sku: skuNorm,
                ColumnaNumero: req.ColumnaNumero,
                Version: agregado.Version
            );
        }

        private static string NormalizarSku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("El SKU no puede estar vacío.", nameof(sku));
            var t = sku.Trim().ToUpperInvariant();
            return Regex.Replace(t, @"\s+", " ");
        }
    }
}