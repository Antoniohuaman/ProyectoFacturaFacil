namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Contrato básico para una Specification de dominio.
    /// </summary>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Devuelve true si el candidato satisface la regla.
        /// </summary>
        SpecificationResult IsSatisfiedBy(T entity);
    }
}
