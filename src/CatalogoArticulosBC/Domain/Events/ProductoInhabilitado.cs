namespace CatalogoArticulosBC.Domain.Events
{
    public record ProductoInhabilitado(Guid Id) : IDomainEvent
    {
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    }
}
