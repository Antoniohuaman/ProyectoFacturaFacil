namespace GestionClientesBC.Domain.Entities
{
    public enum TipoOperacion
    {
        Venta = 1,
        Compra = 2,
        Pago = 3,
        Cobro = 4,
        NotaCredito = 5,
        NotaDebito = 6,
        Otro = 99
    }
}