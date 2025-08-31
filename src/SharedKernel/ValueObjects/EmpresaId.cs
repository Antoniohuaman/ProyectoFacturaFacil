using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects
{
    /// <summary>
    /// Identidad opaca de Empresa (tenant). Wrapper mínimo sobre string:
    /// - No valida ni interpreta el contenido (el BC de Configuración se encarga).
    /// - Solo asegura no-nulo/no-vacío y aplica Trim().
    /// </summary>
    public sealed record EmpresaId
    {
        /// <summary>Valor canónico usado entre BCs.</summary>
        public string Value { get; }
        /// <summary>Alias de compatibilidad (si en tu código existente usas .Valor).</summary>
        public string Valor => Value;

        private EmpresaId(string value) => Value = value;

        /// <summary>
        /// Crea un EmpresaId desde cadena. No interpreta el valor (p.ej. RUC), solo valida no vacío.
        /// </summary>
        public static EmpresaId From(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new BusinessRuleException("EmpresaId es obligatorio.");
            return new EmpresaId(input.Trim());
        }

        /// <summary>
        /// Intenta crear sin lanzar excepción. Retorna false si input es nulo/vacío.
        /// </summary>
        public static bool TryParse(string? input, out EmpresaId? empresaId)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                empresaId = null;
                return false;
            }
            empresaId = new EmpresaId(input.Trim());
            return true;
        }

        /// <summary>Indica si el valor está vacío.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public override string ToString() => Value;

        // Conversiones ergonómicas
        public static explicit operator EmpresaId(string v) => From(v);
        public static implicit operator string(EmpresaId id) => id.Value;

        /// <summary>Comparación semántica explícita.</summary>
        public bool EsMismaEmpresaQue(EmpresaId otra) => otra is not null && Value == otra.Value;
    }
}
