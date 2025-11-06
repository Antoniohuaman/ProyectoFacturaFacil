using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork, ICatalogoReadModel
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, ModoValorizacionColumna, ValorPrecio, PeriodoVigencia
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException, ConcurrencyException
using SharedKernel.ValueObjects;             // Moneda
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Importa PRECIOS FIJOS en bloque. Cada fila corresponde a un SKU y contiene varios ítems (columna + valor + vigencia).
    /// Reglas:
    /// - Debe existir lista activa.
    /// - Cada columna debe existir y ser de modo FIJO.
    /// - Crea el agregado PrecioProducto si no existe.
    /// - Concurrencia optimista por agregado (expectedVersion).
    /// - Puede detener ante el primer error o continuar acumulando errores.
    /// </summary>
    public sealed class ImportarPreciosFijosUseCase
    {
        public readonly record struct ItemPrecio(
            byte ColumnaNumero,
            decimal Monto,
            DateTime? Desde,             // null => hoy (UTC date)
            DateTime? Hasta,             // null => abierto
            bool IncluyeImpuesto = true
        );

        public readonly record struct Fila(
            string Sku,
            IReadOnlyList<ItemPrecio> Items
        );

        public readonly record struct Request(
            IReadOnlyList<Fila> Filas,
            string? Usuario = null,
            DateTimeOffset? Cuando = null,
            int CantidadReferenciaParaEventoBase = 1,
            bool DetenerAntePrimerError = false
        );

        public readonly record struct ErrorItem(
            int FilaIndex,
            int ItemIndex,
            string Sku,
            byte ColumnaNumero,
            string Mensaje
        );

        public readonly record struct Response(
            int FilasProcesadas,
            int ItemsProcesados,
            int ItemsExitosos,
            int ItemsFallidos,
            ErrorItem[] Errores,
            int AgregadosAfectados
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IPrecioProductoRepository _precioRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public ImportarPreciosFijosUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            IUnitOfWork uow,
            ITenantContext tenant,
            ICatalogoReadModel catalogo)
        {
            _listaRepo = listaRepo ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
        }

        // Backward-compatible overload for tests without catalog
        public ImportarPreciosFijosUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            IUnitOfWork uow,
            ITenantContext tenant)
            : this(listaRepo, precioRepo, uow, tenant, new NullCatalogoReadModel())
        { }

        private sealed class NullCatalogoReadModel : ICatalogoReadModel
        {
            public Task<SharedKernel.ValueObjects.ProductoId?> TryGetProductoIdBySkuAsync(SharedKernel.ValueObjects.EmpresaId empresaId, string sku, CancellationToken ct = default)
                => Task.FromResult<SharedKernel.ValueObjects.ProductoId?>(null);
        }

        public async Task<Response> Handle(Request req, CancellationToken ct)
        {
            if (req.Filas is null || req.Filas.Count == 0)
                throw new BusinessRuleException("No se recibieron filas para importar.");

            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Lista de precios activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // Indizar columnas por número
            var columnasPorNumero = lista.Plantilla.Columnas.ToDictionary(c => c.Id.Numero, c => c);

            var moneda = Moneda.PEN();
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;

            var errores = new List<ErrorItem>();
            var itemsTotal = 0;
            var itemsExitosos = 0;
            var afectados = 0;

            // Agrupamos por SKU para minimizar lecturas/escrituras
            for (int fIdx = 0; fIdx < req.Filas.Count; fIdx++)
            {
                ct.ThrowIfCancellationRequested();

                var fila = req.Filas[fIdx];
                if (fila.Items is null || fila.Items.Count == 0)
                    continue;

                var sku = (fila.Sku ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("El SKU no puede estar vacío.");
                var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, sku, ct)
                                 ?? throw new NotFoundException("Producto", sku);
                var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, null, productoId, ct);
                var nuevo = agregado is null;
                if (nuevo)
                {
                    agregado = PrecioProducto.CrearNuevo(empresaId, productoId);
                }

                var expectedVersion = agregado!.Version;
                var huboCambiosParaEsteSku = false;

                for (int iIdx = 0; iIdx < fila.Items.Count; iIdx++)
                {
                    itemsTotal++;
                    var item = fila.Items[iIdx];

                    try
                    {
                        // 2) Validar columna
                        if (!columnasPorNumero.TryGetValue(item.ColumnaNumero, out var colCfg))
                            throw new NotFoundException($"La columna #{item.ColumnaNumero} no existe en la plantilla activa.");

                        // 3) Debe ser modo FIJO
                        if (!colCfg.Modo.Equals(ModoValorizacionColumna.Fijo))
                            throw new BusinessRuleException($"La columna #{item.ColumnaNumero} no es de modo FIJO; usa otro proceso para columnas por volumen.");

                        // 4) Construir VO
                        var valor = ValorPrecio.DesdeMonto(item.Monto, moneda, item.IncluyeImpuesto);
                        var desde = item.Desde ?? DateTime.UtcNow.Date;
                        var vigencia = PeriodoVigencia.Crear(desde, item.Hasta);

                        // 5) Mutación
                        agregado.UpsertPrecioFijo(
                            IdentificadorColumnaPrecio.DesdeNumero(item.ColumnaNumero),
                            valor,
                            vigencia,
                            req.Usuario,
                            cuando,
                            req.CantidadReferenciaParaEventoBase
                        );

                        itemsExitosos++;
                        huboCambiosParaEsteSku = true;
                    }
                    catch (ConcurrencyException) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, sku, item.ColumnaNumero, "Conflicto de concurrencia."));
                    }
                    catch (BusinessRuleException bre) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, sku, item.ColumnaNumero, bre.Message));
                    }
                    catch (NotFoundException nfe) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, sku, item.ColumnaNumero, nfe.Message));
                    }
                    catch (Exception ex) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, sku, item.ColumnaNumero, $"Error inesperado: {ex.Message}"));
                    }

                    if (req.DetenerAntePrimerError && errores.Count > 0)
                        throw new BusinessRuleException($"Importación abortada por error en fila #{fIdx}, item #{iIdx}: {errores[0].Mensaje}");
                }

                if (huboCambiosParaEsteSku)
                {
                    await _precioRepo.GuardarAsync(agregado, empresaId, null, expectedVersion, ct);
                    afectados++;
                }
            }

            await _uow.CommitAsync(ct);

            var fallidos = errores.Count;
            return new Response(
                FilasProcesadas: req.Filas.Count,
                ItemsProcesados: itemsTotal,
                ItemsExitosos: itemsExitosos,
                ItemsFallidos: fallidos,
                Errores: errores.ToArray(),
                AgregadosAfectados: afectados
            );
        }
    }
}
