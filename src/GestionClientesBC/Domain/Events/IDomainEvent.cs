using System;

namespace GestionClientesBC.Domain.Events
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
    
}
    
