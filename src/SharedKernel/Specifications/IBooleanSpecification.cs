#nullable enable
namespace SharedKernel.Specifications
{
    /// <summary>
    /// Especificación booleana simple.
    /// </summary>
    public interface IBooleanSpecification<T>
    {
        bool IsSatisfiedBy(T entity);
    }
}
