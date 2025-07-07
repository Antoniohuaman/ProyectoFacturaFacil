using System.Linq;
using CatalogoArticulosBC.Domain.Entities;
using CatalogoArticulosBC.Domain.Events;
using CatalogoArticulosBC.Domain.ValueObjects;

namespace CatalogoArticulosBC.Domain.Aggregates
{
    public class ProductoVariante
    {
        public Guid Id { get; private set; }
        public string Sku { get; private set; }
        public string Nombre { get; private set; }
        public List<AtributoVariante> Atributos { get; private set; }
        public UnidadMedida Unidad { get; private set; }
        public AfectacionIGV AfectacionIGV { get; private set; }

        private readonly List<IDomainEvent> _events = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

        private ProductoVariante() { }

        public static ProductoVariante Crear(
            Guid id,
            string sku,
            string nombre,
            IEnumerable<AtributoVariante> atributos,
            UnidadMedida unidad,
            AfectacionIGV afectacion)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU no puede estar vacío.", nameof(sku));
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("Nombre no puede estar vacío.", nameof(nombre));
            if (atributos == null || !atributos.Any())
                throw new ArgumentException("Debe tener al menos un atributo.", nameof(atributos));

            var variante = new ProductoVariante
            {
                Id = id,
                Sku = sku,
                Nombre = nombre.Trim(),
                Atributos = atributos.ToList(),
                Unidad = unidad,
                AfectacionIGV = afectacion
            };

            variante.AddDomainEvent(new ProductoCreado(id, sku, nombre));
            return variante;
        }

        public void ModificarNombre(string nuevoNombre)
        {
            if (string.IsNullOrWhiteSpace(nuevoNombre))
                throw new ArgumentException("Nombre inválido.", nameof(nuevoNombre));

            Nombre = nuevoNombre;
            AddDomainEvent(new ProductoModificado(Id));
        }

        private void AddDomainEvent(IDomainEvent @event) => _events.Add(@event);
    }
}
