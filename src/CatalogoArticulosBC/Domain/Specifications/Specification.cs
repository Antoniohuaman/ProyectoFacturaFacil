using System;

namespace CatalogoArticulosBC.Domain.Specifications
{
    /// <summary>
    /// Base abstracta que implementa ISpecification y permite composición AND/OR/NOT.
    /// </summary>
    public abstract class Specification<T> : ISpecification<T>
    {
        public abstract SpecificationResult IsSatisfiedBy(T candidate);

        public Specification<T> And(Specification<T> other) =>
            new AndSpecification<T>(this, other);

        public Specification<T> Or(Specification<T> other) =>
            new OrSpecification<T>(this, other);

        public Specification<T> Not() =>
            new NotSpecification<T>(this);
    }

    internal sealed class AndSpecification<T> : Specification<T>
    {
        readonly Specification<T> _left, _right;
        public AndSpecification(Specification<T> left, Specification<T> right) =>
            (_left, _right) = (left, right);
        public override SpecificationResult IsSatisfiedBy(T c)
        {
            var leftResult = _left.IsSatisfiedBy(c);
            if (!leftResult.IsSatisfied) return leftResult;
            var rightResult = _right.IsSatisfiedBy(c);
            return rightResult;
        }
    }

    internal sealed class OrSpecification<T> : Specification<T>
    {
        readonly Specification<T> _left, _right;
        public OrSpecification(Specification<T> left, Specification<T> right) =>
            (_left, _right) = (left, right);
        public override SpecificationResult IsSatisfiedBy(T c)
        {
            var leftResult = _left.IsSatisfiedBy(c);
            if (leftResult.IsSatisfied) return leftResult;
            var rightResult = _right.IsSatisfiedBy(c);
            return rightResult;
        }
    }

    internal sealed class NotSpecification<T> : Specification<T>
    {
        readonly Specification<T> _inner;
        public NotSpecification(Specification<T> inner) => _inner = inner;
        public override SpecificationResult IsSatisfiedBy(T c)
        {
            var result = _inner.IsSatisfiedBy(c);
            if (result.IsSatisfied)
                return SpecificationResult.Failure(result.ErrorCode ?? "NOT", result.Field ?? "", "La condición NOT no se cumple.");
            return SpecificationResult.Success();
        }
    }
}