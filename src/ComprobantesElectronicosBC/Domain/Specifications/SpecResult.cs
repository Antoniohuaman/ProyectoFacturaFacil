using System.Collections.ObjectModel;

namespace ComprobantesElectronicosBC.Domain.Specifications;

/// <summary>
/// Resultado de evaluar una especificación.
/// </summary>
public readonly struct SpecResult
{
    public bool IsValid { get; }
    public ReadOnlyCollection<string> Errors { get; }

    private SpecResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = new ReadOnlyCollection<string>(errors.ToList());
    }

    public static SpecResult Ok() => new(true, Array.Empty<string>());
    public static SpecResult Fail(string error) => new(false, new[] { error });
    public static SpecResult Fail(IEnumerable<string> errors) => new(false, errors);
}
