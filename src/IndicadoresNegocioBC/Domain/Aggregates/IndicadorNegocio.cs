using System;
using System.Collections.Generic;
using System.Linq;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Domain.Aggregates
{
    /// <summary>
    /// AGGREGATE ROOT: IndicadorNegocio
    ///
    /// Clave natural (unicidad lógica): TipoIndicador + Periodo (alineado) + SegmentoIndicador
    /// Estado de ciclo de vida: EstadoIndicador (CREADO → ACTUALIZADO → CONSOLIDADO).
    ///
    /// Este agregado modela la “fotografía” de KPIs para un periodo/segmento específico y
    /// aplica mutaciones impulsadas por eventos de ventas (aceptadas/anuladas) con idempotencia.
    ///
    /// Invariantes clave:
    /// - El Periodo debe contener todas las ventas aplicadas.
    /// - La Moneda del Segmento gobierna TODAS las cifras monetarias.
    /// - Si Estado = CONSOLIDADO no admite mutaciones.
    /// - Idempotencia por ComprobanteId (no duplicar ni re-anular).
    ///
    /// Datos que mantiene (dependiendo del TipoIndicador):
    /// - VENTA_DIARIA: ventas por fecha (total, IGV, nroComprobantes).
    /// - RANKING_PRODUCTOS: acumulados por producto (cantidad, total vendido).
    /// - RANKING_CLIENTES: acumulados por cliente (frecuencia, total).
    /// - TICKET_PROMEDIO: total y conteo (VO TicketPromedio).
    /// </summary>
    // Enum para tipo de comprobante (Boleta/Factura)

    public sealed class IndicadorNegocio
    // ...existing code...
    {
        // ------------------ Identidad y clave natural ------------------
        public Guid IndicadorId { get; }
        public TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }

        // ------------------ Estado de ciclo de vida --------------------
        public EstadoIndicador Estado { get; private set; }
        public DateTimeOffset CreadoEn { get; }
        public DateTimeOffset? ConsolidadoEn { get; private set; }

        // Concurrencia optimista (si la infraestructura lo requiere)
        public int Version { get; private set; }

        // ------------------ Métricas acumuladas ------------------------
        // Ventas diarias
        private readonly Dictionary<DateOnly, VentaDiaria> _ventasDiarias = new();
        public IReadOnlyCollection<VentaDiaria> VentasDiarias => _ventasDiarias.Values
            .OrderBy(v => v.Fecha)
            .ToList()
            .AsReadOnly();

        // Ranking productos (por productoId)
        private readonly Dictionary<string, RankingProductoEntrada> _rankingProductos = new(StringComparer.Ordinal);
        public IReadOnlyCollection<RankingProductoEntrada> RankingProductos => _rankingProductos.Values.ToList().AsReadOnly();

        // Ranking clientes (por clienteId)
        private readonly Dictionary<Guid, RankingClienteEntrada> _rankingClientes = new();
        public IReadOnlyCollection<RankingClienteEntrada> RankingClientes => _rankingClientes.Values.ToList().AsReadOnly();

        // Ticket promedio
        public TicketPromedio TicketPromedio { get; private set; }

        // Ranking vendedores (por vendedorId)
        private readonly Dictionary<UsuarioId, RankingVendedorEntrada> _rankingVendedores = new();
        public IReadOnlyCollection<RankingVendedorEntrada> RankingVendedores => _rankingVendedores.Values.ToList().AsReadOnly();

        // Idempotencia: ventas aplicadas por ComprobanteId (y su detalle) para permitir reversión
        private readonly Dictionary<Guid, VentaRegistrada> _ventasPorComprobante = new();

        // ------------------ Fábrica ------------------
        private IndicadorNegocio(
            Guid indicadorId,
            TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateTimeOffset creadoEn)
        {
            IndicadorId = indicadorId == Guid.Empty ? Guid.NewGuid() : indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Estado = EstadoIndicador.Creado;
            CreadoEn = creadoEn;

            // Ticket en cero en la moneda del segmento
            TicketPromedio = TicketPromedio.Vacio(Segmento.Moneda);

            // Por seguridad (si el periodo es alineado, validamos)
            Periodo.AsegurarAlineado();
        }

        public static IndicadorNegocio Crear(
            TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateTimeOffset? ahora = null)
        {
            return new IndicadorNegocio(
                indicadorId: Guid.NewGuid(),
                tipo: tipo,
                periodo: periodo,
                segmento: segmento,
                creadoEn: ahora ?? DateTimeOffset.UtcNow);
        }

        // ------------------ Mutaciones de dominio ------------------

        /// <summary>
        /// Aplica una venta aceptada (idempotente). Rechaza si:
        /// - El agregado está CONSOLIDADO,
        /// - La fecha está fuera del Periodo,
        /// - La moneda del comprobante no coincide con Segmento.Moneda.
        /// </summary>
        public void RegistrarVentaAceptada(ComprobanteVenta venta)
        {
            if (venta is null) throw new ArgumentNullException(nameof(venta));
            AsegurarPermiteMutaciones();

            // Moneda y periodo
            AsegurarMismaMoneda(venta.Total);
            AsegurarMismaMoneda(venta.Igv);                    // <-- ajuste
            foreach (var it in venta.Items)                    // <-- ajuste
                AsegurarMismaMoneda(it.Subtotal);              // <-- ajuste

            if (!Periodo.Contiene(venta.Fecha))
                throw new InvalidOperationException("La venta no pertenece al periodo del indicador.");

            // Idempotencia: si ya fue aplicada, no hacer nada.
            if (_ventasPorComprobante.ContainsKey(venta.ComprobanteId))
                return;

            // --- Acumular ventas diarias ---
            var dia = venta.Fecha;
            if (!_ventasDiarias.TryGetValue(dia, out var vd))
            {
                vd = new VentaDiaria(dia, Dinero.Cero(Segmento.Moneda), Dinero.Cero(Segmento.Moneda), 0);
                _ventasDiarias.Add(dia, vd);
            }
            vd.Agregar(venta.Total, venta.Igv);

            // --- Ticket promedio ---
            TicketPromedio = TicketPromedio.AgregarVenta(venta.Total);

            // --- Ranking productos ---
            foreach (var it in venta.Items)
            {
                if (!_rankingProductos.TryGetValue(it.ProductoId, out var rp))
                {
                    rp = new RankingProductoEntrada(it.ProductoId, 0m, Dinero.Cero(Segmento.Moneda));
                    _rankingProductos.Add(it.ProductoId, rp);
                }
                rp.Acumular(it.Cantidad, it.Subtotal);
            }

            // --- Ranking clientes ---
            if (venta.ClienteId.HasValue)
            {
                var clienteId = venta.ClienteId.Value;
                if (!_rankingClientes.TryGetValue(clienteId, out var rc))
                {
                    rc = new RankingClienteEntrada(clienteId, 0, Dinero.Cero(Segmento.Moneda));
                    _rankingClientes.Add(clienteId, rc);
                }
                rc.Acumular(venta.Total);
            }

            // --- Ranking vendedores ---
            if (venta.VendedorId != null)
            {
                var vendedorId = venta.VendedorId;
                if (!_rankingVendedores.TryGetValue(vendedorId, out var rv))
                {
                    rv = new RankingVendedorEntrada(vendedorId, Dinero.Cero(Segmento.Moneda));
                    _rankingVendedores.Add(vendedorId, rv);
                }
                rv.Acumular(venta.Total);
            }

            // Registrar para reversión (normalizamos el tipo de comprobante)  <-- ajuste
            var tipoNorm = NormalizarTipo(venta.TipoComprobante);
            _ventasPorComprobante.Add(venta.ComprobanteId, new VentaRegistrada(
                venta.ComprobanteId,
                venta.Fecha,
                venta.ClienteId,
                venta.Total,
                venta.Igv,
                venta.Items.Select(i => new ItemRegistrado(i.ProductoId, i.Cantidad, i.Subtotal)),
                venta.VendedorId,
                tipoNorm,
                venta.EstablecimientoId
            ));

            // Estado → ACTUALIZADO (si aplica)
            TransicionarA(EstadoIndicador.Actualizado);

            // bump versión
            Version++;
        }

        /// <summary>
        /// Obtiene el total de ventas filtrando por tipo de comprobante (boleta o factura) y rango de fechas.
        /// Si no se especifica rango, usa todo el periodo del aggregate.
        /// </summary>
        public Dinero ObtenerTotalPorTipoComprobante(string tipoComprobante, DateOnly? desde = null, DateOnly? hasta = null, EstablecimientoId? establecimientoId = null)
        {
            var tipoNorm = NormalizarTipo(tipoComprobante); // <-- ajuste

            var ventas = _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.TipoComprobante == tipoNorm);

            // filtros flexibles de rango  <-- ajuste
            if (desde.HasValue) ventas = ventas.Where(v => v.Fecha >= desde.Value);
            if (hasta.HasValue) ventas = ventas.Where(v => v.Fecha <= hasta.Value);

            if (establecimientoId != null)
                ventas = ventas.Where(v => v.EstablecimientoId == establecimientoId);

            return ventas.Select(v => v.Total).Aggregate(Dinero.Cero(Segmento.Moneda), (a, b) => a.Sumar(b));
        }

        /// <summary>
        /// Obtiene el número de comprobantes filtrando por tipo de comprobante y rango de fechas.
        /// </summary>
        public int ObtenerCantidadPorTipoComprobante(string tipoComprobante, DateOnly? desde = null, DateOnly? hasta = null, EstablecimientoId? establecimientoId = null)
        {
            var tipoNorm = NormalizarTipo(tipoComprobante); // <-- ajuste

            var ventas = _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.TipoComprobante == tipoNorm);

            // filtros flexibles de rango  <-- ajuste
            if (desde.HasValue) ventas = ventas.Where(v => v.Fecha >= desde.Value);
            if (hasta.HasValue) ventas = ventas.Where(v => v.Fecha <= hasta.Value);

            if (establecimientoId != null)
                ventas = ventas.Where(v => v.EstablecimientoId == establecimientoId);

            return ventas.Count();
        }

        /// <summary>
        /// Revierte una venta previamente aplicada (idempotente).
        /// Si el comprobante no existe o ya fue anulado, no hace nada.
        /// </summary>
        public void RegistrarAnulacion(Guid comprobanteId)
        {
            AsegurarPermiteMutaciones();

            if (!_ventasPorComprobante.TryGetValue(comprobanteId, out var venta) || venta.Anulada)
                return; // idempotente: nada que revertir

            // --- Revertir ventas diarias ---
            var dia = venta.Fecha;
            if (_ventasDiarias.TryGetValue(dia, out var vd))
            {
                vd.Quitar(venta.Total, venta.Igv);

                // si quedó en cero, limpiar el día
                if (vd.NroComprobantes == 0 && vd.TotalVentas.EsCero && vd.TotalIgv.EsCero)
                    _ventasDiarias.Remove(dia);
            }

            // --- Ticket promedio ---
            TicketPromedio = TicketPromedio.QuitarVenta(venta.Total);

            // --- Ranking productos ---
            foreach (var it in venta.Items)
            {
                if (_rankingProductos.TryGetValue(it.ProductoId, out var rp))
                {
                    rp.Revertir(it.Cantidad, it.Subtotal);
                    if (rp.EsCero)
                        _rankingProductos.Remove(it.ProductoId);
                }
            }

            // --- Ranking clientes ---
            if (venta.ClienteId.HasValue && _rankingClientes.TryGetValue(venta.ClienteId.Value, out var rc))
            {
                rc.Revertir(venta.Total);
                if (rc.EsCero)
                    _rankingClientes.Remove(venta.ClienteId.Value);
            }

            // --- Ranking vendedores ---
            if (venta.VendedorId != null && _rankingVendedores.TryGetValue(venta.VendedorId, out var rv))
            {
                rv.Revertir(venta.Total);
                if (rv.EsCero)
                    _rankingVendedores.Remove(venta.VendedorId);
            }

            venta.MarcarAnulada();
            Version++;
        }

        /// <summary>Entrada de ranking de vendedores.</summary>
        public sealed class RankingVendedorEntrada
        {
            public UsuarioId VendedorId { get; }
            public Dinero TotalVendido { get; private set; }
            public bool EsCero => TotalVendido.EsCero;

            public RankingVendedorEntrada(UsuarioId vendedorId, Dinero totalVendido)
            {
                VendedorId = vendedorId ?? throw new ArgumentNullException(nameof(vendedorId));
                TotalVendido = totalVendido ?? throw new ArgumentNullException(nameof(totalVendido));
            }

            public void Acumular(Dinero total)
            {
                TotalVendido = TotalVendido.Sumar(total);
            }

            public void Revertir(Dinero total)
            {
                TotalVendido = TotalVendido.Restar(total);
                if (TotalVendido.Monto < 0m)
                    throw new InvalidOperationException("Reversión deja valores negativos en ranking de vendedores.");
            }
        }

        /// <summary>
        /// Marca el periodo como CONSOLIDADO (estado final; bloquea mutaciones).
        /// </summary>
        public void ConsolidarPeriodo(DateTimeOffset? ahora = null)
        {
            AsegurarTransicion(EstadoIndicador.Consolidado);
            Estado = EstadoIndicador.Consolidado;
            ConsolidadoEn = ahora ?? DateTimeOffset.UtcNow;
            Version++;
        }

        // ------------------ Consultas de apoyo al dashboard ------------------

        public Dinero TotalVentas => TicketPromedio.MontoTotal;
        public int TotalComprobantes => TicketPromedio.CantidadComprobantes;

        public IReadOnlyList<VentaDiaria> ObtenerVentasDiariasOrdenadas() =>
            _ventasDiarias.Values.OrderBy(v => v.Fecha).ToList();

        public IReadOnlyList<RankingProductoEntrada> ObtenerTopProductos(LimiteTop limite, RankingCriterio criterio)
        {
            IEnumerable<RankingProductoEntrada> q = _rankingProductos.Values;
            q = criterio == RankingCriterio.PorMonto
                ? q.OrderByDescending(x => x.TotalVendido.Monto).ThenByDescending(x => x.Cantidad)
                : q.OrderByDescending(x => x.Cantidad).ThenByDescending(x => x.TotalVendido.Monto);

            return q.Take(limite.Valor).ToList();
        }

        public IReadOnlyList<RankingClienteEntrada> ObtenerTopClientes(LimiteTop limite)
        {
            return _rankingClientes.Values
                .OrderByDescending(x => x.TotalComprado.Monto)
                .ThenByDescending(x => x.Frecuencia)
                .Take(limite.Valor)
                .ToList();
        }

        // ================== Consultas flexibles por rango de fechas ==================

        /// <summary>
        /// Obtiene todas las ventas registradas en el rango de fechas (inclusive).
        /// </summary>
        public IReadOnlyList<VentaRegistrada> ObtenerVentasPorRango(DateOnly desde, DateOnly hasta)
        {
            if (desde > hasta) throw new ArgumentException("El rango de fechas es inválido.");
            return _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta)
                .OrderBy(v => v.Fecha)
                .ToList();
        }

        /// <summary>
        /// Obtiene el ranking de vendedores en el rango de fechas indicado.
        /// </summary>
        public IReadOnlyList<RankingVendedorEntrada> ObtenerRankingVendedoresPorRango(DateOnly desde, DateOnly hasta, LimiteTop? limite = null)
        {
            if (desde > hasta) throw new ArgumentException("El rango de fechas es inválido.");
            var ventas = _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta && v.VendedorId != null);

            var ranking = ventas
                .GroupBy(v => v.VendedorId!)
                .Select(g => new RankingVendedorEntrada(g.Key, g.Select(x => x.Total).Aggregate(Dinero.Cero(Segmento.Moneda), (a, b) => a.Sumar(b))))
                .OrderByDescending(x => x.TotalVendido.Monto)
                .ToList();

            return limite != null ? ranking.Take(limite.Valor).ToList() : ranking;
        }

        /// <summary>
        /// Obtiene el ranking de productos en el rango de fechas indicado.
        /// </summary>
        public IReadOnlyList<RankingProductoEntrada> ObtenerRankingProductosPorRango(DateOnly desde, DateOnly hasta, LimiteTop? limite = null, RankingCriterio criterio = RankingCriterio.PorMonto)
        {
            if (desde > hasta) throw new ArgumentException("El rango de fechas es inválido.");
            var ventas = _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta);

            var productos = ventas
                .SelectMany(v => v.Items)
                .GroupBy(i => i.ProductoId)
                .Select(g => new RankingProductoEntrada(
                    g.Key,
                    g.Sum(x => x.Cantidad),
                    g.Select(x => x.Subtotal).Aggregate(Dinero.Cero(Segmento.Moneda), (a, b) => a.Sumar(b))
                ));

            var ordenado = criterio == RankingCriterio.PorMonto
                ? productos.OrderByDescending(x => x.TotalVendido.Monto).ThenByDescending(x => x.Cantidad)
                : productos.OrderByDescending(x => x.Cantidad).ThenByDescending(x => x.TotalVendido.Monto);

            var lista = ordenado.ToList();
            return limite != null ? lista.Take(limite.Valor).ToList() : lista;
        }

        /// <summary>
        /// Obtiene el ranking de clientes en el rango de fechas indicado.
        /// </summary>
        public IReadOnlyList<RankingClienteEntrada> ObtenerRankingClientesPorRango(DateOnly desde, DateOnly hasta, LimiteTop? limite = null)
        {
            if (desde > hasta) throw new ArgumentException("El rango de fechas es inválido.");
            var ventas = _ventasPorComprobante.Values
                .Where(v => !v.Anulada && v.Fecha >= desde && v.Fecha <= hasta && v.ClienteId.HasValue);

            var clientes = ventas
                .GroupBy(v => v.ClienteId!.Value)
                .Select(g => new RankingClienteEntrada(
                    g.Key,
                    g.Count(),
                    g.Select(x => x.Total).Aggregate(Dinero.Cero(Segmento.Moneda), (a, b) => a.Sumar(b))
                ))
                .OrderByDescending(x => x.TotalComprado.Monto)
                .ThenByDescending(x => x.Frecuencia)
                .ToList();

            return limite != null ? clientes.Take(limite.Valor).ToList() : clientes;
        }

        // ------------------ Helpers de dominio ------------------

        private void AsegurarPermiteMutaciones()
        {
            if (!Estado.PermiteMutaciones)
                throw new InvalidOperationException("El indicador está consolidado y no admite cambios.");
        }

        private void TransicionarA(EstadoIndicador destino)
        {
            // tolera igualdad por referencia o por valor (sin romper tu smart-enum)  <-- ajuste
            if (!ReferenceEquals(Estado, destino) && !Equals(Estado, destino))
                EstadoIndicador.AsegurarTransicionValida(Estado, destino);
            Estado = destino;
        }

        private void AsegurarTransicion(EstadoIndicador destino) =>
            EstadoIndicador.AsegurarTransicionValida(Estado, destino);

        private void AsegurarMismaMoneda(Dinero dinero)
        {
            if (dinero is null) throw new ArgumentNullException(nameof(dinero));
            if (!Equals(dinero.Moneda, Segmento.Moneda))
                throw new InvalidOperationException($"Moneda distinta a la del segmento: {dinero.Moneda} ≠ {Segmento.Moneda}.");
        }

        private static string NormalizarTipo(string tipoComprobante)
        {
            if (string.IsNullOrWhiteSpace(tipoComprobante))
                throw new ArgumentNullException(nameof(tipoComprobante));
            return tipoComprobante.Trim().ToUpperInvariant();
        }

        // ================== Tipos internos del agregado ==================

        /// <summary>Tipo/categoría del indicador (smart-enum).</summary>
        public sealed record TipoIndicador
        {
            public byte Codigo { get; }
            public string Nombre { get; }

            private TipoIndicador(byte codigo, string nombre)
            {
                if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido.", nameof(nombre));
                Codigo = codigo;
                Nombre = nombre.Trim().ToUpperInvariant();
            }

            public override string ToString() => Nombre;

            // Instancias soportadas
            public static readonly TipoIndicador VentaDiaria      = new(1, "VENTA_DIARIA");
            public static readonly TipoIndicador RankingProductos = new(2, "RANKING_PRODUCTOS");
            public static readonly TipoIndicador RankingClientes  = new(3, "RANKING_CLIENTES");
            public static readonly TipoIndicador TicketPromedio   = new(4, "TICKET_PROMEDIO");
        }

        public enum RankingCriterio { PorMonto = 1, PorCantidad = 2 }

        /// <summary>Entrada de ranking de productos.</summary>
        public sealed class RankingProductoEntrada
        {
            public string ProductoId { get; }
            public decimal Cantidad { get; private set; }
            public Dinero TotalVendido { get; private set; }

            public bool EsCero => Cantidad == 0m && TotalVendido.EsCero;

            public RankingProductoEntrada(string productoId, decimal cantidad, Dinero totalVendido)
            {
                ProductoId = !string.IsNullOrWhiteSpace(productoId) ? productoId : throw new ArgumentException("ProductoId requerido.", nameof(productoId));
                if (cantidad < 0m) throw new ArgumentOutOfRangeException(nameof(cantidad));
                TotalVendido = totalVendido ?? throw new ArgumentNullException(nameof(totalVendido));
                Cantidad = cantidad;
            }

            public void Acumular(decimal cantidad, Dinero subtotal)
            {
                if (cantidad < 0m) throw new ArgumentOutOfRangeException(nameof(cantidad));
                TotalVendido = TotalVendido.Sumar(subtotal);
                Cantidad += cantidad;
            }

            public void Revertir(decimal cantidad, Dinero subtotal)
            {
                if (cantidad < 0m) throw new ArgumentOutOfRangeException(nameof(cantidad));
                TotalVendido = TotalVendido.Restar(subtotal);
                Cantidad -= cantidad;
                if (Cantidad < 0m || TotalVendido.Monto < 0m)
                    throw new InvalidOperationException("Reversión deja valores negativos en ranking de productos.");
            }
        }

        /// <summary>Entrada de ranking de clientes.</summary>
        public sealed class RankingClienteEntrada
        {
            public Guid ClienteId { get; }
            public int Frecuencia { get; private set; }
            public Dinero TotalComprado { get; private set; }

            public bool EsCero => Frecuencia == 0 && TotalComprado.EsCero;

            public RankingClienteEntrada(Guid clienteId, int frecuencia, Dinero totalComprado)
            {
                if (clienteId == Guid.Empty) throw new ArgumentException("ClienteId vacío.", nameof(clienteId));
                if (frecuencia < 0) throw new ArgumentOutOfRangeException(nameof(frecuencia));
                ClienteId = clienteId;
                Frecuencia = frecuencia;
                TotalComprado = totalComprado ?? throw new ArgumentNullException(nameof(totalComprado));
            }

            public void Acumular(Dinero total)
            {
                TotalComprado = TotalComprado.Sumar(total);
                Frecuencia++;
            }

            public void Revertir(Dinero total)
            {
                TotalComprado = TotalComprado.Restar(total);
                Frecuencia--;
                if (Frecuencia < 0 || TotalComprado.Monto < 0m)
                    throw new InvalidOperationException("Reversión deja valores negativos en ranking de clientes.");
            }
        }

        /// <summary>Resumen de ventas de un día.</summary>
        public sealed class VentaDiaria
        {
            public DateOnly Fecha { get; }
            public Dinero TotalVentas { get; private set; }
            public Dinero TotalIgv { get; private set; }
            public int NroComprobantes { get; private set; }

            internal VentaDiaria(DateOnly fecha, Dinero totalVentas, Dinero totalIgv, int nro)
            {
                Fecha = fecha;
                TotalVentas = totalVentas ?? throw new ArgumentNullException(nameof(totalVentas));
                TotalIgv = totalIgv ?? throw new ArgumentNullException(nameof(totalIgv));
                if (nro < 0) throw new ArgumentOutOfRangeException(nameof(nro));
                NroComprobantes = nro;
            }

            internal void Agregar(Dinero total, Dinero igv)
            {
                TotalVentas = TotalVentas.Sumar(total);
                TotalIgv = TotalIgv.Sumar(igv);
                NroComprobantes++;
            }

            internal void Quitar(Dinero total, Dinero igv)
            {
                TotalVentas = TotalVentas.Restar(total);
                TotalIgv = TotalIgv.Restar(igv);
                NroComprobantes--;
                if (NroComprobantes < 0 || TotalVentas.Monto < 0m || TotalIgv.Monto < 0m)
                    throw new InvalidOperationException("Reversión deja valores negativos en venta diaria.");
            }
        }

        /// <summary>Venta registrada para asegurar idempotencia y soportar reversión.</summary>
        public sealed class VentaRegistrada
        {
            public Guid ComprobanteId { get; }
            public DateOnly Fecha { get; }
            public Guid? ClienteId { get; }
            public Dinero Total { get; }
            public Dinero Igv { get; }
            public IReadOnlyList<ItemRegistrado> Items { get; }
            public UsuarioId? VendedorId { get; }
            public bool Anulada { get; private set; }
            public string TipoComprobante { get; }
            public EstablecimientoId EstablecimientoId { get; }

            public VentaRegistrada(
                Guid comprobanteId,
                DateOnly fecha,
                Guid? clienteId,
                Dinero total,
                Dinero igv,
                IEnumerable<ItemRegistrado> items,
                UsuarioId? vendedorId,
                string tipoComprobante,
                EstablecimientoId establecimientoId)
            {
                ComprobanteId = comprobanteId;
                Fecha = fecha;
                ClienteId = clienteId;
                Total = total;
                Igv = igv;
                Items = items.ToList();
                VendedorId = vendedorId;
                TipoComprobante = tipoComprobante ?? throw new ArgumentNullException(nameof(tipoComprobante));
                EstablecimientoId = establecimientoId ?? throw new ArgumentNullException(nameof(establecimientoId));
            }

            public void MarcarAnulada() => Anulada = true;
        }

        public sealed record ItemRegistrado(string ProductoId, decimal Cantidad, Dinero Subtotal);

        // ------------------ DTO de entrada (desde Application) ------------------

        /// <summary>
        /// “ComprobanteVenta” es un dato de entrada de la capa de aplicación (evento ya validado)
        /// con los mínimos necesarios para mutar el agregado. Todos los Dinero deben venir en la
        /// misma moneda que Segmento.Moneda.
        /// </summary>
        public sealed class ComprobanteVenta
        {
            public string TipoComprobante { get; }
            public Guid ComprobanteId { get; }
            public DateOnly Fecha { get; }
            public Guid? ClienteId { get; }
            public Dinero Total { get; }
            public Dinero Igv { get; }
            public IReadOnlyList<Item> Items { get; }
            public UsuarioId? VendedorId { get; }
            public EstablecimientoId EstablecimientoId { get; }

            public ComprobanteVenta(
                Guid comprobanteId,
                DateOnly fecha,
                Guid? clienteId,
                Dinero total,
                Dinero igv,
                IEnumerable<Item> items,
                UsuarioId? vendedorId,
                string tipoComprobante,
                EstablecimientoId establecimientoId)
            {
                if (comprobanteId == Guid.Empty) throw new ArgumentException("ComprobanteId vacío.", nameof(comprobanteId));
                if (items is null) throw new ArgumentNullException(nameof(items));
                var lista = items.ToList();
                if (lista.Count == 0) throw new ArgumentException("La venta debe contener al menos un ítem.", nameof(items));
                if (total is null) throw new ArgumentNullException(nameof(total));
                if (igv is null) throw new ArgumentNullException(nameof(igv));
                if (string.IsNullOrWhiteSpace(tipoComprobante)) throw new ArgumentNullException(nameof(tipoComprobante));
                if (establecimientoId is null) throw new ArgumentNullException(nameof(establecimientoId));

                ComprobanteId = comprobanteId;
                Fecha = fecha;
                ClienteId = clienteId;
                Total = total;
                Igv = igv;
                Items = lista;
                VendedorId = vendedorId;
                TipoComprobante = tipoComprobante;
                EstablecimientoId = establecimientoId;
            }

            public sealed class Item
            {
                public string ProductoId { get; }
                public decimal Cantidad { get; }
                public Dinero Subtotal { get; }

                public Item(string productoId, decimal cantidad, Dinero subtotal)
                {
                    if (string.IsNullOrWhiteSpace(productoId)) throw new ArgumentException("ProductoId requerido.", nameof(productoId));
                    if (cantidad <= 0m) throw new ArgumentOutOfRangeException(nameof(cantidad), "Cantidad debe ser > 0.");
                    ProductoId = productoId;
                    Cantidad = cantidad;
                    Subtotal = subtotal ?? throw new ArgumentNullException(nameof(subtotal));
                }
            }
        }
    }
}
