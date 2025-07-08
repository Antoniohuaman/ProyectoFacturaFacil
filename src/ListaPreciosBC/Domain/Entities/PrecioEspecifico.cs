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

        public PrecioEspecifico(
            Guid precioEspecificoId,
            decimal valor,
            Moneda moneda,
            Prioridad prioridad,
            PeriodoVigencia vigencia,
            RangoVolumen? rangoVolumen = null)
        {
            PrecioEspecificoId = precioEspecificoId;
            Valor = valor;
            Moneda = moneda;
            Prioridad = prioridad;
            Vigencia = vigencia;
            RangoVolumen = rangoVolumen;
            Activo = true;
        }

        // Métodos de comportamiento (validarValor, esVigente, etc.) se agregan aquí
    }
}