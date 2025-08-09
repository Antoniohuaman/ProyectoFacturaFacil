namespace ComprobantesElectronicosBC.Domain.ValueObjects;

public enum EstadoComprobante
{
    Draft = 0,
    PendingValidation = 1,
    NeedsCorrection   = 2,
    Accepted          = 3,
    Rejected          = 4,
    Cancelled         = 5
}
