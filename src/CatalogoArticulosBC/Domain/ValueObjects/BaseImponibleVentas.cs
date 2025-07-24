using System;

namespace CatalogoArticulosBC.Domain.ValueObjects
{
    public class BaseImponibleVentas
    {
        public decimal Valor { get; }

        public BaseImponibleVentas(decimal valor)
        {
            if (valor < 0)
                throw new ArgumentException("La base imponible no puede ser negativa.", nameof(valor));
            Valor = valor;
        }

        public override string ToString() => Valor.ToString("F2");
    }
}