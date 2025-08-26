using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    // Identificador de Establecimiento
    public sealed record EstablecimientoId(string Valor)
    {
        public static EstablecimientoId Desde(string v) =>
            string.IsNullOrWhiteSpace(v)
                ? throw new BusinessRuleException("EstablecimientoId es obligatorio.")
                : new(v.Trim());

        public override string ToString() => Valor;
    }
}
