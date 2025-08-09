namespace ComprobantesElectronicosBC.Domain.Specifications;

/// <summary>
/// Contrato de una especificación de dominio (Regla de negocio evaluable).
/// </summary>
public interface ISpecification<in T>
{
    SpecResult Evaluate(T subject);
}
