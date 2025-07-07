using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoSimple
    {
        public Guid Id { get; private set; }
        public CodigoSUNAT Codigo { get; private set; }
        public string Nombre { get; private set; }
        public UnidadMedida Unidad { get; private set; }
        public AfectacionIGV AfectacionIGV { get; private set; }
        public CuentaContable CuentaContable { get; private set; }
        public Presupuesto? Presupuesto { get; private set; }
        public Peso? Peso { get; private set; }

        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

        private ProductoSimple() { }

        public static ProductoSimple Crear(
            Guid id,
            CodigoSUNAT codigo,
            string nombre,
            UnidadMedida unidad,
            AfectacionIGV afectacion,
            CuentaContable cuenta,
            Presupuesto? presupuesto = null,
            Peso? peso = null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));

            var producto = new ProductoSimple
            {
                Id = id,
                Codigo = codigo,
                Nombre = nombre.Trim(),
                Unidad = unidad,
                AfectacionIGV = afectacion,
                CuentaContable = cuenta,
                Presupuesto = presupuesto,
                Peso = peso
            };

            producto.AddDomainEvent(new ProductoCreado(id, codigo.Value, nombre));
            return producto;
        }

        public void Modificar(string nuevoNombre, UnidadMedida nuevaUnidad)
        {
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                throw new ArgumentException("El nombre no puede estar vacío.", nameof(nuevoNombre));

            Nombre = nuevoNombre;
            Unidad = nuevaUnidad;
            AddDomainEvent(new ProductoModificado(Id));
        }

        public void Inhabilitar() => AddDomainEvent(new ProductoInhabilitado(Id));

        private void AddDomainEvent(IDomainEvent @event) => _events.Add(@event);
    }
}
