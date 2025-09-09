using System;
using System.Collections.Generic;
using IndicadoresNegocioBC.Domain.ValueObjects;
using SharedKernel.ValueObjects;

namespace IndicadoresNegocioBC.Application.UseCases.IndicadorNegocio
{
    /// <summary>
    /// DTO de salida para el Top de clientes.
    /// Incluye metadatos del indicador y (si se usó) el rango consultado.
    /// </summary>
    public sealed class ObtenerTopClientesOutputDto
    {
        public Guid IndicadorId { get; }
        public Domain.Aggregates.IndicadorNegocio.TipoIndicador Tipo { get; }
        public Periodo Periodo { get; }
        public SegmentoIndicador Segmento { get; }
        public DateOnly? Desde { get; }
        public DateOnly? Hasta { get; }
        public IReadOnlyList<Item> TopClientes { get; }

        public ObtenerTopClientesOutputDto(
            Guid indicadorId,
            Domain.Aggregates.IndicadorNegocio.TipoIndicador tipo,
            Periodo periodo,
            SegmentoIndicador segmento,
            DateOnly? desde,
            DateOnly? hasta,
            IReadOnlyList<Item> items)
        {
            IndicadorId = indicadorId;
            Tipo = tipo ?? throw new ArgumentNullException(nameof(tipo));
            Periodo = periodo ?? throw new ArgumentNullException(nameof(periodo));
            Segmento = segmento ?? throw new ArgumentNullException(nameof(segmento));
            Desde = desde;
            Hasta = hasta;
            TopClientes = items ?? throw new ArgumentNullException(nameof(items));
        }

        public sealed class Item
        {
            public Guid ClienteId { get; }
            public int Frecuencia { get; }
            public Dinero TotalComprado { get; }

            public Item(Guid clienteId, int frecuencia, Dinero totalComprado)
            {
                if (clienteId == Guid.Empty) throw new ArgumentException("ClienteId vacío.", nameof(clienteId));
                if (frecuencia < 0) throw new ArgumentOutOfRangeException(nameof(frecuencia));
                ClienteId = clienteId;
                Frecuencia = frecuencia;
                TotalComprado = totalComprado ?? throw new ArgumentNullException(nameof(totalComprado));
            }
        }
    }
}
