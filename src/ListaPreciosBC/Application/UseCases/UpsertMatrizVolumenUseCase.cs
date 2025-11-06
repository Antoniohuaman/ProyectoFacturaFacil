using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // ICatalogoReadModel
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // PrecioProducto, ListaPrecio
using ListaPreciosBC.Domain.ValueObjects;    // Sku, IdentificadorColumnaPrecio, ModoValorizacionColumna, TramoVolumen, MatrizVolumen, ValorPrecio
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException
using SharedKernel.ValueObjects;             // Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Upsert de una MATRIZ DE PRECIOS POR VOLUMEN para un SKU en una columna específica.
    /// Reglas:
    ///  - Debe existir Lista de Precios ACTIVA.
    ///  - La columna solicitada debe existir y ser MODO POR VOLUMEN.
    ///  - El agregado PrecioProducto se crea si no existe.
    ///  - Concurrencia optimista con expectedVersion.
    ///  - Validaciones de tramos (no solapados/contiguidad/orden) delegadas a VO/Agregado.
    /// </summary>
    public sealed class UpsertMatrizVolumenUseCase
    {
        // DTO interno de tramo
        public readonly record struct Tramo(
            int DesdeCantidad,        // inclusive
            int? HastaCantidad,       // null = infinito
            decimal Monto,            // importe del tramo
            bool IncluyeImpuesto      // monto incluye IGV
        );

        public readonly record struct Request(
            string Sku,
            byte ColumnaNumero,
            IReadOnlyList<Tramo> Tramos,
            int CantidadReferenciaParaEventoBase = 1,
            string? Usuario = null,
            DateTimeOffset? Cuando = null,
            Guid? SucursalId = null
        );

        public readonly record struct Response(
            string Sku,
            byte ColumnaNumero,
            int TramosActualizados,
            int Version
        );

    private readonly IPrecioProductoRepository _precioRepo;
    private readonly IListaPrecioRepository _listaRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public UpsertMatrizVolumenUseCase(
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
        public UpsertMatrizVolumenUseCase(
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
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, req.SucursalId, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Columna existente y modo POR VOLUMEN
            var colId = IdentificadorColumnaPrecio.DesdeNumero(req.ColumnaNumero);
            var columnaCfg = lista.Plantilla
                                   .Columnas
                                   .SingleOrDefault(c => c.Id.Equals(colId));
            if (columnaCfg is null)
                throw new NotFoundException($"La columna #{req.ColumnaNumero} no existe en la plantilla activa.");

            if (!columnaCfg.Modo.Equals(ModoValorizacionColumna.PorVolumen))
                throw new BusinessRuleException($"La columna #{req.ColumnaNumero} no es de modo POR VOLUMEN; operación no permitida.");

            // 3) Recuperar/crear agregado PrecioProducto
            var sku = Sku.Crear(req.Sku);
            var agregado = await _precioRepo.ObtenerPorSkuAsync(empresaId, req.SucursalId, sku, ct);
            if (agregado is null)
            {
                var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku.Valor, ct)
                                 ?? throw new NotFoundException("Producto", sku.Valor);
                agregado = PrecioProducto.CrearNuevo(empresaId, productoId, req.SucursalId);
            }

            var expectedVersion = agregado.Version; // antes de mutar

            // 4) Construcción de Matriz de Volumen
            var moneda = Moneda.PEN();
            var tramos = new List<TramoVolumen>(req.Tramos.Count);
            foreach (var t in req.Tramos.OrderBy(x => x.DesdeCantidad))
            {
                var valor = ValorPrecio.DesdeMonto(t.Monto, moneda, t.IncluyeImpuesto);
                tramos.Add(TramoVolumen.Crear(t.DesdeCantidad, t.HastaCantidad, valor));
            }
            var matriz = MatrizVolumen.Crear(tramos);

            // 5) Mutación del agregado
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            agregado.UpsertMatrizVolumen(colId, matriz, req.Usuario, cuando, req.CantidadReferenciaParaEventoBase);

            // 6) Persistencia + UoW
            await _precioRepo.GuardarAsync(agregado, empresaId, req.SucursalId, expectedVersion, ct);
            await _uow.SaveChangesAsync(ct);

            // 7) Respuesta
            return new Response(
                Sku: sku.Valor,
                ColumnaNumero: req.ColumnaNumero,
                TramosActualizados: req.Tramos.Count,
                Version: agregado.Version
            );
        }
    }
}
