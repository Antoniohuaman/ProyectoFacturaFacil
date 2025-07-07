namespace CatalogoArticulosBC.Domain.Events
{
    public record ProductoCreado(Guid Id, string Sku, string Nombre) : IDomainEvent
    {
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
}
