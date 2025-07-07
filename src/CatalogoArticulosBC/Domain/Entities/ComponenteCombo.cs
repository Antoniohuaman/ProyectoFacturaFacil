namespace CatalogoArticulosBC.Domain.Entities
{
    public class ComponenteCombo
    {
        public Guid ProductoId { get; private set; }
        public int Cantidad { get; private set; }
        public bool Activo { get; private set; }

        public ComponenteCombo(Guid productoId, int cantidad, bool activo = true)
        {
            if (cantidad <= 0)
                throw new ArgumentException("Cantidad debe ser mayor que cero.", nameof(cantidad));

            ProductoId = productoId;
            Cantidad = cantidad;
            Activo = activo;
        }
    }
}
