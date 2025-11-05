using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestionInventarioBC.Application.Interfaces; // ICatalogoReadModel
using GestionInventarioBC.Domain.Aggregates;
using GestionInventarioBC.Domain.Repositories;
using GestionInventarioBC.Domain.ValueObjects;
using SharedKernel.Application.Interfaces;
using SharedKernel.ValueObjects;

namespace GestionInventarioBC.Tests.TestUtils
{
    internal sealed class FakeTenantContext : ITenantContext
    {
        public TenantId TenantId { get; }
        public EmpresaId EmpresaId { get; }
        public FakeTenantContext(EmpresaId empresaId)
        {
            EmpresaId = empresaId;
            TenantId = new TenantId(Guid.NewGuid());
        }
    }

    internal sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public Task CommitAsync(CancellationToken ct = default)
        {
            CommitCalls++;
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync(CancellationToken ct = default) => CommitAsync(ct);
    }

    internal sealed class FakeCatalogoReadModel : ICatalogoReadModel
    {
    // Claves normalizadas por empresa y SKU en minúsculas
    private readonly Dictionary<string, ProductoId> _skuToId = new();
    private readonly Dictionary<string, (string sku, string nombre)> _idToData = new();

        public void Seed(string empresaId, string sku, ProductoId productoId, string nombre)
        {
            var keySku = KeySku(empresaId, sku);
            var keyId = KeyId(empresaId, productoId.Value);
            _skuToId[keySku] = productoId;
            _idToData[keyId] = (sku, nombre);
        }

        public Task<ProductoId?> TryGetProductoIdBySkuAsync(EmpresaId empresaId, string sku, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sku)) return Task.FromResult<ProductoId?>(null);
            var key = KeySku(empresaId.Value, sku);
            return Task.FromResult(_skuToId.TryGetValue(key, out var id) ? (ProductoId?)id : null);
        }

        public Task<IReadOnlyList<ProductoId>> BuscarProductoIdsAsync(EmpresaId empresaId, string? sku, string? nombre, CancellationToken ct = default)
        {
            IEnumerable<ProductoId> q = _skuToId
                .Where(kv => kv.Key.StartsWith(empresaId.Value, StringComparison.OrdinalIgnoreCase)
                          && (string.IsNullOrWhiteSpace(sku) || kv.Key.Contains(sku!.Trim().ToLowerInvariant())))
                .Select(kv => kv.Value)
                .Distinct();
            // nombre no está indexado en este fake; podríamos cruzar con _idToData
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                q = q.Where(pid => _idToData.TryGetValue(KeyId(empresaId.Value, pid.Value), out var data) && data.nombre.Contains(nombre!, StringComparison.OrdinalIgnoreCase));
            }
            return Task.FromResult((IReadOnlyList<ProductoId>)q.ToList());
        }

        public Task<(string Sku, string Nombre)?> TryGetSkuYNombreAsync(EmpresaId empresaId, ProductoId productoId, CancellationToken ct = default)
        {
            return Task.FromResult(_idToData.TryGetValue(KeyId(empresaId.Value, productoId.Value), out var data)
                ? ((string, string)?)data
                : null);
        }

        private static string KeySku(string empresa, string sku) => $"{empresa}|{sku.Trim().ToLowerInvariant()}";
        private static string KeyId(string empresa, Guid producto) => $"{empresa}|{producto:D}";
    }

    internal sealed class FakeStockPorAlmacenRepository : IStockPorAlmacenRepository
    {
    private readonly ConcurrentDictionary<(string emp, Guid est, Guid alm, Guid prod), StockPorAlmacen> _store = new();

        public Task<StockPorAlmacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, ProductoId productoId, CancellationToken ct = default)
        {
            _store.TryGetValue((empresaId.Value, establecimientoId.Value, almacenId.Value, productoId.Value), out var s);
            return Task.FromResult<StockPorAlmacen?>(s);
        }

        public Task<IReadOnlyList<StockPorAlmacen>> ListarPorAlmacenAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, CancellationToken ct = default)
        {
            var list = _store.Where(kv => kv.Key.emp == empresaId.Value && kv.Key.est == establecimientoId.Value && kv.Key.alm == almacenId.Value)
                              .Select(kv => kv.Value)
                              .ToList();
            return Task.FromResult((IReadOnlyList<StockPorAlmacen>)list);
        }

        public Task GuardarAsync(StockPorAlmacen stock, CancellationToken ct = default)
        {
            _store[(stock.EmpresaId.Value, stock.EstablecimientoId.Value, stock.AlmacenId.Value, stock.ProductoId.Value)] = stock;
            return Task.CompletedTask;
        }

        public StockPorAlmacen Ensure(EmpresaId emp, EstablecimientoId est, AlmacenId alm, ProductoId prod, decimal real, decimal reservado = 0m)
        {
            var s = _store.GetOrAdd((emp.Value, est.Value, alm.Value, prod.Value), _ =>
                StockPorAlmacen.CrearNuevo(emp, est, alm, prod));
            // Overwrite to desired starting state
            if (real >= 0)
            {
                // Set values by adjusting from current to desired
                var delta = real - s.Real.Value;
                if (delta > 0) s.Ingresar(CantidadStock.From(delta));
                if (delta < 0) s.Egresar(CantidadStock.From(-delta));
            }
            if (reservado > 0)
            {
                var deltaR = reservado - s.Reservado.Value;
                if (deltaR > 0) s.Reservar(CantidadStock.From(deltaR));
                if (deltaR < 0) s.LiberarReserva(CantidadStock.From(-deltaR));
            }
            _store[(emp.Value, est.Value, alm.Value, prod.Value)] = s;
            return s;
        }
    }

    internal sealed class FakeMovimientoInventarioRepository : IMovimientoInventarioRepository
    {
        private readonly ConcurrentDictionary<Guid, MovimientoInventario> _movs = new();

        public Task<MovimientoInventario?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Guid movimientoId, CancellationToken ct = default)
        {
            _movs.TryGetValue(movimientoId, out var m);
            if (m is null) return Task.FromResult<MovimientoInventario?>(null);
            return Task.FromResult(m.EmpresaId == empresaId && m.EstablecimientoId == establecimientoId && m.AlmacenId == almacenId ? m : null);
        }

        public Task<IReadOnlyList<MovimientoInventario>> ListarAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, DateTimeOffset? desde = null, DateTimeOffset? hasta = null, ProductoId? productoId = null, TipoMovimiento? tipo = null, MotivoMovimiento? motivo = null, CancellationToken ct = default)
        {
            IEnumerable<MovimientoInventario> q = _movs.Values.Where(m => m.EmpresaId == empresaId && m.EstablecimientoId == establecimientoId && m.AlmacenId == almacenId);
            if (desde is not null) q = q.Where(m => m.Fecha >= desde);
            if (hasta is not null) q = q.Where(m => m.Fecha <= hasta);
            if (tipo is not null) q = q.Where(m => m.Tipo == tipo);
            if (motivo is not null) q = q.Where(m => m.Motivo == motivo);
            if (productoId is not null) q = q.Where(m => m.Lineas.Any(l => l.ProductoId.Equals(productoId.Value)));
            return Task.FromResult((IReadOnlyList<MovimientoInventario>)q.OrderByDescending(m => m.Fecha).ToList());
        }

        public Task GuardarAsync(MovimientoInventario movimiento, CancellationToken ct = default)
        {
            _movs[movimiento.MovimientoId] = movimiento;
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeTransferenciaInventarioRepository : ITransferenciaInventarioRepository
    {
        private readonly ConcurrentDictionary<Guid, TransferenciaInventario> _t = new();

        public Task<TransferenciaInventario?> ObtenerAsync(EmpresaId empresaId, Guid transferenciaId, CancellationToken ct = default)
        {
            if (_t.TryGetValue(transferenciaId, out var tr))
            {
                return Task.FromResult(tr.EmpresaId == empresaId ? tr : null);
            }
            return Task.FromResult<TransferenciaInventario?>(null);
        }

        public Task<IReadOnlyList<TransferenciaInventario>> ListarPendientesAsync(EmpresaId empresaId, EstablecimientoId? origenEstablecimientoId = null, AlmacenId? origenAlmacenId = null, EstablecimientoId? destinoEstablecimientoId = null, AlmacenId? destinoAlmacenId = null, CancellationToken ct = default)
        {
            IEnumerable<TransferenciaInventario> q = _t.Values.Where(x => x.EmpresaId == empresaId && x.Estado == EstadoTransferencia.Creada);
            if (origenEstablecimientoId is not null) q = q.Where(x => x.OrigenEstablecimientoId == origenEstablecimientoId);
            if (origenAlmacenId is not null) q = q.Where(x => x.OrigenAlmacenId == origenAlmacenId);
            if (destinoEstablecimientoId is not null) q = q.Where(x => x.DestinoEstablecimientoId == destinoEstablecimientoId);
            if (destinoAlmacenId is not null) q = q.Where(x => x.DestinoAlmacenId == destinoAlmacenId);
            return Task.FromResult((IReadOnlyList<TransferenciaInventario>)q.ToList());
        }

        public Task GuardarAsync(TransferenciaInventario transferencia, CancellationToken ct = default)
        {
            _t[transferencia.TransferenciaId] = transferencia;
            return Task.CompletedTask;
        }

        public TransferenciaInventario Add(TransferenciaInventario t)
        {
            _t[t.TransferenciaId] = t;
            return t;
        }
    }

    internal sealed class FakeReservaStockRepository : IReservaStockRepository
    {
        private readonly ConcurrentDictionary<Guid, ReservaStock> _r = new();

        public Task<ReservaStock?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, Guid reservaId, CancellationToken ct = default)
        {
            if (_r.TryGetValue(reservaId, out var res))
            {
                return Task.FromResult(res.EmpresaId == empresaId && res.EstablecimientoId == establecimientoId && res.AlmacenId == almacenId ? res : null);
            }
            return Task.FromResult<ReservaStock?>(null);
        }

        public Task GuardarAsync(ReservaStock reserva, CancellationToken ct = default)
        {
            _r[reserva.ReservaId] = reserva;
            return Task.CompletedTask;
        }

        public ReservaStock Add(ReservaStock r)
        {
            _r[r.ReservaId] = r;
            return r;
        }
    }

    internal sealed class FakeAlmacenRepository : IAlmacenRepository
    {
    private readonly ConcurrentDictionary<(string emp, Guid est, Guid alm), Almacen> _a = new();

        public Task<Almacen?> ObtenerAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, AlmacenId almacenId, CancellationToken ct = default)
        {
            _a.TryGetValue((empresaId.Value, establecimientoId.Value, almacenId.Value), out var a);
            return Task.FromResult<Almacen?>(a);
        }

        public Task<IReadOnlyList<Almacen>> ListarAsync(EmpresaId empresaId, EstablecimientoId establecimientoId, CancellationToken ct = default)
        {
            var list = _a.Where(kv => kv.Key.emp == empresaId.Value && kv.Key.est == establecimientoId.Value).Select(kv => kv.Value).ToList();
            return Task.FromResult((IReadOnlyList<Almacen>)list);
        }

        public Task GuardarAsync(Almacen almacen, CancellationToken ct = default)
        {
            _a[(almacen.EmpresaId.Value, almacen.EstablecimientoId.Value, almacen.AlmacenId.Value)] = almacen;
            return Task.CompletedTask;
        }

        public Almacen Ensure(EmpresaId emp, EstablecimientoId est, AlmacenId alm, string nombre)
        {
            var a = _a.GetOrAdd((emp.Value, est.Value, alm.Value), _ => Almacen.Crear(emp, est, alm, nombre));
            return a;
        }
    }
}
