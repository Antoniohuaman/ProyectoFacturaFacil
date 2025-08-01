namespace CatalogoArticulosBC.Domain.Specifications
{
    public class SpecificationResult
    {
        public bool IsSatisfied { get; }
        public string? ErrorCode { get; }
        public string? Field { get; }
        public string? Message { get; }

        public SpecificationResult(bool isSatisfied, string? errorCode = null, string? field = null, string? message = null)
        {
            IsSatisfied = isSatisfied;
            ErrorCode = errorCode;
            Field = field;
            Message = message;
        }

        public static SpecificationResult Success() => new SpecificationResult(true);
        public static SpecificationResult Failure(string errorCode, string field, string message)
            => new SpecificationResult(false, errorCode, field, message);
    }
}