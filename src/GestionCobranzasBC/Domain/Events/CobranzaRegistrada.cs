using System;
using SharedKernel.Events;

namespace GestionCobranzasBC.Domain.Events;

/// <summary>
/// Se dispara cuando se registra una cobranza (documento C1) asociada a una cuenta por cobrar.
/// </summary>
namespace GestionCobranzasBC.Domain.Events;

using GestionCobranzasBC.Domain.ValueObjects;

public sealed record CobranzaRegistrada(CobranzaId CobranzaId, string NumeroCompleto) : DomainEvent;
