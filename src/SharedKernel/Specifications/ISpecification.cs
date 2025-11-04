#nullable enable
namespace SharedKernel.Specifications
{
    /// <summary>
    /// Especificación con resultado enriquecido para reportar motivo/campo/código de error.
    /// </summary>
    public interface ISpecification<T>
    {
        SpecificationResult IsSatisfiedBy(T entity);
    }
}
