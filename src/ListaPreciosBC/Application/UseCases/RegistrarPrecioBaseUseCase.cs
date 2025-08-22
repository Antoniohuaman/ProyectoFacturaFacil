#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using ListaPreciosBC.Domain.Aggregates;
using ListaPreciosBC.Domain.Repositories;
using ListaPreciosBC.Domain.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects; // Sku, Moneda

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

        public RegistrarPrecioBaseUseCase(
            IListaPrecioRepository listaRepo,
            IPrecioProductoRepository precioRepo,
            Interfaces.IUnitOfWork uow)
        {
            _listaRepo  = listaRepo  ?? throw new ArgumentNullException(nameof(listaRepo));
            _precioRepo = precioRepo ?? throw new ArgumentNullException(nameof(precioRepo));
            _uow        = uow        ?? throw new ArgumentNullException(nameof(uow));
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

            // 1) Resolver columna Base desde la lista activa (empresa/sucursal)
            var listaActiva = await _listaRepo.ObtenerActivaAsync(ct);
            if (listaActiva is null)
                throw new NotFoundException("ListaPrecioActiva", $"{req.EmpresaId}/{req.SucursalId}");

            var idBase = listaActiva.Plantilla.IdColumnaBase; // evita fallback P1 hardcodeado

            // 2) Cargar o crear el agregado PrecioProducto del SKU
            var sku = SharedKernel.ValueObjects.Sku.Crear(req.Sku);
            var agregado = await _precioRepo.ObtenerPorSkuAsync(sku, ct);
            if (agregado is null)
            {
                agregado = PrecioProducto.CrearNuevo(sku);
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
            await _precioRepo.GuardarAsync(agregado, expectedVersion, ct);
            await _uow.SaveChangesAsync();

            return new Response(
                Sku: agregado.Sku,
                ColumnaBaseNumero: idBase.Numero,
                Valor: valor,
                Vigencia: vigencia,
                Version: agregado.Version
            );
        }
    }
}
