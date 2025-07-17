using System;
using ListaPreciosBC.Domain.ValueObjects;

namespace ListaPreciosBC.Domain.Entities
{
    public class PrecioEspecifico
    {
        public Guid PrecioEspecificoId { get; private set; }
        public decimal Valor { get; private set; }
        public Moneda Moneda { get; private set; }
        public Prioridad Prioridad { get; private set; }
        public PeriodoVigencia Vigencia { get; private set; }
        public RangoVolumen? RangoVolumen { get; private set; }
        public bool Activo { get; private set; }
        public Guid ListaPrecioId { get; private set; }

        public PrecioEspecifico(
            Guid precioEspecificoId,
            Guid listaPrecioId,
            decimal valor,
            Moneda moneda,
            Prioridad prioridad,
            PeriodoVigencia vigencia,
            RangoVolumen? rangoVolumen = null)
        {
            PrecioEspecificoId = precioEspecificoId;
            ListaPrecioId = listaPrecioId;
            Valor = valor;
            Moneda = moneda;
            Prioridad = prioridad;
            Vigencia = vigencia;
            RangoVolumen = rangoVolumen;
            Activo = true;
        }

        /// <summary>
        /// Modifica los atributos permitidos y retorna los cambios realizados.
        /// </summary>
        public List<(string Campo, object? Anterior, object? Nuevo)> Modificar(
            decimal? nuevoValor,
            Moneda? nuevaMoneda,
            DateTime? nuevaFechaInicio,
            DateTime? nuevaFechaFin)
        {
            var cambios = new List<(string, object?, object?)>();

            if (nuevoValor.HasValue && Valor != nuevoValor.Value)
            {
                cambios.Add(("Valor", Valor, nuevoValor.Value));
                Valor = nuevoValor.Value;
            }
            if (nuevaMoneda.HasValue && Moneda != nuevaMoneda.Value)
            {
                cambios.Add(("Moneda", Moneda, nuevaMoneda.Value));
                Moneda = nuevaMoneda.Value;
            }
            if (nuevaFechaInicio.HasValue && Vigencia.FechaInicio != nuevaFechaInicio.Value)
            {
                cambios.Add(("FechaInicio", Vigencia.FechaInicio, nuevaFechaInicio.Value));
                Vigencia = new PeriodoVigencia(nuevaFechaInicio.Value, Vigencia.FechaFin);
            }
            if (nuevaFechaFin.HasValue && Vigencia.FechaFin != nuevaFechaFin.Value)
            {
                cambios.Add(("FechaFin", Vigencia.FechaFin, nuevaFechaFin.Value));
                Vigencia = new PeriodoVigencia(Vigencia.FechaInicio, nuevaFechaFin.Value);
            }

            return cambios;
        }

        // Otros métodos de comportamiento (validarValor, esVigente, etc.) pueden ir aquí
    
        // Métodos de comportamiento (validarValor, esVigente, etc.) se agregan aquí
    }
}