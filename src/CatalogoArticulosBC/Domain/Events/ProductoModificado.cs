namespace CatalogoArticulosBC.Domain.Events
{
    public record ProductoModificado(Guid Id) : IDomainEvent
    {
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
}
