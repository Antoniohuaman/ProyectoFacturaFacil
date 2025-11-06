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
using SharedKernel.ValueObjects;             // Moneda, Sku
using SharedKernel.Application.Interfaces;   // ITenantContext

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Importación básica de PRECIOS FIJOS por filas (SKU + Columna + Monto + Vigencia).
    /// - Requiere Lista de Precios ACTIVA.
    /// - Cada fila valida que la columna exista y sea de modo FIJO.
    /// - Crea el agregado PrecioProducto si no existe.
    /// - Concurrencia optimista por agregado (expectedVersion).
    /// - Permite continuar ante errores o abortar en el primero (configurable).
    /// </summary>
    public sealed class ImportarPreciosBasicoUseCase
    {
        public readonly record struct Fila(
            string Sku,
            byte ColumnaNumero,
            decimal Monto,
            DateTime? Desde,             // si es null => hoy (UTC date)
            DateTime? Hasta,             // null => abierto
            bool IncluyeImpuesto = true
        );

        public readonly record struct Request(
            IReadOnlyList<Fila> Filas,
            string? Usuario = null,
            DateTimeOffset? Cuando = null,
            int CantidadReferenciaParaEventoBase = 1,
            bool DetenerAntePrimerError = false
        );

        public readonly record struct ErrorItem(
            int Index,              // índice de la fila en el request
            string Sku,
            byte ColumnaNumero,
            string Mensaje
        );

        public readonly record struct Response(
            int Procesadas,
            int Exitosas,
            int Fallidas,
            ErrorItem[] Errores,
            int AgregadosAfectados
        );

    private readonly IListaPrecioRepository _listaRepo;
    private readonly IPrecioProductoRepository _precioRepo;
    private readonly IUnitOfWork _uow;
    private readonly ITenantContext _tenant;
    private readonly ICatalogoReadModel _catalogo;

        public ImportarPreciosBasicoUseCase(
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

        // Backward-compatible overload for tests that didn't pass a catalog
        public ImportarPreciosBasicoUseCase(
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
            // 0) Validaciones básicas
            if (req.Filas is null || req.Filas.Count == 0)
                throw new BusinessRuleException("No se recibieron filas para importar.");

            // 0) Contexto
            var empresaId = _tenant.EmpresaId;
            if (empresaId is null) throw new InvalidOperationException("EmpresaId del contexto es obligatorio.");

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // Precalcular columnas por Id.Numero
            var columnasPorNumero = lista.Plantilla.Columnas.ToDictionary(c => c.Id.Numero, c => c);

            var moneda = Moneda.PEN();
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;
            var errores = new List<ErrorItem>();
            var exitosas = 0;
            var afectados = 0;

            // Agrupar por SKU para minimizar lecturas/escrituras
            var gruposPorSku = req.Filas
                                  .Select((fila, idx) => (fila, idx))
                                  .GroupBy(x => x.fila.Sku, StringComparer.OrdinalIgnoreCase);

            foreach (var grupo in gruposPorSku)
            {
                ct.ThrowIfCancellationRequested();

                var skuVo = Sku.Crear(grupo.Key);
                var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, skuVo.Valor, ct)
                                 ?? throw new NotFoundException("Producto", skuVo.Valor);
                var agregado = await _precioRepo.ObtenerPorProductoIdAsync(empresaId, null, productoId, ct);
                var nuevo = agregado is null;

                if (nuevo)
                {
                    agregado = PrecioProducto.CrearNuevo(empresaId, productoId);
                }

                var expectedVersion = agregado!.Version;
                var huboCambiosParaEsteSku = false;

                foreach (var (fila, index) in grupo)
                {
                    try
                    {
                        // 2) Columna debe existir
                        if (!columnasPorNumero.TryGetValue(fila.ColumnaNumero, out var columnaCfg))
                            throw new NotFoundException($"La columna #{fila.ColumnaNumero} no existe en la plantilla activa.");

                        // 3) Modo debe ser FIJO
                        if (!columnaCfg.Modo.Equals(ModoValorizacionColumna.Fijo))
                            throw new BusinessRuleException($"La columna #{fila.ColumnaNumero} no es de modo FIJO; no se permiten importaciones básicas en columnas por volumen.");

                        // 4) Construcción de VO para valor y vigencia
                        var valor = ValorPrecio.DesdeMonto(fila.Monto, moneda, fila.IncluyeImpuesto);
                        var desde = fila.Desde ?? DateTime.UtcNow.Date;
                        var vigencia = PeriodoVigencia.Crear(desde, fila.Hasta);

                        // 5) Mutación
                        agregado.UpsertPrecioFijo(
                            IdentificadorColumnaPrecio.DesdeNumero(fila.ColumnaNumero),
                            valor,
                            vigencia,
                            req.Usuario,
                            cuando,
                            req.CantidadReferenciaParaEventoBase
                        );

                        exitosas++;
                        huboCambiosParaEsteSku = true;
                    }
                    catch (ConcurrencyException) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(index, fila.Sku, fila.ColumnaNumero, "Conflicto de concurrencia."));
                    }
                    catch (BusinessRuleException bre) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(index, fila.Sku, fila.ColumnaNumero, bre.Message));
                    }
                    catch (NotFoundException nfe) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(index, fila.Sku, fila.ColumnaNumero, nfe.Message));
                    }
                    catch (Exception ex) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(index, fila.Sku, fila.ColumnaNumero, $"Error inesperado: {ex.Message}"));
                    }

                    // Si se debe abortar ante el primer error, relanzar
                    if (req.DetenerAntePrimerError && errores.Count > 0)
                        throw new BusinessRuleException($"Importación abortada por error en fila #{index}: {errores[0].Mensaje}");
                }

                // Persistir este agregado si tuvo cambios válidos
                if (huboCambiosParaEsteSku)
                {
                    await _precioRepo.GuardarAsync(agregado, empresaId, null, expectedVersion, ct);
                    afectados++;
                }
            }

            // Commit general
            await _uow.SaveChangesAsync(ct);

            return new Response(
                Procesadas: req.Filas.Count,
                Exitosas: exitosas,
                Fallidas: errores.Count,
                Errores: errores.ToArray(),
                AgregadosAfectados: afectados
            );
        }
    }
}
