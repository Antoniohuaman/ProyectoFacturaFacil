namespace ComprobantesElectronicosBC.Domain.Specifications;

public static class SpecExtensions
{
    public static SpecResult EvaluateAll<T>(this IEnumerable<ISpecification<T>> specs, T subject)
    {
        var errors = new List<string>();
        foreach (var s in specs)
        {
            var r = s.Evaluate(subject);
            if (!r.IsValid) errors.AddRange(r.Errors);
        }
        return errors.Count == 0 ? SpecResult.Ok() : SpecResult.Fail(errors);
    }
}
