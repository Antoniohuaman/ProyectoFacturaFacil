using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio
using SharedKernel.Exceptions;               // NotFoundException
using SharedKernel.Application.Interfaces;   // ITenantContext
using ListaPreciosBC.Application.Interfaces; // ICatalogoReadModel
using SharedKernel.ValueObjects;

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Consulta: Lista el PRECIO VIGENTE (si existe) para cada columna de la Lista de Precios ACTIVA,
    /// para un SKU, fecha y cantidad dados. No hay mutaciones (no requiere UoW).
    /// </summary>
    public sealed class ListarPreciosDeSkuUseCase
    {
        public readonly record struct Request(
            string Sku,
            int Cantidad,
            DateTimeOffset? Fecha = null,
            bool SoloVisibles = false,
            string? UnidadMedidaCodigo = null
        );

        public readonly record struct ItemPrecio(
            byte ColumnaNumero,
            string NombreColumna,
            string ModoColumna,   // "Fijo" | "PorVolumen"
            bool EsBase,
            bool Visible,
            decimal? Monto,       // null si no hay precio vigente
            bool? IncluyeImpuesto,
            string? Moneda
        );

        public readonly record struct Response(
            string Sku,
            DateTimeOffset FechaConsulta,
            int Cantidad,
            string UnidadMedidaCodigo,
            ItemPrecio[] PreciosPorColumna,
            int VersionAgregado // versión del PrecioProducto consultado
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IPrecioProductoRepository _precioRepo;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public ListarPreciosDeSkuUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            ITenantContext tenant,
            ICatalogoReadModel catalogo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
        }

        // Backward-compatible overload for tests without catalog
        public ListarPreciosDeSkuUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            ITenantContext tenant)
            : this(listaRepo, precioRepo, tenant, new NullCatalogoReadModel())
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

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // 2) Resolver ProductoId y obtener agregado (SKU como string normalizado)
            var sku = (req.Sku ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("El SKU no puede estar vacío.", nameof(req.Sku));
            var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku, ct)
                             ?? throw new NotFoundException("Producto", sku);
            var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, null, productoId, ct);
            if (agregado is null)
                throw new NotFoundException($"No existe PrecioProducto para el SKU {req.Sku}.");

            // 3) Fecha de consulta
            var fecha = req.Fecha ?? DateTimeOffset.UtcNow;
            var unidad = UnidadDeMedida.From(req.UnidadMedidaCodigo ?? UnidadDeMedida.NIU.Codigo);

            // 4) Iterar columnas (opcionalmente solo visibles) y resolver precio vigente
            var columnas = lista.Plantilla.Columnas
                                   .Where(c => !req.SoloVisibles || c.Visible)
                                   .OrderBy(c => c.Orden)
                                   .ToArray();

            var items = columnas.Select(col =>
            {
                var resuelto = agregado.ObtenerPrecioVigente(col.Id, unidad, fecha, req.Cantidad);
                if (resuelto is null)
                {
                    return new ItemPrecio(
                        ColumnaNumero: col.Id.Numero,  // asumiendo que el Identificador expone Numero
                        NombreColumna: col.Nombre.Valor,
                        ModoColumna: col.Modo.ToString(),
                        EsBase: col.EsBase,
                        Visible: col.Visible,
                        Monto: null,
                        IncluyeImpuesto: null,
                        Moneda: null
                    );
                }

                var valor = resuelto.Valor; // asumiendo ValorPrecio (Monto, Moneda, IncluyeImpuesto)
                return new ItemPrecio(
                    ColumnaNumero: col.Id.Numero,
                    NombreColumna: col.Nombre.Valor,
                    ModoColumna: col.Modo.ToString(),
                    EsBase: col.EsBase,
                    Visible: col.Visible,
                    Monto: valor.Monto,
                    IncluyeImpuesto: valor.IncluyeImpuesto,
                    Moneda: valor.Importe.Moneda.Codigo // ADAPTA si tu Moneda expone otro nombre (p.ej., CodigoIso)
                );
            }).ToArray();

            // 5) Respuesta
            return new Response(
                Sku: sku,
                FechaConsulta: fecha,
                Cantidad: req.Cantidad,
                UnidadMedidaCodigo: unidad.Codigo,
                PreciosPorColumna: items,
                VersionAgregado: agregado.Version
            );
        }
    }
}
