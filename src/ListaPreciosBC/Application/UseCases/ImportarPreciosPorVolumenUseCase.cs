using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Application.Interfaces; // IUnitOfWork, ICatalogoReadModel
using ListaPreciosBC.Domain.Repositories;    // IListaPrecioRepository, IPrecioProductoRepository
using ListaPreciosBC.Domain.Aggregates;      // ListaPrecio, PrecioProducto
using ListaPreciosBC.Domain.ValueObjects;    // IdentificadorColumnaPrecio, ModoValorizacionColumna, TramoVolumen, MatrizVolumen, ValorPrecio
using SharedKernel.Exceptions;               // NotFoundException, BusinessRuleException, ConcurrencyException
using SharedKernel.ValueObjects;             // Moneda, Sku
using SharedKernel.Application.Interfaces;   // ITenantContext
// (ICatalogoReadModel included above)

namespace ListaPreciosBC.Application.UseCases
{
    /// <summary>
    /// Importa MATRICES DE VOLUMEN para uno o varios SKUs.
    /// Reglas:
    ///  - Debe existir lista ACTIVA.
    ///  - Cada columna debe existir y ser de modo PorVolumen.
    ///  - Crea el agregado PrecioProducto si no existe.
    ///  - Concurrencia optimista por agregado (expectedVersion).
    ///  - Puede detener ante el primer error o continuar acumulando errores.
    /// </summary>
    public sealed class ImportarPreciosPorVolumenUseCase
    {
        public readonly record struct Rango(
            int DesdeCantidad,         // inclusive, >= 1
            int? HastaCantidad,        // inclusive, null = infinito
            decimal Monto,
            bool IncluyeImpuesto = true
        );

        public readonly record struct ItemColumna(
            byte ColumnaNumero,
            IReadOnlyList<Rango> Rangos
        );

        public readonly record struct Fila(
            string Sku,
            IReadOnlyList<ItemColumna> Columnas
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

        public ImportarPreciosPorVolumenUseCase(
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
        public ImportarPreciosPorVolumenUseCase(
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

            // 1) Lista activa
            var lista = await _listaRepo.ObtenerActivaAsync(empresaId, null, ct);
            if (lista is null)
                throw new NotFoundException("No existe lista de precios activa.");

            // Índice de columnas por número
            var columnasPorNumero = lista.Plantilla.Columnas.ToDictionary(c => c.Id.Numero, c => c);

            var moneda = Moneda.PEN();
            var cuando = req.Cuando ?? DateTimeOffset.UtcNow;

            var errores = new List<ErrorItem>();
            var itemsProcesados = 0;
            var itemsExitosos = 0;
            var agregadosAfectados = 0;

            for (int fIdx = 0; fIdx < req.Filas.Count; fIdx++)
            {
                ct.ThrowIfCancellationRequested();

                var fila = req.Filas[fIdx];
                if (fila.Columnas is null || fila.Columnas.Count == 0)
                    continue;

                    var skuVo = Sku.Crear(fila.Sku);
                    var agregado = await _precioRepo.ObtenerPorSkuAsync(empresaId, null, skuVo, ct);
                var esNuevo = agregado is null;
                if (esNuevo)
                {
                    var productoId = await _catalogo.TryGetProductoIdBySkuAsync(empresaId, skuVo.Valor, ct)
                                     ?? throw new NotFoundException("Producto", skuVo.Valor);
                    agregado = PrecioProducto.CrearNuevo(empresaId, productoId);
                }

                var expectedVersion = agregado!.Version;
                var huboCambios = false;

                for (int iIdx = 0; iIdx < fila.Columnas.Count; iIdx++)
                {
                    itemsProcesados++;
                    var item = fila.Columnas[iIdx];

                    try
                    {
                        // 2) Columna debe existir
                        if (!columnasPorNumero.TryGetValue(item.ColumnaNumero, out var cfgCol))
                            throw new NotFoundException($"La columna #{item.ColumnaNumero} no existe en la plantilla activa.");

                        // 3) Debe ser modo POR VOLUMEN
                        if (!cfgCol.Modo.Equals(ModoValorizacionColumna.PorVolumen))
                            throw new BusinessRuleException($"La columna #{item.ColumnaNumero} no es de modo PorVolumen; use importación de precios fijos para columnas Fijo.");

                        // 4) Debe venir al menos un rango
                        if (item.Rangos is null || item.Rangos.Count == 0)
                            throw new BusinessRuleException($"La columna #{item.ColumnaNumero} no tiene rangos definidos.");

                        // 5) Construir matriz de volumen
                        var tramos = new List<TramoVolumen>(item.Rangos.Count);
                        foreach (var r in item.Rangos)
                        {
                            // Validación básica local (el VO hará sus propias validaciones también)
                            if (r.DesdeCantidad < 1)
                                throw new BusinessRuleException("DesdeCantidad debe ser >= 1.");
                            if (r.HastaCantidad.HasValue && r.HastaCantidad.Value < r.DesdeCantidad)
                                throw new BusinessRuleException("HastaCantidad no puede ser menor que DesdeCantidad.");

                            var valor = ValorPrecio.DesdeMonto(r.Monto, moneda, r.IncluyeImpuesto);
                            tramos.Add(TramoVolumen.Crear(r.DesdeCantidad, r.HastaCantidad, valor));
                        }

                        var matriz = MatrizVolumen.Crear(tramos);

                        // 6) Mutación
                        agregado.UpsertMatrizVolumen(
                            IdentificadorColumnaPrecio.DesdeNumero(item.ColumnaNumero),
                            matriz,
                            req.Usuario,
                            cuando,
                            req.CantidadReferenciaParaEventoBase
                        );

                        itemsExitosos++;
                        huboCambios = true;
                    }
                    catch (ConcurrencyException) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, fila.Sku, item.ColumnaNumero, "Conflicto de concurrencia."));
                    }
                    catch (BusinessRuleException bre) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, fila.Sku, item.ColumnaNumero, bre.Message));
                    }
                    catch (NotFoundException nfe) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, fila.Sku, item.ColumnaNumero, nfe.Message));
                    }
                    catch (Exception ex) when (!req.DetenerAntePrimerError)
                    {
                        errores.Add(new ErrorItem(fIdx, iIdx, fila.Sku, item.ColumnaNumero, $"Error inesperado: {ex.Message}"));
                    }

                    if (req.DetenerAntePrimerError && errores.Count > 0)
                        throw new BusinessRuleException($"Importación abortada por error en fila #{fIdx}, item #{iIdx}: {errores[0].Mensaje}");
                }

                if (huboCambios)
                {
                    await _precioRepo.GuardarAsync(agregado, empresaId, null, expectedVersion, ct);
                    agregadosAfectados++;
                }
            }

            await _uow.SaveChangesAsync(ct);

            return new Response(
                FilasProcesadas: req.Filas.Count,
                ItemsProcesados: itemsProcesados,
                ItemsExitosos: itemsExitosos,
                ItemsFallidos: errores.Count,
                Errores: errores.ToArray(),
                AgregadosAfectados: agregadosAfectados
            );
        }
    }
}
