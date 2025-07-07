namespace ControlCajaBC.Domain.ValueObjects
{
    /// <summary>
    /// Saldo acumulado de la caja.
    /// </summary>
    public sealed record SaldoCaja(Monto Value)
    {
        public SaldoCaja Add(Monto monto) => new(Value.Add(monto));
        public SaldoCaja Subtract(Monto monto) => new(Value.Subtract(monto));
    }
}
