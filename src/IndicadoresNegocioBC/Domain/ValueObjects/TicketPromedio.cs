using System;

namespace IndicadoresNegocioBC.Domain.ValueObjects
{
    /// <summary>
    /// Value Object que representa el Ticket Promedio de ventas.
    /// 
    /// Definición:
    ///   - MontoTotal: suma de importes de ventas (VO Dinero).
    ///   - CantidadComprobantes: cantidad de comprobantes considerados.
    ///   - Promedio: MontoTotal / CantidadComprobantes (o 0 si no hay datos).
    ///
    /// Características:
    ///   - Inmutable, igualdad por valor.
    ///   - Moneda viene de MontoTotal y se mantiene en todas las operaciones.
    ///   - Pensado para Indicadores: se construye desde datos ya consolidados (no input libre).
    ///
    /// Invariantes:
    ///   - MontoTotal no nulo.
    ///   - CantidadComprobantes >= 0.
    ///   - Si CantidadComprobantes == 0 => MontoTotal debe ser 0 en su moneda.
    ///
    /// Operaciones:
    ///   - AgregarVenta(importe): suma una venta y aumenta el contador.
    ///   - QuitarVenta(importe): revierte una venta y disminuye el contador.
    ///   - Combinar(otro): suma totales y cantidades (misma moneda).
    /// </summary>
    public sealed record class TicketPromedio
    {
        /// <summary>Suma de importes considerados (VO Dinero).</summary>
        public Dinero MontoTotal { get; }

        /// <summary>Cantidad de comprobantes considerados.</summary>
        public int CantidadComprobantes { get; }

        /// <summary>Moneda del ticket (proviene de MontoTotal).</summary>
        public Moneda Moneda => MontoTotal.Moneda;

        /// <summary>Indica si hay al menos un comprobante.</summary>
        public bool TieneDatos => CantidadComprobantes > 0;

        /// <summary>Importe promedio = MontoTotal / CantidadComprobantes (o 0 si no hay datos).</summary>
        public Dinero Promedio => TieneDatos
            ? MontoTotal.Dividir(CantidadComprobantes)
            : Dinero.Cero(Moneda);

        private TicketPromedio(Dinero montoTotal, int cantidadComprobantes)
        {
            MontoTotal = montoTotal ?? throw new ArgumentNullException(nameof(montoTotal));

            if (cantidadComprobantes < 0)
                throw new ArgumentOutOfRangeException(nameof(cantidadComprobantes), "La cantidad no puede ser negativa.");

            if (cantidadComprobantes == 0 && !montoTotal.EsCero)
                throw new ArgumentException("Si la cantidad es 0, el monto total debe ser 0 en la misma moneda.", nameof(montoTotal));

            CantidadComprobantes = cantidadComprobantes;
        }

        /// <summary>Fábrica principal.</summary>
        public static TicketPromedio Crear(Dinero montoTotal, int cantidadComprobantes) =>
            new(montoTotal, cantidadComprobantes);

        /// <summary>Representa un ticket promedio vacío (sin datos) para la moneda dada.</summary>
        public static TicketPromedio Vacio(Moneda moneda)
        {
            if (moneda is null) throw new ArgumentNullException(nameof(moneda));
            return new TicketPromedio(Dinero.Cero(moneda), 0);
        }

        /// <summary>
        /// Suma una venta al acumulado. Requiere misma moneda.
        /// </summary>
        public TicketPromedio AgregarVenta(Dinero importe)
        {
            if (importe is null) throw new ArgumentNullException(nameof(importe));
            var nuevoTotal = MontoTotal.Sumar(importe);
            var nuevaCantidad = CantidadComprobantes + 1;
            return new TicketPromedio(nuevoTotal, nuevaCantidad);
        }

        /// <summary>
        /// Revierte una venta (por anulación/ajuste). Requiere tener al menos 1 comprobante y misma moneda.
        /// Si la nueva cantidad resulta 0, el total debe quedar 0 (si no, se lanza excepción).
        /// </summary>
        public TicketPromedio QuitarVenta(Dinero importe)
        {
            if (importe is null) throw new ArgumentNullException(nameof(importe));
            if (CantidadComprobantes == 0)
                throw new InvalidOperationException("No hay comprobantes para revertir.");

            var nuevoTotal = MontoTotal.Restar(importe);
            var nuevaCantidad = CantidadComprobantes - 1;

            if (nuevaCantidad == 0 && !nuevoTotal.EsCero)
                throw new InvalidOperationException("Inconsistencia: al dejar cantidad en 0, el total debe ser 0.");

            return new TicketPromedio(nuevoTotal, nuevaCantidad);
        }

        /// <summary>
        /// Combina dos tickets promedio (acumulados) de la misma moneda.
        /// </summary>
        public TicketPromedio Combinar(TicketPromedio otro)
        {
            if (otro is null) throw new ArgumentNullException(nameof(otro));
            if (!Equals(Moneda, otro.Moneda))
                throw new InvalidOperationException($"Monedas distintas: {Moneda} vs {otro.Moneda}.");

            var total = MontoTotal.Sumar(otro.MontoTotal);
            var cantidad = checked(CantidadComprobantes + otro.CantidadComprobantes); // checked: evita overflow accidental
            return new TicketPromedio(total, cantidad);
        }

        public override string ToString() =>
            $"{Moneda.Codigo} Total={MontoTotal.Monto:F2} / Cant={CantidadComprobantes} -> Prom={Promedio.Monto:F2}";
    }
}