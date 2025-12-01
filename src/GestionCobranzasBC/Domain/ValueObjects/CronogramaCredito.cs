using SharedKernel.Exceptions;

namespace GestionCobranzasBC.Domain.ValueObjects;

/// <summary>
/// Cronograma de cuotas de un crédito asociado a un comprobante.
/// Es un VO que encapsula la colección de cuotas y reglas básicas.
/// </summary>
public sealed record CronogramaCredito
{
    public sealed record Cuota(int Numero, DateOnly FechaVencimiento, decimal Importe);

    public IReadOnlyCollection<Cuota> Cuotas { get; }

    public decimal MontoTotal => Cuotas.Sum(c => c.Importe);

    private CronogramaCredito(IReadOnlyCollection<Cuota> cuotas)
    {
        Cuotas = cuotas;
    }

    public static CronogramaCredito Crear(IEnumerable<Cuota> cuotas)
    {
        if (cuotas is null)
        {
            throw new BusinessRuleException("El cronograma de crédito no puede ser nulo.");
        }

        var lista = cuotas.ToList();

        if (lista.Count == 0)
        {
            throw new BusinessRuleException("El cronograma de crédito debe contener al menos una cuota.");
        }

        if (lista.Any(c => c.Importe <= 0m))
        {
            throw new BusinessRuleException("Todas las cuotas deben tener importe mayor a cero.");
        }

        // Ordenar por número y validar correlatividad.
        lista.Sort((a, b) => a.Numero.CompareTo(b.Numero));

        for (var i = 0; i < lista.Count; i++)
        {
            var esperado = i + 1;
            if (lista[i].Numero != esperado)
            {
                throw new BusinessRuleException("Las cuotas del cronograma deben ser correlativas empezando en 1.");
            }
        }

        // Validar orden de fechas
        for (var i = 1; i < lista.Count; i++)
        {
            if (lista[i].FechaVencimiento < lista[i - 1].FechaVencimiento)
            {
                throw new BusinessRuleException("Las fechas de vencimiento deben ser crecientes.");
            }
        }

        return new CronogramaCredito(lista.AsReadOnly());
    }
}
