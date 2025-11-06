#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // Sku, Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext
using ListaPreciosBC.Application.Interfaces; // ICatalogoReadModel

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Registra o actualiza el Precio Base (columna Base de la plantilla) para un SKU dado.
    /// - Crea el agregado PrecioProducto si no existe.
    /// - Determina la columna Base desde la Lista de Precios activa (empresa/sucursal).
    /// - Upsert de precio fijo (ValorPrecio + PeriodoVigencia) en la columna base.
    /// </summary>
    public sealed class RegistrarPrecioBaseUseCase
    {
    private readonly IListaPrecioRepository _listaRepo;
    private readonly IPrecioProductoRepository _precioRepo;
    private readonly Interfaces.IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public RegistrarPrecioBaseUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            Interfaces.IUnitOfWork uow,
            ITenantContext tenant,
            ICatalogoReadModel catalogo)
        {
            _listaRepo  = listaRepo  ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _uow        = uow        ?? throw new ArgumentNullException(nameof(uow));
            _tenant     = tenant     ?? throw new ArgumentNullException(nameof(tenant));
            _catalogo   = catalogo   ?? throw new ArgumentNullException(nameof(catalogo));
        }

        // Backward-compatible overload for tests without catalog
        public RegistrarPrecioBaseUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            Interfaces.IUnitOfWork uow,
            ITenantContext tenant)
            : this(listaRepo, precioRepo, uow, tenant, new NullCatalogoReadModel())
        { }

        private sealed class NullCatalogoReadModel : ICatalogoReadModel
        {
            public Task<SharedKernel.ValueObjects.ProductoId?> TryGetProductoIdBySkuAsync(SharedKernel.ValueObjects.EmpresaId empresaId, string sku, CancellationToken ct = default)
                => Task.FromResult<SharedKernel.ValueObjects.ProductoId?>(null);
        }

        // DTOs internos para bajo acoplamiento
        public sealed record Request(
            Guid EmpresaId,
            Guid? SucursalId,
            string Sku,                // se normaliza con Sku.Crear(...)
            decimal Monto,             // importe
            Moneda Moneda,             // moneda del SharedKernel
            bool IncluyeImpuesto,      // flag de valor ingresado
            DateTime Desde,            // inicio de vigencia (fecha)
            DateTime? Hasta,           // fin de vigencia (opcional)
            string? Usuario,           // auditoría
            DateTimeOffset? Cuando,    // auditoría
            int CantidadReferenciaParaEventoBase = 1 // para el evento de base (P1)
        );

        public sealed record Response(
            SharedKernel.ValueObjects.Sku Sku,
            byte ColumnaBaseNumero,
            ValorPrecio Valor,
            PeriodoVigencia Vigencia,
            int Version // versión final del agregado PrecioProducto
        );

        public async Task<Response> ExecuteAsync(Request req, CancellationToken ct = default)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));

            // 0) Contexto de empresa
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Resolver columna Base desde la lista activa (empresa/sucursal)
            var listaActiva = await _listaRepo.ObtenerActivaAsync(empresaId, req.SucursalId, ct);
            if (listaActiva is null)
                throw new NotFoundException("ListaPrecioActiva", $"{empresaId}/{req.SucursalId}");

            var idBase = listaActiva.Plantilla.IdColumnaBase; // evita fallback P1 hardcodeado

            // 2) Cargar o crear el agregado PrecioProducto del SKU
            var sku = SharedKernel.ValueObjects.Sku.Crear(req.Sku);
            var agregado = await _precioRepo.ObtenerPorSkuAsync(empresaId, req.SucursalId, sku, ct);
            if (agregado is null)
            {
                var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku.Valor, ct)
                                 ?? throw new NotFoundException("Producto", sku.Valor);
                agregado = PrecioProducto.CrearNuevo(empresaId, productoId);
            }

            // 3) expectedVersion antes de mutar (concurrencia optimista)
            var expectedVersion = agregado.Version;

            // 4) Construir valor y vigencia
            var valor    = ValorPrecio.DesdeMonto(req.Monto, req.Moneda, req.IncluyeImpuesto);
            var vigencia = PeriodoVigencia.Crear(req.Desde, req.Hasta);

            // 5) Upsert fijo en la columna base
            agregado.UpsertPrecioFijo(
                columna: idBase,
                valor: valor,
                vigencia: vigencia,
                usuario: req.Usuario,
                cuando: req.Cuando,
                cantidadReferenciaParaEventoBase: req.CantidadReferenciaParaEventoBase
            );

            // 6) Guardar + UoW
            await _precioRepo.GuardarAsync(agregado, empresaId, req.SucursalId, expectedVersion, ct);
            await _uow.SaveChangesAsync();

            return new Response(
                Sku: sku,
                ColumnaBaseNumero: idBase.Numero,
                Valor: valor,
                Vigencia: vigencia,
                Version: agregado.Version
            );
        }
    }
}
