using System;
using System.Collections.Generic;
using ListaPreciosBC.Domain.ValueObjects;
using ListaPreciosBC.Domain.Entities;

namespace ListaPreciosBC.Domain.Aggregates
{
    public class ListaPrecio
    {
        public Guid ListaPrecioId { get; private set; }
        public TipoLista TipoLista { get; private set; }
        public CriterioLista Criterio { get; private set; }
        public Moneda Moneda { get; private set; }
        public Prioridad Prioridad { get; private set; }
        public PeriodoVigencia Vigencia { get; private set; }
        public bool Activa { get; private set; }
        public DateTime FechaCreacion { get; private set; }

        private readonly List<PrecioEspecifico> _precios = new();
        public IReadOnlyCollection<PrecioEspecifico> Precios => _precios.AsReadOnly();

        public ListaPrecio(
            Guid listaPrecioId,
            TipoLista tipoLista,
            CriterioLista criterio,
            Moneda moneda,
            Prioridad prioridad,
            PeriodoVigencia vigencia,
            DateTime fechaCreacion)
        {
            ListaPrecioId = listaPrecioId;
            TipoLista = tipoLista;
            Criterio = criterio;
            Moneda = moneda;
            Prioridad = prioridad;
            Vigencia = vigencia;
            Activa = true;
            FechaCreacion = fechaCreacion;
        }

        // Métodos de comportamiento (agregarPrecioEspecifico, inhabilitarLista, etc.) se agregan aquí
    }
}