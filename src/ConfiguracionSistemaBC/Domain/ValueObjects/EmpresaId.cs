using System;
using System.Text.RegularExpressions;
using SharedKernel.Exceptions;

namespace ConfiguracionSistemaBC.Domain.ValueObjects
{
    /// <summary>
    /// Identidad de empresa (tenant) como Value Object.
    /// - Inmutable y con semántica por valor.
    /// - Formatos aceptados:
    ///   1) GUID (normalizado a ToString("D"))
    ///   2) Código legible [A-Z0-9_.-], 2..64 chars (normalizado a MAYÚSCULAS)
    /// - Helpers para generar desde ROOC con correlativo.
    /// </summary>
    public sealed record EmpresaId
    {
        private static readonly Regex CodigoPattern =
            new(@"^[A-Z0-9][A-Z0-9_.-]{1,63}$", RegexOptions.Compiled); // 2..64, sin espacios

        public string Valor { get; }

        private EmpresaId(string valor) => Valor = valor;

        /// <summary>
        /// Crea desde cadena. GUID → "D"; código → UPPER + validación regex.
        /// </summary>
        public static EmpresaId Desde(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new BusinessRuleException("EmpresaId es obligatorio.");

            var v = input.Trim();

            // GUID → formato canónico "D"
            if (Guid.TryParse(v, out var g))
                return new EmpresaId(g.ToString("D"));

            // Código legible → UPPER + regex
            v = v.ToUpperInvariant();
            if (!CodigoPattern.IsMatch(v))
                throw new BusinessRuleException("EmpresaId inválido. Use GUID o código [A-Z0-9_.-], 2 a 64 caracteres.");

            return new EmpresaId(v);
        }

        /// <summary>Crea desde GUID (no vacío) en formato canónico "D".</summary>
        public static EmpresaId DesdeGuid(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new BusinessRuleException("El GUID de EmpresaId no puede ser vacío.");
            return new EmpresaId(guid.ToString("D"));
        }

        /// <summary>Genera un nuevo EmpresaId basado en GUID.</summary>
        public static EmpresaId GenerarNueva() => DesdeGuid(Guid.NewGuid());

        /// <summary>
        /// Crea id legible a partir de número (ej. EMP-000123).
        /// Útil para migraciones/semillas.
        /// </summary>
        public static EmpresaId DesdeNumero(int numero, string prefijo = "EMP")
        {
            if (numero <= 0)
                throw new BusinessRuleException("El número para EmpresaId debe ser positivo.");
            if (string.IsNullOrWhiteSpace(prefijo))
                throw new BusinessRuleException("El prefijo para EmpresaId es obligatorio.");

            var codigo = $"{prefijo.Trim().ToUpperInvariant()}-{numero:000000}";
            return Desde(codigo);
        }

        /// <summary>
        /// Genera un EmpresaId legible a partir de un ROOC y un correlativo:
        /// 1 → "ROOC20", 2 → "ROOC20-2", 3 → "ROOC20-3", ...
        /// </summary>
        public static EmpresaId DesdeRooc(string rooc, int correlativo)
        {
            if (string.IsNullOrWhiteSpace(rooc))
                throw new BusinessRuleException("El ROOC es obligatorio.");
            if (correlativo <= 0)
                throw new BusinessRuleException("El correlativo debe ser positivo.");

            var baseCode = rooc.Trim().ToUpperInvariant();
            var codigo = correlativo == 1 ? baseCode : $"{baseCode}-{correlativo}";
            return Desde(codigo);
        }

        /// <summary>
        /// Helper: consulta el siguiente correlativo para el ROOC y devuelve el EmpresaId.
        /// </summary>
        public static EmpresaId GenerarParaRooc(string rooc, IRoocCorrelativoService correlativos)
        {
            if (correlativos is null) throw new ArgumentNullException(nameof(correlativos));
            var n = correlativos.ObtenerSiguienteCorrelativo(rooc);
            return DesdeRooc(rooc, n);
        }

        /// <summary>Intenta parsear sin lanzar excepciones.</summary>
        public static bool TryParse(string? input, out EmpresaId? empresaId)
        {
            try
            {
                empresaId = Desde(input);
                return true;
            }
            catch
            {
                empresaId = null;
                return false;
            }
        }

        public override string ToString() => Valor;

        // Conversiones (ergonomía)
        public static explicit operator EmpresaId(string v) => Desde(v);
        public static implicit operator string(EmpresaId id) => id.Valor;

        public bool EsMismaEmpresaQue(EmpresaId otra) => otra is not null && Valor == otra.Valor;
    }

    /// <summary>
    /// Servicio para obtener el correlativo por ROOC (1 para el primero, 2 para el segundo, ...).
    /// Implementación típica: consulta el conteo actual y retorna count+1 con control de concurrencia.
    /// </summary>
    public interface IRoocCorrelativoService
    {
        int ObtenerSiguienteCorrelativo(string rooc);
    }
}
