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
        public void AgregarPrecioEspecifico(PrecioEspecifico precio)
        {
            if (precio == null)
                throw new ArgumentNullException(nameof(precio));

            // Validar que la vigencia del precio esté dentro de la vigencia de la lista
            if (precio.Vigencia.FechaInicio < Vigencia.FechaInicio || precio.Vigencia.FechaFin > Vigencia.FechaFin)
                throw new InvalidOperationException("La vigencia del precio debe estar dentro de la vigencia de la lista.");

            // No permitir precios duplicados en el mismo rango/vigencia
            bool existeDuplicado = _precios.Exists(p =>
                p.Moneda == precio.Moneda &&
                p.Prioridad.Valor == precio.Prioridad.Valor &&
                ((p.RangoVolumen == null && precio.RangoVolumen == null) ||
                 (p.RangoVolumen != null && precio.RangoVolumen != null && p.RangoVolumen.Equals(precio.RangoVolumen))) &&
                p.Vigencia.FechaInicio == precio.Vigencia.FechaInicio &&
                p.Vigencia.FechaFin == precio.Vigencia.FechaFin
            );

            if (existeDuplicado)
                throw new InvalidOperationException("Ya existe un precio específico con el mismo rango y vigencia.");

            _precios.Add(precio);
        }
        public void Inhabilitar(string usuarioId)
        {
            if (!Activa)
                throw new InvalidOperationException("La lista de precios ya está inhabilitada.");

            Activa = false;
            // Aquí podrías agregar lógica para auditar el usuario que inhabilitó, si lo necesitas
        }
        public void ActualizarVigencia(PeriodoVigencia nuevaVigencia)
        {
         if (!Activa)
        throw new InvalidOperationException("No se puede modificar la vigencia de una lista inhabilitada.");

        if (nuevaVigencia.FechaInicio > nuevaVigencia.FechaFin)
        throw new InvalidOperationException("La fecha de inicio no puede ser mayor que la fecha de fin.");

        Vigencia = nuevaVigencia;
        }
    }
}