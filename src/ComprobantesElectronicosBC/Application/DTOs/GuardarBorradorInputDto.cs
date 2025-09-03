using System;

namespace ComprobantesElectronicosBC.Application.UseCases.GuardarBorrador
{
    /// <summary>
    /// Datos de entrada para guardar un borrador de CPE.
    /// - Si Id es null => crea nuevo borrador.
    /// - Si Id tiene valor => actualiza el borrador existente.
    /// 
    /// Para este caso mínimo:
    /// * Se valida coherencia entre TipoComprobante y Serie.
    /// * (Opcional) Si Numero se envía, se valida unicidad Serie–Número.
    /// * Otros datos de cabecera/líneas pueden formar parte de este DTO en el futuro;
    ///   su mapeo al agregado se deja a la factoría de borradores.
    /// </summary>
    public sealed class GuardarBorradorInputDto
    {
        /// <summary>Id del comprobante a actualizar (null para crear).</summary>
        public Guid? Id { get; init; }

        /// <summary>Código SUNAT del tipo de comprobante: "01" (Factura) o "03" (Boleta).</summary>
        public string TipoComprobante { get; init; } = default!;

        /// <summary>Serie visible (1..4, A–Z/0–9). Debe ser compatible con el tipo.</summary>
        public string Serie { get; init; } = default!;

        /// <summary>Correlativo visible (1..99’999’999). Opcional para borrador si la numeración aún no se asigna.</summary>
        public int? Numero { get; init; }

        /// <summary>Identidad de empresa (opaca, viene del contexto del tenant).</summary>
        public string EmpresaId { get; init; } = default!;

        /// <summary>Identidad de tenant (opaca).</summary>
        public string TenantId { get; init; } = default!;
    }
}
